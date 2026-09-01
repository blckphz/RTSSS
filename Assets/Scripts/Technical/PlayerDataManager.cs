using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // ============================================================
    // PLAYER HEALTH
    // ============================================================

    private int currentHealth;
    private int maxHealth;

    private bool initialized;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    public void Initialize(CharacterSO character)
    {
        if (character == null)
        {
            return;
        }

        maxHealth =
            Mathf.Max(
                1,
                character.maxHealth
            );

        // Only initialize HP once.
        // This prevents the player from being healed
        // when a new encounter starts.
        if (!initialized)
        {
            currentHealth =
                maxHealth;

            initialized = true;
        }
        else
        {
            // In case max health changed between encounters,
            // make sure current health remains valid.
            currentHealth =
                Mathf.Clamp(
                    currentHealth,
                    0,
                    maxHealth
                );
        }
    }


    // ============================================================
    // HEALTH
    // ============================================================

    public void SetHealth(int health)
    {
        currentHealth =
            Mathf.Clamp(
                health,
                0,
                maxHealth
            );
    }


    public int GetHealth()
    {
        return currentHealth;
    }


    public int GetMaxHealth()
    {
        return maxHealth;
    }


    public bool IsDead()
    {
        return currentHealth <= 0;
    }


    public bool IsAlive()
    {
        return currentHealth > 0;
    }


    // ============================================================
    // HEAL
    // ============================================================

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHealth =
            Mathf.Clamp(
                currentHealth + amount,
                0,
                maxHealth
            );
    }


    // ============================================================
    // FULL HEAL
    // ============================================================

    public void FullHeal()
    {
        currentHealth =
            maxHealth;
    }
}
