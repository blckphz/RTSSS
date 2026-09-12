using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip(
        "If enabled, this unit is the main player character. " +
        "If the main player character reaches 0 HP, the game enters Game Over. " +
        "Allies and enemies should have this disabled."
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
    // HEALTH BAR
    // ==================================================

    [Header("Health Bar")]
    [Tooltip(
        "UI Image used as the health bar fill. " +
        "Set Image Type to Filled."
    )]
    [SerializeField]
    private Image healthBarFill;

    [Tooltip(
        "TextMeshPro text used to display current and maximum health."
    )]
    [SerializeField]
    private TMP_Text healthText;


    // ==================================================
    // HEALTH UI FEEDBACK
    // ==================================================

    [Header("Health UI Feedback")]
    [Tooltip(
        "How large the health number becomes when health changes."
    )]
    [SerializeField]
    private float healthTextPopScale = 1.25f;

    [Tooltip(
        "Duration of the health number pop animation."
    )]
    [SerializeField]
    private float healthTextPopDuration = 0.12f;

    [Tooltip(
        "How far the health bar moves during the shake."
    )]
    [SerializeField]
    private float healthBarShakeAmount = 6f;

    [Tooltip(
        "Duration of the health bar shake."
    )]
    [SerializeField]
    private float healthBarShakeDuration = 0.12f;


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
    private Coroutine healthTextPopCoroutine;
    private Coroutine healthBarShakeCoroutine;

    private int pendingFlashes = 0;
    private bool isFlashing = false;

    private AudioFXManager audioFXManager;
    private PlayerDataManager playerDataManager;


    // ==================================================
    // HEALTH UI REFERENCES
    // ==================================================

    private RectTransform healthTextRect;
    private RectTransform healthBarRect;

    private Vector3 healthTextOriginalScale;
    private Vector3 healthBarOriginalPosition;


    // ==================================================
    // COMBINED DAMAGE BATCH
    // ==================================================

    private static int combinedDamageBatchDepth = 0;

    private static readonly HashSet<HealthManager>
        combinedDamageManagers =
        new HashSet<HealthManager>();

    private int combinedDamage = 0;


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

        SetupHealthUI();

        UpdateHealthUI();
    }


    // ==================================================
    // HEALTH UI SETUP
    // ==================================================

    private void SetupHealthUI()
    {
        if (healthText != null)
        {
            healthTextRect =
                healthText.rectTransform;

            healthTextOriginalScale =
                healthTextRect.localScale;
        }

        if (healthBarFill != null)
        {
            healthBarRect =
                healthBarFill.rectTransform;

            healthBarOriginalPosition =
                healthBarRect.localPosition;
        }
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

        team =
            character.team;

        maxHealth =
            Mathf.Max(
                1,
                character.maxHealth
            );

        isPlayerCharacter =
            character.isPlayerCharacter;

        SetupMaterial();

        StopDamageFlash();

        if (audioFXManager == null)
        {
            audioFXManager =
                AudioFXManager.Instance;
        }

        if (isPlayerCharacter)
        {
            if (playerDataManager == null)
            {
                playerDataManager =
                    PlayerDataManager.Instance;
            }

            if (playerDataManager != null)
            {
                playerDataManager.Initialize(
                    character
                );

                health =
                    playerDataManager.GetHealth();
            }
            else
            {
                health =
                    maxHealth;
            }
        }
        else
        {
            health =
                maxHealth;
        }

        health =
            Mathf.Clamp(
                health,
                0,
                maxHealth
            );

        combinedDamage = 0;

        SetupHealthUI();

        ResetHealthUITransform();

        UpdateHealthUI();

        NotifyHealthChanged();
    }


    // ==================================================
    // UPDATE HEALTH UI
    // ==================================================

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            if (maxHealth <= 0)
            {
                healthBarFill.fillAmount =
                    0f;
            }
            else
            {
                float fillAmount =
                    (float)health /
                    maxHealth;

                healthBarFill.fillAmount =
                    Mathf.Clamp01(
                        fillAmount
                    );
            }
        }

        if (healthText != null)
        {
            healthText.text =
                health +
                " / " +
                maxHealth;
        }
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

        health -=
            damage;

        if (health < 0)
        {
            health = 0;
        }

        if (IsCombinedDamageBatchActive())
        {
            combinedDamage +=
                damage;

            combinedDamageManagers.Add(
                this
            );
        }
        else
        {
            SpawnDamageNumber(
                damage
            );
        }

        SavePlayerHealth();

        PlayDamageSound();

        PlayDamageScreenShake();

        NotifyHealthChanged();

        FlashDamage();

        if (health <= 0)
        {
            Die();
        }
    }


    // ==================================================
    // BEGIN COMBINED DAMAGE BATCH
    // ==================================================

    public static void BeginCombinedDamageBatch()
    {
        combinedDamageBatchDepth++;

        if (combinedDamageBatchDepth == 1)
        {
            combinedDamageManagers.Clear();
        }
    }


    // ==================================================
    // END COMBINED DAMAGE BATCH
    // ==================================================

    public static void EndCombinedDamageBatch()
    {
        if (combinedDamageBatchDepth <= 0)
        {
            combinedDamageBatchDepth = 0;
            return;
        }

        combinedDamageBatchDepth--;

        if (combinedDamageBatchDepth > 0)
        {
            return;
        }

        List<HealthManager> managers =
            new List<HealthManager>(
                combinedDamageManagers
            );

        for (
            int i = 0;
            i < managers.Count;
            i++
        )
        {
            HealthManager manager =
                managers[i];

            if (manager == null)
            {
                continue;
            }

            manager.FlushCombinedDamage();
        }

        combinedDamageManagers.Clear();
    }


    // ==================================================
    // IS COMBINED DAMAGE ACTIVE
    // ==================================================

    public static bool IsCombinedDamageBatchActive()
    {
        return combinedDamageBatchDepth > 0;
    }


    // ==================================================
    // FLUSH COMBINED DAMAGE
    // ==================================================

    private void FlushCombinedDamage()
    {
        if (combinedDamage > 0)
        {
            SpawnDamageNumber(
                combinedDamage
            );
        }

        combinedDamage = 0;
    }


    // ==================================================
    // CANCEL COMBINED DAMAGE
    // ==================================================

    public void CancelCombinedDamage()
    {
        combinedDamage = 0;

        combinedDamageManagers.Remove(
            this
        );
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

        Vector3 spawnPosition =
            transform.position +
            damageNumberOffset;

        Transform parent =
            damageNumberParent;

        DamageNumber damageNumber =
            Instantiate(
                damageNumberPrefab,
                spawnPosition,
                Quaternion.identity,
                parent
            );

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
        UpdateHealthUI();

        PlayHealthUIFeedback();

        OnHealthChanged?.Invoke(this);

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
    // HEALTH UI FEEDBACK
    // ==================================================

    private void PlayHealthUIFeedback()
    {
        if (healthTextRect != null)
        {
            if (healthTextPopCoroutine != null)
            {
                StopCoroutine(
                    healthTextPopCoroutine
                );
            }

            healthTextPopCoroutine =
                StartCoroutine(
                    HealthTextPopCoroutine()
                );
        }

        if (healthBarRect != null)
        {
            if (healthBarShakeCoroutine != null)
            {
                StopCoroutine(
                    healthBarShakeCoroutine
                );
            }

            healthBarShakeCoroutine =
                StartCoroutine(
                    HealthBarShakeCoroutine()
                );
        }
    }


    // ==================================================
    // HEALTH TEXT POP
    // ==================================================

    private IEnumerator HealthTextPopCoroutine()
    {
        if (healthTextRect == null)
        {
            yield break;
        }

        float duration =
            Mathf.Max(
                0.01f,
                healthTextPopDuration
            );

        Vector3 originalScale =
            healthTextOriginalScale;

        Vector3 poppedScale =
            originalScale *
            healthTextPopScale;

        float timer = 0f;

        while (timer < duration * 0.5f)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                timer /
                (duration * 0.5f);

            t =
                Mathf.Clamp01(t);

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            healthTextRect.localScale =
                Vector3.Lerp(
                    originalScale,
                    poppedScale,
                    t
                );

            yield return null;
        }

        timer = 0f;

        while (timer < duration * 0.5f)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                timer /
                (duration * 0.5f);

            t =
                Mathf.Clamp01(t);

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            healthTextRect.localScale =
                Vector3.Lerp(
                    poppedScale,
                    originalScale,
                    t
                );

            yield return null;
        }

        healthTextRect.localScale =
            originalScale;

        healthTextPopCoroutine =
            null;
    }


    // ==================================================
    // HEALTH BAR SHAKE
    // ==================================================

    private IEnumerator HealthBarShakeCoroutine()
    {
        if (healthBarRect == null)
        {
            yield break;
        }

        float duration =
            Mathf.Max(
                0.01f,
                healthBarShakeDuration
            );

        Vector3 originalPosition =
            healthBarOriginalPosition;

        float timer = 0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float strength =
                Mathf.Lerp(
                    1f,
                    0f,
                    t
                );

            Vector2 randomOffset =
                UnityEngine.Random.insideUnitCircle *
                healthBarShakeAmount *
                strength;

            healthBarRect.localPosition =
                originalPosition +
                new Vector3(
                    randomOffset.x,
                    randomOffset.y,
                    0f
                );

            yield return null;
        }

        healthBarRect.localPosition =
            originalPosition;

        healthBarShakeCoroutine =
            null;
    }


    // ==================================================
    // RESET HEALTH UI TRANSFORM
    // ==================================================

    private void ResetHealthUITransform()
    {
        if (healthTextRect != null)
        {
            healthTextRect.localScale =
                healthTextOriginalScale;
        }

        if (healthBarRect != null)
        {
            healthBarRect.localPosition =
                healthBarOriginalPosition;
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
        SavePlayerHealth();

        if (isPlayerCharacter)
        {
            GameStateManager gameStateManager =
                FindFirstObjectByType<GameStateManager>();

            if (gameStateManager != null)
            {
                gameStateManager.GameOver();
            }

            StopDamageFlash();

            gameObject.SetActive(false);

            return;
        }

        StopDamageFlash();

        EncounterUnit encounterUnit =
            GetComponent<EncounterUnit>();

        string encounterUnitId = null;

        if (encounterUnit != null)
        {
            encounterUnitId =
                encounterUnit.GetEncounterUnitId();
        }

        EncounterManager encounterManager =
            FindFirstObjectByType<EncounterManager>();

        if (encounterManager != null)
        {
            encounterManager.HandleUnitKilled(
                this,
                encounterUnitId
            );
        }

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

            GridHighlightBrain highlightBrain =
                FindFirstObjectByType<GridHighlightBrain>();

            if (highlightBrain != null)
            {
                highlightBrain.RefreshAfterUnitStateChanged();
            }
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
        team =
            newTeam;
    }


    public void SetMaxHealth(
        int newMaxHealth)
    {
        maxHealth =
            Mathf.Max(
                1,
                newMaxHealth
            );

        health =
            Mathf.Clamp(
                health,
                0,
                maxHealth
            );

        SavePlayerHealth();

        StopDamageFlash();

        NotifyHealthChanged();
    }

}