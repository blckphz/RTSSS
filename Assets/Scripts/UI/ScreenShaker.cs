using UnityEngine;
using Unity.Cinemachine;
using Diagnostics = System.Diagnostics;

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
        // FIND CAMERA
        // -------------------------------------------------

        if (cinemachineCamera == null)
        {
            cinemachineCamera =
                GetComponent<CinemachineCamera>();
        }


        if (cinemachineCamera == null)
        {
            cinemachineCamera =
                FindFirstObjectByType<CinemachineCamera>();
        }


        // -------------------------------------------------
        // FIND PERLIN
        // -------------------------------------------------

        if (cinemachineCamera != null)
        {
            perlin =
                cinemachineCamera.GetComponent<
                    CinemachineBasicMultiChannelPerlin>();
        }


        // -------------------------------------------------
        // VALIDATION
        // -------------------------------------------------

        if (cinemachineCamera == null)
        {
            Debug.LogError(
                "[ScreenShaker] Could not find a CinemachineCamera!"
            );

            return;
        }


        if (perlin == null)
        {
            Debug.LogError(
                "[ScreenShaker] CinemachineCamera was found, " +
                "but it does NOT have a " +
                "CinemachineBasicMultiChannelPerlin component!",
                cinemachineCamera
            );

            return;
        }


        Debug.Log(
            "[ScreenShaker] Successfully connected to Cinemachine noise.",
            this
        );


        // Make sure it starts with no shake.
        perlin.AmplitudeGain = 0f;
        perlin.FrequencyGain = 0f;
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (perlin == null)
        {
            return;
        }


        if (shakeTimer > 0f)
        {
            shakeTimer -=
                Time.unscaledDeltaTime;


            float t =
                shakeTimer /
                shakeTimerTotal;


            // Smooth fade out.
            float intensity =
                Mathf.Lerp(
                    0f,
                    startingIntensity,
                    t
                );


            perlin.AmplitudeGain =
                intensity;

            perlin.FrequencyGain =
                1f;
        }
        else
        {
            perlin.AmplitudeGain = 0f;
            perlin.FrequencyGain = 0f;
        }
    }


    // =====================================================
    // SHAKE
    // =====================================================

    public void Shake(
        float intensity,
        float duration = 0.15f)
    {
        if (perlin == null)
        {
            Debug.LogWarning(
                "[ScreenShaker] Shake requested, " +
                "but Perlin noise is not connected!"
            );

            return;
        }


        // -------------------------------------------------
        // DEBUG
        // -------------------------------------------------

        if (debugShakeCalls)
        {
            Diagnostics.StackTrace trace =
                new Diagnostics.StackTrace();


            string caller =
                "Unknown Caller";


            if (trace.FrameCount > 1)
            {
                var frame =
                    trace.GetFrame(1);


                if (frame != null)
                {
                    var method =
                        frame.GetMethod();


                    if (method != null)
                    {
                        caller =
                            $"{method.DeclaringType.Name}." +
                            $"{method.Name}";
                    }
                }
            }


            Debug.Log(
                $"[ScreenShaker] Shake called by {caller} | " +
                $"Intensity: {intensity} | " +
                $"Duration: {duration}",
                this
            );
        }


        // -------------------------------------------------
        // PROTECT AGAINST INVALID VALUES
        // -------------------------------------------------

        intensity =
            Mathf.Max(
                0f,
                intensity
            );


        duration =
            Mathf.Max(
                0.01f,
                duration
            );


        // -------------------------------------------------
        // APPLY SHAKE
        // -------------------------------------------------

        startingIntensity =
            intensity;


        shakeTimerTotal =
            duration;


        shakeTimer =
            duration;


        perlin.AmplitudeGain =
            intensity;


        perlin.FrequencyGain =
            1f;
    }


    // =====================================================
    // STATIC SHAKE
    // =====================================================

    public static void ShakeScreen(
        float intensity,
        float duration = 0.15f)
    {
        if (Instance == null)
        {
            Debug.LogWarning(
                "[ScreenShaker] Instance not found!"
            );

            return;
        }


        Instance.Shake(
            intensity,
            duration
        );
    }
}