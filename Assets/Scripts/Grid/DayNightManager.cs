
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
    private Color nightColor = new Color(0.25f, 0.3f, 0.5f);

    [SerializeField]
    [Range(0f, 1f)]
    private float nightIntensity = 0.35f;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (globalLight == null)
        {
            globalLight = FindFirstObjectByType<Light2D>();
        }

        ApplyCurrentLighting();
    }

    // ============================================================
    // SET DAY / NIGHT
    // ============================================================

    public void SetDayNight(DayNight dayNight)
    {
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
        currentDayNight = DayNight.Day;
        ApplyCurrentLighting();
    }

    // ============================================================
    // NIGHT
    // ============================================================

    public void SetNight()
    {
        currentDayNight = DayNight.Night;
        ApplyCurrentLighting();
    }

    // ============================================================
    // APPLY LIGHTING
    // ============================================================

    private void ApplyCurrentLighting()
    {
        if (globalLight == null)
            return;

        if (currentDayNight == DayNight.Day)
        {
            globalLight.color = dayColor;
            globalLight.intensity = dayIntensity;
        }
        else
        {
            globalLight.color = nightColor;
            globalLight.intensity = nightIntensity;
        }
    }

    // ============================================================
    // STATE
    // ============================================================

    public DayNight GetCurrentDayNight()
    {
        return currentDayNight;
    }

    public bool IsDay()
    {
        return currentDayNight == DayNight.Day;
    }

    public bool IsNight()
    {
        return currentDayNight == DayNight.Night;
    }
}
