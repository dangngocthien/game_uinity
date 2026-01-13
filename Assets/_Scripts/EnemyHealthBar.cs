using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private HealthComponent healthComponent;

    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);

    private Quaternion _initialRotation;

    public override void Spawned()
    {
        //dây có phải là bản than không
        if(Object.HasInputAuthority)
        {
            canvas.enabled = false;
        }
        else
        {
            canvas.enabled = true;
        }

        // Lưu góc quay ban đầu
        _initialRotation = canvas.transform.rotation;
    }

    // Dùng LateUpdate để xử lý hình ảnh sau khi xe tăng đã di chuyển xong
    private void LateUpdate()
    {
        if (!canvas.enabled) return;

        if(healthComponent != null && healthComponent.MaxHealth > 0)
        {
            float hpPercent = (float)healthComponent.CurrentHealth / healthComponent.MaxHealth;

            fillImage.fillAmount = hpPercent;
        }

        canvas.transform.rotation = Quaternion.identity;

        canvas.transform.position = transform.position + offset;
    }
}
