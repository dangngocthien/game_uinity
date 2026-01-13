using UnityEngine;

public class SmoothVisual : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform physicsTarget; // Cái xác vật lý (PlayerShip)
    public float smoothSpeed = 25f; // Tốc độ đuổi theo (Càng cao càng dính, 20-25 là đẹp)
    private Vector3 _currentVelocity;
    public float smoothTime = 0.05f;

    void LateUpdate() // LateUpdate chạy SAU CÙNG, ngay trước khi Camera vẽ hình -> Siêu mượt
    {
        if (physicsTarget == null)
        {
            Destroy(gameObject); // Chủ chết thì tớ cũng đi
            return;
        }

        // Kỹ thuật LERP: Lướt từ từ đến vị trí vật lý
        // Time.deltaTime giúp mượt trên mọi màn hình (60Hz, 144Hz đều ngon)
        Vector3 desiredPosition = physicsTarget.position;
        Quaternion desiredRotation = physicsTarget.rotation;

        //transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        transform.position = Vector3.SmoothDamp(transform.position, physicsTarget.position, ref _currentVelocity, smoothTime);

        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * smoothSpeed);
    }
}