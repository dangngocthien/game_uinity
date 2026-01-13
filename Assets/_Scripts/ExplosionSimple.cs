using Fusion;
using UnityEngine;

public class ExplosionSimple : NetworkBehaviour
{
    [Networked] private TickTimer lifeTimer { get; set; }

    // Thời gian tồn tại của vụ nổ (bạn canh theo thời gian animation, vd: 0.5s)
    [SerializeField] private float lifeTime = 0.5f;

    public override void Spawned()
    {
        // Khi vừa sinh ra, Server đặt giờ hẹn tử
        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayExplosion();
            Debug.Log("phat am thanh");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Hết giờ -> Hủy object
        if (lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }
}