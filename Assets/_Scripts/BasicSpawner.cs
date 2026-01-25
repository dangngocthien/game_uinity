using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class BasicSpawner : SimulationBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    [Header("Lobby Settings")]
    [SerializeField] private NetworkPrefabRef lobbyPlayerPrefab;

    [Header("References")]
    [SerializeField] private AppManager appManager;

    [Header("Session Settings")]
    [Range(2, 16)]
    [SerializeField] private int maxPlayers = 4;

    private NetworkRunner _spawnedRunner;
    private Dictionary<PlayerRef, string> _playerNamesCache = new Dictionary<PlayerRef, string>();
    public new NetworkRunner Runner => _spawnedRunner;
    public static BasicSpawner Instance;

    // ✅ THÊM: Biến track trạng thái kết nối
    private bool _isConnecting = false;
    private bool _isInLobby = false;
    private bool _isInGame = false;

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

    /// <summary>
    /// ✅ FIX: Ngắt kết nối và reset toàn bộ trạng thái
    /// Được gọi khi bấm Back từ màn hình danh sách phòng
    /// </summary>
    public async void LeaveLobby()
    {
        Debug.Log("[LeaveLobby] Đang ngắt kết nối...");

        _isInLobby = false;
        _isInGame = false;
        _isConnecting = false;

        if (_spawnedRunner != null)
        {
            try
            {
                if (_spawnedRunner.IsRunning)
                {
                    await _spawnedRunner.Shutdown();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LeaveLobby] Exception khi Shutdown: {e.Message}");
            }

            // ✅ FIX: Xóa GameObject ngay lập tức
            if (_spawnedRunner.gameObject != null)
            {
                Destroy(_spawnedRunner.gameObject);
            }

            // ✅ CRITICAL: Set reference thành null để tránh ghost reference
            _spawnedRunner = null;
            
            Debug.Log("[LeaveLobby] ✅ Runner đã được reset!");
        }

        // ✅ Clear cache khi rời lobby
        _playerNamesCache.Clear();
    }

    /// <summary>
    /// ✅ FIX: Chỉ join vào sảnh chờ để ngóng tin (Chưa chơi)
    /// </summary>
    public async void JoinLobby()
    {
        // ✅ THÊM: Guard clause để tránh spam multiple connection attempts
        if (_isConnecting || _isInLobby)
        {
            Debug.LogWarning("[JoinLobby] ⚠️ Đang kết nối hoặc đã ở Lobby, không gọi lại!");
            return;
        }

        _isConnecting = true;
        Debug.Log("[JoinLobby] Đang tham gia Lobby...");

        try
        {
            // ✅ FIX: Cleanup runner cũ từ game nếu có
            if (_isInGame && _spawnedRunner != null && _spawnedRunner.IsRunning)
            {
                Debug.Log("[JoinLobby] Game vừa kết thúc, cleanup runner...");
                try
                {
                    await _spawnedRunner.Shutdown();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[JoinLobby] Exception khi Shutdown runner game: {e.Message}");
                }
                await System.Threading.Tasks.Task.Delay(200);

                if (_spawnedRunner.gameObject != null)
                {
                    Destroy(_spawnedRunner.gameObject);
                }
                _spawnedRunner = null;
            }

            _isInGame = false;
            CreateRunner();

            // ✅ FIX: Đợi CreateRunner hoàn tất
            await System.Threading.Tasks.Task.Delay(50);

            // Tham gia vào Lobby mặc định của Photon
            await _spawnedRunner.JoinSessionLobby(SessionLobby.ClientServer);
            
            _isInLobby = true;
            _isConnecting = false;

            Debug.Log("[JoinLobby] ✅ Đã tham gia Lobby thành công!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JoinLobby] ❌ Lỗi: {e.Message}");
            _isConnecting = false;
            _isInLobby = false;
            _isInGame = false;
            LeaveLobby();
        }
    }

    /// <summary>
    /// ✅ FIX: Được gọi bởi nút "VÀO" trên thẻ phòng
    /// </summary>
    public async void JoinSession(string roomName)
    {
        // ✅ THÊM: Guard clause
        if (_isConnecting)
        {
            Debug.LogWarning("[JoinSession] ⚠️ Đang kết nối, không gọi lại!");
            return;
        }

        _isConnecting = true;
        Debug.Log($"[JoinSession] Đang tham gia phòng: {roomName}");

        try
        {
            // ✅ FIX CRITICAL: Shutdown runner cũ (từ Lobby) TRƯỚC khi tạo runner mới
            if (_spawnedRunner != null && _spawnedRunner.IsRunning)
            {
                Debug.Log("[JoinSession] Đang shutdown runner cũ từ Lobby...");
                try
                {
                    await _spawnedRunner.Shutdown();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[JoinSession] Exception khi Shutdown: {e.Message}");
                }

                // ✅ FIX: Đợi đủ lâu để cleanup hoàn tất
                await System.Threading.Tasks.Task.Delay(200);
                
                if (_spawnedRunner.gameObject != null)
                {
                    Destroy(_spawnedRunner.gameObject);
                }
                _spawnedRunner = null;
            }

            _isInLobby = false;
            _isInGame = false;
            CreateRunner();

            // ✅ FIX: Đợi CreateRunner hoàn tất
            await System.Threading.Tasks.Task.Delay(50);

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), LoadSceneMode.Single);

            // ✅ FIX: Normalize room name (uppercase, trim)
            string normalizedRoomName = roomName.ToUpper().Trim();

            await _spawnedRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = normalizedRoomName,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            _isConnecting = false;
            _isInGame = true;
            Debug.Log($"[JoinSession] ✅ Đã tham gia phòng: {normalizedRoomName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JoinSession] ❌ Lỗi: {e.Message}");
            _isConnecting = false;
            _isInLobby = false;
            _isInGame = false;
            LeaveLobby();
        }
    }

    /// <summary>
    /// ✅ FIX: Callback được gọi mỗi khi danh sách phòng thay đổi
    /// </summary>
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log($"[OnSessionListUpdated] Cập nhật danh sách: {sessionList.Count} phòng");
        
        // ✅ FIX: Log các phòng available
        foreach (var session in sessionList)
        {
            if (session.IsVisible && session.IsOpen)
            {
                Debug.Log($"  - Room: {session.Name} | Players: {session.PlayerCount}/{session.MaxPlayers} | IsOpen: {session.IsOpen}");
            }
        }

        if (appManager == null)
        {
            appManager = FindObjectOfType<AppManager>();
        }

        if (appManager != null)
        {
            appManager.UpdateSessionListUI(sessionList);
        }
    }

    /// <summary>
    /// ✅ FIX CRITICAL: Tạo hoặc reset Runner
    /// </summary>
    private void CreateRunner()
    {
        Debug.Log("[CreateRunner] Đang tạo/reset Runner...");

        // ✅ FIX: Kiểm tra và cleanup runner cũ
        if (_spawnedRunner != null)
        {
            try
            {
                // Nếu runner vẫn chạy, shutdown trước
                if (_spawnedRunner.IsRunning)
                {
                    // Không await ở đây - chỉ gọi shutdown
                    _spawnedRunner.Shutdown(true);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CreateRunner] Exception khi Shutdown runner cũ: {e.Message}");
            }

            // ✅ CRITICAL: Xóa GameObject
            if (_spawnedRunner.gameObject != null)
            {
                Destroy(_spawnedRunner.gameObject);
            }

            // ✅ CRITICAL: Xóa reference
            _spawnedRunner = null;
            
            Debug.Log("[CreateRunner] Đã cleanup runner cũ, đang tạo runner mới...");
        }

        // Tạo GameObject mới
        GameObject runnerObj = new GameObject("SessionRunner");
        runnerObj.transform.SetParent(transform);

        // Thêm NetworkRunner component
        _spawnedRunner = runnerObj.AddComponent<NetworkRunner>();
        _spawnedRunner.ProvideInput = true;

        // ✅ CRITICAL: Đăng ký callbacks
        _spawnedRunner.AddCallbacks(this);

        Debug.Log("[CreateRunner] ✅ Runner mới được tạo thành công!");
    }

    public void CachePlayerNames()
    {
        _playerNamesCache.Clear();

        foreach (var lobbyPlayer in LobbyPlayer.List)
        {
            if (lobbyPlayer != null && lobbyPlayer.Object != null)
            {
                _playerNamesCache[lobbyPlayer.Object.InputAuthority] = lobbyPlayer.PlayerName.ToString();
            }
        }
        Debug.Log($"[CachePlayerNames] ✅ Đã lưu {_playerNamesCache.Count} người chơi!");
    }

    /// <summary>
    /// ✅ FIX: Bắt đầu game (Host tạo phòng hoặc Client join)
    /// </summary>
    public async void StartGameByKey(GameMode mode, string roomName)
    {
        // ✅ THÊM: Guard clause
        if (_isConnecting)
        {
            Debug.LogWarning("[StartGameByKey] ⚠️ Đang kết nối, không gọi lại!");
            return;
        }

        _isConnecting = true;
        Debug.Log($"[StartGameByKey] Bắt đầu game - Mode: {mode}, Room: {roomName}");

        try
        {
            // ✅ FIX: Nếu đang ở Lobby, shutdown trước
            if (_isInLobby && _spawnedRunner != null && _spawnedRunner.IsRunning)
            {
                Debug.Log("[StartGameByKey] Đang shutdown runner Lobby...");
                try
                {
                    await _spawnedRunner.Shutdown();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[StartGameByKey] Exception: {e.Message}");
                }
                await System.Threading.Tasks.Task.Delay(200);

                if (_spawnedRunner.gameObject != null)
                {
                    Destroy(_spawnedRunner.gameObject);
                }
                _spawnedRunner = null;
            }

            _isInLobby = false;
            _isInGame = false;
            CreateRunner();

            // ✅ FIX: Đợi CreateRunner hoàn tất
            await System.Threading.Tasks.Task.Delay(50);

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), LoadSceneMode.Single);

            // ✅ FIX: Normalize room name
            string normalizedRoomName = roomName.ToUpper().Trim();

            await _spawnedRunner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = normalizedRoomName,
                Scene = sceneInfo,
                PlayerCount = maxPlayers,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            _isConnecting = false;
            _isInGame = true;
            Debug.Log($"[StartGameByKey] ✅ Đã tham gia: {normalizedRoomName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StartGameByKey] ❌ Lỗi: {e.Message}");
            _isConnecting = false;
            _isInLobby = false;
            _isInGame = false;
            LeaveLobby();
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[OnPlayerJoined] Player join: {player}");

        if (runner.IsServer)
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                NetworkObject networkObj = runner.Spawn(lobbyPlayerPrefab, Vector3.zero, Quaternion.identity, player);
                Debug.Log($"[OnPlayerJoined] Spawn LobbyPlayer: {player}");

                if (networkObj.TryGetComponent<LobbyPlayer>(out var lobbyPlayer))
                {
                    lobbyPlayer.IsHost = (player == runner.LocalPlayer);
                }
            }
        }

        if (player == runner.LocalPlayer)
        {
            if (appManager == null) appManager = FindObjectOfType<AppManager>();

            if (appManager != null)
            {
                appManager.OnSuccessfullyJoinedSession();
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            input.Set(data);
            return;
        }

        float x = 0;
        float y = 0;
        bool isFire = false;
        bool isDash = false;

        if (MobileInputManager.Instance != null)
        {
            if (MobileInputManager.Instance.MoveDirection != Vector2.zero)
            {
                x = MobileInputManager.Instance.MoveDirection.x;
                y = MobileInputManager.Instance.MoveDirection.y;
            }

            if (MobileInputManager.Instance.IsFiring) isFire = true;

            if (MobileInputManager.Instance.IsDashing)
            {
                isDash = true;
                MobileInputManager.Instance.SetDashing(false);
            }
        }

        if (!Application.isMobilePlatform || Application.isEditor)
        {
            if (x == 0) x = Input.GetAxisRaw("Horizontal");
            if (y == 0) y = Input.GetAxisRaw("Vertical");

            if (!isFire) isFire = Input.GetMouseButton(0);
            if (!isDash) isDash = Input.GetKey(KeyCode.Space);
        }

        data.direction = new Vector2(x, y);
        data.isFire = isFire;
        data.isDash = isDash;

        input.Set(data);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            Debug.Log("[OnSceneLoadDone] ✅ Load Game Scene xong! Spawn tàu...");

            foreach (var player in runner.ActivePlayers)
            {
                SpawnShipForPlayer(runner, player);
            }

            AudioManager.Instance.StopAudio();
        }
    }

    private void SpawnShipForPlayer(NetworkRunner runner, PlayerRef player)
    {
        Vector3 spawnPosition = Vector3.zero;
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

        if (spawnPoints.Length > 0)
        {
            int index = player.AsIndex % spawnPoints.Length;
            spawnPosition = spawnPoints[index].transform.position;
        }

        NetworkObject networkPlayer = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

        if (networkPlayer.TryGetComponent<PlayerController>(out var pc))
        {
            pc.NetworkedSpriteIndex = player.AsIndex;

            if (_playerNamesCache.ContainsKey(player))
            {
                pc.NickName = _playerNamesCache[player];
            }
            else
            {
                pc.NickName = $"Player {player.AsIndex}";
            }
        }

        Debug.Log($"[SpawnShipForPlayer] ✅ Spawn tàu: {player}");
    }

    // ✅ FIX: Implement OnShutdown để cleanup khi Runner tắt
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[OnShutdown] Runner shutdown - Reason: {shutdownReason}");

        // ✅ FIX: Reset tất cả flags khi runner shutdown
        _isConnecting = false;
        _isInGame = false;
        
        // ⚠️ IMPORTANT: Nếu shutdown từ Game Scene, đó là game kết thúc
        // Không reset _isInLobby ở đây vì client có thể quay lại lobby
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            Debug.Log("[OnShutdown] Game scene shutdown -> Reset game state");
            _isInGame = false;
        }
        
        // Chỉ log error nếu không phải expected errors
        if (shutdownReason != ShutdownReason.Ok && 
            shutdownReason != ShutdownReason.GameNotFound &&
            shutdownReason != ShutdownReason.GameClosed)
        {
            Debug.LogError($"[OnShutdown] ❌ Shutdown với lỗi: {shutdownReason}");
        }
        else
        {
            Debug.Log($"[OnShutdown] ✅ Normal shutdown: {shutdownReason}");
        }
    }

    // Các callback khác (giữ nguyên)
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    { 
        Debug.Log($"[OnPlayerLeft] Player left: {player}");
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) 
    { 
        Debug.Log("[OnConnectedToServer] ✅ Kết nối server thành công!");
        _isConnecting = false;
    }
    
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) 
    { 
        Debug.LogError($"[OnDisconnectedFromServer] ❌ Mất kết nối: {reason}");
        _isConnecting = false;
        _isInLobby = false;
        _isInGame = false;
    }
    
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) 
    { 
        Debug.LogError($"[OnConnectFailed] ❌ Kết nối thất bại: {reason}");
        _isConnecting = false;
        _isInLobby = false;
        _isInGame = false;
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}