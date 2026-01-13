using Fusion;
using UnityEngine;

public class BaseBullet : NetworkBehaviour
{
    [Header("Base Settings")]
    public int damage = 10; // Biến này dùng chung cho các con

    [Header("VFX")]
    [SerializeField] protected NetworkPrefabRef explosionAnimPrefab; // Prefab vụ nổ

    // Hàm sinh ra vụ nổ (Con gọi hàm này khi cần nổ)
    protected void SpawnExplosion(Vector3 position)
    {
        // Chỉ Server mới spawn và phải có prefab hợp lệ
        if (Object.HasStateAuthority && explosionAnimPrefab.IsValid)
        {
            Runner.Spawn(explosionAnimPrefab, position, Quaternion.identity, null);
        }
    }

    // Hàm gây sát thương (Con gọi hàm này khi trúng người)
    protected void DealDamage(GameObject target)
    {
        if (Object.HasStateAuthority)
        {
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
        }
    }

    // Hàm kiểm tra xem có bắn trúng chính mình không
    protected bool IsOwnerHit(GameObject hitObject)
    {
        var hitNetworkObject = hitObject.GetComponent<NetworkObject>();
        // Nếu đối tượng bị trúng có cùng InputAuthority với viên đạn -> Là chính mình
        if (hitNetworkObject != null && hitNetworkObject.InputAuthority == Object.InputAuthority)
        {
            return true;
        }
        return false;
    }
}