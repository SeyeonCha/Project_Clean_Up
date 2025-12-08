using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    public GameObject bombPrefab;       // 폭탄 프리팹
    public Vector2 spawnAreaMin;       // 스폰 가능한 영역 최소값
    public Vector2 spawnAreaMax;       // 스폰 가능한 영역 최대값
    public float minSpawnTime = 5f;    // 최소 스폰 대기 시간
    public float maxSpawnTime = 15f;   // 최대 스폰 대기 시간

    void Start()
    {
        // 코루틴을 시작하여 주기적으로 폭탄 스폰을 시도합니다.
        StartCoroutine(SpawnBombsRoutine());
    }

    IEnumerator SpawnBombsRoutine()
    {
        while (true)
        {
            // 1. 랜덤한 시간 대기
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            // 2. 랜덤한 위치 계산
            Vector2 randomPosition = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );

            // 3. 폭탄 생성
            Instantiate(bombPrefab, randomPosition, Quaternion.identity);
            Debug.Log($"폭탄 스폰됨: {randomPosition}");
        }
    }
}
