using Fusion;
using UnityEngine;

// 1. Kế thừa từ BaseBullet thay vì NetworkBehaviour
public class BouncingBullet : BaseBullet
{
    [Header("Bouncing Settings")]
    [SerializeField] private float initialSpeed = 15f;
    [SerializeField] private float speedMultiplier = 1.2f;
    [SerializeField] private int maxBounces = 3;
    // [Delete] private int damage = 10; -> Đã có ở BaseBullet

    [Header("Life Time")]
    [SerializeField] private float lifeTime = 5f;

    [Networked] private TickTimer lifeTimer { get; set; }
    private int _currentBounceCount = 0;
    private Rigidbody2D _rb;
    private bool _isHit = false;

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            _rb.velocity = transform.up * initialSpeed;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (lifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isHit) return;
        if (!Object.HasStateAuthority) return;

        ////if (collision.gameobject.comparetag("player"))
        ////{
        ////    if (isownerhit(collision.gameobject)) return;
        ////}

        if (collision.gameObject.CompareTag("Wall"))
        {
            HandleWallBounce();
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerHit(collision.gameObject);
        }
    }

    private void HandleWallBounce()
    {
        _currentBounceCount++;

        if (_currentBounceCount >= maxBounces)
        {
            // Nảy quá nhiều -> NỔ và HỦY
            Debug.Log("Đạn vỡ do nảy quá nhiều!");

            SpawnExplosion(transform.position); // <--- Gọi nổ
            Runner.Despawn(Object);
        }
        else
        {
            // Nảy tiếp -> Tăng tốc
            _rb.velocity = _rb.velocity * speedMultiplier;
        }
    }

    private void HandlePlayerHit(GameObject playerObj)
    {
        // Trừ máu (Dùng hàm của Cha)
        DealDamage(playerObj);

        // Trúng người -> NỔ và HỦY
        _isHit = true;
        SpawnExplosion(transform.position); // <--- Gọi nổ
        Runner.Despawn(Object);
    }
}