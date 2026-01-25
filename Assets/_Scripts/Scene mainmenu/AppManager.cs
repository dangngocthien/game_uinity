using Fusion;
using System.Collections.Generic;
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

    [Header("--- ROOM LIST UI ---")]
    [SerializeField] private GameObject panelRoomList;       //Panel_RoomList 
    [SerializeField] private Transform roomListContent;      //"Content" của ScrollView
    [SerializeField] private GameObject roomItemPrefab;      //Prefab RoomItemPrefab vào

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
        
        // ✅ FIX: Lấy BasicSpawner từ Singleton thay vì FindObjectOfType
        // Vì BasicSpawner dùng DontDestroyOnLoad nên phải lấy qua Instance
        if (basicSpawner == null)
        {
            basicSpawner = BasicSpawner.Instance;
        }
        
        // ✅ FALLBACK: Nếu vẫn null thì thử FindObjectOfType
        if (basicSpawner == null)
        {
            basicSpawner = FindObjectOfType<BasicSpawner>();
        }

        // Đảm bảo ô nhập tên trống trơn, để người chơi tự quyết định có nhập hay không
        if (inputPlayerName != null) inputPlayerName.text = "";
        
        // ✅ FIX: Clear LobbyPlayer list khi scene load lại
        // Vì các LobbyPlayer cũ đã bị destroy khi chuyển scene
        LobbyPlayer.List.Clear();
        
        Debug.Log($"[AppManager] Start - BasicSpawner: {(basicSpawner != null ? "Found" : "NULL")}");
    }

    // ✅ THÊM: Đảm bảo luôn có reference đến BasicSpawner
    private BasicSpawner GetSpawner()
    {
        if (basicSpawner == null)
        {
            basicSpawner = BasicSpawner.Instance;
        }
        if (basicSpawner == null)
        {
            basicSpawner = FindObjectOfType<BasicSpawner>();
        }
        return basicSpawner;
    }

    public void OnSuccessfullyJoinedSession()
    {
        // 1. Chuyển sang màn hình Lobby
        ShowPanel(panelLobby);

        // 2. Cập nhật cái tên phòng lên góc màn hình (nếu cần)
        var spawner = GetSpawner();
        if (spawner != null && spawner.Runner != null)
        {
            SetupLobbyUI(spawner.Runner.SessionInfo.Name);
        }
    }

    // Nút "TÌM PHÒNG" sẽ gọi hàm này
    public void OnClickOpenRoomList()
    {
        ShowPanel(panelRoomList);

        // Bảo Spawner kết nối vào Sảnh Chờ (Lobby) để bắt đầu nhận danh sách
        var spawner = GetSpawner();
        if (spawner != null)
        {
            spawner.JoinLobby();
        }
    }

    public void UpdateSessionListUI(List<SessionInfo> sessionList)
    {
        // 1. Dọn dẹp: Xóa sạch danh sách cũ
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // 2. Vẽ mới: Spawn thẻ cho từng phòng
        foreach (var session in sessionList)
        {
            // Bỏ qua các phòng ẩn hoặc phòng đã đóng
            if (!session.IsVisible || !session.IsOpen) continue;

            GameObject newItem = Instantiate(roomItemPrefab, roomListContent);

            // Điền thông tin vào thẻ
            if (newItem.TryGetComponent<RoomListEntry>(out var entry))
            {
                entry.SetInfo(session, GetSpawner());
            }
        }
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
        var spawner = GetSpawner();
        
        // 1. Nếu đang ở màn hình Connection (Màn hình chọn Tạo/Tìm/Nhập ID)
        if (panelConnection.activeSelf)
        {
            if (groupJoinInput != null && groupJoinInput.activeSelf)
            {
                groupJoinInput.SetActive(false);
                groupMainOptions.SetActive(true);
                return;
            }
            ShowPanel(panelModeSelect);
        }
        // 2. Nếu đang ở trong Lobby (Đã vào phòng chờ)
        else if (panelLobby.activeSelf)
        {
            // ✅ FIX: Disconnect khi rời lobby
            if (spawner != null)
            {
                spawner.LeaveLobby();
            }
            OnClickPlayOnline();
        }
        // 3. Nếu đang ở Danh Sách Phòng (Room List)
        else if (panelRoomList.activeSelf)
        {
            ShowPanel(panelConnection);

            if (spawner != null)
            {
                spawner.LeaveLobby();
            }
        }
        else
        {
            ShowPanel(panelTapToStart);
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
        var spawner = GetSpawner();
        if (spawner != null)
        {
            spawner.StartGameByKey(_targetMode, _targetRoomName);
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
        var spawner = GetSpawner();
        if (spawner != null)
        {
            spawner.StartGameByKey(_targetMode, _targetRoomName);
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

        var spawner = GetSpawner();
        if (spawner != null)
        {
            spawner.StartGameByKey(_targetMode, _targetRoomName);
        }
    }


    private void ShowPanel(GameObject panelToShow)
    {
        panelTapToStart.SetActive(false);
        panelModeSelect.SetActive(false);
        panelConnection.SetActive(false);
        panelLobby.SetActive(false);
        panelRoomList.SetActive(false);

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

        var spawner = GetSpawner();
        
        string content = "";
        bool amIHost = spawner != null && spawner.Runner != null && spawner.Runner.IsServer;
        bool allReady = true;

        // Xóa những thằng đã bị Destroy (null) ra khỏi danh sách trước khi vẽ
        LobbyPlayer.List.RemoveAll(x => x == null || x.Object == null);

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
            var myPlayer = LobbyPlayer.List.FirstOrDefault(x => x.Object != null && x.Object.HasInputAuthority);
            if (myPlayer != null)
            {
                textBtnAction.text = myPlayer.IsReady ? "HỦY SẴN SÀNG" : "SẴN SÀNG";
            }
            btnAction.interactable = true;
        }
    }

    public void OnClickLobbyAction()
    {
        var spawner = GetSpawner();
        
        // ✅ FIX: Kiểm tra kỹ hơn và log rõ ràng hơn
        if (spawner == null)
        {
            Debug.LogError("[AppManager] BasicSpawner is NULL!");
            return;
        }
        
        if (spawner.Runner == null)
        {
            Debug.LogError("[AppManager] Runner is NULL! Đang đợi kết nối...");
            // ✅ THÊM: Không return, có thể đang trong quá trình kết nối
            // Hiển thị thông báo cho user
            return;
        }
        
        if (!spawner.Runner.IsRunning)
        {
            Debug.LogError("[AppManager] Runner không đang chạy!");
            return;
        }

        // Tìm thẻ bài của chính mình
        var myPlayer = LobbyPlayer.List.FirstOrDefault(x => x.Object != null && x.Object.HasInputAuthority);

        if (myPlayer == null)
        {
            Debug.LogWarning("[AppManager] Chưa tìm thấy LobbyPlayer của mình, đang đợi spawn...");
            return;
        }

        string nameInInput = (inputPlayerName != null) ? inputPlayerName.text : "";
       
        // --- XỬ LÝ CHO HOST (SERVER) ---
        if (spawner.Runner.IsServer)
        {
            // Kiểm tra đổi tên (Logic cũ giữ nguyên)
            if (!string.IsNullOrEmpty(nameInInput) && myPlayer.PlayerName.ToString() != nameInInput)
            {
                myPlayer.SetNameDirectly(nameInInput);
                Debug.Log("Host đã cập nhật tên...");
                return;
            }

            // TRƯỜNG HỢP 2: TÊN ĐÃ KHỚP -> BẮT ĐẦU GAME
            Debug.Log("Tên đã chốt, bắt đầu vào game!");

            // --- THÊM DÒNG NÀY ---
            // Bảo Spawner: "Ghi lại tên của mọi người ngay đi, sắp chuyển cảnh rồi!"
            spawner.CachePlayerNames();
            // ---------------------

            spawner.Runner.SessionInfo.IsOpen = false;
            spawner.Runner.LoadScene(SceneRef.FromIndex(1));
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