using UnityEngine;

public class trapbehav : MonoBehaviour
{
    private GameObject owner;
    private AbilitySO ability;

    private Vector2Int gridPosition;

    private int damage;

    private bool destroyAfterTrigger;

    private bool initialized;
    private bool triggered;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        SetupTriggerCollider();
    }


    private void SetupTriggerCollider()
    {
        BoxCollider2D trigger =
            GetComponent<BoxCollider2D>();

        if (trigger == null)
        {
            trigger =
                gameObject.AddComponent<BoxCollider2D>();
        }

        trigger.isTrigger = true;
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(
        GameObject owner,
        AbilitySO ability,
        Vector2Int gridPosition,
        int damage,
        bool destroyAfterTrigger
    )
    {
        this.owner =
            owner;

        this.ability =
            ability;

        this.gridPosition =
            gridPosition;

        this.damage =
            Mathf.Max(0, damage);

        this.destroyAfterTrigger =
            destroyAfterTrigger;

        initialized = true;
        triggered = false;

        SetupTriggerCollider();
    }


    // ============================================================
    // TRIGGER COLLIDER
    // ============================================================

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other == null)
        {
            return;
        }

        AttackUnit targetUnit =
            other.GetComponentInParent<AttackUnit>();

        GameObject target =
            targetUnit != null
                ? targetUnit.gameObject
                : other.gameObject;

        TryTrigger(target);
    }


    // ============================================================
    // TRY TRIGGER
    // ============================================================

    public bool TryTrigger(
        GameObject target
    )
    {
        if (!initialized)
        {
            return false;
        }

        if (triggered)
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }


        // --------------------------------------------------------
        // DON'T TRIGGER ON OWNER
        // --------------------------------------------------------

        if (target == owner)
        {
            return false;
        }


        // --------------------------------------------------------
        // TARGET MUST BE ACTIVE
        // --------------------------------------------------------

        if (!target.activeInHierarchy)
        {
            return false;
        }


        // --------------------------------------------------------
        // TARGET MUST BE A UNIT
        // --------------------------------------------------------

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (targetUnit == null)
        {
            targetUnit =
                target.GetComponentInParent<AttackUnit>();
        }

        if (targetUnit == null)
        {
            return false;
        }


        // --------------------------------------------------------
        // TARGET MUST BE ALIVE
        // --------------------------------------------------------

        HealthManager health =
            target.GetComponent<HealthManager>();

        if (health == null)
        {
            health =
                target.GetComponentInParent<HealthManager>();
        }

        if (health == null)
        {
            return false;
        }

        if (health.IsDead())
        {
            return false;
        }


        // --------------------------------------------------------
        // TARGET TEAM
        // --------------------------------------------------------

        if (!CanDamage(targetUnit.gameObject))
        {
            return false;
        }


        // --------------------------------------------------------
        // TRIGGER
        // --------------------------------------------------------

        Trigger(targetUnit.gameObject);

        return true;
    }


    // ============================================================
    // CAN DAMAGE
    // ============================================================

    private bool CanDamage(
        GameObject target
    )
    {
        if (
            owner == null ||
            target == null
        )
        {
            return false;
        }

        if (ability != null)
        {
            return ability.CanTargetObject(
                owner,
                target
            );
        }


        // --------------------------------------------------------
        // FALLBACK
        // --------------------------------------------------------

        AttackUnit ownerUnit =
            owner.GetComponent<AttackUnit>();

        AttackUnit targetUnit =
            target.GetComponent<AttackUnit>();

        if (
            ownerUnit == null ||
            targetUnit == null
        )
        {
            return false;
        }

        return
            ownerUnit.GetTeam() !=
            targetUnit.GetTeam();
    }


    // ============================================================
    // TRIGGER
    // ============================================================

    private void Trigger(
        GameObject target
    )
    {
        if (triggered)
        {
            return;
        }

        triggered = true;


        // --------------------------------------------------------
        // STOP UNIT MOVEMENT
        // --------------------------------------------------------

        UnitMoveBrain moveBrain =
            target.GetComponent<UnitMoveBrain>();

        if (moveBrain == null)
        {
            moveBrain =
                target.GetComponentInParent<UnitMoveBrain>();
        }

        if (moveBrain != null)
        {
            moveBrain.StopMovement();
        }


        // --------------------------------------------------------
        // DAMAGE
        // --------------------------------------------------------

        HealthManager health =
            target.GetComponent<HealthManager>();

        if (health == null)
        {
            health =
                target.GetComponentInParent<HealthManager>();
        }

        if (health != null)
        {
            health.TakeDamage(damage);
        }


        // --------------------------------------------------------
        // DESTROY
        // --------------------------------------------------------

        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }


    public GameObject GetOwner()
    {
        return owner;
    }


    public AbilitySO GetAbility()
    {
        return ability;
    }


    public bool HasTriggered()
    {
        return triggered;
    }
}