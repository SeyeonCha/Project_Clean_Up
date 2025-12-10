using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Monster : MonoBehaviourPun
{
    public PhotonView PV;
     private Collider2D col;
    [SerializeField] private AttackRange attackRange;

    public float attackCooldown = 1.5f;
    private float timer = 0f;

    private bool canAttack = false;

    public int ownerId; // Photon ActorNumber 저장

    public void SetOwner(int id)
    {
        ownerId = id;

        attackRange.SetOwner(ownerId);
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return; // 공격 판정은 마스터만

        timer += Time.deltaTime;

        if (attackRange.OpponentInside && canAttack)
        {
            Attack();
            canAttack = false;
        }
        else if (!attackRange.OpponentInside)
        {
            canAttack = true;
        }
    }
    private void Attack()
    {
        Debug.Log("Monster attacks the opponent");

        photonView.RPC("RpcDamagePlayer", RpcTarget.All, ownerId);
    }
    [PunRPC]
    void RpcDamagePlayer(int attackerOwnerId)
    {
        PhotonView myPV = GetComponent<PhotonView>();

        col.GetComponent<PlayerMovement>().Hit();
        PV.RPC("DestroyRPC", RpcTarget.AllBuffered);

        // // 로컬플레이어의 ActorNumber 가 이 실험대의 주인이 아니면
        // if (PhotonNetwork.LocalPlayer.ActorNumber != attackerOwnerId)
        // {
        //     col.GetComponent<PlayerMovement>().Hit();
        //     PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        //     // 여기!!!!!!!!!!!!! "내(로컬 플레이어) 데미지 감소!"
            
        //     Debug.Log("------ 내 데미지 감소 ------");
        // }
    }

    // void OnTriggerEnter2D(Collider2D col)
    // {
    //     if (PhotonNetwork.LocalPlayer.ActorNumber != attackerOwnerId)
    //     {
    //         col.GetComponent<PlayerMovement>().Hit();
    //         PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
    //         // 여기!!!!!!!!!!!!! "내(로컬 플레이어) 데미지 감소!"
            
    //         Debug.Log("------ 내 데미지 감소 ------");
    //     }
    // }

}
