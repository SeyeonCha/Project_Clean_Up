using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CompletingMonster : MonoBehaviourPun
{
    public GameObject monsterPrefab;       // 등장할 몬스터
    public Transform spawnPoint;           // 등장 위치

    public ParticleSystem explosionEffect; // 폭발 파티클
    public AudioSource explosionSfx;       // 폭발 사운드

    private GameManager gameManager;
    private bool alreadySpawned = false;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    // void Update()
    // {
    //     // 실험 완성되었고, 아직 폭발이 안 일어났다면
    //     if (gameManager != null && gameManager.ExperimentCompleted && !alreadySpawned)
    //     {
    //         StartCoroutine(SpawnMonsterSequence());
    //     }
    // }
    public void MonsterGenerate()
    {
        if (alreadySpawned) return;
        alreadySpawned = true;

        photonView.RPC("RpcSpawnMonsterSequence", RpcTarget.All);
    }
    [PunRPC]
    private void RpcSpawnMonsterSequence()
    {
        StartCoroutine(SpawnMonster());
    }

    IEnumerator SpawnMonster()
    {
        // 1. 폭발 파티클 재생
        if (explosionEffect != null)
            explosionEffect.Play();

        // 2. 폭발 사운드
        if (explosionSfx != null)
            explosionSfx.Play();

        // 3. 화면 흔들림
        CameraShake.Shake(0.2f, 0.4f);

        // 4. 잠깐 딜레이 후 몬스터 등장
        yield return new WaitForSeconds(0.25f);

        GameObject monster = PhotonNetwork.Instantiate(monsterPrefab.name,
                                spawnPoint.position,
                                Quaternion.identity);
    
        Animator anim = monster.GetComponent<Animator>();
        anim.SetTrigger("StartRise");

        Debug.Log("몬스터 등장!");
    }
}
