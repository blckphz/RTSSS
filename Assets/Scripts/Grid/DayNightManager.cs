using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightManager : MonoBehaviour
{
    [Header("Global Light")]
    [SerializeField]
    private Light2D globalLight;

    [Header("Current State")]
    [SerializeField]
    private DayNight currentDayNight = DayNight.Day;

    [Header("Day Settings")]
    [SerializeField]
    private Color dayColor = Color.white;

    [SerializeField]
    [Range(0f, 1f)]
    private float dayIntensity = 1f;

    [Header("Night Settings")]
    [SerializeField]
    private Color nightColor =
        new Color(0.25f, 0.3f, 0.5f);

    [SerializeField]
    [Range(0f, 1f)]
    private float nightIntensity = 0.35f;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        Debug.Log(
            "[DayNightManager] AWAKE",
            this
        );

        if (globalLight == null)
        {
            globalLight =
                FindFirstObjectByType<Light2D>();
        }

        Debug.Log(
            "[DayNightManager] Global Light = " +
            (globalLight != null
                ? globalLight.name
                : "NULL"),
            this
        );

        Debug.Log(
            "[DayNightManager] Initial state = " +
            currentDayNight,
            this
        );
    }


    // ============================================================
    // SET DAY / NIGHT
    // ============================================================

    public void SetDayNight(
        DayNight dayNight
    )
    {
        Debug.Log(
            "[DayNightManager] SetDayNight called: " +
            dayNight,
            this
        );

        if (dayNight == DayNight.Day)
        {
            SetDay();
        }
        else
        {
            SetNight();
        }
    }


    // ============================================================
    // DAY
    // ============================================================

    public void SetDay()
    {
        currentDayNight =
            DayNight.Day;

        Debug.Log(
            "[DayNightManager] =====================",
            this
        );

        Debug.Log(
            "[DayNightManager] SETTING DAY",
            this
        );

        Debug.Log(
            "[DayNightManager] Current state = " +
            currentDayNight,
            this
        );

        if (globalLight == null)
        {
            Debug.LogWarning(
                "[DayNightManager] Cannot set light because globalLight is NULL.",
                this
            );

            return;
        }

        globalLight.color =
            dayColor;

        globalLight.intensity =
            dayIntensity;

        Debug.Log(
            "[DayNightManager] Day light applied.",
            this
        );
    }


    // ============================================================
    // NIGHT
    // ============================================================

    public void SetNight()
    {
        currentDayNight =
            DayNight.Night;

        Debug.Log(
            "[DayNightManager] =====================",
            this
        );

        Debug.Log(
            "[DayNightManager] SETTING NIGHT",
            this
        );

        Debug.Log(
            "[DayNightManager] Current state = " +
            currentDayNight,
            this
        );

        if (globalLight == null)
        {
            Debug.LogWarning(
                "[DayNightManager] Cannot set light because globalLight is NULL.",
                this
            );

            return;
        }

        globalLight.color =
            nightColor;

        globalLight.intensity =
            nightIntensity;

        Debug.Log(
            "[DayNightManager] Night light applied.",
            this
        );
    }


    // ============================================================
    // STATE
    // ============================================================

    public DayNight GetCurrentDayNight()
    {
        Debug.Log(
            "[DayNightManager] GetCurrentDayNight = " +
            currentDayNight,
            this
        );

        return currentDayNight;
    }


    public bool IsDay()
    {
        bool result =
            currentDayNight == DayNight.Day;

        Debug.Log(
            "[DayNightManager] IsDay = " +
            result +
            " | CurrentState = " +
            currentDayNight,
            this
        );

        return result;
    }


    public bool IsNight()
    {
        bool result =
            currentDayNight == DayNight.Night;

        Debug.Log(
            "[DayNightManager] IsNight = " +
            result +
            " | CurrentState = " +
            currentDayNight,
            this
        );

        return result;
    }
}