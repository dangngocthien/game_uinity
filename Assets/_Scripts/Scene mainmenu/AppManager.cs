using Fusion;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AppManager : MonoBehaviour
{
    [Header("--- UI PANELS (MAIN) ---")]
    [SerializeField] private GameObject panelTapToStart;
    [SerializeField] private GameObject panelModeSelect;
    [SerializeField] private GameObject panelConnection;
    [SerializeField] private GameObject panelLobby;

    [Header("--- PANEL CONNECTION SUB-PARTS ---")]
    [SerializeField] private GameObject groupMainOptions;

    // Kéo cái Panel_JoinRoom (chứa Input và nút Join) vào đây
    [SerializeField] private GameObject groupJoinInput;

    [Header("--- UI COMPONENTS ---")]
    [SerializeField] private TMP_InputField inputRoomID;
    [SerializeField] private TMP_InputField inputPlayerName;
    [SerializeField] private TextMeshProUGUI textRoomIDDisplay;

    // THÊM: Text để hiển thị danh sách người chơi
    [SerializeField] private TextMeshProUGUI textPlayerList;
    // THÊM: Nút hành động chính (Sẵn sàng/Bắt đầu)
    [SerializeField] private Button btnAction;
    [SerializeField] private TextMeshProUGUI textBtnAction;

    [Header("--- REFERENCES ---")]
    [SerializeField] private BasicSpawner basicSpawner;

    private string _targetRoomName;
    private Fusion.GameMode _targetMode;

    private void Start()
    {
        ShowPanel(panelTapToStart);
        if (basicSpawner == null) basicSpawner = FindObjectOfType<BasicSpawner>();

        // Đảm bảo ô nhập tên trống trơn, để người chơi tự quyết định có nhập hay không
        if (inputPlayerName != null) inputPlayerName.text = "";
    }

    // --- 1. LOGIC CHUYỂN PANEL CHÍNH ---

    public void OnTapToStart()
    {
        ShowPanel(panelModeSelect);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot();
    }

    public void OnClickPlayOnline()
    {
        ShowPanel(panelConnection);

        // [MỚI] Reset trạng thái của Panel Connection
        // Mặc định: Hiện 2 nút chọn, Ẩn ô nhập ID đi
        if (groupMainOptions != null) groupMainOptions.SetActive(true);
        if (groupJoinInput != null) groupJoinInput.SetActive(false);
    }

    public void OnClickBack()
    {
        // Logic quay lại tùy ngữ cảnh
        if (panelConnection.activeSelf)
        {
            // Nếu đang ở màn Connection mà đang hiện ô nhập ID -> Quay lại chọn nút
            if (groupJoinInput != null && groupJoinInput.activeSelf)
            {
                groupJoinInput.SetActive(false);
                groupMainOptions.SetActive(true);
                return;
            }
            // Nếu đang ở màn Connection thường -> Quay về Mode Select
            ShowPanel(panelModeSelect);
        }
        else if (panelLobby.activeSelf)
        {
            // Nếu đang ở Lobby -> Rời phòng về Connection
            // (Cần thêm logic ngắt kết nối runner ở đây nếu muốn kỹ hơn)
            OnClickPlayOnline();
        }
        else
        {
            ShowPanel(panelModeSelect);
        }
    }

    // --- 2. LOGIC TRONG PANEL CONNECTION

    public void OnClickOpenJoinInput()
    {
        // Ẩn 2 nút chọn đi
        if (groupMainOptions != null) groupMainOptions.SetActive(false);
        // Hiện ô nhập và nút Join lên
        if (groupJoinInput != null) groupJoinInput.SetActive(true);
    }

    public void OnClickCreateRoom()
    {
        _targetRoomName = GenerateRandomID();
        _targetMode = Fusion.GameMode.Host;

        SetupLobbyUI(_targetRoomName);
        ShowPanel(panelLobby);

        // [QUAN TRỌNG] Gọi kết nối ngay tại đây!
        if (basicSpawner != null)
        {
            basicSpawner.StartGameByKey(_targetMode, _targetRoomName);
        }
    }

    public void OnClickJoinRoom()
    {
        if (string.IsNullOrEmpty(inputRoomID.text)) return;

        _targetRoomName = inputRoomID.text.ToUpper();
        _targetMode = Fusion.GameMode.Client;

        SetupLobbyUI(_targetRoomName);
        ShowPanel(panelLobby);

        // [QUAN TRỌNG] Gọi kết nối ngay tại đây!
        if (basicSpawner != null)
        {
            basicSpawner.StartGameByKey(_targetMode, _targetRoomName);
        }
    }

    // --- 3. LOGIC VÀO GAME (LOBBY) ---

    public void OnClickStartGame()
    {
        string playerName = "Player";
        if (inputPlayerName != null && !string.IsNullOrEmpty(inputPlayerName.text))
        {
            playerName = inputPlayerName.text;
        }

        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        if (basicSpawner != null)
        {
            basicSpawner.StartGameByKey(_targetMode, _targetRoomName);
        }
    }


    private void ShowPanel(GameObject panelToShow)
    {
        panelTapToStart.SetActive(false);
        panelModeSelect.SetActive(false);
        panelConnection.SetActive(false);
        panelLobby.SetActive(false);

        if (panelToShow != null) panelToShow.SetActive(true);
    }

    private void SetupLobbyUI(string roomId)
    {
        if (textRoomIDDisplay != null) textRoomIDDisplay.text = "ROOM ID: " + roomId;
    }

    private string GenerateRandomID()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[4];
        for (int i = 0; i < 4; i++)
        {
            stringChars[i] = chars[Random.Range(0, chars.Length)];
        }
        return new string(stringChars);
    }

    // Tìm đến hàm UpdateLobbyList trong AppManager.cs
    public void UpdateLobbyList()
    {
        if (textPlayerList == null) return;

        string content = "";
        bool amIHost = basicSpawner.Runner != null && basicSpawner.Runner.IsServer;
        bool allReady = true;

        foreach (var p in LobbyPlayer.List)
        {
            string statusString = "";

            if (p.IsHost)
            {
                statusString = "<color=yellow>[HOST]</color>";
            }
            else
            {
                // Nếu là Khách -> Hiện Ready hoặc Wait
                if (p.IsReady)
                {
                    statusString = "<color=green>[READY]</color>";
                }
                else
                {
                    statusString = "<color=red>[WAIT]</color>";
                    allReady = false;
                }
            }

            content += $"{p.PlayerName} {statusString}\n";
        }

        textPlayerList.text = content;
        UpdateActionButton(amIHost, allReady);
    }

    private void UpdateActionButton(bool isHost, bool allReady)
    {
        if (btnAction == null) return;

        if (isHost)
        {
            textBtnAction.text = "BẮT ĐẦU GAME";
            // Chỉ cho bấm khi tất cả đã Ready (và phải có người khác ngoài mình, tùy bạn chọn)
            btnAction.interactable = allReady;
        }
        else
        {
            // Là khách
            // Tìm đối tượng LobbyPlayer của chính mình
            var myPlayer = LobbyPlayer.List.FirstOrDefault(x => x.Object.HasInputAuthority);
            if (myPlayer != null)
            {
                textBtnAction.text = myPlayer.IsReady ? "HỦY SẴN SÀNG" : "SẴN SÀNG";
            }
            btnAction.interactable = true;
        }
    }

    public void OnClickLobbyAction()
    {
        if (basicSpawner == null || basicSpawner.Runner == null)
        {
            Debug.LogError("Chưa kết nối mạng!");
            return;
        }

        // Tìm thẻ bài của chính mình
        var myPlayer = LobbyPlayer.List.FirstOrDefault(x => x.Object != null && x.Object.HasInputAuthority);

        if (myPlayer == null) return;

        string nameInInput = (inputPlayerName != null) ? inputPlayerName.text : "";
        if (!string.IsNullOrEmpty(nameInInput))
        {
            basicSpawner.TempPlayerName = nameInInput;
        }
        else
        {
            basicSpawner.TempPlayerName = myPlayer.PlayerName.ToString();
        }

        // --- XỬ LÝ CHO HOST (SERVER) ---
        if (basicSpawner.Runner.IsServer)
        {
            // Kiểm tra xem tên trong ô nhập có khác tên đang hiển thị không?
            // (ToString() là bắt buộc vì PlayerName là dạng NetworkString)
            if (!string.IsNullOrEmpty(nameInInput) && myPlayer.PlayerName.ToString() != nameInInput)
            {
                // TRƯỜNG HỢP 1: CẬP NHẬT TÊN TRƯỚC
                // Host tự sửa trực tiếp (nhanh hơn RPC)
                myPlayer.SetNameDirectly(nameInInput);

                Debug.Log("Host đã cập nhật tên. Bấm lần nữa để bắt đầu game.");
                return; // QUAN TRỌNG: Dừng lại ở đây, KHÔNG load scene ngay!
            }

            // TRƯỜNG HỢP 2: TÊN ĐÃ KHỚP -> BẮT ĐẦU GAME
            Debug.Log("Tên đã chốt, bắt đầu vào game!");
            basicSpawner.Runner.SessionInfo.IsOpen = false;
            basicSpawner.Runner.LoadScene(SceneRef.FromIndex(1));
        }
        // --- XỬ LÝ CHO CLIENT (KHÁCH) ---
        else
        {
            // Client thì cứ gửi RPC cập nhật tên như bình thường
            if (!string.IsNullOrEmpty(nameInInput))
            {
                myPlayer.RPC_SetName(nameInInput);
            }

            // Và đổi trạng thái Ready
            myPlayer.RPC_SetReady(!myPlayer.IsReady);
        }
    }
}