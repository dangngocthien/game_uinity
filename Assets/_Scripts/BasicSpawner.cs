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

    private NetworkRunner _spawnedRunner;
    public string TempPlayerName;

    public async void StartGameByKey(GameMode mode, string roomName)
    {
        if (_spawnedRunner != null) Destroy(_spawnedRunner.gameObject);

        _spawnedRunner = gameObject.AddComponent<NetworkRunner>();
        _spawnedRunner.ProvideInput = true;

        // 2. Cấu hình Scene: Load Scene số 1 (GameMap)
        var sceneInfo = new NetworkSceneInfo();
        // Lưu ý: Đảm bảo Scene GameMap nằm ở index 1 trong Build Settings
        sceneInfo.AddSceneRef(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), LoadSceneMode.Single);

        // 3. Kết nối tới Photon Cloud
        await _spawnedRunner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName, // Dùng tên phòng (ID) được truyền vào
            Scene = sceneInfo,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        Debug.Log($"Đang kết nối vào phòng: {roomName} với chế độ {mode}");
    }

    // --- CÁC CALLBACKS (GIỮ NGUYÊN LOGIC CŨ) ---

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                // Spawn ra thẻ tên
                NetworkObject networkObj = runner.Spawn(lobbyPlayerPrefab, Vector3.zero, Quaternion.identity, player);
                Debug.Log($"Đã spawn LobbyPlayer cho: {player}");

                // --- SỬA ĐOẠN NÀY: KIỂM TRA & GÁN HOST ---
                if (networkObj.TryGetComponent<LobbyPlayer>(out var lobbyPlayer))
                {
                    if (player == runner.LocalPlayer)
                    {
                        lobbyPlayer.IsHost = true; // Đánh dấu đây là Chủ phòng
                    }
                    else
                    {
                        lobbyPlayer.IsHost = false; // Còn lại là Khách
                    }
                }
            }
        }
    }

    // Phần Input giữ nguyên để điều khiển nhân vật
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // CHẶN INPUT NẾU ĐANG Ở MENU (Logic bổ sung)
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            input.Set(data); // Gửi data rỗng
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        data.direction = new Vector2(x, y);
        data.isFire = Input.GetMouseButton(0);
        data.isDash = Input.GetKey(KeyCode.Space);

        input.Set(data);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Chỉ Server mới có quyền Spawn
        if (!runner.IsServer) return;

        // Kiểm tra xem Scene vừa load xong có phải là Scene Game (Index 1) không?
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            Debug.Log("Đã vào Scene Game! Bắt đầu spawn tàu chiến...");

            // Lặp qua tất cả người chơi đang kết nối để spawn tàu cho họ
            foreach (var player in runner.ActivePlayers)
            {
                SpawnShipForPlayer(runner, player);
            }
        }
    }

    private void SpawnShipForPlayer(NetworkRunner runner, PlayerRef player)
    {
        // Tìm điểm spawn
        Vector3 spawnPosition = Vector3.zero;
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

        if (spawnPoints.Length > 0)
        {
            // Tính vị trí dựa trên ID người chơi để không trùng
            int index = player.AsIndex % spawnPoints.Length;
            spawnPosition = spawnPoints[index].transform.position;
        }
        else
        {
            Debug.LogWarning("Chưa đặt điểm spawn (Tag: Respawn) ở Scene 1!");
        }

        // Spawn Tàu
        NetworkObject networkPlayer = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);

        // Set Skin
        if (networkPlayer.TryGetComponent<PlayerController>(out var pc))
        {
            pc.NetworkedSpriteIndex = player.AsIndex;
        }

        Debug.Log($"Đã spawn Tàu cho player: {player}");
    }

    // Các hàm bắt buộc khác của interface (để trống)
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}