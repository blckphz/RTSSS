
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
    [SerializeField] private WaterManager waterManager;

    [Header("Low Health Retreat")]
    [SerializeField, Range(0.01f, 1f)]
    private float retreatHealthThreshold = 0.25f;

    [Header("Retreat")]
    [SerializeField] private bool retreating;

    [SerializeField, Min(0)]
    private int retreatArrivalDistance = 0;

    [Header("Water Bonuses")]
    [SerializeField, Range(0f, 100f)]
    private float waterDamageReductionPercent = 30f;

    [SerializeField, Range(0f, 100f)]
    private float waterHealingPercentPerRound = 5f;

    [Header("Debug")]
    [SerializeField] private bool enableBossDebug = true;

    private Vector2Int spawnTile;
    private bool spawnTileCached;
    private bool lowHealthRetreatTriggered;
    private bool retreatRoutineRunning;

    public bool isOnWater;

    private Coroutine retreatCoroutine;

    private void BossDebug(string message)
    {
        if (enableBossDebug)
            Debug.Log("[BossLobsterBrain] " + message, this);
    }

    private void Awake()
    {
        CacheReferences();

        BossDebug("Awake. References cached.");
    }

    private void OnEnable()
    {
        HealthManager.OnHealthChanged += HandleHealthChanged;

        BossDebug("Enabled. Listening for health changes.");
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

        BossDebug("Disabled. Retreat coroutine stopped.");
    }

    private void Start()
    {
        CacheReferences();
        CacheSpawnTile();

        RefreshWaterStatus();
        CheckCurrentHealth();

        BossDebug(
            "Start complete. Retreating: " + retreating +
            " | Spawn tile cached: " + spawnTileCached +
            " | Spawn tile: " + spawnTile
        );
    }

    // ==================================================
    // REFERENCES
    // ==================================================

    private void CacheReferences()
    {
        if (moveBrain == null)
            moveBrain = GetComponent<UnitMoveBrain>();

        if (attackUnit == null)
            attackUnit = GetComponent<AttackUnit>();

        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();

        if (waterManager == null)
            waterManager = FindFirstObjectByType<WaterManager>();
    }

    private void CacheSpawnTile()
    {
        if (spawnTileCached)
            return;

        CacheReferences();

        if (moveBrain == null)
        {
            BossDebug("Cannot cache spawn tile: UnitMoveBrain missing.");
            return;
        }

        if (moveBrain.TryGetCurrentTile(out Vector2Int initialTile))
        {
            spawnTile = initialTile;
            spawnTileCached = true;

            BossDebug("Spawn tile cached: " + spawnTile);
        }
        else
        {
            BossDebug("Failed to cache spawn tile: current tile unavailable.");
        }
    }

    public Vector2Int GetSpawnTile()
    {
        CacheSpawnTile();
        return spawnTile;
    }

    // ==================================================
    // HEALTH
    // ==================================================

    private void HandleHealthChanged(HealthManager changedHealth)
    {
        if (changedHealth == null)
            return;

        if (changedHealth.gameObject != gameObject)
            return;

        BossDebug(
            "Health changed. Current HP: " +
            changedHealth.GetHealth() +
            " / " + changedHealth.GetMaxHealth()
        );

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
        if (currentHealth == null || lowHealthRetreatTriggered)
            return;

        if (!currentHealth.IsAlive())
        {
            BossDebug("Health check: boss is dead.");
            return;
        }

        int currentHP = currentHealth.GetHealth();
        int maxHP = Mathf.Max(1, currentHealth.GetMaxHealth());

        float healthFraction = (float)currentHP / maxHP;

        BossDebug(
            "Health check: " + currentHP +
            " / " + maxHP +
            " = " + (healthFraction * 100f).ToString("F1") + "%"
        );

        if (healthFraction <= retreatHealthThreshold)
        {
            lowHealthRetreatTriggered = true;

            BossDebug("Low health threshold reached. Starting retreat.");

            BeginRetreat();
        }
    }

    // ==================================================
    // WATER STATUS
    // ==================================================

    public void RefreshWaterStatus()
    {
        CacheReferences();

        if (waterManager == null || moveBrain == null)
        {
            isOnWater = false;
            return;
        }

        if (!moveBrain.TryGetCurrentTile(out Vector2Int currentTile))
        {
            isOnWater = false;
            return;
        }

        // TRUE on land, FALSE in water.
        isOnWater =
            waterManager.IsGridPositionOnWater(currentTile);
    }

    public bool IsOnWater()
    {
        RefreshWaterStatus();

        return !isOnWater;
    }

    public void SetOnWater(bool value)
    {
        RefreshWaterStatus();
    }

    // ==================================================
    // WATER DAMAGE REDUCTION
    // ==================================================

    public int ModifyIncomingDamage(int damage)
    {
        if (damage <= 0)
            return damage;

        RefreshWaterStatus();

        if (isOnWater)
            return damage;

        float reduction = Mathf.Clamp01(
            waterDamageReductionPercent / 100f
        );

        int reducedDamage = Mathf.RoundToInt(
            damage * (1f - reduction)
        );

        return Mathf.Max(0, reducedDamage);
    }

    // ==================================================
    // WATER ROUND HEALING
    // ==================================================

    public void HealFromWater()
    {
        RefreshWaterStatus();

        if (isOnWater)
            return;

        CacheReferences();

        if (healthManager == null || !healthManager.IsAlive())
            return;

        int healingAmount = Mathf.RoundToInt(
            healthManager.GetMaxHealth() *
            (waterHealingPercentPerRound / 100f)
        );

        if (healingAmount <= 0)
            return;

        healthManager.Heal(healingAmount);
    }

    // ==================================================
    // RETREAT STATE
    // ==================================================

    public bool IsRetreating()
    {
        return retreating;
    }

    public void BeginRetreat()
    {
        CacheReferences();
        CacheSpawnTile();

        BossDebug(
            "BeginRetreat called. " +
            "MoveBrain: " + (moveBrain != null) +
            " | Spawn tile cached: " + spawnTileCached +
            " | Spawn tile: " + spawnTile +
            " | Already retreating: " + retreating
        );

        if (moveBrain == null)
        {
            BossDebug("RETREAT FAILED: UnitMoveBrain missing.");
            return;
        }

        if (!spawnTileCached)
        {
            BossDebug("RETREAT FAILED: Spawn tile was not cached.");
            return;
        }

        if (retreating)
        {
            BossDebug("Retreat already active.");
            return;
        }

        retreating = true;

        BossDebug("RETREAT STARTED.");
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

        BossDebug("RETREAT ENDED.");
    }

    // ==================================================
    // RETREAT MOVEMENT
    // ==================================================

    public IEnumerator TryRetreat()
    {
        BossDebug(
            "TryRetreat called. Retreating: " + retreating +
            " | Routine running: " + retreatRoutineRunning
        );

        if (retreatRoutineRunning || !retreating)
        {
            BossDebug("TryRetreat stopped: already running or not retreating.");
            yield break;
        }

        CacheReferences();
        CacheSpawnTile();

        if (moveBrain == null || !spawnTileCached)
        {
            BossDebug("TryRetreat stopped: missing MoveBrain or spawn tile.");
            yield break;
        }

        GridManager gridManager = moveBrain.GetGridManager();

        UnitMoveBrainManager pathManager =
            UnitMoveBrainManager.Instance;

        if (gridManager == null)
        {
            BossDebug("TryRetreat stopped: GridManager missing.");
            yield break;
        }

        if (pathManager == null)
        {
            BossDebug("TryRetreat stopped: UnitMoveBrainManager missing.");
            yield break;
        }

        if (!gridManager.IsInsideGrid(spawnTile))
        {
            BossDebug(
                "TryRetreat stopped: spawn tile outside grid: " +
                spawnTile
            );

            yield break;
        }

        retreatRoutineRunning = true;

        BossDebug("Retreat movement routine started.");

        try
        {
            while (retreating)
            {
                if (moveBrain == null)
                {
                    BossDebug("Retreat stopped: MoveBrain destroyed.");
                    yield break;
                }

                while (moveBrain != null && moveBrain.IsMoving())
                    yield return null;

                if (moveBrain == null || !retreating)
                    yield break;

                Vector2Int currentTile =
                    moveBrain.GetCurrentTile();

                int distance = pathManager.GetMovementDistance(
                    currentTile,
                    spawnTile,
                    moveBrain.CanWalkDiagonally()
                );

                BossDebug(
                    "Retreat position: " + currentTile +
                    " | Destination: " + spawnTile +
                    " | Distance: " + distance +
                    " | Steps remaining: " + moveBrain.GetStepsRemaining()
                );

                if (distance <= retreatArrivalDistance)
                {
                    retreating = false;

                    BossDebug("Reached retreat destination. Retreat ended.");

                    yield break;
                }

                if (moveBrain.GetStepsRemaining() <= 0)
                {
                    BossDebug("Retreat paused: no movement steps remaining.");
                    yield break;
                }

                List<Vector2Int> path =
                    new List<Vector2Int>();

                bool pathFound = pathManager.FindPath(
                    currentTile,
                    spawnTile,
                    moveBrain.CanWalkDiagonally(),
                    path,
                    gameObject
                );

                if (!pathFound || path == null || path.Count < 2)
                {
                    BossDebug(
                        "Retreat stopped: no valid path to spawn tile."
                    );

                    yield break;
                }

                Vector2Int nextTile = path[1];

                BossDebug(
                    "Retreat path found. Next tile: " + nextTile
                );

                bool moveStarted =
                    moveBrain.TryMoveTo(nextTile);

                if (!moveStarted)
                {
                    BossDebug(
                        "Retreat stopped: TryMoveTo failed for " +
                        nextTile
                    );

                    yield break;
                }

                while (moveBrain != null && moveBrain.IsMoving())
                    yield return null;

                if (moveBrain == null || !retreating)
                    yield break;

                Vector2Int newTile =
                    moveBrain.GetCurrentTile();

                if (newTile == currentTile)
                {
                    BossDebug(
                        "Retreat stopped: movement finished but tile did not change."
                    );

                    yield break;
                }

                BossDebug(
                    "Retreat movement successful. New tile: " + newTile
                );

                RefreshWaterStatus();

                yield return null;
            }
        }
        finally
        {
            retreatRoutineRunning = false;
            retreatCoroutine = null;

            BossDebug(
                "Retreat movement routine finished. " +
                "Retreating state: " + retreating
            );
        }
    }
}
