using UnityEngine;

public class AppSetup : MonoBehaviour
{
    void Awake()
    {
        // Mở khóa FPS lên 60
        Application.targetFrameRate = 60;

        // Tắt VSync (để không bị ép theo tần số quét màn hình nếu nó thấp)
        QualitySettings.vSyncCount = 0;

        // Giữ màn hình luôn sáng (không bị tắt khi đang chơi)
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}