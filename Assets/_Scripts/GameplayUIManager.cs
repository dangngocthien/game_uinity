using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cinemachine;
using Unity.VisualScripting;

public class GameplayUIManager : MonoBehaviour
{
    public static GameplayUIManager Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("--- UI REFERENCES ---")]
    [SerializeField] private GameObject panelSpectator;
    [SerializeField] private TextMeshProUGUI txtSpectatingName;

    [Header("--- WINNER UI ---")]
    [SerializeField] private GameObject panelWinner;
    [SerializeField] private TextMeshProUGUI txtWinnerName;

    [Header("--- KILL FEED ---")]
    [SerializeField] private GameObject killFeedContainer; // Kéo cái Content/Panel chứa dòng chữ vào
    [SerializeField] private TextMeshProUGUI killFeedTextPrefab; // Tạo 1 prefab Text mẫu

    [Header("--- BUTTONS ---")]
    [SerializeField] private Button btnExitMatch;
    [SerializeField] private Button btnNextCam;
    [SerializeField] private Button btnPrevCam;

    private CinemachineVirtualCamera _cam;
    //vị trí của người xem hiện tại
    private int _spectatorIndex = 0;


    private void Start()
    {
        if (panelSpectator != null)
        {
            panelSpectator.SetActive(false);
        }

        if(btnExitMatch != null)
        {
            btnExitMatch.onClick.AddListener(OnClickExitMatch);
        }

        if (btnNextCam != null) btnNextCam.onClick.AddListener(() => ChangeSpectatorTarget(1));  // 1 là tiếng tới
        if (btnPrevCam != null) btnPrevCam.onClick.AddListener(() => ChangeSpectatorTarget(-1)); // -1 là lùi lại

        _cam = FindObjectOfType<CinemachineVirtualCamera>();
    }



    //// Hàm Bật chế độ quan sát (Player gọi hàm này khi chết)
    public void EnableSpectatorMode()
    {
        if(panelSpectator != null)
        {
            panelSpectator.SetActive(true);
        }

        // xem 1 người khi mới vừa bật panel spectator
        ChangeSpectatorTarget(1);
    }

    private void ChangeSpectatorTarget(int direction)
    {
        if(PlayerController.ActivePlayers.Count == 0) return;

        _spectatorIndex += direction;

        if (_spectatorIndex >= PlayerController.ActivePlayers.Count)
        {
            _spectatorIndex = 0;
        }
        else if(_spectatorIndex < 0)
        {
            _spectatorIndex = PlayerController.ActivePlayers.Count - 1;
        }

        var targetPlayer = PlayerController.ActivePlayers[_spectatorIndex];

        if(targetPlayer == null || targetPlayer.Object.HasInputAuthority)
        {
            if (PlayerController.ActivePlayers.Count > 1)
            {
                ChangeSpectatorTarget(direction);
            }
            return; 

        }

        // Cập nhật Camera & UI
        UpdateCameraFollow(targetPlayer);
    }

    private void UpdateCameraFollow(PlayerController target)
    {
        if(_cam != null)
        {
            _cam.Follow = target.transform;
        }

        if (txtSpectatingName != null)
        {
            txtSpectatingName.text = $"Đang xem: {target.NickName}";
        }
    }


    // Hàm Xử lý nút Thoát Trận
    public void OnClickExitMatch()
    {
        if(BasicSpawner.Instance != null)
        {
            BasicSpawner.Instance.LeaveLobby();
        }

        SceneManager.LoadScene(0);
    }

    public void ShowWinnerPanel(string winnerName)
    {
        if (panelWinner != null)
        {
            panelWinner.SetActive(true);

            if (panelSpectator != null) panelSpectator.SetActive(false);
        }

        if (txtWinnerName != null)
        {
            txtWinnerName.text = $"CHIẾN THẮNG!\n{winnerName}";
        }

        StartCoroutine(AutoExitDelay());
    }

    private IEnumerator AutoExitDelay()
    {
        yield return new WaitForSeconds(5f);
        OnClickExitMatch();
    }

    // 2. Thêm hàm hiển thị thông báo hạ gục
    public void AddKillFeed(string killer, string victim)
    {
        if (killFeedContainer == null || killFeedTextPrefab == null) return;

        // Tạo dòng chữ mới
        TextMeshProUGUI newLog = Instantiate(killFeedTextPrefab, killFeedContainer.transform);

        // Tô màu: Kẻ giết (Vàng) - Nạn nhân (Trắng)
        newLog.text = $"<color=yellow>{killer}</color> đã hạ gục <color=white>{victim}</color>";

        // Quan trọng: Đặt lại Scale về 1 để không bị biến dạng
        newLog.transform.localScale = Vector3.one;

        // Tự hủy sau 4 giây
        Destroy(newLog.gameObject, 4f);
    }
}
