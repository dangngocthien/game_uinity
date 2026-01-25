using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nhớ thêm thư viện này để dùng Text

public class LocalUI : MonoBehaviour
{
    // Tạo Singleton để Player dễ dàng tìm thấy
    public static LocalUI Instance;

    [Header("1. Health UI")]
    [SerializeField] private Image healthFillImage; // Kéo ảnh Fill của thanh máu vào đây

    [Header("2. Dash UI")]
    [SerializeField] private Image[] dashFillImages;   // Kéo ảnh Fill của Dash vào đây
    [SerializeField] private GameObject dashIconGroup; // (Tùy chọn) Để tắt bật cả cụm Dash

    [Header("3. Ammo UI")]
    [SerializeField] private TextMeshProUGUI ammoText; // Kéo Text hiển thị số đạn vào

    [Header("4. PC, Mobile Instructions")]
    [SerializeField] private GameObject panelSkillHUD;
    [SerializeField] private GameObject MobileControls;
    private void Awake()
    {
        // Đảm bảo chỉ có 1 UI duy nhất tồn tại
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CheckThePhone();
    }

    private void CheckThePhone()
    {
        if (Application.isMobilePlatform)
        {
    
            if (panelSkillHUD != null && MobileControls != null)
            {
                panelSkillHUD.SetActive(false);
                MobileControls.SetActive(true);
            }
        }
        else
        {
            if (panelSkillHUD != null && MobileControls != null)
            {
                panelSkillHUD.SetActive(false);
                MobileControls.SetActive(true);
            }
        }
    }

    // --- CÁC HÀM CẬP NHẬT (Player sẽ gọi mấy hàm này) ---

    // 1. Cập nhật Máu
    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthFillImage != null)
        {
            // Tránh chia cho 0
            float percent = (maxHealth > 0) ? (currentHealth / maxHealth) : 0;
            healthFillImage.fillAmount = percent;
        }
    }

    // 2. Cập nhật Dash (Hiển thị thời gian hồi chiêu)
    public void UpdateDashUI(float currentTimer, float maxCooldown)
    {
        // Tính toán phần trăm (chỉ tính 1 lần)
        float percent = 0;
        bool isCooldown = (currentTimer > 0 && maxCooldown > 0);

        if (isCooldown)
        {
            percent = currentTimer / maxCooldown;
        }

        // 3. Lặp qua tất cả các ảnh trong danh sách và cập nhật từng cái
        if (dashFillImages != null)
        {
            foreach (var img in dashFillImages)
            {
                if (img != null)
                {
                    if (isCooldown)
                    {
                        img.enabled = true; 
                        img.fillAmount = percent;
                    }
                    else
                    {
                        img.enabled = false;
                    }
                }
            }
        }
    }

    // 3. Cập nhật Đạn
    public void UpdateAmmoUI(int current, int max)
    {
        if (ammoText != null)
        {
            ammoText.text = $"{current}/{max}";
        }
    }
}