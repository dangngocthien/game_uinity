using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public static FPSCounter Instance;

    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f; // Cập nhật mỗi 0.5 giây

    private float _deltaTime = 0f;
    private float _timer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load cài đặt từ PlayerPrefs xem có hiển thị FPS không
        bool showFPS = PlayerPrefs.GetInt("Settings_ShowFPS", 0) == 1;
        SetVisible(showFPS);
    }

    private void Update()
    {
        // Cập nhật deltaTime
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        _timer += Time.unscaledDeltaTime;

        // Cập nhật text FPS mỗi updateInterval giây
        if (_timer >= updateInterval)
        {
            int fps = Mathf.RoundToInt(1f / _deltaTime);

            if (fpsText != null)
            {
                fpsText.text = $"FPS: {fps}";

                // Tô màu dựa trên FPS (Green >= 60, Yellow 30-60, Red < 30)
                if (fps >= 60)
                    fpsText.color = Color.green;
                else if (fps >= 30)
                    fpsText.color = Color.yellow;
                else
                    fpsText.color = Color.red;
            }

            _timer = 0f;
        }
    }

    /// <summary>
    /// Bật/Tắt hiển thị FPS counter
    /// </summary>
    public void SetVisible(bool isVisible)
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(isVisible);
        }

        // Lưu cài đặt
        PlayerPrefs.SetInt("Settings_ShowFPS", isVisible ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Toggle (bật/tắt) FPS counter
    /// </summary>
    public void Toggle()
    {
        if (fpsText != null)
        {
            bool isActive = fpsText.gameObject.activeSelf;
            SetVisible(!isActive);
        }
    }

    /// <summary>
    /// Lấy trạng thái hiển thị FPS
    /// </summary>
    public bool IsVisible()
    {
        return fpsText != null && fpsText.gameObject.activeSelf;
    }
}