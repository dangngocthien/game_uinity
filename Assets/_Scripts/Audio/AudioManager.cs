using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;        // Nhạc nền
    [SerializeField] private AudioSource sfxSource;          // Tiếng động (Bắn, nổ...)

    [Header("Spatial Audio Settings")]
    [SerializeField] private float spatialMaxDistance = 50f;
    [SerializeField] private float spatialMinDistance = 1f;
    [SerializeField]
    private SpatialAudioManager.AttenuationMode attenuationMode =
        SpatialAudioManager.AttenuationMode.InverseSquare;
    [SerializeField] private float baseSFXVolume = 0.8f;

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
        LoadAudioSettings();
        //PlayMusic(backgroundClip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource == null) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // ✅ THÊM: Phát SFX với spatial audio (dùng cho player khác)
    public void PlaySpatialShoot(Vector3 shooterPosition, Vector3 listenerPosition)
    {
        PlaySpatialSFX(shootClip, shooterPosition, listenerPosition);
    }

    public void PlaySpatialExplosion(Vector3 explosionPosition, Vector3 listenerPosition)
    {
        PlaySpatialSFX(explosionClip, explosionPosition, listenerPosition);
    }

    public void PlaySpatialDash(Vector3 dashPosition, Vector3 listenerPosition)
    {
        PlaySpatialSFX(dash, dashPosition, listenerPosition);
    }

    public void PlaySpatialOpenChest(Vector3 chestPosition, Vector3 listenerPosition)
    {
        PlaySpatialSFX(openChest, chestPosition, listenerPosition);
    }

    /// <summary>
    /// ✅ Internal: Phát spatial SFX chung
    /// </summary>
    private void PlaySpatialSFX(AudioClip clip, Vector3 soundSourcePos, Vector3 listenerPos)
    {
        SpatialAudioManager.PlaySpatialSFX(
            clip,
            soundSourcePos,
            listenerPos,
            spatialMaxDistance,
            spatialMinDistance,
            attenuationMode,
            baseSFXVolume
        );
    }

    // Hàm phát tiếng động cũ (cho âm thanh local - 100% volume)
    public void PlayShoot() => PlaySFX(shootClip);
    public void PlayExplosion() => PlaySFX(explosionClip);
    public void PlayOpenChest() => PlaySFX(openChest);
    public void PlayDash() => PlaySFX(dash);

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void StopAudio()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // --- METHODS ĐỂ ĐIỀU CHỈNH ÂM LƯỢNG ---

    public void SetMusicVolume(float volume)
    {
        if (musicSource == null) return;
        volume = Mathf.Clamp01(volume);
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("Audio_Music", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource == null) return;
        volume = Mathf.Clamp01(volume);
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("Audio_SFX", volume);
        PlayerPrefs.Save();
    }

    public void LoadAudioSettings()
    {
        float musicVolume = PlayerPrefs.GetFloat("Audio_Music", 0.6f);
        float sfxVolume = PlayerPrefs.GetFloat("Audio_SFX", 0.8f);

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public float GetMusicVolume()
    {
        return musicSource != null ? musicSource.volume : 0f;
    }

    public float GetSFXVolume()
    {
        return sfxSource != null ? sfxSource.volume : 0f;
    }
}