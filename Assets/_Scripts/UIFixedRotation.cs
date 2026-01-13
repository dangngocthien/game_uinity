// Dùng cái này NẾU cái trên bị lệch vị trí
using UnityEngine;

public class UIFixedRotation : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2.5f, 0); // Khoảng cách so với tàu (chỉnh số Y cho vừa)

    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;

        if (transform.parent != null)
        {
            transform.position = transform.parent.position + offset;
        }
    }
}