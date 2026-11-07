
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "EggAttack",
    menuName = "Combat/Abilities/EggAttack"
)]
public class EggAbil : AbilitySO
{
    [Header("Egg Projectile")]
    [SerializeField] private GameObject eggPrefab;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 8f;

    [Header("Barricade Targeting")]
    [Tooltip("How much the egg prefers extending an existing barricade.")]
    [SerializeField, Min(0f)]
    private float barricadeBonus = 12f;

    [Tooltip("How strongly the egg prefers the side facing the pond when retreating.")]
    [SerializeField, Min(0f)]
    private float pondDirectionWeight = 100f;

    [Header("Ensnare Pattern")]
    [Tooltip("Bonus for landing behind the player.")]
    [SerializeField, Min(0f)]
    private float behindPlayerBonus = 100f;

    [Tooltip("Bonus for landing beside the player.")]
    [SerializeField, Min(0f)]
    private float sidePlayerBonus = 75f;

    [Tooltip("Penalty for landing between the lobster and player.")]
    [SerializeField, Min(0f)]
    private float frontPlayerPenalty = 100f;

    [Header("Debug")]
    [SerializeField] private bool enableEggDebug = true;

    public GameObject GetEggPrefab() => eggPrefab;

    public float GetProjectileSpeed() => projectileSpeed;

    private void EggDebug(string message, Object context = null)
    {
        if (enableEggDebug)
            Debug.Log("[EggAbil] " + message, context);
    }

    // ============================================================
    // USE ABILITY
    // ============================================================

