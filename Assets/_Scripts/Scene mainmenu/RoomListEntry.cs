using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button joinButton;

    private string _sessionName;

    // Hàm này sẽ được AppManager gọi để điền thông tin
    public void SetInfo(SessionInfo session, BasicSpawner spawner)
    {
        _sessionName = session.Name;
        roomNameText.text = session.Name;
        playerCountText.text = $"{session.PlayerCount}/{session.MaxPlayers}";
        

        // Gài logic cho nút bấm: Nếu bấm -> Gọi spawner để vào phòng này
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() =>
        {
            Debug.Log($"Đã bấm nút vào phòng: {_sessionName}");

            if (spawner != null)
            {
                spawner.JoinSession(_sessionName);
            }
            else
            {
                Debug.LogError("Lỗi: Spawner bị Null, không gọi lệnh Join được!");
            }
        });

        // Nếu phòng đầy thì tắt nút Join đi
        joinButton.interactable = session.PlayerCount < session.MaxPlayers;
    }

}
