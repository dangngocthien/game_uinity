using UnityEngine;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance;

    public Vector2 MoveDirection { get; private set; }

    // Lưu trạng thái các nút hành động
    public bool IsFiring { get; private set; }
    public bool IsDashing { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Hàm để các nút gọi vào khi BỊ NHẤN
    public void SetHorizontal(float val) { MoveDirection = new Vector2(val, MoveDirection.y); }
    public void SetVertical(float val) { MoveDirection = new Vector2(MoveDirection.x, val); }

    // Hàm cho nút Bắn/Lướt
    public void SetFiring(bool state) => IsFiring = state;
    public void SetDashing(bool state) => IsDashing = state;
}