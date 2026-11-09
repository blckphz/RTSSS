
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


    private Vector2Int spawnTile;
    private bool spawnTileCached;
    private bool lowHealthRetreatTriggered;
    private bool retreatRoutineRunning;

    public bool isOnWater;

    private Coroutine retreatCoroutine;


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

        RefreshWaterStatus();
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
            return;
        }

        if (moveBrain.TryGetCurrentTile(out Vector2Int initialTile))
        {
            spawnTile = initialTile;
            spawnTileCached = true;

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
       

        CacheReferences();
        CacheSpawnTile();

     
        GridManager gridManager = moveBrain.GetGridManager();

        UnitMoveBrainManager pathManager =
            UnitMoveBrainManager.Instance;

      

     

        retreatRoutineRunning = true;


        try
        {
            while (retreating)
            {
                if (moveBrain == null)
                {
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


                if (distance <= retreatArrivalDistance)
                {
                    retreating = false;


                    yield break;
                }

                if (moveBrain.GetStepsRemaining() <= 0)
                {
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

            

                Vector2Int nextTile = path[1];

               

                bool moveStarted =
                    moveBrain.TryMoveTo(nextTile);

       

                while (moveBrain != null && moveBrain.IsMoving())
                    yield return null;

                if (moveBrain == null || !retreating)
                    yield break;

                Vector2Int newTile =
                    moveBrain.GetCurrentTile();

           


                RefreshWaterStatus();

                yield return null;
            }
        }
        finally
        {
            retreatRoutineRunning = false;
            retreatCoroutine = null;

          
        }
    }
}
