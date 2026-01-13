using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;// Nhạc nền
    [SerializeField] private AudioSource sfxSource;// Tiếng động (Bắn, nổ...)

    [Header("Clips")]
    public AudioClip backgroundClip;
    public AudioClip shootClip;
    public AudioClip explosionClip;
    public AudioClip openChest;
    public AudioClip dash;

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
        }
    }

    private void Start()
    {
        PlayMusic(backgroundClip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // Hàm phát tiếng động (Sẽ được gọi bởi các script khác)
    public void PlayShoot() => PlaySFX(shootClip);
    public void PlayExplosion() => PlaySFX(explosionClip);

    public void PlayOpenChest() => PlaySFX(openChest);
    public void PlayDash() => PlaySFX(dash);

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }

    }

    public void StopAudio()
    {
        musicSource.Stop();
    }
}
