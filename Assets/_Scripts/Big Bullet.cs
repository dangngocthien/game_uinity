using Fusion;
using UnityEngine;

// 1. Kế thừa từ BaseBullet
public class BigBullet : BaseBullet
{
    [Header("Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;
    // [Delete] private int damage = 10; -> Đã có ở BaseBullet

    [Networked] private TickTimer lifeTimer { get; set; }
    private bool _isHit = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }
        _isHit = false;
        GetComponent<SpriteRenderer>().enabled = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (_isHit) return;

        // Di chuyển (BigBullet dùng Transform thay vì Rigidbody)
        transform.position += transform.up * speed * Runner.DeltaTime;

        if (Object.HasStateAuthority)
        {
            if (lifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isHit) return;

        // 1. Kiểm tra bắn trúng bản thân (Dùng hàm của Cha cho gọn)
        if (other.CompareTag("Player"))
        {
            if (IsOwnerHit(other.gameObject)) return;
        }

        if (other.CompareTag("Wall") || other.CompareTag("Player"))
        {
            _isHit = true;
            GetComponent<SpriteRenderer>().enabled = false;

            if (Object.HasStateAuthority)
            {
                // --- GỌI HIỆU ỨNG NỔ ---
                SpawnExplosion(transform.position);
                // -----------------------

                if (other.CompareTag("Player"))
                {
                    // --- GỌI HÀM TRỪ MÁU ---
                    DealDamage(other.gameObject);
                }

                Runner.Despawn(Object);
            }
        }
    }
}