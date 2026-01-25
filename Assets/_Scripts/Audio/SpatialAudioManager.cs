using UnityEngine;

/// <summary>
/// ✅ Quản lý âm thanh 3D với attenuation theo khoảng cách
/// - Dùng cho Dash, Shoot, Explosion của player khác
/// - Volume tự động giảm dần khi cách xa
/// </summary>
public class SpatialAudioManager : MonoBehaviour
{
    [Header("Spatial Audio Settings")]
    [SerializeField] private float maxDistance = 50f;        // Khoảng cách tối đa nghe thấy
    [SerializeField] private float minDistance = 1f;         // Khoảng cách tối thiểu (nghe rõ 100%)
    [SerializeField] private AttenuationMode attenuationMode = AttenuationMode.InverseSquare;
    [SerializeField] private float baseVolume = 1f;          // Volume base (0-1)

    public enum AttenuationMode
    {
        Linear,           // Giảm tuyến tính
        InverseSquare,    // Giảm theo bình phương (tự nhiên hơn)
        Logarithmic       // Giảm theo logarit
    }

    /// <summary>
    /// ✅ Tính toán volume dựa vào khoảng cách
    /// </summary>
    public static float CalculateVolumeByDistance(
        Vector3 soundSourcePosition,      // Vị trí phát âm thanh
        Vector3 listenerPosition,         // Vị trí người nghe (player mình)
        float maxDistance,
        float minDistance,
        AttenuationMode mode
    )
    {
        float distance = Vector3.Distance(soundSourcePosition, listenerPosition);

        // Nếu quá gần, phát 100%
        if (distance <= minDistance)
            return 1f;

        // Nếu quá xa, không nghe thấy
        if (distance >= maxDistance)
            return 0f;

        // Tính volume dựa vào mode
        float normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);

        switch (mode)
        {
            case AttenuationMode.Linear:
                // Giảm tuyến tính từ 1 → 0
                return 1f - normalizedDistance;

            case AttenuationMode.InverseSquare:
                // Inverse Square Law (tự nhiên nhất)
                // Volume = 1 / (1 + distance²)
                float ratio = distance / maxDistance;
                return 1f / (1f + ratio * ratio);

            case AttenuationMode.Logarithmic:
                // Logarithmic falloff
                return Mathf.Log(maxDistance / Mathf.Max(distance, 0.1f)) / Mathf.Log(maxDistance / minDistance);

            default:
                return 1f - normalizedDistance;
        }
    }

    /// <summary>
    /// ✅ Phát SFX với volume dựa vào khoảng cách
    /// </summary>
    public static void PlaySpatialSFX(
        AudioClip clip,
        Vector3 soundSourcePosition,
        Vector3 listenerPosition,
        float maxDistance,
        float minDistance,
        AttenuationMode mode,
        float baseVolume = 1f
    )
    {
        if (clip == null)
        {
            Debug.LogWarning("[SpatialAudio] AudioClip is null!");
            return;
        }

        // Tính volume
        float volume = CalculateVolumeByDistance(
            soundSourcePosition,
            listenerPosition,
            maxDistance,
            minDistance,
            mode
        );

        // Scale bằng base volume
        volume *= baseVolume;

        // Nếu volume quá nhỏ, không phát
        if (volume < 0.01f)
        {
            Debug.Log($"[SpatialAudio] Volume quá nhỏ ({volume:F2}), bỏ qua phát âm thanh");
            return;
        }

        // Phát âm thanh
        AudioSource tempAudio = new GameObject("TempAudioSource").AddComponent<AudioSource>();
        tempAudio.transform.position = soundSourcePosition;
        tempAudio.clip = clip;
        tempAudio.volume = volume;
        tempAudio.spatialBlend = 1f;  // 100% 3D sound
        tempAudio.Play();

        // Xóa sau khi phát xong
        Destroy(tempAudio.gameObject, clip.length);

        Debug.Log($"[SpatialAudio] ✅ Phát SFX tại {soundSourcePosition}, volume: {volume:F2}");
    }
}