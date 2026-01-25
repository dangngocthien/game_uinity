using Fusion;
using UnityEngine;

/// <summary>
/// ✅ Spawn vụ nổ khi player chết
/// Đảm bảo tất cả player thấy (Network sync)
/// </summary>
public class DeathExplosionSpawner : MonoBehaviour
{
    [Header("Explosion Prefab")]
    [SerializeField] private NetworkPrefabRef explosionPrefab;  // Prefab vụ nổ
    [SerializeField] private float spawnOffsetY = 0.5f;         // Offset Y so với player

    private NetworkRunner _runner;

    private void Awake()
    {
        _runner = FindObjectOfType<NetworkRunner>();
    }

    /// <summary>
    /// ✅ Spawn vụ nổ tại vị trí player
    /// Gọi từ PlayerController.RPC_SpawnDeathExplosion()
    /// </summary>
    public void SpawnDeathExplosion(Vector3 spawnPosition)
    {
        // Safety check: Tìm runner nếu chưa được gán
        if (_runner == null)
        {
            _runner = FindObjectOfType<NetworkRunner>();
        }

        if (_runner == null)
        {
            Debug.LogError("[DeathExplosion] ❌ Không tìm thấy NetworkRunner!");
            return;
        }

        // Kiểm tra explosion prefab
        if (explosionPrefab.Equals(default(NetworkPrefabRef)))
        {
            Debug.LogError("[DeathExplosion] ❌ Explosion Prefab chưa được gán!");
            return;
        }

        // Offset vị trí spawn
        Vector3 explosionPos = spawnPosition + Vector3.up * spawnOffsetY;

        // ✅ QUAN TRỌNG: Dùng Runner.Spawn để sync network
        // Tất cả player sẽ thấy vụ nổ này
        NetworkObject spawnedExplosion = _runner.Spawn(
            explosionPrefab,                          // Prefab cần spawn
            explosionPos,                             // Vị trí spawn
            Quaternion.identity,                      // Rotation (không xoay)
            inputAuthority: default                   // Không cần authority
        );

        Debug.Log($"[DeathExplosion] ✅ Vụ nổ spawned tại {explosionPos}");

        // Optional: Auto-despawn explosion sau khi animation chạy xong
        if (spawnedExplosion != null)
        {
            // Nếu explosion có ParticleSystem, chúng ta sẽ despawn sau khi hết duration
            ParticleSystem particleSystem = spawnedExplosion.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                // Despawn sau duration của particle (+ 0.5s buffer)
                float despawnTime = particleSystem.main.duration + 0.5f;
                Destroy(spawnedExplosion.gameObject, despawnTime);
            }
            else
            {
                // Nếu không có ParticleSystem, despawn sau 1 giây
                Destroy(spawnedExplosion.gameObject, 1f);
            }
        }
    }
}