using Fusion;
using UnityEngine;

public class BulletController : BaseBullet
{
    [Header("Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 3f;


    [Networked] private TickTimer lifeTimer { get; set; }

    // Biến local để xử lý hình ảnh phía Client (Client Prediction)
    private bool _isHit = false;    

    public override void Spawned()
    {
        // Server đặt giờ tự hủy
        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }

        // Reset trạng thái hình ảnh mỗi khi đạn được sinh ra (hoặc lấy từ Pool)
        _isHit = false;
        GetComponent<SpriteRenderer>().enabled = true;
    }

    public override void FixedUpdateNetwork()
    {
        // Nếu đã trúng (về mặt hình ảnh) thì dừng di chuyển ngay
        if (_isHit) return;

        // Di chuyển đạn
        transform.position += transform.up * speed * Runner.DeltaTime;

        // Server kiểm tra hết thời gian sống -> Hủy
        if (Object.HasStateAuthority)
        {
            if (lifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }

    // --- PHẦN QUAN TRỌNG: XỬ LÝ VA CHẠM ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Nếu đã trúng cái gì trước đó rồi thì bỏ qua (tránh trừ máu 2 lần)
        if (_isHit) return;

        // Nếu bắn trúng người chơi, kiểm tra xem có phải là chính mình không
        if (other.CompareTag("Player"))
        {
            // Gọi hàm IsOwnerHit từ BaseBullet
            if (IsOwnerHit(other.gameObject)) return;
        }

        // Kiểm tra xem đụng trúng Tường hoặc Người
        if (other.CompareTag("Wall") || other.CompareTag("Player") || other.CompareTag("Rock"))
        {

            _isHit = true;
            GetComponent<SpriteRenderer>().enabled = false;

            if (Object.HasStateAuthority)
            {
                // 1. GỌI HÀM NỔ CỦA CHA
                SpawnExplosion(transform.position);

                // 2. GỌI HÀM GÂY DAMAGE CỦA CHA
                if (other.CompareTag("Player"))
                {
                    DealDamage(other.gameObject);
                }

                // 3. Tự hủy
                Runner.Despawn(Object);
            }
        }
    }
}