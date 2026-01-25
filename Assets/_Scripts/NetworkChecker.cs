using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// ✅ Kiểm tra kết nối Internet ngay khi app khởi động
/// - Level 1: Quick check (Application.internetReachability)
/// - Level 2: Ping URL (UnityWebRequest)
/// - Nếu không có mạng → Hiển thị popup lỗi + thoát
/// - Nếu có mạng → Tiếp tục chơi bình thường
/// </summary>
public class NetworkChecker : MonoBehaviour
{
    [Header("--- NETWORK CHECK SETTINGS ---")]
    [SerializeField] private float checkTimeout = 5f;
    [SerializeField] private string testURL = "https://www.google.com";

    [Header("--- ERROR UI ---")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TextMeshProUGUI errorTitle;
    [SerializeField] private TextMeshProUGUI errorMessage;
    [SerializeField] private Button okButton;

    [Header("--- DEBUG MODE ---")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool forceNoNetwork = false; // ✅ Set true để test lỗi mạng

    private static NetworkChecker _instance;
    private bool _hasInternetConnection = false;
    private bool _checkingNetwork = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ✅ Gán listener cho nút OK
        if (okButton != null)
        {
            okButton.onClick.AddListener(OnClickOK);
        }

        // ✅ Ẩn error panel lúc đầu
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }

        // ✅ Bắt đầu check kết nối (async)
        StartCoroutine(CheckNetworkConnectionAsync());
    }

    /// <summary>
    /// ✅ Check kết nối Internet (Async - Level 1 + Level 2)
    /// </summary>
    private IEnumerator CheckNetworkConnectionAsync()
    {
        if (_checkingNetwork) yield break;
        _checkingNetwork = true;

        if (debugMode)
        {
            Debug.Log("[NetworkChecker] 🔍 BẮT ĐẦU KIỂM TRA MẠNG (Level 1 + Level 2)...");
        }

        // ✅ LEVEL 1: Quick Check
        if (CheckNetworkLevel1())
        {
            if (debugMode)
            {
                Debug.Log("[NetworkChecker] ✅ Level 1 OK - Tiếp tục Level 2");
            }

            // ✅ LEVEL 2: Ping URL
            yield return StartCoroutine(CheckNetworkLevel2());
        }
        else
        {
            if (debugMode)
            {
                Debug.LogError("[NetworkChecker] ❌ Level 1 FAIL - Không có mạng!");
            }
            ShowNetworkError("Không Có Mạng", "Vui lòng bật WiFi hoặc 4G");
        }

        // Khi network check thành công:
        if (_hasInternetConnection)
        {
            // ✅ Gọi AudioManager để phát nhạc
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(AudioManager.Instance.backgroundClip);
            }
        }

        _checkingNetwork = false;
    }

    /// <summary>
    /// ✅ LEVEL 1: Kiểm tra nhanh dùng Application.internetReachability
    /// </summary>
    private bool CheckNetworkLevel1()
    {
        // ✅ DEBUG: Cho phép force no network
        if (forceNoNetwork)
        {
            if (debugMode)
            {
                Debug.LogWarning("[NetworkChecker] ⚠️ DEBUG: Forcing NO NETWORK!");
            }
            return false;
        }

        // ✅ Kiểm tra kết nối nhanh
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (debugMode)
            {
                Debug.LogError("[NetworkChecker] ❌ Level 1: KHÔNG CÓ MẠNG!");
            }
            return false;
        }

        // ✅ Kiểm tra loại mạng
        bool isWifi = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
        bool isMobile = Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;

        if (isWifi)
        {
            if (debugMode)
            {
                Debug.Log("[NetworkChecker] ✅ Level 1: Kết nối WiFi");
            }
            return true;
        }
        else if (isMobile)
        {
            if (debugMode)
            {
                Debug.Log("[NetworkChecker] ✅ Level 1: Kết nối 4G/3G");
            }
            return true;
        }

        if (debugMode)
        {
            Debug.LogError("[NetworkChecker] ❌ Level 1: Không rõ loại mạng!");
        }
        return false;
    }

    /// <summary>
    /// ✅ LEVEL 2: Ping URL để kiểm tra thực tế
    /// </summary>
    private IEnumerator CheckNetworkLevel2()
    {
        if (debugMode)
        {
            Debug.Log($"[NetworkChecker] 🔍 Level 2: Đang ping {testURL}...");
        }

        using (UnityWebRequest request = UnityWebRequest.Head(testURL))
        {
            request.timeout = (int)checkTimeout;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (debugMode)
                {
                    Debug.Log("[NetworkChecker] ✅ Level 2: Ping THÀNH CÔNG!");
                }
                _hasInternetConnection = true;
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogError($"[NetworkChecker] ❌ Level 2: Ping THẤT BẠI - {request.error}");
                }
                ShowNetworkError(
                    "Kết Nối Không Ổn Định",
                    $"Server không phản hồi.\nVui lòng kiểm tra kết nối Internet"
                );
            }
        }
    }

    /// <summary>
    /// ✅ Hiển thị lỗi kết nối
    /// </summary>
    private void ShowNetworkError(string title, string message)
    {
        if (debugMode)
        {
            Debug.LogError($"[NetworkChecker] 🚨 Hiển thị lỗi: {title}\n{message}");
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
        }

        if (errorTitle != null)
        {
            errorTitle.text = title;
        }

        if (errorMessage != null)
        {
            errorMessage.text = message;
        }

        // ✅ Dừng game
        Time.timeScale = 0f;
    }

    /// <summary>
    /// ✅ Callback nút OK - Thoát game
    /// </summary>
    private void OnClickOK()
    {
        if (debugMode)
        {
            Debug.Log("[NetworkChecker] User bấm OK - Thoát game");
        }

        // ✅ Resume game
        Time.timeScale = 1f;

        // ✅ Thoát ứng dụng
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// ✅ Getter để check từ script khác
    /// </summary>
    public static bool HasInternetConnection => 
        Application.internetReachability != NetworkReachability.NotReachable;
}