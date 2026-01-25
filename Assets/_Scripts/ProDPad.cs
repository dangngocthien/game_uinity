using UnityEngine;
using UnityEngine.EventSystems;

// Script này gắn vào cái PANEL CHA chứa 4 nút mũi tên
public class ProDPad : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Cài đặt")]
    [SerializeField] private float range = 100f; // Bán kính hoạt động
    [SerializeField] private float deadZone = 0.2f; // Vùng chết ở giữa tâm

    private RectTransform rectTransform;
    private Vector2 inputVector;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CalculateInput(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        CalculateInput(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        SendInputToManager();
    }

    // 3. Logic tính toán toán học
    private void CalculateInput(PointerEventData eventData)
    {
        Vector2 localPoint;
        // Đổi từ tọa độ màn hình sang tọa độ của cái khung DPad
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            // Chuẩn hóa vị trí theo kích thước (để giá trị từ -1 đến 1)
            // Giả sử Pivot nằm ở giữa (0.5, 0.5)
            Vector2 normalizedPoint = new Vector2(localPoint.x / (rectTransform.rect.width / 2), localPoint.y / (rectTransform.rect.height / 2));

            inputVector = normalizedPoint;

            // Giới hạn độ dài vector không quá 1 (để chéo không chạy nhanh hơn thẳng)
            if (inputVector.magnitude > 1) inputVector = inputVector.normalized;

            // Xử lý Deadzone (nếu chạm quá gần tâm thì không tính)
            if (inputVector.magnitude < deadZone) inputVector = Vector2.zero;

            SendInputToManager();
        }
    }

    // 4. Gửi dữ liệu sang MobileInputManager
    private void SendInputToManager()
    {
        if (MobileInputManager.Instance != null)
        {

            float x = 0;
            float y = 0;

            float threshold = 0.3f;

            if (inputVector.x > threshold) x = 1;       // Phải
            else if (inputVector.x < -threshold) x = -1; // Trái

            if (inputVector.y > threshold) y = 1;       // Lên
            else if (inputVector.y < -threshold) y = -1; // Xuống

            MobileInputManager.Instance.SetHorizontal(x);
            MobileInputManager.Instance.SetVertical(y);
        }
    }
}