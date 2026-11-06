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

    [Tooltip("How strongly the egg prefers the side facing the pond.")]
    [SerializeField, Min(0f)]
    private float pondDirectionWeight = 100f;

    public GameObject GetEggPrefab() => eggPrefab;

    public float GetProjectileSpeed() => projectileSpeed;

    public override bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile)
    {
        if (user == null || gridManager == null)
            return false;

        if (eggPrefab == null)
            return false;

        if (!TryGetBarricadeTile(gridManager, out targetTile))
            return false;

        Transform spawnPoint =
            user.transform.Find("AbilitySpawnPoint");

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : user.transform.position;

        Vector3 targetPosition =
            gridManager.GridToWorldPosition(targetTile);

        targetPosition.z = spawnPosition.z;

        GameObject egg = Instantiate(
            eggPrefab,
            spawnPosition,
            Quaternion.identity
        );

        EggBehav eggBehaviour = egg.GetComponent<EggBehav>();

        if (eggBehaviour == null)
        {
            Destroy(egg);
            return false;
        }

        AttackUnit attacker = user.GetComponent<AttackUnit>();

        if (attacker == null)
        {
            Destroy(egg);
            return false;
        }

        eggBehaviour.Initialize(
            targetPosition,
            projectileSpeed,
            attacker
        );

        return true;
    }

    public override bool Use(GameObject user, GameObject target)
    {
        if (user == null)
            return false;

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return false;

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
        out Vector2Int selectedTile)
    {
        selectedTile = Vector2Int.zero;

        List<AttackUnit> players =
            CombatUtility.GetUnitsByTeam(Team.Player);

        if (players == null || players.Count == 0)
            return false;

        Vector2Int pondTile = Vector2Int.zero;

        BossLobsterBrain boss =
            FindFirstObjectByType<BossLobsterBrain>();

        if (boss != null)
        {
            pondTile = boss.GetSpawnTile();
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

        List<float> scores = new List<float>();

        float bestScore = float.NegativeInfinity;

        for (int p = 0; p < players.Count; p++)
        {
            AttackUnit player = players[p];

            if (player == null || !player.gameObject.activeInHierarchy)
                continue;

            HealthManager health =
                player.GetComponent<HealthManager>();

            if (health != null && !health.IsAlive())
                continue;

            Vector2Int playerTile =
                gridManager.WorldToGridPosition(
                    player.transform.position
                );

            Vector2 directionToPond = new Vector2(
                pondTile.x - playerTile.x,
                pondTile.y - playerTile.y
            );

            if (directionToPond.sqrMagnitude > 0.001f)
                directionToPond.Normalize();

            for (int d = 0; d < directions.Length; d++)
            {
                Vector2Int candidate =
                    playerTile + directions[d];

                if (!checkedTiles.Add(candidate))
                    continue;

                if (!gridManager.IsInsideGrid(candidate))
                    continue;

                if (gridManager.IsWalkableObstacle(candidate))
                    continue;

                if (gridManager.IsCellOccupied(candidate))
                    continue;

                Vector2 candidateDirection =
                    new Vector2(directions[d].x, directions[d].y)
                    .normalized;

                float directionScore = Vector2.Dot(
                    candidateDirection,
                    directionToPond
                );

                float score =
                    directionScore * pondDirectionWeight;

                int nearbyEggObstacles =
                    CountAdjacentObstacles(gridManager, candidate);

                score += nearbyEggObstacles * barricadeBonus;

                candidates.Add(candidate);
                scores.Add(score);

                if (score > bestScore)
                    bestScore = score;
            }
        }

        if (candidates.Count == 0)
            return false;

        List<int> bestCandidates = new List<int>();

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

        return true;
    }

    // ============================================================
    // COUNT NEARBY REGISTERED EGG OBSTACLES
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