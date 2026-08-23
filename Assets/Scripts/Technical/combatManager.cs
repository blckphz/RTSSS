using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GridManager gridManager;

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private CharacterSO enemyCharacter;

    [Header("Enemy Spawning")]
    [SerializeField]
    private int minEnemiesToSpawn = 1;

    [SerializeField]
    private int maxEnemiesToSpawn = 3;

    [Header("Enemy Turn")]
    [SerializeField]
    private bool enemiesMoveAfterRound = true;

    [SerializeField]
    private bool enemiesAttackAfterMoving = true;

    [Header("Testing")]
    [SerializeField]
    private bool spawnEnemiesAutomatically = true;


    // ============================================================
    // DEBUG
    // ============================================================

    private void DebugLog(string message)
    {
        Debug.Log(
            $"<color=orange>[CombatManager]</color> " +
            $"[Time: {Time.time:F3}s] " +
            $"[Real: {Time.realtimeSinceStartup:F3}s] " +
            $"[Frame: {Time.frameCount}] " +
            $"{message}",
            this
        );
    }


    private void EnemyDebug(string message)
    {
        Debug.Log(
            $"<color=red>[ENEMY]</color> " +
            $"[Time: {Time.time:F3}s] " +
            $"[Real: {Time.realtimeSinceStartup:F3}s] " +
            $"[Frame: {Time.frameCount}] " +
            $"{message}",
            this
        );
    }


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        DebugLog("==========================================");
        DebugLog("Awake()");

        if (gridManager == null)
        {
            gridManager =
                FindObjectOfType<GridManager>();

            DebugLog(
                $"GridManager found automatically: " +
                $"{gridManager != null}"
            );
        }

        DebugLog(
            $"Awake() COMPLETE"
        );

        DebugLog("==========================================");
    }


    private void Start()
    {
        DebugLog("==========================================");
        DebugLog("Start()");

        DebugLog(
            $"spawnEnemiesAutomatically = " +
            $"{spawnEnemiesAutomatically}"
        );

        if (spawnEnemiesAutomatically)
        {
            CheckForEnemies();
        }

        DebugLog(
            $"Start() COMPLETE"
        );

        DebugLog("==========================================");
    }


    // ============================================================
    // ENEMY ROUND
    // ============================================================

    public IEnumerator RunEnemyRound()
    {
        EnemyDebug(
            ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
        );

        EnemyDebug(
            "RUN ENEMY ROUND COROUTINE STARTED"
        );

        EnemyDebug(
            $"Coroutine start Time.time = " +
            $"{Time.time:F3}s"
        );

        EnemyDebug(
            $"Coroutine start RealtimeSinceStartup = " +
            $"{Time.realtimeSinceStartup:F3}s"
        );

        EnemyDebug(
            $"Coroutine start Frame = " +
            $"{Time.frameCount}"
        );

        EnemyDebug(
            "=========================================="
        );

        EnemyDebug(
            "========== ENEMY ROUND START =========="
        );

        EnemyDebug(
            "=========================================="
        );

        List<AttackUnit> enemies =
            CombatUtility.GetUnitsByTeam(
                Team.Enemy
            );

        EnemyDebug(
            $"Enemy list created. Count = {enemies.Count}"
        );

        for (
            int i = 0;
            i < enemies.Count;
            i++
        )
        {
            AttackUnit enemy =
                enemies[i];

            // ----------------------------------------------------
            // NULL CHECK
            // ----------------------------------------------------

            if (enemy == null)
            {
                EnemyDebug(
                    $"Enemy [{i}] is NULL. Skipping."
                );

                continue;
            }

            // ----------------------------------------------------
            // BASIC ENEMY INFO
            // ----------------------------------------------------

            EnemyDebug(
                "------------------------------------------"
            );

            EnemyDebug(
                $"Enemy [{i}] FOUND: {enemy.name}"
            );

            EnemyDebug(
                $"Enemy [{i}] GameObject Active = " +
                $"{enemy.gameObject.activeSelf}"
            );

            EnemyDebug(
                $"Enemy [{i}] Active In Hierarchy = " +
                $"{enemy.gameObject.activeInHierarchy}"
            );

            EnemyDebug(
                $"Enemy [{i}] World Position = " +
                $"{enemy.transform.position}"
            );

            // ----------------------------------------------------
            // ALIVE CHECK
            // ----------------------------------------------------

            bool isAlive =
                CombatUtility.IsAlive(enemy);

            EnemyDebug(
                $"Enemy [{i}] Alive = {isAlive}"
            );

            if (!isAlive)
            {
                EnemyDebug(
                    $"Enemy [{i}] {enemy.name} is DEAD. " +
                    $"Skipping turn."
                );

                continue;
            }

            // ----------------------------------------------------
            // TURN START
            // ----------------------------------------------------

            EnemyDebug(
                "=========================================="
            );

            EnemyDebug(
                $"Enemy [{i}] {enemy.name} TURN START"
            );

            EnemyDebug(
                $"Enemy [{i}] Position BEFORE TURN = " +
                $"{enemy.transform.position}"
            );

            EnemyDebug(
                $"Enemy [{i}] Move After Round = " +
                $"{enemiesMoveAfterRound}"
            );

            EnemyDebug(
                $"Enemy [{i}] Attack After Moving = " +
                $"{enemiesAttackAfterMoving}"
            );

            float startTime =
                Time.time;

            float startRealtime =
                Time.realtimeSinceStartup;

            int startFrame =
                Time.frameCount;

            EnemyDebug(
                $"Enemy [{i}] Turn start Time.time = " +
                $"{startTime:F3}s"
            );

            EnemyDebug(
                $"Enemy [{i}] Turn start Realtime = " +
                $"{startRealtime:F3}s"
            );

            EnemyDebug(
                $"Enemy [{i}] Turn start Frame = " +
                $"{startFrame}"
            );

            // ----------------------------------------------------
            // EXECUTE ENEMY TURN
            // ----------------------------------------------------

            EnemyDebug(
                $"Enemy [{i}] START ExecuteEnemyTurn()"
            );

            EnemyDebug(
                $"Enemy [{i}] Calling CombatUtility.ExecuteEnemyTurn..."
            );

            yield return StartCoroutine(
                CombatUtility.ExecuteEnemyTurn(
                    enemy,
                    !enemiesMoveAfterRound,
                    enemiesAttackAfterMoving
                )
            );

            // ----------------------------------------------------
            // TURN COMPLETE
            // ----------------------------------------------------

            float duration =
                Time.time - startTime;

            float realtimeDuration =
                Time.realtimeSinceStartup -
                startRealtime;

            int frameDuration =
                Time.frameCount -
                startFrame;

            EnemyDebug(
                $"Enemy [{i}] FINISHED ExecuteEnemyTurn()"
            );

            EnemyDebug(
                $"Enemy [{i}] Turn Duration Game Time = " +
                $"{duration:F3}s"
            );

            EnemyDebug(
                $"Enemy [{i}] Turn Duration Real Time = " +
                $"{realtimeDuration:F3}s"
            );

            EnemyDebug(
                $"Enemy [{i}] Turn Duration Frames = " +
                $"{frameDuration}"
            );

            // ----------------------------------------------------
            // CHECK ENEMY AFTER TURN
            // ----------------------------------------------------

            if (enemy == null)
            {
                EnemyDebug(
                    $"Enemy [{i}] WAS DESTROYED during its turn."
                );
            }
            else
            {
                EnemyDebug(
                    $"Enemy [{i}] Position AFTER TURN = " +
                    $"{enemy.transform.position}"
                );

                bool aliveAfterTurn =
                    CombatUtility.IsAlive(enemy);

                EnemyDebug(
                    $"Enemy [{i}] Alive AFTER TURN = " +
                    $"{aliveAfterTurn}"
                );
            }

            // ----------------------------------------------------
            // ONE FRAME DELAY
            // ----------------------------------------------------

            EnemyDebug(
                $"Enemy [{i}] yielding one frame..."
            );

            int yieldFrame =
                Time.frameCount;

            float yieldRealtime =
                Time.realtimeSinceStartup;

            yield return null;

            EnemyDebug(
                $"Enemy [{i}] resumed after yield."
            );

            EnemyDebug(
                $"Enemy [{i}] Yield lasted " +
                $"{Time.realtimeSinceStartup - yieldRealtime:F3}s"
            );

            EnemyDebug(
                $"Enemy [{i}] Yield frame transition: " +
                $"{yieldFrame} -> {Time.frameCount}"
            );

            EnemyDebug(
                $"Enemy [{i}] TURN END"
            );

            EnemyDebug(
                "=========================================="
            );
        }

        // --------------------------------------------------------
        // ENEMY ROUND END
        // --------------------------------------------------------

        EnemyDebug(
            "=========================================="
        );

        EnemyDebug(
            "========== ENEMY ROUND END =========="
        );

        EnemyDebug(
            $"Remaining enemy count = {GetEnemyCount()}"
        );

        EnemyDebug(
            $"Round end Time.time = " +
            $"{Time.time:F3}s"
        );

        EnemyDebug(
            $"Round end RealtimeSinceStartup = " +
            $"{Time.realtimeSinceStartup:F3}s"
        );

        EnemyDebug(
            $"Round end Frame = " +
            $"{Time.frameCount}"
        );

        EnemyDebug(
            "=========================================="
        );

        EnemyDebug(
            "RUN ENEMY ROUND COROUTINE FINISHED"
        );

        EnemyDebug(
            "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<"
        );
    }


    public void StartEnemyRound()
    {
        EnemyDebug(
            ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
        );

        EnemyDebug(
            "BUTTON PRESSED / StartEnemyRound() CALLED"
        );

        EnemyDebug(
            $"Time.time = " +
            $"{Time.time:F3}s"
        );

        EnemyDebug(
            $"RealtimeSinceStartup = " +
            $"{Time.realtimeSinceStartup:F3}s"
        );

        EnemyDebug(
            $"Frame = " +
            $"{Time.frameCount}"
        );

        EnemyDebug(
            $"Current enemy count = " +
            $"{GetEnemyCount()}"
        );

        EnemyDebug(
            $"Time.timeScale = " +
            $"{Time.timeScale}"
        );

        EnemyDebug(
            $"Starting RunEnemyRound coroutine..."
        );

        StartCoroutine(
            RunEnemyRound()
        );

        EnemyDebug(
            $"RunEnemyRound coroutine STARTED"
        );

        EnemyDebug(
            "<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<"
        );
    }


    // ============================================================
    // ENEMIES
    // ============================================================

    public void CheckForEnemies()
    {
        EnemyDebug(
            "=========================================="
        );

        EnemyDebug(
            "CheckForEnemies() CALLED"
        );

        int currentEnemyCount =
            GetEnemyCount();

        EnemyDebug(
            $"Current enemy count = {currentEnemyCount}"
        );

        if (currentEnemyCount == 0)
        {
            EnemyDebug(
                "No enemies found. " +
                "Spawning test enemies."
            );

            SpawnTestEnemies();
        }
        else
        {
            EnemyDebug(
                $"Enemies already exist. " +
                $"Count = {currentEnemyCount}. " +
                $"No spawning required."
            );
        }

        EnemyDebug(
            "CheckForEnemies() COMPLETE"
        );

        EnemyDebug(
            "=========================================="
        );
    }


    public int GetEnemyCount()
    {
        int count =
            CombatUtility.GetUnitCount(
                Team.Enemy
            );

        return count;
    }


    public List<GameObject> GetAllEnemies()
    {
        List<GameObject> enemies =
            CombatUtility.GetObjectsByTeam(
                Team.Enemy
            );

        EnemyDebug(
            $"GetAllEnemies() - Count = {enemies.Count}"
        );

        return enemies;
    }


    public List<GameObject> GetAllAllies()
    {
        return CombatUtility.GetObjectsByTeam(
            Team.Ally
        );
    }


    // ============================================================
    // SPAWN
    // ============================================================

    private void SpawnTestEnemies()
    {
        EnemyDebug(
            "=========================================="
        );

        EnemyDebug(
            "SpawnTestEnemies() START"
        );

        EnemyDebug(
            $"Spawn start Time = " +
            $"{Time.time:F3}s"
        );

        EnemyDebug(
            $"Spawn start Real Time = " +
            $"{Time.realtimeSinceStartup:F3}s"
        );

        EnemyDebug(
            $"Spawn start Frame = " +
            $"{Time.frameCount}"
        );

        if (
            gridManager == null ||
            enemyPrefab == null ||
            enemyCharacter == null
        )
        {
            EnemyDebug(
                "Spawn aborted because a reference is missing."
            );

            EnemyDebug(
                $"GridManager = {gridManager != null}"
            );

            EnemyDebug(
                $"EnemyPrefab = {enemyPrefab != null}"
            );

            EnemyDebug(
                $"EnemyCharacter = {enemyCharacter != null}"
            );

            return;
        }

        int amount =
            Random.Range(
                minEnemiesToSpawn,
                maxEnemiesToSpawn + 1
            );

        EnemyDebug(
            $"Attempting to spawn {amount} enemies."
        );

        List<Vector2Int> availableCells =
            GetAvailableCells();

        EnemyDebug(
            $"Available cells = {availableCells.Count}"
        );

        if (availableCells.Count == 0)
        {
            EnemyDebug(
                "No available cells. Spawn aborted."
            );

            return;
        }

        amount =
            Mathf.Min(
                amount,
                availableCells.Count
            );

        EnemyDebug(
            $"Final enemy spawn amount = {amount}"
        );

        for (
            int i = 0;
            i < amount;
            i++
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    availableCells.Count
                );

            Vector2Int position =
                availableCells[randomIndex];

            availableCells.RemoveAt(
                randomIndex
            );

            EnemyDebug(
                $"Spawning enemy {i + 1}/{amount} " +
                $"at grid position {position}"
            );

            bool spawned =
                SpawnEnemy(position);

            EnemyDebug(
                $"Spawn result for enemy {i + 1} = {spawned}"
            );
        }

        EnemyDebug(
            $"SpawnTestEnemies() COMPLETE. " +
            $"Current enemy count = {GetEnemyCount()}"
        );

        EnemyDebug(
            $"Spawn end Time = " +
            $"{Time.time:F3}s"
        );

        EnemyDebug(
            $"Spawn end Real Time = " +
            $"{Time.realtimeSinceStartup:F3}s"
        );

        EnemyDebug(
            $"Spawn end Frame = " +
            $"{Time.frameCount}"
        );

        EnemyDebug(
            "=========================================="
        );
    }


    private bool SpawnEnemy(
        Vector2Int gridPosition
    )
    {
        EnemyDebug(
            "------------------------------------------"
        );

        EnemyDebug(
            $"SpawnEnemy({gridPosition}) START"
        );

        if (gridManager == null)
        {
            EnemyDebug(
                "SpawnEnemy FAILED: gridManager is null."
            );

            return false;
        }

        if (!gridManager.IsInsideGrid(
                gridPosition))
        {
            EnemyDebug(
                $"SpawnEnemy FAILED: {gridPosition} " +
                $"is outside grid."
            );

            return false;
        }

        if (gridManager.IsCellOccupied(
                gridPosition))
        {
            EnemyDebug(
                $"SpawnEnemy FAILED: {gridPosition} " +
                $"is occupied."
            );

            return false;
        }

        // --------------------------------------------------------
        // CREATE ENEMY
        // --------------------------------------------------------

        EnemyDebug(
            $"Instantiating enemy prefab."
        );

        GameObject enemy =
            Instantiate(enemyPrefab);

        if (enemy == null)
        {
            EnemyDebug(
                "SpawnEnemy FAILED: " +
                "Instantiate returned null."
            );

            return false;
        }

        enemy.name =
            $"{enemyCharacter.name}_Enemy_" +
            $"{Random.Range(1000, 9999)}";

        EnemyDebug(
            $"Created enemy object: {enemy.name}"
        );

        EnemyDebug(
            $"Enemy initial world position = " +
            $"{enemy.transform.position}"
        );

        // --------------------------------------------------------
        // COMPONENT CHECK
        // --------------------------------------------------------

        HealthManager health =
            enemy.GetComponent<HealthManager>();

        AttackUnit attackUnit =
            enemy.GetComponent<AttackUnit>();

        EnemyDebug(
            $"{enemy.name} HealthManager found = " +
            $"{health != null}"
        );

        EnemyDebug(
            $"{enemy.name} AttackUnit found = " +
            $"{attackUnit != null}"
        );

        if (
            health == null ||
            attackUnit == null
        )
        {
            EnemyDebug(
                $"Spawn FAILED: {enemy.name} " +
                $"is missing required component."
            );

            Destroy(enemy);

            return false;
        }

        // --------------------------------------------------------
        // PLACE ON GRID
        // --------------------------------------------------------

        EnemyDebug(
            $"{enemy.name} attempting PlaceUnit() " +
            $"at {gridPosition}"
        );

        if (!gridManager.PlaceUnit(
                enemy,
                gridPosition))
        {
            EnemyDebug(
                $"{enemy.name} Spawn FAILED: " +
                $"PlaceUnit() returned false."
            );

            Destroy(enemy);

            return false;
        }

        EnemyDebug(
            $"{enemy.name} successfully placed on grid " +
            $"at {gridPosition}"
        );

        EnemyDebug(
            $"{enemy.name} world position after placement = " +
            $"{enemy.transform.position}"
        );

        // --------------------------------------------------------
        // HEALTH INITIALIZATION
        // --------------------------------------------------------

        EnemyDebug(
            $"{enemy.name} initializing HealthManager."
        );

        health.Initialize(
            enemyCharacter
        );

        EnemyDebug(
            $"{enemy.name} HealthManager initialized."
        );

        // --------------------------------------------------------
        // ATTACK UNIT INITIALIZATION
        // --------------------------------------------------------

        EnemyDebug(
            $"{enemy.name} initializing AttackUnit."
        );

        attackUnit.Initialize(
            enemyCharacter
        );

        EnemyDebug(
            $"{enemy.name} AttackUnit initialized."
        );

        // --------------------------------------------------------
        // SPAWN COMPLETE
        // --------------------------------------------------------

        EnemyDebug(
            $"{enemy.name} SPAWN COMPLETE."
        );

        EnemyDebug(
            $"{enemy.name} final world position = " +
            $"{enemy.transform.position}"
        );

        EnemyDebug(
            $"Current total enemy count = " +
            $"{GetEnemyCount()}"
        );

        EnemyDebug(
            $"SpawnEnemy({gridPosition}) COMPLETE"
        );

        EnemyDebug(
            "------------------------------------------"
        );

        return true;
    }


    // ============================================================
    // AVAILABLE CELLS
    // ============================================================

    private List<Vector2Int> GetAvailableCells()
    {
        List<Vector2Int> cells =
            new List<Vector2Int>();

        if (gridManager == null)
        {
            EnemyDebug(
                "GetAvailableCells() failed: " +
                "gridManager is null."
            );

            return cells;
        }

        int width =
            gridManager.GetWidth();

        int height =
            gridManager.GetHeight();

        EnemyDebug(
            $"Checking available cells. " +
            $"Grid size = {width} x {height}"
        );

        for (
            int x = 0;
            x < width;
            x++
        )
        {
            for (
                int y = 0;
                y < height;
                y++
            )
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

                if (!gridManager.IsCellOccupied(
                        position))
                {
                    cells.Add(position);
                }
            }
        }

        EnemyDebug(
            $"GetAvailableCells() found " +
            $"{cells.Count} available cells."
        );

        return cells;
    }


    // ============================================================
    // ACCESSORS
    // ============================================================

    public GridManager GetGridManager()
    {
        return gridManager;
    }
}