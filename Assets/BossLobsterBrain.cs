
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
    [SerializeField, Range(0.01f, 1f)]
    private float retreatHealthThreshold = 0.25f;

    [Header("Retreat")]
    [SerializeField] private bool retreating;

    [SerializeField, Min(0)]
    private int retreatArrivalDistance = 0;

    private Vector2Int spawnTile;
    private bool spawnTileCached;
    private bool lowHealthRetreatTriggered;
    private bool retreatRoutineRunning;
    public bool isOnWater;

    private Coroutine retreatCoroutine;

    // ==================================================
    // UNITY LIFECYCLE
    // ==================================================

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        HealthManager.OnHealthChanged += HandleHealthChanged;
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

    private void Start()
    {
        CacheReferences();
        CacheSpawnTile();
        CheckCurrentHealth();
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
    }

    // ==================================================
    // SPAWN TILE
    // ==================================================

    private void CacheSpawnTile()
    {
        if (spawnTileCached)
            return;

        CacheReferences();

        if (moveBrain == null)
            return;

        Vector2Int initialTile;

        bool foundTile = moveBrain.TryGetCurrentTile(out initialTile);

        if (!foundTile)
            return;

        spawnTile = initialTile;
        spawnTileCached = true;
    }

    public Vector2Int GetSpawnTile()
    {
        CacheSpawnTile();
        return spawnTile;
    }

    // ==================================================
    // HEALTH TRIGGER
    // ==================================================

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

        if (healthFraction <= retreatHealthThreshold)
        {
            lowHealthRetreatTriggered = true;
            BeginRetreat();
        }
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

        if (moveBrain == null)
            return;

        if (!spawnTileCached)
            return;

        if (retreating)
            return;

        retreating = true;
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
    }

    // ==================================================
    // RETREAT MOVEMENT
    // ==================================================

    public IEnumerator TryRetreat()
    {
        if (retreatRoutineRunning)
            yield break;

        if (!retreating)
            yield break;

        CacheReferences();
        CacheSpawnTile();

        if (moveBrain == null || !spawnTileCached)
            yield break;

        GridManager gridManager = moveBrain.GetGridManager();
        UnitMoveBrainManager pathManager = UnitMoveBrainManager.Instance;

        if (gridManager == null || pathManager == null)
            yield break;

        if (!gridManager.IsInsideGrid(spawnTile))
            yield break;

        retreatRoutineRunning = true;

        try
        {
            while (retreating)
            {
                if (moveBrain == null)
                    yield break;

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

                if (distance <= retreatArrivalDistance)
                {
                    retreating = false;
                    yield break;
                }

                if (moveBrain.GetStepsRemaining() <= 0)
                    yield break;

                List<Vector2Int> path = new List<Vector2Int>();

                bool diagonal = moveBrain.CanWalkDiagonally();

                bool pathFound = pathManager.FindPath(
                    currentTile,
                    spawnTile,
                    diagonal,
                    path,
                    gameObject
                );

                if (!pathFound || path == null || path.Count < 2)
                    yield break;

                Vector2Int nextTile = path[1];

                bool moveStarted = moveBrain.TryMoveTo(nextTile);

                if (!moveStarted)
                    yield break;

                while (moveBrain != null && moveBrain.IsMoving())
                    yield return null;

                if (moveBrain == null || !retreating)
                    yield break;

                Vector2Int newTile = moveBrain.GetCurrentTile();

                if (newTile == currentTile)
                    yield break;

                yield return null;
            }
        }
        finally
        {
            retreatRoutineRunning = false;
            retreatCoroutine = null;
        }
    }

    public bool IsOnWater()
    {
        return isOnWater;
    }

    public void SetOnWater(bool value)
    {
        isOnWater = value;
    }


}

