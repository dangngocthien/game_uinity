// Giữ nguyên các thư viện
using UnityEngine;
using Fusion;
using TMPro;
using System.Collections.Generic;

public class LobbyPlayer : NetworkBehaviour
{
    [Networked] public NetworkString<_16> PlayerName { get; set; }
    [Networked] public bool IsReady { get; set; }
    [Networked] public bool IsHost { get; set; }
    public static List<LobbyPlayer> List = new List<LobbyPlayer>();

    private string _lastName;
    private bool _lastReady;

    public override void Spawned()
    {
        List.Add(this);

        if (Object.HasInputAuthority)
        {
            // Logic đặt tên tạm lúc đầu (Code cũ)
            string tempName = "Guest " + Random.Range(1000, 9999);
            RPC_SetName(tempName);
        }
        UpdateUI();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        List.Remove(this);
        UpdateUI();
    }

    public override void Render()
    {
        string currentName = PlayerName.ToString();
        bool currentReady = IsReady;

        if (currentName != _lastName || currentReady != _lastReady)
        {
            UpdateUI();
            _lastName = currentName;
            _lastReady = currentReady;
        }
    }

    private void UpdateUI()
    {
        AppManager app = FindObjectOfType<AppManager>();
        if (app != null) app.UpdateLobbyList();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetName(string name)
    {
        this.PlayerName = name;
    }
    public void SetNameDirectly(string name)
    {
        // Vì Host nắm quyền StateAuthority, Host có thể gán thẳng biến Networked
        // Fusion sẽ tự đồng bộ cái này xuống các máy con
        this.PlayerName = name;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool ready)
    {
        this.IsReady = ready;
    }
}