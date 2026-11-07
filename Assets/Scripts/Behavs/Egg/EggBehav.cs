
using UnityEngine;

public class EggBehav : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private int arcSamples = 50;

    [Header("Hatching")]
    [SerializeField] private int roundsToHatch = 3;
    [SerializeField] private GameObject hatchEnemyPrefab;
    [SerializeField] private CharacterSO hatchCharacter;

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private RoundManager roundManager;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Quaternion originalRotation;

    private float speed = 8f;
    private float flightTime;
    private float elapsedTime;

    private float[] arcDistances;
    private float totalArcLength;

    private bool initialized;
    private bool hasLanded;
    private bool hasHatched;

    // Tracks whether this egg currently owns a registered obstacle.
    private bool obstacleRegistered;

    private int roundsRemaining;
    private Vector2Int landedGridPosition;

    private AttackUnit attacker;

    // ==================================================
    // INITIALIZE
    // ==================================================

    public void Initialize(
        Vector3 targetPosition,
        float speed,
        AttackUnit attacker)
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (roundManager == null)
            roundManager = FindFirstObjectByType<RoundManager>();

        this.targetPosition = targetPosition;
        this.speed = Mathf.Max(0.01f, speed);
        this.attacker = attacker;

        startPosition = transform.position;
        originalRotation = transform.rotation;

        elapsedTime = 0f;

        hasLanded = false;
        hasHatched = false;
        obstacleRegistered = false;
        initialized = true;

        BuildArcDistanceTable();

        flightTime = Mathf.Max(
            0.05f,
            totalArcLength / this.speed
        );
    }

    // ==================================================
    // ARC CALCULATION
    // ==================================================

    private Vector3 GetArcPosition(float t)
    {
        Vector3 position = Vector3.Lerp(
            startPosition,
            targetPosition,
            t
        );

        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

        return position;
    }

    private void BuildArcDistanceTable()
    {
        int samples = Mathf.Max(2, arcSamples);

        arcDistances = new float[samples + 1];

        Vector3 previousPosition = GetArcPosition(0f);

        totalArcLength = 0f;
        arcDistances[0] = 0f;

        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;

            Vector3 currentPosition = GetArcPosition(t);

            totalArcLength += Vector3.Distance(
                previousPosition,
                currentPosition
            );

            arcDistances[i] = totalArcLength;
            previousPosition = currentPosition;
        }
    }

    private float GetArcProgress(float distance)
    {
        if (totalArcLength <= 0f)
            return 1f;

        distance = Mathf.Clamp(
            distance,
            0f,
            totalArcLength
        );

        for (int i = 1; i < arcDistances.Length; i++)
        {
            if (arcDistances[i] >= distance)
            {
                float segmentLength =
                    arcDistances[i] - arcDistances[i - 1];

                float segmentProgress =
                    segmentLength > 0f
                        ? (distance - arcDistances[i - 1]) /
                          segmentLength
                        : 0f;

                return Mathf.Lerp(
                    (float)(i - 1) / arcSamples,
                    (float)i / arcSamples,
                    segmentProgress
                );
            }
        }

        return 1f;
    }

    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (!initialized || hasLanded)
            return;

        elapsedTime += Time.deltaTime;

        float distanceTravelled = Mathf.Min(
            elapsedTime * speed,
            totalArcLength
        );

        float t = GetArcProgress(distanceTravelled);

        transform.position = GetArcPosition(t);
        transform.rotation = originalRotation;

        if (elapsedTime >= flightTime)
            Land();
    }

    // ==================================================
    // LAND
    // ==================================================

    private void Land()
    {
        if (hasLanded)
            return;

        hasLanded = true;
        initialized = false;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        landedGridPosition =
            gridManager != null
                ? gridManager.WorldToGridPosition(targetPosition)
                : Vector2Int.zero;

        if (gridManager != null)
        {
            transform.position =
                gridManager.GridToWorldPosition(
                    landedGridPosition
                );

            if (gridManager.IsInsideGrid(landedGridPosition))
            {
                gridManager.RegisterWalkableObstacle(
                    landedGridPosition
                );

                obstacleRegistered = true;
            }
            else
            {
                Debug.LogWarning(
                    "EggBehav: Landing position is outside the grid.",
                    this
                );
            }
        }
        else
        {
            transform.position = targetPosition;
        }

        if (attacker != null)
            attacker.OnEggLanded();

        StartHatchCountdown();
    }

    // ==================================================
    // COUNTDOWN
    // ==================================================

    private void StartHatchCountdown()
    {
        roundsRemaining = Mathf.Max(1, roundsToHatch);

        if (roundManager == null)
            roundManager = FindFirstObjectByType<RoundManager>();

        if (roundManager == null)
            return;

        roundManager.OnRoundChanged += HandleRoundChanged;
    }

    private void HandleRoundChanged(int currentRound)
    {
        if (!hasLanded || hasHatched)
            return;

        roundsRemaining--;

        if (roundsRemaining <= 0)
            Hatch();
    }

    // ==================================================
    // HATCH
    // ==================================================

    private void Hatch()
    {
        if (hasHatched)
            return;

        if (hatchEnemyPrefab == null)
        {
            Debug.LogWarning(
                "EggBehav: Hatch enemy prefab is missing.",
                this
            );

            return;
        }

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            return;

        if (!gridManager.IsInsideGrid(landedGridPosition))
        {
            Debug.LogWarning(
                "EggBehav: Original egg position is outside the grid.",
                this
            );

            return;
        }

        // Temporarily remove this egg's obstacle to make room
        // for the hatchling.
        if (obstacleRegistered)
        {
            gridManager.UnregisterWalkableObstacle(
                landedGridPosition
            );

            obstacleRegistered = false;
        }

        Vector3 spawnPosition =
            gridManager.GridToWorldPosition(
                landedGridPosition
            );

        GameObject enemy = Instantiate(
            hatchEnemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (enemy == null)
        {
            RestoreEggObstacle();
            return;
        }

        if (hatchCharacter != null)
        {
            UnitData unitData =
                enemy.GetComponent<UnitData>();

            if (unitData == null)
                unitData = enemy.AddComponent<UnitData>();

            unitData.Initialize(hatchCharacter);
        }

        bool placed = gridManager.PlaceUnit(
            enemy,
            landedGridPosition
        );

        if (!placed)
        {
            Destroy(enemy);

            // Keep the egg blocking the tile and retry next round.
            RestoreEggObstacle();

            return;
        }

        hasHatched = true;

        UnsubscribeFromRoundManager();

        Debug.Log(
            $"{enemy.name} hatched at grid position " +
            $"{landedGridPosition}!"
        );

        Destroy(gameObject);
    }

    // ==================================================
    // RESTORE OBSTACLE
    // ==================================================

    private void RestoreEggObstacle()
    {
        if (gridManager == null ||
            hasHatched ||
            !hasLanded ||
            obstacleRegistered)
        {
            return;
        }

        if (!gridManager.IsInsideGrid(landedGridPosition))
            return;

        gridManager.RegisterWalkableObstacle(
            landedGridPosition
        );

        obstacleRegistered = true;
    }

    // ==================================================
    // CLEANUP
    // ==================================================

    private void UnsubscribeFromRoundManager()
    {
        if (roundManager != null)
            roundManager.OnRoundChanged -= HandleRoundChanged;
    }

    private void OnDestroy()
    {
        UnsubscribeFromRoundManager();

        // If the egg is destroyed before hatching,
        // free the tile so it doesn't remain permanently blocked.
        if (obstacleRegistered && !hasHatched && gridManager != null)
        {
            gridManager.UnregisterWalkableObstacle(
                landedGridPosition
            );

            obstacleRegistered = false;
        }
    }
}
