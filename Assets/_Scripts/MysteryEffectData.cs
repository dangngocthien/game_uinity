using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum MysteryAction
{
    ModifyStat,
    ApplyStatus,
    ChangeBullet
}

[CreateAssetMenu(fileName = "NewMysteryItem", menuName = "Game/Mystery Item Data")]
public class MysteryItemData : ScriptableObject
{
    [Header("Thông tin chung")]
    public string itemData;
    public MysteryAction actionType;

    [Header("1. Dành cho Modifi Stat")]
    public int valueAmount;

    [Header("2. Dành cho Apply Status")]
    public float duration;// hiệu lực
    public bool isStun;// choáng
    public bool Inverted;// hiệu ứng đảo ngược

    [Header("3. Dành cho Change Bullet")]
    public NetworkPrefabRef bulletPrefab;
    public float bulletDuration;
}
