using Fusion;
using System;
using UnityEngine;

public class HealthComponent : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;
    public int MaxHealth => maxHealth;

    // --- ĐÃ XÓA BIẾN LocalUI ĐỂ KHÔNG BỊ LỖI KÉO THẢ ---

    [Networked] public int CurrentHealth { get; set; }

    // Sự kiện để UI tự nghe (Observer Pattern)
    public event Action<float> OnHealthChangedEvent;
    public event Action OnDeathEvent;

    public bool IsDead => CurrentHealth <= 0;

    public override void Spawned()
    {
        // 1. Chỉ Server mới được set máu ban đầu
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }

        // 2. Cập nhật UI ngay khi sinh ra
        OnHealthChangedEvent?.Invoke((float)CurrentHealth / maxHealth);
    }

    // Hàm nhận sát thương
    public void TakeDamage(int damageAmount, PlayerRef attackerRef = default)
    {
        if (IsDead) return;

        // Chỉ Server tính toán sát thương
        if (Object.HasStateAuthority)
        {
            CurrentHealth -= damageAmount;

            // Báo cho UI cập nhật
            OnHealthChangedEvent?.Invoke((float)CurrentHealth / maxHealth);

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                // Báo tử cho toàn server
                RPC_BroadcastDeath(attackerRef);
            }
        }
    }

    // Hàm Hồi máu (ĐÂY LÀ HÀM BẠN ĐANG BỊ THIẾU)
    public void Heal(int healAmount)
    {
        if (IsDead) return;

        // Chỉ Server mới được hồi máu
        if (Object.HasStateAuthority)
        {
            CurrentHealth += healAmount;

            // Không được hồi quá máu tối đa
            if (CurrentHealth > maxHealth)
            {
                CurrentHealth = maxHealth;
            }

            // Báo cho UI cập nhật
            OnHealthChangedEvent?.Invoke((float)CurrentHealth / maxHealth);
        }
    }

    // Hàm báo tử qua mạng (RPC)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastDeath(PlayerRef killerRef)
    {

        // Kích hoạt sự kiện -> LocalUI và PlayerController sẽ tự nghe thấy và xử lý
        OnDeathEvent?.Invoke();

        if(GameplayUIManager.Instance != null)
        {
            string killerName = "Môi trường";
            string victimName = "Ai đó";

            //nếu killer = none thì là môi trường kill
            if (killerRef == PlayerRef.None)
            {
                killerName = "Môi trường";
            }
            else if (killerRef == Object.InputAuthority)
            {
                killerName = "Chính mình";
            }
            else
            {
                var killerObj = PlayerController.GetPlayerFromRef(killerRef);
                if (killerObj != null) killerName = killerObj.NickName.ToString();
            }

            // 2. Tìm tên NẠN NHÂN (Chính là thằng đang giữ script này)
            var victimObj = GetComponent<PlayerController>();
            if (victimObj != null) victimName = victimObj.NickName.ToString();

            // 3. Gửi sang UI
            GameplayUIManager.Instance.AddKillFeed(killerName, victimName);

        }
        
        
    }
}