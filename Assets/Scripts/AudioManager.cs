using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip basicTheme;
    public AudioClip devilTheme;
    public AudioClip towerTheme;
    public AudioClip sfxCardClick;
    public AudioClip sfxAttackHit;
    public AudioClip sfxDefeat;
    public AudioClip sfxDoorTransition;
    public static AudioManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        if (musicSource.clip == clip)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource == null)
            return;

        musicSource.volume = volume;
    }

    public void SetMasterVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;
        if (sfxSource != null)
            sfxSource.volume = volume;
    }

    public bool IsMusicPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
    }
}