    public override bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile)
    {
        EggDebug(
            "UseAtTile called. User: " +
            (user != null ? user.name : "NULL") +
            ", GridManager: " +
            (gridManager != null ? "OK" : "NULL"),
            user
        );

        if (user == null || gridManager == null)
        {
            EggDebug("FAILED: User or GridManager is null.", user);
            return false;
        }

        if (eggPrefab == null)
        {
            EggDebug("FAILED: Egg prefab is missing.", user);
            return false;
        }

        BossLobsterBrain boss =
            user.GetComponent<BossLobsterBrain>();

        bool isRetreating =
            boss != null && boss.IsRetreating();

        EggDebug(
            "Retreat state: " + isRetreating +
            " | Boss component found: " + (boss != null),
            user
        );

        if (!TryGetBarricadeTile(
            gridManager,
            user,
            out targetTile))
        {
            EggDebug(
                "FAILED: No valid egg landing tile found.",
                user
            );

            return false;
        }

        EggDebug(
            "Selected landing tile: " + targetTile,
            user
        );

        Transform spawnPoint =
            user.transform.Find("AbilitySpawnPoint");

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : user.transform.position;

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(targetTile);

        targetPosition.z = spawnPosition.z;

        EggDebug(
            "Projectile spawn: " + spawnPosition +
            " | Target position: " + targetPosition,
            user
        );

        GameObject egg = Instantiate(
            eggPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (egg == null)
        {
            EggDebug("FAILED: Egg instantiation returned null.", user);
            return false;
        }

        EggBehav eggBehaviour =
            egg.GetComponent<EggBehav>();

        if (eggBehaviour == null)
        {
            EggDebug(
                "FAILED: Egg prefab does not contain EggBehav.",
                egg
            );

            Destroy(egg);
            return false;
        }

        AttackUnit attacker =
            user.GetComponent<AttackUnit>();

        if (attacker == null)
        {
            EggDebug(
                "FAILED: Lobster does not have AttackUnit.",
                user
            );

            Destroy(egg);
            return false;
        }

        eggBehaviour.Initialize(
            targetPosition,
            projectileSpeed,
            attacker
        );

        EggDebug(
            "SUCCESS: Egg projectile created and initialized. " +
            "Retreating: " + isRetreating,
            egg
        );

        return true;
    }

    // ============================================================
    // USE
    // ============================================================

    public override bool Use(
        GameObject user,
        GameObject target)
    {
        if (user == null)
        {
            EggDebug("Use failed: user is null.");
            return false;
        }

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager == null)
        {
            EggDebug("Use failed: GridManager not found.", user);
            return false;
        }

        return UseAtTile(
            user,
            gridManager,
            Vector2Int.zero
        );
    }

    // ============================================================
    // BARRICADE TARGET SELECTION
    // ============================================================

    private bool TryGetBarricadeTile(
        GridManager gridManager,
        GameObject lobster,
        out Vector2Int selectedTile)
    {
        selectedTile = Vector2Int.zero;

        List<AttackUnit> players =
            CombatUtility.GetUnitsByTeam(Team.Player);

        if (players == null || players.Count == 0)
        {
            EggDebug("No player units found.", lobster);
            return false;
        }

        BossLobsterBrain boss =
            lobster.GetComponent<BossLobsterBrain>();

        bool isRetreating =
            boss != null && boss.IsRetreating();

        Vector2Int pondTile = Vector2Int.zero;

        if (boss != null)
            pondTile = boss.GetSpawnTile();

        Vector2Int lobsterTile =
            gridManager.WorldToGridPosition(
                lobster.transform.position
            );

        EggDebug(
            "TARGET SEARCH STARTED | " +
            "Retreating: " + isRetreating +
            " | Lobster tile: " + lobsterTile +
            " | Pond tile: " + pondTile +
            " | Players found: " + players.Count,
            lobster
        );

        if (isRetreating &&
            !gridManager.IsInsideGrid(pondTile))
        {
            EggDebug(
                "WARNING: Pond tile is outside the grid: " +
                pondTile,
                lobster
            );
        }

        Vector2Int[] directions =
        {
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1),
            new Vector2Int(-1,  0),
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  1),
            new Vector2Int( 1,  1),
            new Vector2Int(-1, -1),
            new Vector2Int( 1, -1)
        };

        HashSet<Vector2Int> checkedTiles =
            new HashSet<Vector2Int>();

        List<Vector2Int> candidates =
            new List<Vector2Int>();

        List<float> scores =
            new List<float>();

        float bestScore = float.NegativeInfinity;

        // --------------------------------------------------------
        // CHECK EACH PLAYER
        // --------------------------------------------------------

        for (int p = 0; p < players.Count; p++)
        {
            AttackUnit player = players[p];

            if (player == null ||
                !player.gameObject.activeInHierarchy)
            {
                EggDebug(
                    "Player index " + p +
                    " is null or inactive. Skipping."
                );

                continue;
            }

            HealthManager health =
                player.GetComponent<HealthManager>();

            if (health != null && !health.IsAlive())
            {
                EggDebug(
                    "Player " + player.name +
                    " is dead. Skipping.",
                    player
                );

                continue;
            }

            Vector2Int playerTile =
                gridManager.WorldToGridPosition(
                    player.transform.position
                );

            Vector2 directionToPlayer = new Vector2(
                playerTile.x - lobsterTile.x,
                playerTile.y - lobsterTile.y
            );

            if (directionToPlayer.sqrMagnitude > 0.001f)
                directionToPlayer.Normalize();

            Vector2 directionToPond = new Vector2(
                pondTile.x - playerTile.x,
                pondTile.y - playerTile.y
            );

            if (directionToPond.sqrMagnitude > 0.001f)
                directionToPond.Normalize();

            EggDebug(
                "Checking player: " + player.name +
                " | Player tile: " + playerTile +
                " | Direction to lobster: " + directionToPlayer +
                " | Direction to pond: " + directionToPond,
                player
            );

            // ----------------------------------------------------
            // CHECK EIGHT NEIGHBOURING TILES
            // ----------------------------------------------------

            for (int d = 0; d < directions.Length; d++)
            {
                Vector2Int offset = directions[d];

                Vector2Int candidate =
                    playerTile + offset;

                if (!checkedTiles.Add(candidate))
                {
                    EggDebug(
                        "Rejected " + candidate +
                        ": already checked."
                    );

                    continue;
                }

                if (!gridManager.IsInsideGrid(candidate))
                {
                    EggDebug(
                        "Rejected " + candidate +
                        ": outside grid."
                    );

                    continue;
                }

                if (gridManager.IsWalkableObstacle(candidate))
                {
                    EggDebug(
                        "Rejected " + candidate +
                        ": walkable obstacle."
                    );

                    continue;
                }

                if (gridManager.IsCellOccupied(candidate))
                {
                    EggDebug(
                        "Rejected " + candidate +
                        ": occupied."
                    );

                    continue;
                }

                Vector2 candidateDirection =
                    new Vector2(offset.x, offset.y).normalized;

                float score = 0f;

                // ------------------------------------------------
                // RETREATING
                // ------------------------------------------------

                if (isRetreating)
                {
                    float directionScore = Vector2.Dot(
                        candidateDirection,
                        directionToPond
                    );

                    score +=
                        directionScore * pondDirectionWeight;

                    EggDebug(
                        "RETREAT candidate: " + candidate +
                        " | Direction score: " +
                        directionScore.ToString("F2") +
                        " | Base score: " +
                        score.ToString("F2")
                    );
                }

                // ------------------------------------------------
                // NOT RETREATING
                // ------------------------------------------------

                else
                {
                    float forwardDot = Vector2.Dot(
                        candidateDirection,
                        directionToPlayer
                    );

                    if (forwardDot > 0.5f)
                    {
                        score += behindPlayerBonus;
                    }
                    else if (Mathf.Abs(forwardDot) <= 0.5f)
                    {
                        score += sidePlayerBonus;
                    }
                    else
                    {
                        score -= frontPlayerPenalty;
                    }

                    EggDebug(
                        "NORMAL candidate: " + candidate +
                        " | Forward dot: " +
                        forwardDot.ToString("F2") +
                        " | Score: " +
                        score.ToString("F2")
                    );
                }

                // ------------------------------------------------
                // BARRICADE BONUS
                // ------------------------------------------------

                int nearbyEggObstacles =
                    CountAdjacentObstacles(
                        gridManager,
                        candidate
                    );

                score +=
                    nearbyEggObstacles * barricadeBonus;

                EggDebug(
                    "VALID candidate: " + candidate +
                    " | Nearby obstacles: " +
                    nearbyEggObstacles +
                    " | Final score: " +
                    score.ToString("F2")
                );

                candidates.Add(candidate);
                scores.Add(score);

                if (score > bestScore)
                    bestScore = score;
            }
        }

        // --------------------------------------------------------
        // SELECT BEST TILE
        // --------------------------------------------------------

        EggDebug(
            "TARGET SEARCH FINISHED | Valid candidates: " +
            candidates.Count +
            " | Best score: " + bestScore.ToString("F2"),
            lobster
        );

        if (candidates.Count == 0)
        {
            EggDebug(
                "FAILED: Every candidate tile was rejected.",
                lobster
            );

            return false;
        }

        List<int> bestCandidates =
            new List<int>();

        const float scoreTolerance = 0.01f;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (scores[i] >= bestScore - scoreTolerance)
                bestCandidates.Add(i);
        }

        int selectedIndex = bestCandidates[
            Random.Range(0, bestCandidates.Count)
        ];

        selectedTile = candidates[selectedIndex];

        EggDebug(
            "TARGET SELECTED: " + selectedTile +
            " | Score: " +
            scores[selectedIndex].ToString("F2") +
            " | Equal-best options: " +
            bestCandidates.Count,
            lobster
        );

        return true;
    }

    // ============================================================
    // COUNT ADJACENT OBSTACLES
    // ============================================================

    private int CountAdjacentObstacles(
        GridManager gridManager,
        Vector2Int position)
    {
        int count = 0;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                Vector2Int neighbour =
                    position + new Vector2Int(x, y);

                if (gridManager.IsWalkableObstacle(neighbour))
                    count++;
            }
        }

        return count;
    }
}
