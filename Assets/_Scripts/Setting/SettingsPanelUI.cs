using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanelUI : MonoBehaviour
{
    [Header("--- PANEL REFERENCES ---")]
    [SerializeField] private GameObject panelSettings;
    [SerializeField] private Button buttonClose;
    [SerializeField] private Button buttonSettingsIcon;

    [Header("--- MUSIC VOLUME SLIDER ---")]
    [SerializeField] private Slider sliderMusicVolume;
    [SerializeField] private TextMeshProUGUI textMusicValue;

    [Header("--- VFX/SFX VOLUME SLIDER ---")]
    [SerializeField] private Slider sliderSFXVolume;
    [SerializeField] private TextMeshProUGUI textSFXValue;

    [Header("--- FPS TOGGLE ---")]
    [SerializeField] private Toggle toggleShowFPS;

    private void Start()
    {
        // Đảm bảo panel ẩn lúc khởi động
        if (panelSettings != null)
        {
            panelSettings.SetActive(false);
        }

        // Gắn sự kiện cho các button
        if (buttonClose != null)
        {
            buttonClose.onClick.AddListener(CloseSettingsPanel);
        }

        if (buttonSettingsIcon != null)
        {
            buttonSettingsIcon.onClick.AddListener(OpenSettingsPanel);
        }

        // Gắn sự kiện cho các slider
        if (sliderMusicVolume != null)
        {
            sliderMusicVolume.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sliderSFXVolume != null)
        {
            sliderSFXVolume.onValueChanged.AddListener(SetSFXVolume);
        }

        // Gắn sự kiện cho toggle FPS
        if (toggleShowFPS != null)
        {
            toggleShowFPS.onValueChanged.AddListener(SetShowFPS);
        }

        // Load các cài đặt hiện tại
        LoadSettings();
    }

    /// <summary>
    /// Mở Settings Panel
    /// </summary>
    public void OpenSettingsPanel()
    {
        if (panelSettings != null)
        {
            panelSettings.SetActive(true);
            LoadSettings();
        }

        Time.timeScale = 0f; // Tạm dừng game
    }

    /// <summary>
    /// Đóng Settings Panel
    /// </summary>
    public void CloseSettingsPanel()
    {
        if (panelSettings != null)
        {
            panelSettings.SetActive(false);
        }

        Time.timeScale = 1f; // Tiếp tục game
    }

    /// <summary>
    /// Load tất cả cài đặt từ PlayerPrefs vào UI
    /// </summary>
    private void LoadSettings()
    {
        // Load Music Volume
        float musicVolume = PlayerPrefs.GetFloat("Audio_Music", 0.6f);
        if (sliderMusicVolume != null)
        {
            sliderMusicVolume.SetValueWithoutNotify(musicVolume);
        }
        UpdateMusicVolumeText(musicVolume);

        // Load SFX Volume
        float sfxVolume = PlayerPrefs.GetFloat("Audio_SFX", 0.8f);
        if (sliderSFXVolume != null)
        {
            sliderSFXVolume.SetValueWithoutNotify(sfxVolume);
        }
        UpdateSFXVolumeText(sfxVolume);

        // Load Show FPS
        bool showFPS = PlayerPrefs.GetInt("Settings_ShowFPS", 0) == 1;
        if (toggleShowFPS != null)
        {
            toggleShowFPS.SetIsOnWithoutNotify(showFPS);
        }
    }

    /// <summary>
    /// Điều chỉnh âm lượng nhạc
    /// </summary>
    public void SetMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }

        UpdateMusicVolumeText(value);
    }

    /// <summary>
    /// Cập nhật text hiển thị % âm lượng nhạc
    /// </summary>
    private void UpdateMusicVolumeText(float value)
    {
        if (textMusicValue != null)
        {
            int percentage = Mathf.RoundToInt(value * 100);
            textMusicValue.text = $"{percentage}%";
        }
    }

    /// <summary>
    /// Điều chỉnh âm lượng VFX/SFX
    /// </summary>
    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }

        UpdateSFXVolumeText(value);
    }

    /// <summary>
    /// Cập nhật text hiển thị % âm lượng VFX
    /// </summary>
    private void UpdateSFXVolumeText(float value)
    {
        if (textSFXValue != null)
        {
            int percentage = Mathf.RoundToInt(value * 100);
            textSFXValue.text = $"{percentage}%";
        }
    }

    /// <summary>
    /// Bật/Tắt hiển thị FPS
    /// </summary>
    public void SetShowFPS(bool isOn)
    {
        if (FPSCounter.Instance != null)
        {
            FPSCounter.Instance.SetVisible(isOn);
        }
    }

    /// <summary>
    /// Lưu tất cả cài đặt (tùy chọn - nếu muốn lưu khi đóng panel)
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}