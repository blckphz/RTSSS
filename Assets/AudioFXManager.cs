using UnityEngine;

public class AudioFXManager : MonoBehaviour
{
    public static AudioFXManager Instance { get; private set; }


    [Header("Audio Source")]
    [SerializeField]
    private AudioSource audioSource;


    // ============================================================
    // UNIT HOVER
    // ============================================================

    [Header("Unit Hover")]
    [SerializeField]
    private AudioClip unitHoverClip;

    [SerializeField, Range(0f, 1f)]
    private float unitHoverVolume = 1f;


    // ============================================================
    // UNIT CLICK
    // ============================================================

    [Header("Unit Click")]
    [SerializeField]
    private AudioClip unitClickClip;

    [SerializeField, Range(0f, 1f)]
    private float unitClickVolume = 1f;


    // ============================================================
    // ABILITY SELECT
    // ============================================================

    [Header("Ability Select")]
    [SerializeField]
    private AudioClip abilitySelectClip;

    [SerializeField, Range(0f, 1f)]
    private float abilitySelectVolume = 1f;


    // ============================================================
    // UNIT DESELECT
    // ============================================================

    [Header("Unit Deselect")]
    [SerializeField]
    private AudioClip unitDeselectClip;

    [SerializeField, Range(0f, 1f)]
    private float unitDeselectVolume = 1f;


    // ============================================================
    // UNIT DAMAGE
    // ============================================================

    [Header("Unit Damage")]
    [SerializeField]
    private AudioClip unitDamageClip;

    [SerializeField, Range(0f, 1f)]
    private float unitDamageVolume = 1f;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // SINGLETON
        // --------------------------------------------------------

        if (
            Instance != null &&
            Instance != this
        )
        {
            Destroy(gameObject);
            return;
        }


        Instance =
            this;


        // --------------------------------------------------------
        // AUDIO SOURCE
        // --------------------------------------------------------

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }


        if (audioSource == null)
        {
            audioSource =
                gameObject.AddComponent<AudioSource>();
        }


        audioSource.playOnAwake =
            false;
    }


    // ============================================================
    // UNIT HOVER
    // ============================================================

    public void PlayUnitHover()
    {
        if (unitHoverClip == null || audioSource == null)
        {
            return;
        }


        audioSource.PlayOneShot(
            unitHoverClip,
            unitHoverVolume
        );
    }


    // ============================================================
    // UNIT CLICK
    // ============================================================

    public void PlayUnitClick()
    {
        if (unitClickClip == null || audioSource == null)
        {
            return;
        }


        audioSource.PlayOneShot(
            unitClickClip,
            unitClickVolume
        );
    }


    // ============================================================
    // ABILITY SELECT
    // ============================================================

    public void PlayAbilitySelect()
    {
        if (abilitySelectClip == null || audioSource == null)
        {
            return;
        }


        audioSource.PlayOneShot(
            abilitySelectClip,
            abilitySelectVolume
        );
    }


    // ============================================================
    // UNIT DESELECT
    // ============================================================

    public void PlayUnitDeselect()
    {
        if (unitDeselectClip == null || audioSource == null)
        {
            return;
        }


        audioSource.PlayOneShot(
            unitDeselectClip,
            unitDeselectVolume
        );
    }


    // ============================================================
    // UNIT DAMAGE
    // ============================================================

    public void PlayUnitDamage()
    {
        if (unitDamageClip == null || audioSource == null)
        {
            return;
        }


        audioSource.PlayOneShot(
            unitDamageClip,
            unitDamageVolume
        );
    }
}