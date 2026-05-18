using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    public AudioClip musicClip;

    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(musicClip);
    }
}
