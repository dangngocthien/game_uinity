using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1. Tạo một class con để lưu Item kèm theo tỷ lệ rơi
[System.Serializable]
public class MysteryDropItem
{
    public string name; // Đặt tên để dễ nhìn trong Inspector (không bắt buộc)
    public MysteryItemData itemData;

    [Range(0, 100)] // <--- Đây chính là thứ tạo ra THANH KÉO NGANG
    public float dropChance = 10f;
}


public class MysteryBox : NetworkBehaviour
{
    [Header("Cấu hình hộp quà")]
    // Thay đổi List cũ thành List mới chứa cả tỷ lệ
    [SerializeField] private List<MysteryDropItem> possibleItems;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!Object.HasStateAuthority) return;

        var player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            // Kiểm tra danh sách có item không
            if (possibleItems != null && possibleItems.Count > 0)
            {
                // Gọi hàm chọn item theo tỷ lệ
                MysteryItemData selectedItem = GetRandomItemByWeight();

                if (selectedItem != null)
                {
                    player.ApplyMysteryData(selectedItem);
                }
            }

            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayOpenChest();
            }

            Runner.Despawn(Object);
        }
    }

    // --- LOGIC CHỌN ITEM THEO TỶ LỆ (WEIGHTED RANDOM) ---
    private MysteryItemData GetRandomItemByWeight()
    {
        // Bước 1: Tính tổng trọng số (Total Weight)
        // Ví dụ: Item A (80) + Item B (20) => Tổng = 100
        float totalWeight = 0f;
        foreach (var item in possibleItems)
        {
            totalWeight += item.dropChance;
        }

        // Bước 2: Random một số từ 0 đến Tổng
        float randomValue = Random.Range(0f, totalWeight);

        // Bước 3: Duyệt qua từng item để xem số random rơi vào khoảng nào
        foreach (var item in possibleItems)
        {
            // Trừ dần giá trị random
            randomValue -= item.dropChance;

            // Nếu giá trị <= 0, nghĩa là đã trúng item này
            if (randomValue <= 0)
            {
                return item.itemData;
            }
        }

        // Fallback: Nếu có lỗi gì đó (ví dụ list rỗng), trả về item đầu tiên hoặc null
        return possibleItems[0].itemData;
    }
}