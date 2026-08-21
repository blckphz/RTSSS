using UnityEngine;
using Unity.Cinemachine;
using Diagnostics = System.Diagnostics;

public class ScreenShaker : MonoBehaviour
{
    public static ScreenShaker Instance { get; private set; }


    // =====================================================
    // CINEMACHINE
    // =====================================================

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
        // GET CINEMACHINE CAMERA
        // -------------------------------------------------

        CinemachineCamera cam =
            GetComponent<CinemachineCamera>();


        if (cam != null)
        {
            perlin =
                cam.GetComponent<
                    CinemachineBasicMultiChannelPerlin>();
        }


        // -------------------------------------------------
        // VALIDATION
        // -------------------------------------------------

        if (perlin == null)
        {
            Debug.LogError(
                "[ScreenShaker] " +
                "No CinemachineBasicMultiChannelPerlin " +
                "found on this CinemachineCamera!",
                this
            );
        }
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
                Time.deltaTime;


            float t =
                shakeTimer /
                shakeTimerTotal;


            perlin.AmplitudeGain =
                Mathf.Lerp(
                    0f,
                    startingIntensity,
                    t
                );
        }
        else
        {
            perlin.AmplitudeGain =
                0f;
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
                "[ScreenShaker] " +
                "Shake requested but no " +
                "CinemachineBasicMultiChannelPerlin " +
                "component was found."
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
                "[ScreenShaker] SHAKE REQUESTED " +
                $"| Intensity: {intensity} " +
                $"| Duration: {duration} " +
                $"| From: {caller}"
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
                "[ScreenShaker] " +
                "Instance not found!"
            );

            return;
        }


        Instance.Shake(
            intensity,
            duration
        );
    }
}