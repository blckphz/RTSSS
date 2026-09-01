using System;
using System.Collections;
using TMPro;
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
    // PLAYER
    // ==================================================

    [Header("Player")]
    [Tooltip(
        "If enabled, this unit's health is saved in PlayerDataManager " +
        "and restored when the next encounter starts."
    )]
    [SerializeField]
    private bool isPlayerCharacter;


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
    [Tooltip(
        "Enables a small screen shake whenever this unit loses health."
    )]
    [SerializeField]
    private bool enableDamageScreenShake = true;

    [Tooltip(
        "Strength of the screen shake when this unit takes damage."
    )]
    [SerializeField]
    private float damageScreenShakeMagnitude = 1f;

    [Tooltip(
        "Duration of the screen shake when this unit takes damage."
    )]
    [SerializeField]
    private float damageScreenShakeDuration = 0.15f;


    // ==================================================
    // DAMAGE SOUND
    // ==================================================

    [Header("Damage Sound")]
    [Tooltip(
        "Enables the damage sound whenever this unit loses health."
    )]
    [SerializeField]
    private bool enableDamageSound = true;


    // ==================================================
    // DAMAGE NUMBERS
    // ==================================================

    [Header("Damage Numbers")]
    [Tooltip(
        "Prefab used to display floating damage numbers."
    )]
    [SerializeField]
    private DamageNumber damageNumberPrefab;

    [Tooltip(
        "Canvas or transform that will contain spawned damage numbers."
    )]
    [SerializeField]
    private Transform damageNumberParent;

    [Tooltip(
        "World-space offset from the unit where the damage number appears."
    )]
    [SerializeField]
    private Vector3 damageNumberOffset =
        new Vector3(0f, 1f, 0f);


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

    private PlayerDataManager playerDataManager;


    // ==================================================
    // UNITY
    // ==================================================

    private void Awake()
    {
        maxHealth =
            Mathf.Max(
                1,
                maxHealth
            );

        health =
            maxHealth;

        SetupMaterial();

        audioFXManager =
            AudioFXManager.Instance;

        playerDataManager =
            PlayerDataManager.Instance;
    }


    // ==================================================
    // MATERIAL SETUP
    // ==================================================

    private void SetupMaterial()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (material == null)
        {
            material =
                spriteRenderer.material;
        }
    }


    // ==================================================
    // INITIALIZE
    // ==================================================

    public void Initialize(
        CharacterSO character)
    {
        if (character == null)
        {
            return;
        }


        // --------------------------------------------------
        // CHARACTER DATA
        // --------------------------------------------------

        team =
            character.team;

        maxHealth =
            Mathf.Max(
                1,
                character.maxHealth
            );

        isPlayerCharacter =
            character.isPlayerCharacter;


        // --------------------------------------------------
        // MATERIAL
        // --------------------------------------------------

        SetupMaterial();

        StopDamageFlash();


        // --------------------------------------------------
        // AUDIO
        // --------------------------------------------------

        if (audioFXManager == null)
        {
            audioFXManager =
                AudioFXManager.Instance;
        }


        // --------------------------------------------------
        // PLAYER HEALTH
        // --------------------------------------------------

        if (isPlayerCharacter)
        {
            if (playerDataManager == null)
            {
                playerDataManager =
                    PlayerDataManager.Instance;
            }


            if (playerDataManager != null)
            {
                // Initialize persistent player data.
                // This only sets the initial HP the first time.
                playerDataManager.Initialize(
                    character
                );


                // Restore HP from the previous encounter.
                health =
                    playerDataManager.GetHealth();
            }
            else
            {
                // Fallback if PlayerDataManager doesn't exist.
                health =
                    maxHealth;
            }
        }


        // --------------------------------------------------
        // NORMAL UNIT
        // --------------------------------------------------

        else
        {
            health =
                maxHealth;
        }


        // --------------------------------------------------
        // NOTIFY UI
        // --------------------------------------------------

        NotifyHealthChanged();
    }


    // ==================================================
    // SET HEALTH
    // ==================================================

    public void SetHealth(
        int newHealth)
    {
        health =
            Mathf.Clamp(
                newHealth,
                0,
                maxHealth
            );

        NotifyHealthChanged();
    }


    // ==================================================
    // DAMAGE
    // ==================================================

    public void TakeDamage(
        int damage)
    {
        if (damage <= 0)
        {
            return;
        }


        if (IsDead())
        {
            return;
        }


        // --------------------------------------------------
        // APPLY DAMAGE
        // --------------------------------------------------

        health -=
            damage;


        if (health < 0)
        {
            health = 0;
        }


        // --------------------------------------------------
        // DAMAGE NUMBER
        // --------------------------------------------------

        SpawnDamageNumber(
            damage
        );


        // --------------------------------------------------
        // SAVE PLAYER HP
        // --------------------------------------------------

        SavePlayerHealth();


        // --------------------------------------------------
        // DAMAGE FEEDBACK
        // --------------------------------------------------

        PlayDamageSound();

        PlayDamageScreenShake();

        NotifyHealthChanged();

        FlashDamage();


        // --------------------------------------------------
        // DEATH
        // --------------------------------------------------

        if (health <= 0)
        {
            Die();
        }
    }


    // ==================================================
    // DAMAGE NUMBER
    // ==================================================

    private void SpawnDamageNumber(
        int damage)
    {
        if (damageNumberPrefab == null)
        {
            return;
        }


        // --------------------------------------------------
        // SPAWN POSITION
        // --------------------------------------------------

        Vector3 spawnPosition =
            transform.position
            + damageNumberOffset;


        // --------------------------------------------------
        // PARENT
        // --------------------------------------------------

        Transform parent =
            damageNumberParent;


        // --------------------------------------------------
        // INSTANTIATE
        // --------------------------------------------------

        DamageNumber damageNumber =
            Instantiate(
                damageNumberPrefab,
                spawnPosition,
                Quaternion.identity,
                parent
            );


        // --------------------------------------------------
        // INITIALIZE
        // --------------------------------------------------

        damageNumber.Setup(
            damage
        );
    }


    // ==================================================
    // SAVE PLAYER HEALTH
    // ==================================================

    private void SavePlayerHealth()
    {
        if (!isPlayerCharacter)
        {
            return;
        }


        if (playerDataManager == null)
        {
            playerDataManager =
                PlayerDataManager.Instance;
        }


        if (playerDataManager != null)
        {
            playerDataManager.SetHealth(
                health
            );
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
            audioFXManager =
                AudioFXManager.Instance;
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
    // HEALTH CHANGE
    // ==================================================

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(this);


        // --------------------------------------------------
        // SAVE PLAYER HEALTH
        // --------------------------------------------------

        if (isPlayerCharacter)
        {
            if (playerDataManager == null)
            {
                playerDataManager =
                    PlayerDataManager.Instance;
            }


            if (playerDataManager != null)
            {
                playerDataManager.SetHealth(
                    health
                );
            }
        }
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


        if (flashCoroutine != null)
        {
            StopCoroutine(
                flashCoroutine
            );
        }


        flashCoroutine =
            StartCoroutine(
                SingleDamageFlashCoroutine()
            );
    }


    // ==================================================
    // QUEUED FLASH
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


        material.SetFloat(
            "_Intensity",
            flashIntensity
        );


        yield return null;


        float timer = 0f;


        while (timer < flashDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                flashDuration <= 0f
                    ? 1f
                    : timer / flashDuration;


            t =
                Mathf.Clamp01(t);


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
    // STOP FLASH
    // ==================================================

    public void StopDamageFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(
                flashCoroutine
            );


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

    public void Heal(
        int amount)
    {
        if (amount <= 0)
        {
            return;
        }


        if (IsDead())
        {
            return;
        }


        health +=
            amount;


        if (health > maxHealth)
        {
            health =
                maxHealth;
        }


        NotifyHealthChanged();
    }


    // ==================================================
    // FULL HEAL
    // ==================================================

    public void FullHeal()
    {
        if (IsDead())
        {
            return;
        }


        health =
            maxHealth;


        NotifyHealthChanged();
    }


    // ==================================================
    // DEATH
    // ==================================================

    private void Die()
    {
        // Save zero HP before the unit is disabled.
        SavePlayerHealth();


        StopDamageFlash();


        // --------------------------------------------------
        // GET ENCOUNTER INFORMATION BEFORE DISABLING
        // --------------------------------------------------

        EncounterUnit encounterUnit =
            GetComponent<EncounterUnit>();


        string encounterUnitId = null;


        if (encounterUnit != null)
        {
            encounterUnitId =
                encounterUnit.GetEncounterUnitId();
        }


        // --------------------------------------------------
        // NOTIFY ENCOUNTER MANAGER
        // --------------------------------------------------

        EncounterManager encounterManager =
            FindFirstObjectByType<EncounterManager>();


        if (encounterManager != null)
        {
            encounterManager.HandleUnitKilled(
                this,
                encounterUnitId
            );
        }


        // --------------------------------------------------
        // REMOVE FROM GRID
        // --------------------------------------------------

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


        // --------------------------------------------------
        // DISABLE
        // --------------------------------------------------

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


    public bool IsPlayerCharacter()
    {
        return isPlayerCharacter;
    }


    // ==================================================
    // SETTERS
    // ==================================================

    public void SetTeam(
        Team newTeam)
    {
        team = newTeam;
    }


    public void SetMaxHealth(
        int newMaxHealth)
    {
        maxHealth =
            Mathf.Max(
                1,
                newMaxHealth
            );


        // IMPORTANT:
        // Do NOT automatically restore health to max.
        // This is what allows HP to persist between encounters.

        health =
            Mathf.Clamp(
                health,
                0,
                maxHealth
            );


        // Keep persistent player data in sync.
        SavePlayerHealth();


        StopDamageFlash();


        NotifyHealthChanged();
    }
}