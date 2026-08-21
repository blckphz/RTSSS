using System;
using System.Collections;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    // ==================================================
    // EVENTS
    // ==================================================

    public static event Action<HealthManager> OnHealthChanged;


    // ==================================================
    // TEAM
    // ==================================================

    [Header("Team")]
    [SerializeField]
    private Team team;


    // ==================================================
    // HEALTH
    // ==================================================

    [Header("Health")]
    [SerializeField]
    private int maxHealth = 100;

    [SerializeField]
    private int health;


    // ==================================================
    // DAMAGE FLASH
    // ==================================================

    [Header("Damage Flash")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private float flashIntensity = 1f;

    [SerializeField]
    private float flashDuration = 0.15f;


    // ==================================================
    // DAMAGE SCREEN SHAKE
    // ==================================================

    [Header("Damage Screen Shake")]
    [Tooltip("Enables a small screen shake whenever this unit loses health.")]
    [SerializeField]
    private bool enableDamageScreenShake = true;

    [Tooltip("Strength of the screen shake when this unit takes damage.")]
    [SerializeField]
    private float damageScreenShakeMagnitude = 1f;

    [Tooltip("Duration of the screen shake when this unit takes damage.")]
    [SerializeField]
    private float damageScreenShakeDuration = 0.15f;


    // ==================================================
    // DAMAGE SOUND
    // ==================================================

    [Header("Damage Sound")]
    [Tooltip("Enables the damage sound whenever this unit loses health.")]
    [SerializeField]
    private bool enableDamageSound = true;


    // ==================================================
    // QUEUED FLASH
    // ==================================================

    [Header("Queued Flash")]
    [Tooltip(
        "When multiple attacks happen very quickly, " +
        "queue their flashes so every attack gets a visible flash."
    )]
    [SerializeField]
    private bool queueDamageFlashes = true;

    [SerializeField, Min(0f)]
    private float minimumFlashInterval = 0.03f;


    // ==================================================
    // PRIVATE
    // ==================================================

    private Material material;

    private Coroutine flashCoroutine;

    private int pendingFlashes = 0;

    private bool isFlashing = false;

    private AudioFXManager audioFXManager;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);

        health = maxHealth;

        SetupMaterial();

        audioFXManager = AudioFXManager.Instance;
    }


    // ==================================================
    // MATERIAL SETUP
    // ==================================================

    private void SetupMaterial()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (material == null)
        {
            material = spriteRenderer.material;
        }
    }


    // ==================================================
    // INITIALIZE FROM CHARACTER SO
    // ==================================================

    public void Initialize(CharacterSO character)
    {
        if (character == null)
        {
            return;
        }

        team = character.team;

        maxHealth = Mathf.Max(1, character.maxHealth);

        health = maxHealth;

        SetupMaterial();

        StopDamageFlash();

        if (audioFXManager == null)
        {
            audioFXManager = AudioFXManager.Instance;
        }

        NotifyHealthChanged();
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

        // DAMAGE SOUND
        PlayDamageSound();

        // DAMAGE SCREEN SHAKE
        PlayDamageScreenShake();

        // HEALTH CHANGE
        NotifyHealthChanged();

        // DAMAGE FLASH
        FlashDamage();

        // DEATH
        if (health <= 0)
        {
            Die();
        }
    }


    // ==================================================
    // DAMAGE SOUND
    // ==================================================

    private void PlayDamageSound()
    {
        if (!enableDamageSound)
        {
            return;
        }

        if (audioFXManager == null)
        {
            audioFXManager = AudioFXManager.Instance;
        }

        if (audioFXManager != null)
        {
            audioFXManager.PlayUnitDamage();
        }
    }


    // ==================================================
    // DAMAGE SCREEN SHAKE
    // ==================================================

    private void PlayDamageScreenShake()
    {
        if (!enableDamageScreenShake)
        {
            return;
        }

        if (ScreenShaker.Instance == null)
        {
            return;
        }

        ScreenShaker.Instance.Shake(
            damageScreenShakeMagnitude,
            damageScreenShakeDuration
        );
    }


    // ==================================================
    // HEALTH CHANGE NOTIFICATION
    // ==================================================

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(this);
    }


    // ==================================================
    // DAMAGE FLASH
    // ==================================================

    public void FlashDamage()
    {
        SetupMaterial();

        if (material == null)
        {
            return;
        }

        if (!material.HasProperty("_Intensity"))
        {
            return;
        }

        // =================================================
        // QUEUED MODE
        // =================================================

        if (queueDamageFlashes)
        {
            pendingFlashes++;

            if (!isFlashing)
            {
                flashCoroutine =
                    StartCoroutine(
                        DamageFlashQueueCoroutine()
                    );
            }

            return;
        }


        // =================================================
        // NORMAL MODE
        // =================================================

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine =
            StartCoroutine(
                SingleDamageFlashCoroutine()
            );
    }


    // ==================================================
    // QUEUED FLASH COROUTINE
    // ==================================================

    private IEnumerator DamageFlashQueueCoroutine()
    {
        isFlashing = true;

        while (pendingFlashes > 0)
        {
            pendingFlashes--;

            yield return StartCoroutine(
                SingleDamageFlashCoroutine()
            );

            if (
                pendingFlashes > 0 &&
                minimumFlashInterval > 0f
            )
            {
                yield return new WaitForSeconds(
                    minimumFlashInterval
                );
            }
        }

        isFlashing = false;

        flashCoroutine = null;
    }


    // ==================================================
    // SINGLE FLASH
    // ==================================================

    private IEnumerator SingleDamageFlashCoroutine()
    {
        SetupMaterial();

        if (
            material == null ||
            !material.HasProperty("_Intensity")
        )
        {
            yield break;
        }

        // FORCE FULL FLASH
        material.SetFloat(
            "_Intensity",
            flashIntensity
        );

        yield return null;

        // FLASH FADE
        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;

            float t =
                flashDuration <= 0f
                    ? 1f
                    : timer / flashDuration;

            t = Mathf.Clamp01(t);

            float intensity =
                Mathf.Lerp(
                    flashIntensity,
                    0f,
                    t
                );

            if (
                material != null &&
                material.HasProperty("_Intensity")
            )
            {
                material.SetFloat(
                    "_Intensity",
                    intensity
                );
            }

            yield return null;
        }

        // RESET
        if (
            material != null &&
            material.HasProperty("_Intensity")
        )
        {
            material.SetFloat(
                "_Intensity",
                0f
            );
        }

        yield return null;
    }


    // ==================================================
    // STOP DAMAGE FLASH
    // ==================================================

    public void StopDamageFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);

            flashCoroutine = null;
        }

        pendingFlashes = 0;

        isFlashing = false;

        if (
            material != null &&
            material.HasProperty("_Intensity")
        )
        {
            material.SetFloat(
                "_Intensity",
                0f
            );
        }
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

        NotifyHealthChanged();
    }


    // ==================================================
    // DEATH
    // ==================================================

    private void Die()
    {
        StopDamageFlash();

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
        }

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


    // ==================================================
    // SETTERS
    // ==================================================

    public void SetTeam(Team newTeam)
    {
        team = newTeam;
    }


    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);

        health = maxHealth;

        StopDamageFlash();

        NotifyHealthChanged();
    }
}