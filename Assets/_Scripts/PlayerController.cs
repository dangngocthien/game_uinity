using Cinemachine;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Sprite[] _avatarSprites;
    [SerializeField] private TMP_Text nameText;

    [Networked] public NetworkString<_16> NickName { get; set; }
    // Biến để kiểm tra thay đổi (cho hàm Render)
    private string _oldName;
    [Networked] public int NetworkedSpriteIndex { get; set; }
    private int _lastVisibleIndex = -1; 

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Shooting")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private Transform firePoint;

    [Header("Ammo & Reload")]
    public int MaxAmmo = 20;
    [SerializeField] private float reloadTime = 3f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;     // Tốc độ lướt (nhanh hơn speed thường)
    [SerializeField] private float dashDuration = 0.2f; // Thời gian lướt (ngắn thôi, tầm 0.2s)
    public float dashCooldown = 1.5f; // Hồi chiêu (để không spam liên tục)

    // Các biến Network để đồng bộ trạng thái
    [Networked] private TickTimer DashActiveTimer { get; set; }   // Đếm thời gian đang lướt
    public TickTimer DashCooldownTimer { get; set; } // Đếm thời gian hồi chiêu
    [Networked] private Vector2 DashDirection { get; set; }       // Lưu hướng lướt

    private HealthComponent _healthComponent;

    [Networked] public TickTimer StunTimer { get; set; }
    [Networked] public TickTimer InvertedTimer { get; set; }
    [Networked] public TickTimer BulletTimer { get; set; }

    [Header("Weapon Config")]
    [SerializeField] private NetworkPrefabRef defaultBullet;

    [Networked] public NetworkPrefabRef ActiveBulletPrefab { get; set; }
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public bool IsReloading { get; set; }
    [Networked] private TickTimer delayFire { get; set; }
    [Networked] private TickTimer reloadTimer { get; set; }

    private Rigidbody2D _rb;
    [SerializeField] private Transform _visualTransform;
    [SerializeField] private GameObject visualPrefab;
    private GameObject _myVisual;

    //1 danh sách để chứa các player có mặc trong game
    public static List<PlayerController> ActivePlayers = new List<PlayerController>();

    private DeathExplosionSpawner _explosionSpawner;

    [Header("Visual Effects")]
    public TrailRenderer dashTrail;
    // ✅ THÊM: Reference đến Visual Trail (sẽ lấy từ prefab)
    private TrailRenderer _visualTrail;

    public override void Spawned()
    {
        // ✅ THÊM: Lấy DeathExplosionSpawner component
        _explosionSpawner = GetComponent<DeathExplosionSpawner>();
        if (_explosionSpawner == null)
        {
            Debug.LogWarning("[PlayerController] Chưa gán DeathExplosionSpawner component!");
        }

        if (dashTrail != null)
        {
            dashTrail.Clear(); 
            dashTrail.emitting = false;
        }

        //báo danh(thêm vào list)
        ActivePlayers.Add(this);

        _rb = GetComponent<Rigidbody2D>();

        // --- SỬA 2: Lấy component máu ---
        _healthComponent = GetComponent<HealthComponent>();

        if (_healthComponent != null)
        {
            _healthComponent.OnDeathEvent += OnDeathHandler;
        }

        if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
        UpdateSprite();

        if (Object.HasStateAuthority)
        {
            
            CurrentAmmo = MaxAmmo;
            IsReloading = false;

            // Mặc định dùng đạn thường khi mới sinh ra
            ActiveBulletPrefab = defaultBullet;
        }

        GetCamara();
        FixMove();
        // Cập nhật tên lần đầu
        UpdateNameUI();

        if (Object.HasInputAuthority) // Chỉ làm việc này nếu đây là máy của MÌNH
        {
            // 1. Cập nhật Máu ngay lập tức
            if (LocalUI.Instance != null && _healthComponent != null)
            {
                LocalUI.Instance.UpdateHealthUI(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);

                // Đăng ký nghe sự kiện máu thay đổi để cập nhật UI
                _healthComponent.OnHealthChangedEvent += OnLocalHealthChanged;
            }
        }
    }

    // Thêm hàm này vào trong class PlayerController
    public static PlayerController GetPlayerFromRef(PlayerRef playerRef)
    {
        foreach (var player in ActivePlayers)
        {
            // Kiểm tra nếu InputAuthority khớp với ID cần tìm
            if (player.Object != null && player.Object.InputAuthority == playerRef)
            {
                return player;
            }
        }
        return null;
    }

    // Hàm trung gian để cập nhật LocalUI
    private void OnLocalHealthChanged(float percent)
    {
        // Vì sự kiện trả về % (0-1), ta nhân lại để hàm bên kia hiểu
        // Hoặc truyền thẳng percent cũng được, nhưng ở đây tôi gọi UpdateHealthUI cho đồng bộ
        if (LocalUI.Instance != null && _healthComponent != null)
        {
            LocalUI.Instance.UpdateHealthUI(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);
        }
    }

    // ✅ FIX MEMORY LEAK: Quan trọng phải hủy đăng ký khi Despawn để tránh lỗi bộ nhớ
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // ✅ FIX: Unsubscribe tất cả events từ HealthComponent
        if (_healthComponent != null)
        {
            // Hủy đăng ký sự kiện chết
            _healthComponent.OnDeathEvent -= OnDeathHandler;
            
            // ✅ THÊM: Hủy đăng ký sự kiện thay đổi máu (LOCAL PLAYER ONLY)
            // Điều này ngăn chặn memory leak khi player bị despawn
            _healthComponent.OnHealthChangedEvent -= OnLocalHealthChanged;
        }

        // Hủy visual object
        if (_myVisual != null)
        {
            Destroy(_myVisual);
        }

        // Xóa bản thân khỏi danh sách người chơi
        ActivePlayers.Remove(this);

        // Chỉ Server mới kiểm tra điều kiện thắng
        if (runner.IsServer)
        {
            CheckWinCondition();
        }

        Debug.Log($"[Despawned] Player {NickName} đã bị unsubscribe tất cả events - Memory leak fixed! ✅");
    }

    // Hàm kiểm tra logic thắng
    private void CheckWinCondition()
    {
        Debug.Log($"[Check Win] Số người còn sống: {ActivePlayers.Count} | Tổng người trong phòng: {Runner.SessionInfo.PlayerCount}");
        // Nếu danh sách chỉ còn đúng 1 người
        // VÀ GameMode không phải là Single Player (đề phòng test một mình)
        if (ActivePlayers.Count == 1 && Runner.SessionInfo.PlayerCount > 1)
        {
            // Lấy người sống sót cuối cùng ra
            PlayerController winner = ActivePlayers[0];

            if (winner != null)
            {
                Debug.Log($"[Check Win] Tìm thấy Winner: {winner.NickName} -> Gửi RPC!");
                // Gọi RPC trên chính đối tượng thắng cuộc để báo tin
                winner.RPC_AnnounceWinner(winner.NickName.ToString());
            }
            else
            {
                Debug.LogError("[Check Win] Lỗi: Winner bị Null trong List?");
            }
        }
    }

    // ✅ THÊM: RPC để spawn vụ nổ (sync cho tất cả)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnDeathExplosion(Vector3 explosionPos)
    {
        if (_explosionSpawner != null)
        {
            _explosionSpawner.SpawnDeathExplosion(explosionPos);
        }
    }

    private void OnDeathHandler()
    {

        // ✅ THAY THẾ: Dùng RPC để spawn (đảm bảo sync)
        if (Object.HasStateAuthority)
        {
            RPC_SpawnDeathExplosion(transform.position);
        }

        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.simulated = false; // Tắt va chạm vật lý
        }

        if (_myVisual != null) _myVisual.SetActive(false);
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        if(nameText != null) nameText.enabled = false;

        // Tắt khả năng điều khiển (Chặn Input)
        this.enabled = false; // không chạy FixedUpdateNetwork 

        // Gọi UI Spectator 
        if (Object.HasInputAuthority)
        {
            if (GameplayUIManager.Instance != null)
            {
                GameplayUIManager.Instance.EnableSpectatorMode();
            }
        }

        //remove bản thân ra khỏi list khi Death
        if (ActivePlayers.Contains(this))
        {
            ActivePlayers.Remove(this);
        }

        if (Runner.IsServer)
        {
            CheckWinCondition();
        }
    }

    private void UpdateNameUI()
    {
        if (nameText != null)
        {
            nameText.text = NickName.ToString();

            // (Tùy chọn) Đổi màu tên mình cho dễ nhận biết
            if (Object.HasInputAuthority)
                nameText.color = Color.green;
            else
                nameText.color = Color.white;
        }
    }

    // Hàm gửi tên lên Server
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickName(string name)
    {
        this.NickName = name;
    }

    // --- SỬA 3: Hàm xử lý Hộp Bí Ẩn (Mystery Box) ---
    public void ApplyMysteryData(MysteryItemData data)
    {
        // Chỉ Server mới có quyền thay đổi dữ liệu game
        if (!Object.HasStateAuthority) return;
        if (data == null) return;

        // Phân loại công việc
        switch (data.actionType)
        {
            case MysteryAction.ModifyStat:
                HandleModifyStat(data);
                break;

            case MysteryAction.ApplyStatus:
                HandleApplyStatus(data);
                break;

            case MysteryAction.ChangeBullet:
                HandleChangeBullet(data);
                break;
        }
    }

    // --- KHU VỰC CÁC HÀM XỬ LÝ CHI TIẾT (HANDLERS) ---

    // 1. Chuyên viên xử lý chỉ số (Máu)
    private void HandleModifyStat(MysteryItemData data)
    {
        // Kiểm tra an toàn: Nếu không có component máu thì thôi
        if (_healthComponent == null) return;

        // Quy ước: ModifyStat trong game này dùng để Hồi Máu hoặc Trừ Máu
        int amount = data.valueAmount;

        if (amount > 0)
        {
            _healthComponent.Heal(amount);
        }
        else
        {
            // Vì amount là số âm (vd: -20), ta lấy trị tuyệt đối để trừ
            _healthComponent.TakeDamage(Mathf.Abs(amount));
        }
    }

    // 2. Chuyên viên xử lý hiệu ứng (Choáng, Đảo ngược)
    private void HandleApplyStatus(MysteryItemData data)
    {
        // Xử lý Choáng (Stun)
        if (data.isStun)
        {
            // Kích hoạt đồng hồ đếm ngược StunTimer
            StunTimer = TickTimer.CreateFromSeconds(Runner, data.duration);
            Debug.Log($"Bị choáng trong {data.duration} giây!");
        }

        // Xử lý Đảo ngược (Inverted)
        if (data.Inverted)
        {
            // Kích hoạt đồng hồ đếm ngược InvertedTimer
            InvertedTimer = TickTimer.CreateFromSeconds(Runner, data.duration);
            Debug.Log($"Bị đảo ngược điều khiển trong {data.duration} giây!");
        }
    }

    // 3. Chuyên viên xử lý đạn dược
    private void HandleChangeBullet(MysteryItemData data)
    {
        // Thay đổi loại đạn hiện tại sang loại mới
        ActiveBulletPrefab = data.bulletPrefab;

        if (data.bulletDuration > 0)
        {
            BulletTimer = TickTimer.CreateFromSeconds(Runner, data.bulletDuration);
        }
        else
        {
            // Nếu duration = 0 nghĩa là vĩnh viễn (hoặc đến khi nhặt súng khác)
            BulletTimer = TickTimer.None;
        }

        // 3. Reset đạn đầy băng (Tùy chọn)
        CurrentAmmo = MaxAmmo;
        IsReloading = false;
    }

    private void GetCamara()
    {
        if (Object.HasInputAuthority)
        {
            CinemachineVirtualCamera cam = FindObjectOfType<CinemachineVirtualCamera>();
            if (cam != null)
            {
                cam.Follow = _visualTransform != null ? _visualTransform : transform;
                cam.LookAt = null;
            }
        }
    }

    private void FixMove()
    {
        // Tạo Visual object
        _myVisual = Instantiate(visualPrefab, transform.position, transform.rotation);

        if (_myVisual.TryGetComponent<SmoothVisual>(out var smoother))
        {
            smoother.physicsTarget = transform;
        }

        // ✅ SIMPLIFY: Không cần runtime shader fix nếu prefab setup đúng
        _visualTrail = _myVisual.GetComponent<TrailRenderer>();
        if (_visualTrail != null)
        {
            _visualTrail.Clear();
            _visualTrail.emitting = false;
            Debug.Log("[FixMove] ✅ Trail Renderer ready!");
        }
        else
        {
            _visualTrail.Clear();
            _visualTrail.emitting = false;

            // --- RUNTIME FIX: đảm bảo trail render phía trên background ---
            const string fallbackSortingLayer = "Default"; 
            try
            {
                _visualTrail.sortingLayerName = fallbackSortingLayer;
                _visualTrail.sortingOrder = 1;
            }
            catch { }

            var mat = _visualTrail.material;
            if (mat != null)
            {
                if (mat.shader == null || mat.shader.name != "Sprites/Default")
                {
                    Shader s = Shader.Find("Sprites/Default");
                    if (s != null)
                    {
                        mat.shader = s;
                    }
                }
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
        }

        if (Object.HasInputAuthority)
        {
            var cam = FindObjectOfType<CinemachineVirtualCamera>();
            if (cam != null)
            {
                cam.Follow = _myVisual.transform;
                cam.LookAt = null;
            }
        }
    }

    public override void Render()
    {
        if (NetworkedSpriteIndex != _lastVisibleIndex)
        {
            UpdateSprite();
        }

        // Kiểm tra xem tên trên mạng có thay đổi không
        string currentName = NickName.ToString();
        if (currentName != _oldName)
        {
            UpdateNameUI();
            _oldName = currentName;
        }

        // (Tùy chọn) Hiển thị visual khi bị Choáng hoặc Đảo ngược
        if (_spriteRenderer != null)
        {
            if (StunTimer.IsRunning) _spriteRenderer.color = Color.gray; // Bị choáng hóa xám
            else if (InvertedTimer.IsRunning) _spriteRenderer.color = Color.magenta; // Đảo ngược hóa tím
            else _spriteRenderer.color = Color.white;
        }

        if (Object.HasInputAuthority)
        {
            if (LocalUI.Instance != null && _healthComponent != null)
            {
                // Cập nhật liên tục mỗi frame -> Đảm bảo luôn đúng 100% với server
                LocalUI.Instance.UpdateHealthUI(_healthComponent.CurrentHealth, _healthComponent.MaxHealth);
            }
        }

        // ✅ FIX: Sử dụng _visualTrail thay vì dashTrail
        if (_visualTrail != null)
        {
            bool isDashing = DashActiveTimer.IsRunning;
            _visualTrail.emitting = isDashing;

            Debug.Log($"[Render] Trail emitting: {isDashing}");
        }
        else if (dashTrail != null)
        {
            // Fallback nếu không lấy được từ visual
            bool isDashing = DashActiveTimer.IsRunning;
            dashTrail.emitting = isDashing;
        }
    }

    private void UpdateSprite()
    {
        if (_spriteRenderer == null || _avatarSprites == null || _avatarSprites.Length == 0) return;
        int safeIndex = NetworkedSpriteIndex % _avatarSprites.Length;
        _spriteRenderer.sprite = _avatarSprites[safeIndex];
        _lastVisibleIndex = NetworkedSpriteIndex;
    }

    public override void FixedUpdateNetwork()
    {
        // 1. Bị Choáng -> Đứng im
        if (ProcessStunStatus()) return;

        // 2. Kiểm tra súng hết hạ
        ProcessWeaponStatus();

        // 3. Xử lý Input
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();

            // Xử lý Đảo ngược (Code cũ)
            ProcessInputModifiers(ref data);

            // Nếu đang Dash -> Bỏ qua phần di chuyển thường và bắn súng bên dưới
            if (ProcessDashLogic(data)) return;
            // -----------------------

            // 4. Di chuyển bình thường (Chỉ chạy khi không Dash)
            MovePlayer(data);

            // 5. Bắn súng (Chỉ chạy khi không Dash)
            HandleShooting(data);

            HandleReloadLogic();

            // --- ĐOẠN MỚI THÊM: CẬP NHẬT DASH & ĐẠN ---
            if (Object.HasInputAuthority && LocalUI.Instance != null)
            {
                // 1. Tính toán Dash Cooldown
                float timeRemaining = 0f;
                if (DashCooldownTimer.IsRunning)
                {
                    // Lấy thời gian còn lại (tính bằng giây)
                    timeRemaining = (float)DashCooldownTimer.RemainingTime(Runner);
                }

                // Gửi sang UI
                LocalUI.Instance.UpdateDashUI(timeRemaining, dashCooldown);

                // 2. Cập nhật Đạn
                LocalUI.Instance.UpdateAmmoUI(CurrentAmmo, MaxAmmo);
            }
        }
        else
        {
            // Không có input -> Dừng xe
            if (_rb != null) _rb.velocity = Vector2.zero;
        }
    }

    private void MovePlayer(NetworkInputData data)
    {
        // Xoay nhân vật theo hướng di chuyển
        if (data.direction != Vector2.zero)
        {
            transform.up = data.direction;
        }

        // Di chuyển Rigidbody
        if (_rb != null)
        {
            Vector2 targetPos = _rb.position + (data.direction * speed * Runner.DeltaTime);
            _rb.MovePosition(targetPos);
        }
    }

    // Hàm hỗ trợ nạp đạn (Copy thêm vào script)
    private void HandleReloadLogic()
    {
        if (!Object.HasStateAuthority) return;

        if (IsReloading)
        {
            if (reloadTimer.Expired(Runner))
            {
                CurrentAmmo = MaxAmmo;
                IsReloading = false;
            }
        }
        else if (CurrentAmmo <= 0)
        {
            IsReloading = true;
            reloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
        }
    }

    // Hàm hỗ trợ bắn súng (Copy thêm vào script)
    private void HandleShooting(NetworkInputData data)
    {
        // Điều kiện bắn: Có nhấn nút + Đã hết delay + Không đang nạp đạn
        if (data.isFire && delayFire.ExpiredOrNotRunning(Runner) && !IsReloading)
        {
            if (Runner.IsServer)
            {
                if (CurrentAmmo > 0)
                {
                    Vector3 spawnPosition = (firePoint != null) ? firePoint.position : (transform.position + transform.up);


                    // Bắn loại đạn đang active
                    Runner.Spawn(ActiveBulletPrefab, spawnPosition, transform.rotation, Object.InputAuthority);
                    if (Object.HasInputAuthority)
                    {
                        RPC_PlayShootSound();
                    }

                    delayFire = TickTimer.CreateFromSeconds(Runner, fireRate);
                    CurrentAmmo--;
                }
            }
        }
    }



    // --- NHÓM HÀM XỬ LÝ TRẠNG THÁI (STATUS HANDLERS) ---

    /// <summary>
    /// Kiểm tra và xử lý trạng thái Choáng.
    /// Trả về true nếu đang bị choáng (để chặn các hành động khác).
    /// </summary>
    private bool ProcessStunStatus()
    {
        // Nếu Timer không chạy hoặc Đã hết hạn => KHÔNG choáng
        if (StunTimer.ExpiredOrNotRunning(Runner))
        {
            return false;
        }

        // Ngược lại => ĐANG choáng
        if (_rb != null) _rb.velocity = Vector2.zero;
        return true;
    }

    /// Xử lý các hiệu ứng làm thay đổi Input (Đảo ng reverse, Say rượu, v.v...)
    /// Dùng từ khóa 'ref' để sửa trực tiếp biến data được truyền vào
    private void ProcessInputModifiers(ref NetworkInputData data)
    {
        // Kiểm tra hiệu ứng Đảo ngược
        if (InvertedTimer.IsRunning && !InvertedTimer.Expired(Runner))
        {
            data.direction *= -1;
        }

    }

    private void ProcessWeaponStatus()
    {
        if (!Object.HasStateAuthority) return;
        // Nếu Timer đang chạy VÀ Đã hết giờ (Expired)
        if (BulletTimer.Expired(Runner))
        {
            ActiveBulletPrefab = defaultBullet;

            BulletTimer = TickTimer.None;

            // (Tùy chọn) Reset lại đạn cho súng mặc định
            CurrentAmmo = MaxAmmo;
            IsReloading = false;
        }
    }

    /// <summary>
    /// ✅ FIX GAME BALANCE: Xử lý logic Dash với guard clause cho Stun
    /// Nếu đang bị Choáng → không cho phép sử dụng Dash
    /// </summary>
    private bool ProcessDashLogic(NetworkInputData data)
    {
        // ✅ THÊM: Nếu đang bị Choáng → chặn khả năng Dash
        // Điều này đảm bảo Stun effect có ưu tiên cao hơn và loại bỏ exploit
        if (StunTimer.IsRunning && !StunTimer.Expired(Runner))
        {
            Debug.Log($"[Dash Blocked] Player {NickName} bị Choáng - Không thể Dash!");
            return false; // Không cho phép dash, tiếp tục frame bình thường
        }

        // Kiểm tra xem đang ở giữa Dash hay không
        if (DashActiveTimer.IsRunning && !DashActiveTimer.Expired(Runner))
        {
            if (_rb != null)
            {
                Vector2 targetPos = _rb.position + (DashDirection * dashSpeed * Runner.DeltaTime);
                _rb.MovePosition(targetPos);
                
                if(AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayDash();
                }
            }

            // Đang lướt -> Chặn các hành động khác
            return true;
        }

        // (Tùy chọn) Nếu hết giờ thì tắt Timer đi cho sạch
        if (DashActiveTimer.Expired(Runner))
        {
            DashActiveTimer = TickTimer.None;
        }

        // Logic kích hoạt Dash (Chỉ được kích hoạt nếu không choáng)
        if (data.isDash && DashCooldownTimer.ExpiredOrNotRunning(Runner)/* && data.direction != Vector2.zero*/)
        {
            // ✅ DOUBLE CHECK: Kiểm tra lần nữa trước khi tạo Dash
            // Để chắc chắn rằng không có condition race nào xảy ra
            if (!StunTimer.IsRunning || StunTimer.Expired(Runner))
            {
                DashActiveTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);
                DashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
                DashDirection = data.direction;

                Debug.Log($"[Dash Activated] Player {NickName} bắt đầu Dash!");
            }
        }

        return false;
    }


    // [Rpc] là attribute của Fusion để thay thế [PunRPC]
    // RpcSources.InputAuthority: Chỉ người điều khiển mới được gọi lệnh này
    // RpcTargets.All: Gửi lệnh này đến TẤT CẢ mọi người (bao gồm cả Server và các Client khác)
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_PlayShootSound()
    {
        // Khi lệnh đến máy của mỗi người, nó sẽ chạy dòng này
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayShoot();
        }
    }

    // [RPC MỚI] Dùng để thông báo người thắng cuộc cho toàn bộ server
    // Static = false, nghĩa là hàm này chạy trên một instance cụ thể (người thắng)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AnnounceWinner(string winnerNickName)
    {
        Debug.Log($"WINNER IS: {winnerNickName}");

        if (GameplayUIManager.Instance != null)
        {
            GameplayUIManager.Instance.ShowWinnerPanel(winnerNickName);
        }
    }


}