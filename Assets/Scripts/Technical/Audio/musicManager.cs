using UnityEngine;

public class musicManager : MonoBehaviour
{
    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField]
    private AudioSource musicSource;


    // ============================================================
    // MUSIC
    // ============================================================

    [Header("Music")]

    [Tooltip(
        "Music that plays while the player is on the map."
    )]
    [SerializeField]
    private AudioClip mapMusic;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource =
                GetComponent<AudioSource>();
        }


        if (musicSource == null)
        {
            Debug.LogError(
                "[MusicManager] No AudioSource found.",
                this
            );

            return;
        }


        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }


    // ============================================================
    // PLAY MAP MUSIC
    // ============================================================

    public void PlayMapMusic()
    {
        if (musicSource == null)
        {
            Debug.LogWarning(
                "[MusicManager] AudioSource missing.",
                this
            );

            return;
        }


        if (mapMusic == null)
        {
            Debug.LogWarning(
                "[MusicManager] Map music is not assigned.",
                this
            );

            return;
        }


        PlayMusic(mapMusic);
    }


    // ============================================================
    // PLAY MUSIC
    // ============================================================

    public void PlayMusic(
        AudioClip clip
    )
    {
        if (musicSource == null)
        {
            Debug.LogWarning(
                "[MusicManager] AudioSource missing.",
                this
            );

            return;
        }


        if (clip == null)
        {
            Debug.LogWarning(
                "[MusicManager] No music clip assigned.",
                this
            );

            return;
        }


        if (
            musicSource.clip == clip &&
            musicSource.isPlaying
        )
        {
            return;
        }


        musicSource.Stop();

        musicSource.clip = clip;
        musicSource.loop = true;

        musicSource.Play();


    }


    // ============================================================
    // STOP MUSIC
    // ============================================================

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }


        musicSource.Stop();

        musicSource.clip = null;


        Debug.Log(
            "[MusicManager] Music stopped.",
            this
        );
    }


    // ============================================================
    // RETURN TO MAP MUSIC
    // ============================================================

    public void ReturnToMapMusic()
    {
        PlayMapMusic();
    }


    // ============================================================
    // IS PLAYING
    // ============================================================

    public bool IsPlaying()
    {
        if (musicSource == null)
        {
            return false;
        }


        return musicSource.isPlaying;
    }


    // ============================================================
    // CURRENT CLIP
    // ============================================================

    public AudioClip GetCurrentMusic()
    {
        if (musicSource == null)
        {
            return null;
        }


        return musicSource.clip;
    }
}