using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class LocalUI : MonoBehaviour
{
    [Header("UI Compoment")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI ammoText;

    [SerializeField] private Image dashOverlayImage;

    private PlayerController _targetPlayer;
    private HealthComponent _targetHealth;

    
    private void Update()
    {
        if (ammoText == null || healthFillImage == null) return;

        if (_targetPlayer == null)
        {
            FindLocalPlayer();
            if (_targetPlayer == null) return;
        }

        if(_targetHealth == null)
        {
            _targetHealth = _targetPlayer.GetComponent<HealthComponent>();

            if (_targetHealth == null) return;
        }

        if(_targetHealth.MaxHealth > 0)
        {
            float hpPercent = (float)_targetHealth.CurrentHealth / _targetHealth.MaxHealth;
            healthFillImage.fillAmount = hpPercent;
        }

        // Cập nhật Đạn
        if (_targetPlayer.IsReloading)
        {
            ammoText.text = "RELOAD...";
            ammoText.color = Color.yellow;
        }
        else
        {
            ammoText.text = $"{_targetPlayer.CurrentAmmo}";
            ammoText.color = Color.white;
        }

        if (dashOverlayImage != null)
        {
            UpdateDashUI();
        }


    }


    void FindLocalPlayer()
    {
        var players = FindObjectsOfType<PlayerController>();
        foreach (var p in players)
        {
            if (p.Object != null && p.Object.HasInputAuthority)
            {
                _targetPlayer = p;
                _targetHealth = p.GetComponent<HealthComponent>();
                break;
            }
        }
    }

    void UpdateDashUI()
    {
        // Kiểm tra xem Timer hồi chiêu có đang chạy không
        if (_targetPlayer.DashCooldownTimer.IsRunning)
        {
            // Tính thời gian còn lại
            // RemainingTime trả về float? (nullable), nên cần ?? 0 để lấy giá trị mặc định
            float remainingTime = _targetPlayer.DashCooldownTimer.RemainingTime(_targetPlayer.Runner) ?? 0;

            // Tính phần trăm: Thời gian còn lại / Tổng thời gian hồi
            float fillRatio = remainingTime / _targetPlayer.dashCooldown;

            // Cập nhật hình ảnh (0 = hết che, 1 = che kín)
            dashOverlayImage.enabled = true;
            dashOverlayImage.fillAmount = fillRatio;
        }
        else
        {
            // Nếu không chạy (đã hồi xong) -> Xóa lớp che đi (fill = 0)
            dashOverlayImage.enabled =false;
            dashOverlayImage.fillAmount = 0f;
        }
    }
}
