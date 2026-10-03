using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    // ============================================================
    // EXPLOSION ABILITY
    // ============================================================

    [Header("Explosion Ability")]

    [SerializeField]
    private AbilitySO explosionAbility;


    // ============================================================
    // EXPLOSION PREFAB
    // ============================================================

    [Header("Explosion")]

    [SerializeField]
    private GameObject explosionPrefab;


    // ============================================================
    // PRIVATE
    // ============================================================

    private HealthManager healthManager;

    private GridManager gridManager;

    private int previousHealth;

    private bool hasExploded;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        healthManager =
            GetComponent<HealthManager>();
    }


    private void Start()
    {
        gridManager =
            FindFirstObjectByType<GridManager>();

        if (healthManager != null)
        {
            previousHealth =
                healthManager.GetHealth();
        }
    }


    private void OnEnable()
    {
        HealthManager.OnHealthChanged +=
            HandleHealthChanged;
    }


    private void OnDisable()
    {
        HealthManager.OnHealthChanged -=
            HandleHealthChanged;
    }


    // ============================================================
    // HEALTH CHANGED
    // ============================================================

    private void HandleHealthChanged(
        HealthManager changedHealth
    )
    {
        // Ignore other units.
        if (changedHealth != healthManager)
        {
            return;
        }


        int currentHealth =
            healthManager.GetHealth();


        // --------------------------------------------------------
        // BARREL WAS DAMAGED
        // --------------------------------------------------------

        if (
            currentHealth < previousHealth &&
            !hasExploded
        )
        {
            ActivateExplosion();
        }


        previousHealth =
            currentHealth;
    }


    // ============================================================
    // ACTIVATE EXPLOSION
    // ============================================================

    private void ActivateExplosion()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;


        // --------------------------------------------------------
        // VALIDATION
        // --------------------------------------------------------

        if (explosionAbility == null)
        {
            Debug.LogError(
                $"[{name}] No explosion ability assigned!",
                this
            );

            return;
        }


        if (explosionPrefab == null)
        {
            Debug.LogError(
                $"[{name}] No explosion prefab assigned!",
                this
            );

            return;
        }


        if (gridManager == null)
        {
            gridManager =
                FindFirstObjectByType<GridManager>();
        }


        if (gridManager == null)
        {
            Debug.LogError(
                $"[{name}] No GridManager found!",
                this
            );

            return;
        }


        // --------------------------------------------------------
        // GET ABILITY VALUES
        // --------------------------------------------------------

        int explosionDamage =
            explosionAbility.GetEffectiveDamage(
                gameObject
            );


        int explosionRadius =
            explosionAbility.GetEffectiveRange(
                gameObject
            );


        AbilitySO.RangeShape explosionShape =
            explosionAbility.GetRangeShape();


        // --------------------------------------------------------
        // DEBUG
        // --------------------------------------------------------

        Debug.Log(
            $"[{name}] Explosion | " +
            $"Damage={explosionDamage} | " +
            $"Radius={explosionRadius} | " +
            $"Shape={explosionShape}"
        );


        // --------------------------------------------------------
        // GET BARREL TILE
        // --------------------------------------------------------

        Vector2Int barrelTile =
            gridManager.WorldToGridPosition(
                transform.position
            );


        // --------------------------------------------------------
        // GET WORLD POSITION
        // --------------------------------------------------------

        Vector3 explosionPosition =
            gridManager.GridToWorldPosition(
                barrelTile
            );


        // Keep the explosion on the
        // same Z level as the barrel.

        explosionPosition.z =
            transform.position.z;


        // --------------------------------------------------------
        // CREATE EXPLOSION
        // --------------------------------------------------------

        GameObject explosionObject =
            Instantiate(
                explosionPrefab,
                explosionPosition,
                Quaternion.identity
            );


        ExplosionAttack explosion =
            explosionObject.GetComponent<ExplosionAttack>();


        if (explosion == null)
        {
            Debug.LogError(
                $"[{name}] Explosion prefab does not have " +
                $"an ExplosionAttack component!",
                explosionObject
            );

            Destroy(
                explosionObject
            );

            return;
        }


        // --------------------------------------------------------
        // PASS ABILITY DATA
        // --------------------------------------------------------

        explosion.SetOwner(
            gameObject
        );


        explosion.SetExplosionDamage(
            explosionDamage
        );


        explosion.SetExplosionRadius(
            explosionRadius
        );


        explosion.SetExplosionShape(
            explosionShape
        );


        // --------------------------------------------------------
        // EXPLODE IMMEDIATELY
        // --------------------------------------------------------

        explosion.Explode();
    }
}