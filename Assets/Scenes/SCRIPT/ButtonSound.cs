using UnityEngine;

public class ButtonSound : MonoBehaviour
{
    public static ButtonSound Instance { get; private set; }

    [Header("音效设置")]
    public AudioClip defaultClickSound;  // 👈 默认音效（给其他4个按钮用）
    public AudioClip closeSound;         // 👈 关闭音效（给 CloseButton 专用）

    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.5f, 3f)]
    public float pitch = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.mute = false;
    }

    /// <summary>
    /// 播放默认点击音效（给其他4个按钮用）
    /// </summary>
    public void PlayClickSound()
    {
        PlaySound(defaultClickSound, "defaultClickSound");
    }

    /// <summary>
    /// 播放关闭音效（给 CloseButton 专用）
    /// </summary>
    public void PlayCloseSound()
    {
        PlaySound(closeSound, "closeSound");
    }

    /// <summary>
    /// 通用播放方法
    /// </summary>
    private void PlaySound(AudioClip clip, string soundName)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[ButtonSound] {soundName} 为空，请拖入音效文件");
            return;
        }

        if (audioSource == null)
            return;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip);
        Debug.Log($"[ButtonSound] 播放音效：{soundName} -> {clip.name}");
    }
}