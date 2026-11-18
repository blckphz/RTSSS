using UnityEngine;

[CreateAssetMenu(
    fileName = "BearTrapAttack",
    menuName = "Combat/Abilities/Bear Trap"
)]
public class BearTrapAttack : AbilitySO
{
    // ============================================================
    // BEAR TRAP
    // ============================================================

    [Header("Bear Trap")]

    [SerializeField]
    private GameObject bearTrapPrefab;

    [SerializeField]
    private bool destroyAfterTrigger = true;


    // ============================================================
    // GETTERS
    // ============================================================

    public GameObject GetBearTrapPrefab()
    {
        return bearTrapPrefab;
    }

    public bool GetDestroyAfterTrigger()
    {
        return destroyAfterTrigger;
    }


    // ============================================================
    // CAN HIT TILE
    // ============================================================

    public override bool CanHitTile(
        GridManager gridManager,
        GameObject user,
        Vector2Int targetPosition
    )
    {
        Debug.Log(
            $"[BearTrapAttack] CanHitTile START | " +
            $"User={(user != null ? user.name : "NULL")} | " +
            $"Tile={targetPosition}"
        );

        if (gridManager == null)
        {
            Debug.LogWarning(
                "[BearTrapAttack] FAILED: GridManager is NULL."
            );

            return false;
        }

        if (user == null)
        {
            Debug.LogWarning(
                "[BearTrapAttack] FAILED: User is NULL."
            );

            return false;
        }

        if (!CanUseAfterMovement(user))
        {
            Debug.Log(
                "[BearTrapAttack] FAILED: Cannot use after movement."
            );

            return false;
        }

        if (!gridManager.IsInsideGrid(targetPosition))
        {
            Debug.Log(
                $"[BearTrapAttack] FAILED: Tile {targetPosition} " +
                "is outside the grid."
            );

            return false;
        }

        if (gridManager.IsCellOccupied(targetPosition))
        {
            Debug.Log(
                $"[BearTrapAttack] FAILED: Tile {targetPosition} " +
                "is occupied by a unit."
            );

            return false;
        }

        bool baseResult =
            base.CanHitTile(
                gridManager,
                user,
                targetPosition
            );

        Debug.Log(
            $"[BearTrapAttack] Base.CanHitTile result = {baseResult}"
        );

        return baseResult;
    }


    // ============================================================
    // USE AT TILE
    // ============================================================

    public override bool UseAtTile(
        GameObject user,
        GridManager gridManager,
        Vector2Int targetTile
    )
    {
        Debug.Log(
            $"[BearTrapAttack] =================================="
        );

        Debug.Log(
            $"[BearTrapAttack] UseAtTile START | " +
            $"User={(user != null ? user.name : "NULL")} | " +
            $"Tile={targetTile}"
        );


        // --------------------------------------------------------
        // USER
        // --------------------------------------------------------

        if (user == null)
        {
            Debug.LogError(
                "[BearTrapAttack] FAILED: User is NULL."
            );

            return false;
        }


        // --------------------------------------------------------
        // GRID MANAGER
        // --------------------------------------------------------

        if (gridManager == null)
        {
            Debug.LogError(
                "[BearTrapAttack] FAILED: GridManager is NULL."
            );

            return false;
        }


        // --------------------------------------------------------
        // PREFAB
        // --------------------------------------------------------

        if (bearTrapPrefab == null)
        {
            Debug.LogError(
                "[BearTrapAttack] FAILED: Bear Trap Prefab " +
                "has NOT been assigned."
            );

            return false;
        }


        Debug.Log(
            $"[BearTrapAttack] Prefab = {bearTrapPrefab.name}"
        );


        // --------------------------------------------------------
        // VALIDATE TILE
        // --------------------------------------------------------

        if (
            !CanHitTile(
                gridManager,
                user,
                targetTile
            )
        )
        {
            Debug.LogWarning(
                $"[BearTrapAttack] FAILED: Cannot place trap " +
                $"on tile {targetTile}."
            );

            return false;
        }


        // --------------------------------------------------------
        // WORLD POSITION
        // --------------------------------------------------------

        Vector3 spawnPosition =
            gridManager.GridToWorldPosition(
                targetTile
            );

        Debug.Log(
            $"[BearTrapAttack] Spawn position = {spawnPosition}"
        );


        // --------------------------------------------------------
        // CREATE TRAP
        // --------------------------------------------------------

        GameObject trapObject =
            Instantiate(
                bearTrapPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (trapObject == null)
        {
            Debug.LogError(
                "[BearTrapAttack] FAILED: Instantiate returned NULL."
            );

            return false;
        }


        Debug.Log(
            $"[BearTrapAttack] Trap instantiated: " +
            $"{trapObject.name}"
        );


        // --------------------------------------------------------
        // GET BEAR TRAP COMPONENT
        // --------------------------------------------------------

        trapbehav trap =
            trapObject.GetComponent<trapbehav>();

        if (trap == null)
        {
            Debug.LogError(
                "[BearTrapAttack] FAILED: The Bear Trap prefab " +
                "does not contain a BearTrap component.",
                trapObject
            );

            Destroy(trapObject);

            return false;
        }


        // --------------------------------------------------------
        // DAMAGE
        // --------------------------------------------------------

        int effectiveDamage =
            GetEffectiveDamage(user);

        Debug.Log(
            $"[BearTrapAttack] Effective Damage = {effectiveDamage}"
        );


        // --------------------------------------------------------
        // INITIALIZE
        // --------------------------------------------------------

        trap.Initialize(
            user,
            this,
            targetTile,
            effectiveDamage,
            destroyAfterTrigger
        );


        Debug.Log(
            $"[BearTrapAttack] SUCCESS: Trap placed at " +
            $"{targetTile}"
        );

        Debug.Log(
            $"[BearTrapAttack] =================================="
        );

        return true;
    }


    // ============================================================
    // NORMAL USE
    // ============================================================

    public override bool Use(
        GameObject user,
        GameObject target
    )
    {
        Debug.Log(
            "[BearTrapAttack] Use(GameObject, GameObject) called. " +
            "Bear Trap requires a tile, not an enemy target."
        );

        return false;
    }
}