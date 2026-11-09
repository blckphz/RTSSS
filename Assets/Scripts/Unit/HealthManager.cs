
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    public static event Action<HealthManager> OnHealthChanged;
    public static event Action<HealthManager> OnDie;

    [Header("Team")]
    [SerializeField] private Team team;

    [Header("Player")]
    [SerializeField] private bool isPlayerCharacter;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health;

    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Health UI Feedback")]
    [SerializeField] private float healthTextPopScale = 1.25f;
    [SerializeField] private float healthTextPopDuration = 0.12f;
    [SerializeField] private float healthBarShakeAmount = 6f;
    [SerializeField] private float healthBarShakeDuration = 0.12f;

    [Header("Damage Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashIntensity = 1f;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("Damage Screen Shake")]
    [SerializeField] private bool enableDamageScreenShake = true;
    [SerializeField] private float damageScreenShakeMagnitude = 1f;
    [SerializeField] private float damageScreenShakeDuration = 0.15f;

    [Header("Damage Sound")]
    [SerializeField] private bool enableDamageSound = true;

    [Header("Heal Sound")]
    [SerializeField] private bool enableHealSound = true;

    [Header("Damage Numbers")]
    [SerializeField] private DamageNumber damageNumberPrefab;
    [SerializeField] private Transform damageNumberParent;
    [SerializeField]
    private Vector3 damageNumberOffset =
        new Vector3(0f, 1f, 0f);

    [Header("Heal Numbers")]
    [SerializeField] private DamageNumber healNumberPrefab;
    [SerializeField] private Transform healNumberParent;
    [SerializeField]
    private Vector3 healNumberOffset =
        new Vector3(0f, 1f, 0f);

    [Header("Queued Flash")]
    [SerializeField] private bool queueDamageFlashes = true;
    [SerializeField, Min(0f)] private float minimumFlashInterval = 0.03f;

    private Material material;
    private Coroutine flashCoroutine;
    private Coroutine healthTextPopCoroutine;
    private Coroutine healthBarShakeCoroutine;

    private int pendingFlashes;
    private bool isFlashing;

    private AudioFXManager audioFXManager;
    private PlayerDataManager playerDataManager;

    private GridManager gridManager;

    private RectTransform healthTextRect;
    private RectTransform healthBarRect;

    private Vector3 healthTextOriginalScale;
    private Vector3 healthBarOriginalPosition;

    private static int combinedDamageBatchDepth;

    private static readonly HashSet<HealthManager>
        combinedDamageManagers =
        new HashSet<HealthManager>();

    private int combinedDamage;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        health = maxHealth;

        SetupMaterial();

        audioFXManager = AudioFXManager.Instance;
        playerDataManager = PlayerDataManager.Instance;

        SetupHealthUI();
        UpdateHealthUI();

        Debug.Log(
            "[HealthManager] Awake: " + gameObject.name +
            " | Health: " + health + " / " + maxHealth
        );
    }

    private void OnEnable()
    {
        RegisterGridObstacle();
    }

    private void OnDisable()
    {
        UnregisterGridObstacle();
    }

    private void RegisterGridObstacle()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
            gridManager.RegisterHealthManager(this);
    }

    private void UnregisterGridObstacle()
    {
        if (gridManager != null)
            gridManager.UnregisterHealthManager(this);
    }

    // =========================================================
    // UI SETUP
    // =========================================================

    private void SetupHealthUI()
    {
        if (healthText != null)
        {
            healthTextRect = healthText.rectTransform;
            healthTextOriginalScale = healthTextRect.localScale;
        }

        if (healthBarFill != null)
        {
            healthBarRect = healthBarFill.rectTransform;
            healthBarOriginalPosition = healthBarRect.localPosition;
        }
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            float fillAmount = maxHealth <= 0
                ? 0f
                : (float)health / maxHealth;

            healthBarFill.fillAmount = Mathf.Clamp01(fillAmount);
        }

        if (healthText != null)
            healthText.text = health + " / " + maxHealth;
    }

    // =========================================================
    // MATERIAL
    // =========================================================

    private void SetupMaterial()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && material == null)
            material = spriteRenderer.material;
    }

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(CharacterSO character)
    {
        if (character == null)
            return;

        team = character.team;
        maxHealth = Mathf.Max(1, character.maxHealth);
        isPlayerCharacter = character.isPlayerCharacter;

        SetupMaterial();
        StopDamageFlash();

        if (audioFXManager == null)
            audioFXManager = AudioFXManager.Instance;

        if (isPlayerCharacter)
        {
            if (playerDataManager == null)
                playerDataManager = PlayerDataManager.Instance;

            if (playerDataManager != null)
            {
                playerDataManager.Initialize(character);
                health = playerDataManager.GetHealth();
            }
            else
            {
                health = maxHealth;
            }
        }
        else
        {
            health = maxHealth;
        }

        health = Mathf.Clamp(health, 0, maxHealth);
        combinedDamage = 0;

        SetupHealthUI();
        ResetHealthUITransform();
        UpdateHealthUI();

        NotifyHealthChanged();

        Debug.Log(
            "[HealthManager] Initialized: " + gameObject.name +
            " | " + health + " / " + maxHealth +
            " | Player: " + isPlayerCharacter
        );
    }

    // =========================================================
    // SET HEALTH
    // =========================================================

    public void SetHealth(int newHealth)
    {
        health = Mathf.Clamp(newHealth, 0, maxHealth);
        SavePlayerHealth();
        NotifyHealthChanged();
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead())
            return;

        BossLobsterBrain lobster =
            GetComponent<BossLobsterBrain>();

        if (lobster != null)
            damage = lobster.ModifyIncomingDamage(damage);

        if (damage <= 0)
            return;

        health = Mathf.Max(0, health - damage);

        if (IsCombinedDamageBatchActive())
        {
            combinedDamage += damage;
            combinedDamageManagers.Add(this);
        }
        else
        {
            SpawnDamageNumber(damage);
        }

        SavePlayerHealth();
        PlayDamageSound();
        PlayDamageScreenShake();
        NotifyHealthChanged();
        FlashDamage();

        if (health <= 0)
            Die();
    }

    // =========================================================
    // COMBINED DAMAGE
    // =========================================================

    public static void BeginCombinedDamageBatch()
    {
        combinedDamageBatchDepth++;

        if (combinedDamageBatchDepth == 1)
            combinedDamageManagers.Clear();
    }

    public static void EndCombinedDamageBatch()
    {
        if (combinedDamageBatchDepth <= 0)
        {
            combinedDamageBatchDepth = 0;
            return;
        }

        combinedDamageBatchDepth--;

        if (combinedDamageBatchDepth > 0)
            return;

        List<HealthManager> managers =
            new List<HealthManager>(combinedDamageManagers);

        foreach (HealthManager manager in managers)
        {
            if (manager != null)
                manager.FlushCombinedDamage();
        }

        combinedDamageManagers.Clear();
    }

    public static bool IsCombinedDamageBatchActive()
    {
        return combinedDamageBatchDepth > 0;
    }

    private void FlushCombinedDamage()
    {
        if (combinedDamage > 0)
            SpawnDamageNumber(combinedDamage);

        combinedDamage = 0;
    }

    public void CancelCombinedDamage()
    {
        combinedDamage = 0;
        combinedDamageManagers.Remove(this);
    }

    // =========================================================
    // DAMAGE NUMBERS
    // =========================================================

    private void SpawnDamageNumber(int damage)
    {
        if (damageNumberPrefab == null)
            return;

        DamageNumber number = Instantiate(
            damageNumberPrefab,
            transform.position + damageNumberOffset,
            Quaternion.identity,
            damageNumberParent
        );

        number.Setup(damage);
    }

    private void SpawnHealNumber(int amount)
    {
        if (amount <= 0 || healNumberPrefab == null)
            return;

        Transform parent = healNumberParent != null
            ? healNumberParent
            : damageNumberParent;

        DamageNumber number = Instantiate(
            healNumberPrefab,
            transform.position + healNumberOffset,
            Quaternion.identity,
            parent
        );

        number.Setup(amount);
    }

    // =========================================================
    // PLAYER HEALTH SAVE
    // =========================================================

    private void SavePlayerHealth()
    {
        if (!isPlayerCharacter)
            return;

        if (playerDataManager == null)
            playerDataManager = PlayerDataManager.Instance;

        if (playerDataManager != null)
            playerDataManager.SetHealth(health);
    }

    // =========================================================
    // SOUNDS
    // =========================================================

    private void PlayDamageSound()
    {
        if (!enableDamageSound)
            return;

        if (audioFXManager == null)
            audioFXManager = AudioFXManager.Instance;

        if (audioFXManager != null)
            audioFXManager.PlayUnitDamage();
    }

    private void PlayHealSound()
    {
        if (!enableHealSound)
            return;

        if (audioFXManager == null)
            audioFXManager = AudioFXManager.Instance;

        if (audioFXManager != null)
            audioFXManager.PlayUnitHeal();
    }

    // =========================================================
    // SCREEN SHAKE
    // =========================================================

    private void PlayDamageScreenShake()
    {
        if (!enableDamageScreenShake)
            return;

        if (ScreenShaker.Instance != null)
        {
            ScreenShaker.Instance.Shake(
                damageScreenShakeMagnitude,
                damageScreenShakeDuration
            );
        }
    }

    // =========================================================
    // HEALTH CHANGE
    // =========================================================

    private void NotifyHealthChanged()
    {
        UpdateHealthUI();
        PlayHealthUIFeedback();

        OnHealthChanged?.Invoke(this);

        SavePlayerHealth();
    }

    // =========================================================
    // HEALTH UI FEEDBACK
    // =========================================================

    private void PlayHealthUIFeedback()
    {
        if (healthTextRect != null)
        {
            if (healthTextPopCoroutine != null)
                StopCoroutine(healthTextPopCoroutine);

            healthTextPopCoroutine =
                StartCoroutine(HealthTextPopCoroutine());
        }

        if (healthBarRect != null)
        {
            if (healthBarShakeCoroutine != null)
                StopCoroutine(healthBarShakeCoroutine);

            healthBarShakeCoroutine =
                StartCoroutine(HealthBarShakeCoroutine());
        }
    }

    private IEnumerator HealthTextPopCoroutine()
    {
        if (healthTextRect == null)
            yield break;

        float duration = Mathf.Max(0.01f, healthTextPopDuration);

        Vector3 original = healthTextOriginalScale;
        Vector3 popped = original * healthTextPopScale;

        float timer = 0f;

        while (timer < duration * 0.5f)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / (duration * 0.5f));
            t = Mathf.SmoothStep(0f, 1f, t);

            healthTextRect.localScale =
                Vector3.Lerp(original, popped, t);

            yield return null;
        }

        timer = 0f;

        while (timer < duration * 0.5f)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / (duration * 0.5f));
            t = Mathf.SmoothStep(0f, 1f, t);

            healthTextRect.localScale =
                Vector3.Lerp(popped, original, t);

            yield return null;
        }

        healthTextRect.localScale = original;
        healthTextPopCoroutine = null;
    }

    private IEnumerator HealthBarShakeCoroutine()
    {
        if (healthBarRect == null)
            yield break;

        float duration = Mathf.Max(0.01f, healthBarShakeDuration);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float strength = Mathf.Lerp(
                1f,
                0f,
                Mathf.Clamp01(timer / duration)
            );

            Vector2 offset =
                UnityEngine.Random.insideUnitCircle *
                healthBarShakeAmount * strength;

            healthBarRect.localPosition =
                healthBarOriginalPosition +
                new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        healthBarRect.localPosition = healthBarOriginalPosition;
        healthBarShakeCoroutine = null;
    }

    private void ResetHealthUITransform()
    {
        if (healthTextRect != null)
            healthTextRect.localScale = healthTextOriginalScale;

        if (healthBarRect != null)
            healthBarRect.localPosition = healthBarOriginalPosition;
    }

    // =========================================================
    // DAMAGE FLASH
    // =========================================================

    public void FlashDamage()
    {
        SetupMaterial();

        if (material == null ||
            !material.HasProperty("_Intensity"))
            return;

        if (queueDamageFlashes)
        {
            pendingFlashes++;

            if (!isFlashing)
                flashCoroutine =
                    StartCoroutine(DamageFlashQueueCoroutine());

            return;
        }

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine =
            StartCoroutine(SingleDamageFlashCoroutine());
    }

    private IEnumerator DamageFlashQueueCoroutine()
    {
        isFlashing = true;

        while (pendingFlashes > 0)
        {
            pendingFlashes--;

            yield return StartCoroutine(
                SingleDamageFlashCoroutine()
            );

            if (pendingFlashes > 0 && minimumFlashInterval > 0f)
            {
                yield return new WaitForSeconds(
                    minimumFlashInterval
                );
            }
        }

        isFlashing = false;
        flashCoroutine = null;
    }

    private IEnumerator SingleDamageFlashCoroutine()
    {
        SetupMaterial();

        if (material == null ||
            !material.HasProperty("_Intensity"))
            yield break;

        material.SetFloat("_Intensity", flashIntensity);

        yield return null;

        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;

            float t = flashDuration <= 0f
                ? 1f
                : Mathf.Clamp01(timer / flashDuration);

            material.SetFloat(
                "_Intensity",
                Mathf.Lerp(flashIntensity, 0f, t)
            );

            yield return null;
        }

        material.SetFloat("_Intensity", 0f);
        yield return null;
    }

    public void StopDamageFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        pendingFlashes = 0;
        isFlashing = false;

        if (material != null &&
            material.HasProperty("_Intensity"))
        {
            material.SetFloat("_Intensity", 0f);
        }
    }

    // =========================================================
    // HEAL
    // =========================================================

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead())
            return;

        int oldHealth = health;

        health = Mathf.Min(health + amount, maxHealth);

        int actualHealing = health - oldHealth;

        if (actualHealing > 0)
        {
            SpawnHealNumber(actualHealing);
            PlayHealSound();
        }

        NotifyHealthChanged();
    }

    public void FullHeal()
    {
        if (IsDead())
            return;

        int oldHealth = health;
        health = maxHealth;

        int actualHealing = health - oldHealth;

        if (actualHealing > 0)
        {
            SpawnHealNumber(actualHealing);
            PlayHealSound();
        }

        NotifyHealthChanged();
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        SavePlayerHealth();

        if (isPlayerCharacter)
        {
            GameStateManager gameStateManager =
                FindFirstObjectByType<GameStateManager>();

            if (gameStateManager != null)
                gameStateManager.GameOver();

            StopDamageFlash();
            gameObject.SetActive(false);
            return;
        }

        OnDie?.Invoke(this);

        StopDamageFlash();

        EncounterUnit encounterUnit =
            GetComponent<EncounterUnit>();

        string encounterUnitId = encounterUnit != null
            ? encounterUnit.GetEncounterUnitId()
            : null;

        EncounterManager encounterManager =
            FindFirstObjectByType<EncounterManager>();

        if (encounterManager != null)
        {
            encounterManager.HandleUnitKilled(
                this,
                encounterUnitId
            );
        }

        GridManager currentGridManager =
            FindFirstObjectByType<GridManager>();

        if (currentGridManager != null)
        {
            currentGridManager.RemoveUnit(gameObject);

            GridHighlightBrain highlightBrain =
                FindFirstObjectByType<GridHighlightBrain>();

            if (highlightBrain != null)
                highlightBrain.RefreshAfterUnitStateChanged();
        }

        gameObject.SetActive(false);
    }

    // =========================================================
    // GETTERS
    // =========================================================

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

    // =========================================================
    // SETTERS
    // =========================================================

    public void SetTeam(Team newTeam)
    {
        team = newTeam;
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        health = Mathf.Clamp(health, 0, maxHealth);

        SavePlayerHealth();
        StopDamageFlash();
        NotifyHealthChanged();
    }

    // =========================================================
    // ADD MAX HEALTH
    // =========================================================

    public void AddMaxHealth(int amount)
    {
        if (amount <= 0)
            return;

        Debug.Log(
            "[HealthManager] AddMaxHealth on " + gameObject.name +
            " | Before: " + health + " / " + maxHealth
        );

        maxHealth += amount;
        health += amount;

        health = Mathf.Clamp(health, 0, maxHealth);

        SavePlayerHealth();

        // Explicitly refresh the assigned UI.
        UpdateHealthUI();

        NotifyHealthChanged();

        Debug.Log(
            "[HealthManager] After upgrade: " +
            health + " / " + maxHealth +
            " | Fill assigned: " + (healthBarFill != null) +
            " | Text assigned: " + (healthText != null)
        );
    }
}
