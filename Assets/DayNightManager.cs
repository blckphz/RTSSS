using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightManager : MonoBehaviour
{
    [Header("Global Light")]
    [SerializeField]
    private Light2D globalLight;

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


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (globalLight == null)
        {
            globalLight = FindFirstObjectByType<Light2D>();
        }
    }


    // =========================================================
    // SET DAY / NIGHT
    // =========================================================

    public void SetDayNight(DayNight dayNight)
    {
        if (globalLight == null)
        {
            Debug.LogWarning(
                "[DayNightManager] Global Light 2D is not assigned.",
                this
            );

            return;
        }

        if (dayNight == DayNight.Day)
        {
            SetDay();
        }
        else
        {
            SetNight();
        }
    }


    // =========================================================
    // DAY
    // =========================================================

    public void SetDay()
    {
        if (globalLight == null)
        {
            return;
        }

        globalLight.color = dayColor;
        globalLight.intensity = dayIntensity;

        Debug.Log(
            "[DayNightManager] Set to DAY.",
            this
        );
    }


    // =========================================================
    // NIGHT
    // =========================================================

    public void SetNight()
    {
        if (globalLight == null)
        {
            return;
        }

        globalLight.color = nightColor;
        globalLight.intensity = nightIntensity;

        Debug.Log(
            "[DayNightManager] Set to NIGHT.",
            this
        );
    }
}