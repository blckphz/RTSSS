using UnityEngine;
using Unity.Cinemachine;

public class ScreenShaker : MonoBehaviour
{
    public static ScreenShaker Instance { get; private set; }

    // =====================================================
    // CINEMACHINE
    // =====================================================

    [Header("Cinemachine")]
    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    private CinemachineBasicMultiChannelPerlin perlin;

    // =====================================================
    // SHAKE STATE
    // =====================================================

    private float shakeTimer;
    private float shakeTimerTotal;
    private float startingIntensity;

    // =====================================================
    // DEBUG
    // =====================================================

    [Header("Debug")]
    [SerializeField]
    private bool debugShakeCalls = true;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        // -------------------------------------------------
        // SINGLETON
        // -------------------------------------------------
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // -------------------------------------------------
        // FIND CAMERA & PERLIN
        // -------------------------------------------------
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>() ?? FindFirstObjectByType<CinemachineCamera>();
        }

        if (cinemachineCamera != null)
        {
            perlin = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        // -------------------------------------------------
        // VALIDATION
        // -------------------------------------------------
        if (cinemachineCamera == null)
        {
            Debug.LogError("[ScreenShaker] Could not find a CinemachineCamera!");
            return;
        }

        if (perlin == null)
        {
            Debug.LogError(
                "[ScreenShaker] CinemachineCamera was found, but it does NOT have a CinemachineBasicMultiChannelPerlin component!",
                cinemachineCamera
            );
            return;
        }

        ResetPerlin();
    }

    private void Update()
    {
        if (perlin == null || shakeTimer <= 0f)
        {
            return;
        }

        shakeTimer -= Time.unscaledDeltaTime;

        if (shakeTimer > 0f)
        {
            float t = shakeTimer / shakeTimerTotal;
            perlin.AmplitudeGain = Mathf.Lerp(0f, startingIntensity, t);
            perlin.FrequencyGain = 1f;
        }
        else
        {
            ResetPerlin();
        }
    }

    // =====================================================
    // SHAKE
    // =====================================================

    public void Shake(float intensity, float duration = 0.15f)
    {
        if (perlin == null)
        {
            Debug.LogWarning("[ScreenShaker] Shake requested, but Perlin noise is not connected!");
            return;
        }

        // -------------------------------------------------
        // PROTECT AGAINST INVALID VALUES
        // -------------------------------------------------
        startingIntensity = Mathf.Max(0f, intensity);
        shakeTimerTotal = Mathf.Max(0.01f, duration);
        shakeTimer = shakeTimerTotal;

        perlin.AmplitudeGain = startingIntensity;
        perlin.FrequencyGain = 1f;
    }

    // =====================================================
    // STATIC SHAKE
    // =====================================================

    public static void ShakeScreen(float intensity, float duration = 0.15f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[ScreenShaker] Instance not found!");
            return;
        }

        Instance.Shake(intensity, duration);
    }

    // =====================================================
    // HELPER METHODS
    // =====================================================

    private void ResetPerlin()
    {
        if (perlin != null)
        {
            perlin.AmplitudeGain = 0f;
            perlin.FrequencyGain = 0f;
        }
    }
}