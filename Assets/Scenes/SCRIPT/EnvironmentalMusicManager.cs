using UnityEngine;
using System.Collections;

public class EnvironmentMusicManager : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource bgmSource;

    [Header("Music Clips")]
    public AudioClip americaMusic;
    public AudioClip japanMusic;

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;
    public float musicVolume = 0.3f;

    private Coroutine fadeCoroutine;

    void Start()
    {
        // Try to find AudioSource on same object
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }

        // If not found, try to find AudioSource from child object
        if (bgmSource == null)
        {
            bgmSource = GetComponentInChildren<AudioSource>();
        }

        // If still not found, stop to prevent error
        if (bgmSource == null)
        {
            Debug.LogError("No AudioSource found. Please add an AudioSource to AudioManager or drag it into Bgm Source.");
            return;
        }

        PlayJapanMusic();
    }

    public void PlayAmericaMusic()
    {
        ChangeMusic(americaMusic);
    }

    public void PlayJapanMusic()
    {
        ChangeMusic(japanMusic);
    }

    private void ChangeMusic(AudioClip newClip)
    {
        if (bgmSource == null)
        {
            Debug.LogError("Bgm Source is missing.");
            return;
        }

        if (newClip == null)
        {
            Debug.LogWarning("Music clip is missing.");
            return;
        }

        if (bgmSource.clip == newClip)
        {
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeToNewMusic(newClip));
    }

    private IEnumerator FadeToNewMusic(AudioClip newClip)
    {
        float startVolume = bgmSource.volume;

        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();

        while (bgmSource.volume < musicVolume)
        {
            bgmSource.volume += musicVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        bgmSource.volume = musicVolume;
    }
}