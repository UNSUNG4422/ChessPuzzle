using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;
    [SerializeField] private float bgmVolume = 0.5f;
    [SerializeField] private float seVolume = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null)
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || seSource == null)
        {
            return;
        }

        seSource.PlayOneShot(clip, seVolume * Mathf.Max(0f, volumeScale));
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetSEVolume(float volume)
    {
        seVolume = Mathf.Clamp01(volume);

        if (seSource != null)
        {
            seSource.volume = seVolume;
        }
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
        }

        if (seSource != null)
        {
            seSource.volume = seVolume;
        }
    }
}
