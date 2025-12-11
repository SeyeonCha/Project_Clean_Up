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

    private Experiment experimentTable;

    void Start()
    {
        experimentTable = GetComponent<Experiment>();
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

        // ⭐ 핵심 수정: Master Client만 RPC를 호출합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RpcSpawnMonsterSequence", RpcTarget.All);
        }
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

        GameObject monster = null;
        PhotonView monsterPV = null;
        int owner = experimentTable.ownerId;

        // ⭐ 핵심 수정: Master Client만 몬스터를 생성하고 RPC를 호출합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            monster = PhotonNetwork.Instantiate(monsterPrefab.name,
                                            spawnPoint.position,
                                            Quaternion.identity);

            monsterPV = monster.GetComponent<PhotonView>();

            // 몬스터에게 실험대 소유자 정보 전달 -> 같은 ownerId로 만들기
            if (monsterPV != null)
            {
                // Master Client가 생성 후, 모든 클라이언트에게 초기화 명령을 내립니다.
                photonView.RPC("RpcInitMonsterOwner", RpcTarget.All, monsterPV.ViewID, owner);
            }
        }
        
        // ⭐ 비마스터 클라이언트도 애니메이션을 재생할 수 있도록,
        // Animator와 애니메이션 트리거 코드는 Monster Prefab의 Start/Awake에 두는 것이 일반적입니다.
        
        // 하지만 현재는 Master Client가 생성 후 코드를 실행하므로,
        // Master Client가 애니메이션을 트리거하고, 이것이 PhotonView를 통해 동기화되도록 합니다.
        // (Monster Prefab에 Animator 동기화 설정이 되어 있다고 가정)
        if (monster != null)
        {
            Animator anim = monster.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("StartRise");
            }
        }


        Debug.Log("몬스터 등장!");
    }

    [PunRPC]
    void RpcInitMonsterOwner(int monsterViewID, int ownerID) // 생성한 monster에 ownerId 설정
    {
        PhotonView pv = PhotonView.Find(monsterViewID);
        if (pv == null) return;

        Monster monster = pv.GetComponent<Monster>();
        if (monster != null)
        {
            monster.SetOwner(ownerID);
            
            // ⭐ 애니메이션을 RPC 초기화 시점에서 실행하도록 보강 (선택 사항)
            Animator anim = monster.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("StartRise");
            }
        }
    }
}
