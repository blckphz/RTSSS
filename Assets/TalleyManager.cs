using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class TalleyManager : MonoBehaviour
{
    // ============================================================
    // SINGLETON
    // ============================================================

    public static TalleyManager Instance { get; private set; }


    // ============================================================
    // PLAYER PREFS
    // ============================================================

    private const string TotalEnemiesKilledKey =
        "TotalEnemiesKilled";


    // ============================================================
    // KILLS
    // ============================================================

    [Header("Kills")]

    [SerializeField]
    private int enemiesKilledThisMatch = 0;

    [SerializeField]
    private int totalEnemiesKilled = 0;


    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    [SerializeField]
    private TMP_Text thisMatchKillsText;


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


        // --------------------------------------------------------
        // KEEP ALIVE BETWEEN SCENES
        // --------------------------------------------------------

        DontDestroyOnLoad(gameObject);


        // --------------------------------------------------------
        // LOAD TOTAL KILLS
        // --------------------------------------------------------

        LoadTotalKills();


        // --------------------------------------------------------
        // UPDATE UI
        // --------------------------------------------------------

        UpdateKillText();
    }


    private void OnEnable()
    {
        HealthManager.OnDie += HandleUnitDied;
    }


    private void OnDisable()
    {
        HealthManager.OnDie -= HandleUnitDied;
    }


    // ============================================================
    // UNIT DIED
    // ============================================================

    private void HandleUnitDied(
        HealthManager healthManager
    )
    {
        if (healthManager == null)
        {
            return;
        }


        // --------------------------------------------------------
        // IGNORE PLAYER DEATH
        // --------------------------------------------------------

        if (healthManager.IsPlayerCharacter())
        {
            return;
        }


        // --------------------------------------------------------
        // THIS MATCH
        // --------------------------------------------------------

        enemiesKilledThisMatch++;


        // --------------------------------------------------------
        // TOTAL
        // --------------------------------------------------------

        totalEnemiesKilled++;


        // --------------------------------------------------------
        // SAVE TOTAL
        // --------------------------------------------------------

        SaveTotalKills();


        // --------------------------------------------------------
        // UPDATE UI
        // --------------------------------------------------------

        UpdateKillText();


        // --------------------------------------------------------
        // DEBUG
        // --------------------------------------------------------

        Debug.Log(
            "[TalleyManager] Enemy killed. " +
            "This Match: " +
            enemiesKilledThisMatch +
            " | Total: " +
            totalEnemiesKilled,
            this
        );
    }


    // ============================================================
    // START NEW MATCH
    // ============================================================

    public void StartNewMatch()
    {
        enemiesKilledThisMatch = 0;


        // --------------------------------------------------------
        // UPDATE UI
        // --------------------------------------------------------

        UpdateKillText();


        Debug.Log(
            "[TalleyManager] New match started. " +
            "This Match kills reset to 0. " +
            "Total kills remain at " +
            totalEnemiesKilled +
            ".",
            this
        );
    }


    // ============================================================
    // UPDATE KILL TEXT
    // ============================================================

    private void UpdateKillText()
    {
        if (thisMatchKillsText == null)
        {
            return;
        }


        thisMatchKillsText.text =
            "Enemies Killed: " + enemiesKilledThisMatch;
    }


    // ============================================================
    // SAVE TOTAL
    // ============================================================

    private void SaveTotalKills()
    {
        PlayerPrefs.SetInt(
            TotalEnemiesKilledKey,
            totalEnemiesKilled
        );


        PlayerPrefs.Save();
    }


    // ============================================================
    // LOAD TOTAL
    // ============================================================

    private void LoadTotalKills()
    {
        totalEnemiesKilled =
            PlayerPrefs.GetInt(
                TotalEnemiesKilledKey,
                0
            );
    }


    // ============================================================
    // GETTERS
    // ============================================================

    public int GetEnemiesKilledThisMatch()
    {
        return enemiesKilledThisMatch;
    }


    public int GetTotalEnemiesKilled()
    {
        return totalEnemiesKilled;
    }


    // ============================================================
    // RESET TOTAL
    // ============================================================

    public void ResetTotalKills()
    {
        totalEnemiesKilled = 0;


        SaveTotalKills();


        Debug.Log(
            "[TalleyManager] Total enemy kills reset.",
            this
        );
    }


    // ============================================================
    // RESET EVERYTHING
    // ============================================================

    public void ResetAllKills()
    {
        enemiesKilledThisMatch = 0;
        totalEnemiesKilled = 0;


        SaveTotalKills();


        UpdateKillText();


        Debug.Log(
            "[TalleyManager] All kill statistics reset.",
            this
        );
    }
}
