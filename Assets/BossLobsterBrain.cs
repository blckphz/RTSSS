
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BossLobsterBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitMoveBrain moveBrain;
    [SerializeField] private AttackUnit attackUnit;
    [SerializeField] private HealthManager healthManager;

    [Header("Low Health Retreat")]
    [Tooltip("The boss starts retreating when health reaches this fraction of maximum health.")]
    [SerializeField, Range(0.01f, 1f)]
    private float retreatHealthThreshold = 0.25f;

    [Header("Retreat")]
    [SerializeField] private bool retreating;

    [Tooltip("Distance from the spawn tile at which the boss stops retreating.")]
    [SerializeField, Min(0)]
    private int retreatArrivalDistance = 0;

    [Header("Water Status")]
    [SerializeField] private bool isOnWater;

    [Header("Debug")]
    [SerializeField] private bool enableRetreatDebug = true;

    private Vector2Int spawnTile;
    private bool spawnTileCached;
    private bool lowHealthRetreatTriggered;
    private bool retreatRoutineRunning;

    private Coroutine retreatCoroutine;

    // --------------------------------------------------
    // UNITY LIFECYCLE
    // --------------------------------------------------

    private void Awake()
    {
        CacheReferences();
        LogRetreatState("Awake");
    }

    private void OnEnable()
    {
        HealthManager.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        CacheReferences();
        CacheSpawnTile();

        CheckCurrentHealth();

        LogRetreatState("Start completed");
    }

    private void OnDisable()
    {
        HealthManager.OnHealthChanged -= HandleHealthChanged;

        if (retreatCoroutine != null)
        {
            StopCoroutine(retreatCoroutine);
            retreatCoroutine = null;
        }

        retreatRoutineRunning = false;
    }

    // --------------------------------------------------
    // DEBUG
    // --------------------------------------------------

    private void LogRetreatState(string message)
    {
        if (!enableRetreatDebug)
            return;

        string actualTile = "UNKNOWN";
        string movement = "UNKNOWN";
        string moving = "UNKNOWN";

        if (moveBrain != null)
        {
            actualTile = moveBrain.GetCurrentTile().ToString();
            movement = moveBrain.GetStepsRemaining().ToString();
            moving = moveBrain.IsMoving().ToString();
        }

        Debug.Log(
            $"[LOBSTER RETREAT DEBUG] {message} | " +
            $"Boss={name} | " +
            $"WorldPosition={transform.position} | " +
            $"ActualTile={actualTile} | " +
            $"SpawnTile={spawnTile} | " +
            $"SpawnCached={spawnTileCached} | " +
            $"Retreating={retreating} | " +
            $"LowHealthTriggered={lowHealthRetreatTriggered} | " +
            $"RoutineRunning={retreatRoutineRunning} | " +
            $"Moving={moving} | " +
            $"StepsRemaining={movement}",
            this
        );
    }

    // --------------------------------------------------
    // REFERENCES
    // --------------------------------------------------

    private void CacheReferences()
    {
        if (moveBrain == null)
            moveBrain = GetComponent<UnitMoveBrain>();

        if (attackUnit == null)
            attackUnit = GetComponent<AttackUnit>();

        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();
    }

    // --------------------------------------------------
    // SPAWN TILE
    // --------------------------------------------------

    private void CacheSpawnTile()
    {
        if (spawnTileCached)
        {
            LogRetreatState("Spawn tile already cached");
            return;
        }

        CacheReferences();

        if (moveBrain == null)
        {
            Debug.LogError(
                "[LOBSTER RETREAT DEBUG] Cannot cache spawn tile: " +
                "UnitMoveBrain is missing.",
                this
            );
            return;
        }

        Vector2Int initialTile;

        bool foundTile = moveBrain.TryGetCurrentTile(out initialTile);

        if (enableRetreatDebug)
        {
            Debug.Log(
                $"[LOBSTER RETREAT DEBUG] CacheSpawnTile | " +
                $"Found={foundTile} | " +
                $"Tile={initialTile} | " +
                $"WorldPosition={transform.position}",
                this
            );
        }

        if (!foundTile)
        {
            Debug.LogWarning(
                "[LOBSTER RETREAT DEBUG] Could not determine initial tile. " +
                "Will retry later.",
                this
            );
            return;
        }

        spawnTile = initialTile;
        spawnTileCached = true;

        LogRetreatState("INITIAL SPAWN TILE CACHED");
    }

    public Vector2Int GetSpawnTile()
    {
        CacheSpawnTile();
        return spawnTile;
    }

    // --------------------------------------------------
    // HEALTH TRIGGER
    // --------------------------------------------------

    private void HandleHealthChanged(HealthManager changedHealth)
    {
        if (changedHealth == null)
            return;

        if (changedHealth.gameObject != gameObject)
            return;

        CheckHealthForRetreat(changedHealth);
    }

    private void CheckCurrentHealth()
    {
        CacheReferences();

        if (healthManager != null)
            CheckHealthForRetreat(healthManager);
    }

    private void CheckHealthForRetreat(HealthManager currentHealth)
    {
        if (currentHealth == null)
            return;

        if (lowHealthRetreatTriggered)
            return;

        if (!currentHealth.IsAlive())
            return;

        int currentHP = currentHealth.GetHealth();
        int maxHP = Mathf.Max(1, currentHealth.GetMaxHealth());

        float healthFraction = (float)currentHP / maxHP;

        if (enableRetreatDebug)
        {
            Debug.Log(
                $"[LOBSTER RETREAT DEBUG] Health check | " +
                $"HP={currentHP}/{maxHP} | " +
                $"HealthFraction={healthFraction:P1} | " +
                $"Threshold={retreatHealthThreshold:P1}",
                this
            );
        }

        if (healthFraction <= retreatHealthThreshold)
        {
            lowHealthRetreatTriggered = true;

            Debug.Log(
                $"[LOBSTER RETREAT DEBUG] Low health threshold reached. " +
                $"HP={currentHP}/{maxHP} | " +
                $"Destination={GetSpawnTile()}",
                this
            );

            BeginRetreat();
        }
    }

    // --------------------------------------------------
    // WATER STATUS
    // --------------------------------------------------

    public bool IsOnWater()
    {
        return isOnWater;
    }

    public void SetOnWater(bool value)
    {
        isOnWater = value;
    }

    // --------------------------------------------------
    // RETREAT STATE
    // --------------------------------------------------

    public bool IsRetreating()
    {
        return retreating;
    }

    public void BeginRetreat()
    {
        LogRetreatState("BeginRetreat called");

        CacheReferences();
        CacheSpawnTile();

        if (moveBrain == null)
        {
            Debug.LogError(
                "[LOBSTER RETREAT DEBUG] Retreat failed: " +
                "UnitMoveBrain is missing.",
                this
            );
            return;
        }

        if (!spawnTileCached)
        {
            Debug.LogError(
                "[LOBSTER RETREAT DEBUG] Retreat failed: " +
                "spawn tile has not been cached.",
                this
            );
            return;
        }

        if (retreating)
        {
            LogRetreatState("Retreat ignored: already retreating");
            return;
        }

        retreating = true;

        LogRetreatState("RETREAT MODE ACTIVATED");
    }

    public void EndRetreat()
    {
        retreating = false;

        if (retreatCoroutine != null)
        {
            StopCoroutine(retreatCoroutine);
            retreatCoroutine = null;
        }

        retreatRoutineRunning = false;

        LogRetreatState("Retreat manually ended");
    }

    // --------------------------------------------------
    // RETREAT MOVEMENT
    // --------------------------------------------------

    public IEnumerator TryRetreat()
    {
        LogRetreatState("TryRetreat ENTERED");

        if (retreatRoutineRunning)
        {
            Debug.LogWarning(
                "[LOBSTER RETREAT DEBUG] Retreat routine is already running.",
                this
            );
            yield break;
        }

        if (!retreating)
        {
            LogRetreatState("TryRetreat aborted: not retreating");
            yield break;
        }

        CacheReferences();
        CacheSpawnTile();

        if (moveBrain == null || !spawnTileCached)
        {
            Debug.LogError(
                "[LOBSTER RETREAT DEBUG] Retreat aborted: " +
                "movement reference or spawn tile is missing.",
                this
            );
            yield break;
        }

        GridManager gridManager = moveBrain.GetGridManager();
        UnitMoveBrainManager pathManager = UnitMoveBrainManager.Instance;

        if (enableRetreatDebug)
        {
            Debug.Log(
                $"[LOBSTER RETREAT DEBUG] Dependencies | " +
                $"GridManager={(gridManager != null ? gridManager.name : "NULL")} | " +
                $"PathManager={(pathManager != null ? pathManager.name : "NULL")}",
                this
            );
        }

        if (gridManager == null || pathManager == null)
        {
            Debug.LogError(
                "[LOBSTER RETREAT DEBUG] Retreat aborted: " +
                "grid manager or path manager is missing.",
                this
            );
            yield break;
        }

        if (!gridManager.IsInsideGrid(spawnTile))
        {
            Debug.LogError(
                $"[LOBSTER RETREAT DEBUG] Spawn tile {spawnTile} " +
                "is outside the current grid.",
                this
            );
            yield break;
        }

        retreatRoutineRunning = true;

        try
        {
            LogRetreatState("Retreat movement loop started");

            while (retreating)
            {
                if (moveBrain == null)
                {
                    Debug.LogError(
                        "[LOBSTER RETREAT DEBUG] Movement brain was destroyed.",
                        this
                    );
                    yield break;
                }

                // Wait for existing movement.
                while (moveBrain != null && moveBrain.IsMoving())
                    yield return null;

                if (moveBrain == null || !retreating)
                    yield break;

                Vector2Int currentTile = moveBrain.GetCurrentTile();

                int distance = pathManager.GetMovementDistance(
                    currentTile,
                    spawnTile,
                    moveBrain.CanWalkDiagonally()
                );

                LogRetreatState(
                    $"Evaluating path | DistanceToSpawn={distance}"
                );

                // Destination reached.
                if (distance <= retreatArrivalDistance)
                {
                    Debug.Log(
                        $"[LOBSTER RETREAT DEBUG] DESTINATION REACHED | " +
                        $"Current={currentTile} | Spawn={spawnTile}",
                        this
                    );

                    retreating = false;
                    yield break;
                }

                // No movement remaining this turn.
                if (moveBrain.GetStepsRemaining() <= 0)
                {
                    Debug.LogWarning(
                        $"[LOBSTER RETREAT DEBUG] Out of movement points. " +
                        $"Current={currentTile} | Destination={spawnTile}. " +
                        "Will retry on a future enemy turn.",
                        this
                    );
                    yield break;
                }

                List<Vector2Int> path = new List<Vector2Int>();

                bool diagonal = moveBrain.CanWalkDiagonally();

                if (enableRetreatDebug)
                {
                    Debug.Log(
                        $"[LOBSTER RETREAT DEBUG] FIND PATH | " +
                        $"From={currentTile} | " +
                        $"Destination={spawnTile} | " +
                        $"Diagonal={diagonal} | " +
                        $"Steps={moveBrain.GetStepsRemaining()} | " +
                        $"Distance={distance} | " +
                        $"DestinationInsideGrid={gridManager.IsInsideGrid(spawnTile)} | " +
                        $"DestinationOccupied={gridManager.IsCellOccupied(spawnTile)}",
                        this
                    );
                }

                bool pathFound = pathManager.FindPath(
                    currentTile,
                    spawnTile,
                    diagonal,
                    path,
                    gameObject
                );

                if (enableRetreatDebug)
                {
                    Debug.Log(
                        $"[LOBSTER RETREAT DEBUG] PATH RESULT | " +
                        $"Found={pathFound} | " +
                        $"PathNull={path == null} | " +
                        $"PathCount={(path == null ? -1 : path.Count)}",
                        this
                    );

                    if (path != null)
                    {
                        Debug.Log(
                            "[LOBSTER RETREAT DEBUG] Path: " +
                            string.Join(" -> ", path),
                            this
                        );
                    }
                }

                if (!pathFound || path == null || path.Count < 2)
                {
                    Debug.LogWarning(
                        $"[LOBSTER RETREAT DEBUG] No usable path found. " +
                        $"Current={currentTile} | Destination={spawnTile}. " +
                        "Check obstacles, occupied cells and pathfinder rules.",
                        this
                    );
                    yield break;
                }

                // FindPath returns the current tile at index 0.
                Vector2Int nextTile = path[1];

                Debug.Log(
                    $"[LOBSTER RETREAT DEBUG] REQUESTING MOVE | " +
                    $"{currentTile} -> {nextTile} | " +
                    $"FinalDestination={spawnTile}",
                    this
                );

                bool moveStarted = moveBrain.TryMoveTo(nextTile);

                Debug.Log(
                    $"[LOBSTER RETREAT DEBUG] MOVE REQUEST RESULT | " +
                    $"Target={nextTile} | Accepted={moveStarted}",
                    this
                );

                if (!moveStarted)
                {
                    Debug.LogWarning(
                        $"[LOBSTER RETREAT DEBUG] Move request rejected. " +
                        $"Current={moveBrain.GetCurrentTile()} | " +
                        $"Target={nextTile} | " +
                        $"Steps={moveBrain.GetStepsRemaining()}",
                        this
                    );
                    yield break;
                }

                while (moveBrain != null && moveBrain.IsMoving())
                    yield return null;

                if (moveBrain == null || !retreating)
                    yield break;

                Vector2Int newTile = moveBrain.GetCurrentTile();

                Debug.Log(
                    $"[LOBSTER RETREAT DEBUG] MOVE COMPLETED | " +
                    $"Requested={nextTile} | " +
                    $"Actual={newTile} | " +
                    $"CorrectTile={newTile == nextTile} | " +
                    $"FinalDestination={spawnTile}",
                    this
                );

                if (newTile == currentTile)
                {
                    Debug.LogError(
                        "[LOBSTER RETREAT DEBUG] Movement finished without " +
                        "changing the tile. Check UnitMoveBrain movement completion.",
                        this
                    );
                    yield break;
                }

                // Recalculate the path from the actual new position.
                yield return null;
            }
        }
        finally
        {
            retreatRoutineRunning = false;
            retreatCoroutine = null;

            LogRetreatState("Retreat movement routine exited");
        }
    }
}
