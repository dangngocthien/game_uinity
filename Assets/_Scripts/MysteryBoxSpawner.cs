using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class MysteryBoxSpawner : NetworkBehaviour
{
    [Header("Cấu hình")]
    [SerializeField] private NetworkPrefabRef mysteryBoxPrefab;
    [SerializeField] private float spawnTime = 15f;

    [Header("Vị trí spawn")]
    [SerializeField] private Transform[] spawnPoints;

    [Networked] TickTimer spawnTimer {  get; set; }

    public override void Spawned()
    {
        if(Object.HasStateAuthority)
        {
            spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if(spawnTimer.Expired(Runner))
        {
            SpawnBox();

            // Đặt lại đồng hồ đếm ngược cho lần sau
            spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnTime);
        }
    }

    private void SpawnBox()
    {
        if (spawnPoints == null && spawnPoints.Length == 0) return;

        int index = Random.Range(0, spawnPoints.Length);
        Vector3 spawnPos = spawnPoints[index].position;

        Runner.Spawn(mysteryBoxPrefab,spawnPos,Quaternion.identity,null);
    }
}
