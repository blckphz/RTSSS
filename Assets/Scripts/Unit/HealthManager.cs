using System.Collections;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private Team team;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [SerializeField] private int health;

    [Header("Damage Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private float flashIntensity = 1f;

    [SerializeField] private float flashDuration = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private Material material;

    private Coroutine flashCoroutine;

    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        health = maxHealth;

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            material =
                spriteRenderer.material;
        }
    }

    // ==================================================
    // INITIALIZE FROM CHARACTER SO
    // ==================================================

    public void Initialize(CharacterSO character)
    {
        if (character == null)
        {
            Debug.LogWarning(
                $"[HealthManager] {gameObject.name}: " +
                "CharacterSO is null."
            );

            return;
        }

        team =
            character.team;

        maxHealth =
            character.maxHealth;

        health =
            maxHealth;
    }

    // ==================================================
    // DAMAGE
    // ==================================================

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        if (IsDead())
        {
            return;
        }

        health -= damage;

        if (health < 0)
        {
            health = 0;
        }

        DebugLog(
            $"{gameObject.name} took {damage} damage. " +
            $"Health: {health}/{maxHealth}"
        );

        FlashDamage();

        if (health <= 0)
        {
            Die();
        }
    }

    // ==================================================
    // DAMAGE FLASH
    // ==================================================

    private void FlashDamage()
    {
        if (material == null)
        {
            return;
        }

        if (!material.HasProperty("_Intensity"))
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(
                flashCoroutine
            );
        }

        flashCoroutine =
            StartCoroutine(
                DamageFlashCoroutine()
            );
    }

    private IEnumerator DamageFlashCoroutine()
    {
        float timer = 0f;

        material.SetFloat(
            "_Intensity",
            flashIntensity
        );

        while (timer < flashDuration)
        {
            timer +=
                Time.deltaTime;

            float t =
                timer / flashDuration;

            float intensity =
                Mathf.Lerp(
                    flashIntensity,
                    0f,
                    t
                );

            material.SetFloat(
                "_Intensity",
                intensity
            );

            yield return null;
        }

        material.SetFloat(
            "_Intensity",
            0f
        );

        flashCoroutine =
            null;
    }

    // ==================================================
    // HEAL
    // ==================================================

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (IsDead())
        {
            return;
        }

        health += amount;

        if (health > maxHealth)
        {
            health = maxHealth;
        }

        DebugLog(
            $"{gameObject.name} healed {amount}. " +
            $"Health: {health}/{maxHealth}"
        );
    }

    // ==================================================
    // DEATH
    // ==================================================

    private void Die()
    {
        DebugLog(
            $"{gameObject.name} has died."
        );

        if (material != null &&
            material.HasProperty("_Intensity"))
        {
            material.SetFloat(
                "_Intensity",
                0f
            );
        }

        // ==============================================
        // REMOVE FROM GRID BEFORE DISABLING
        // ==============================================

        GridManager gridManager =
            FindFirstObjectByType<GridManager>();

        if (gridManager != null)
        {
            Vector2Int gridPosition =
                gridManager.WorldToGridPosition(
                    transform.position
                );

            gridManager.RemoveUnit(
                gridPosition
            );

            DebugLog(
                $"{gameObject.name} removed from " +
                $"grid cell {gridPosition}."
            );
        }

        // ==============================================
        // DISABLE UNIT
        // ==============================================

        gameObject.SetActive(false);
    }

    // ==================================================
    // GETTERS
    // ==================================================

    public int GetHealth()
    {
        return health;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public Team GetTeam()
    {
        return team;
    }

    public bool IsDead()
    {
        return health <= 0;
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    public void SetTeam(Team newTeam)
    {
        team =
            newTeam;
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth =
            Mathf.Max(
                1,
                newMaxHealth
            );

        health =
            maxHealth;
    }

    // ==================================================
    // DEBUG
    // ==================================================

    private void DebugLog(
        string message
    )
    {
        if (!enableDebugLogs)
        {
            return;
        }

        Debug.Log(
            $"[HealthManager] {message}"
        );
    }
}