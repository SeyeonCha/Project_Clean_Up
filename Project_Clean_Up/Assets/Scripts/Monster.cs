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
        if (attackRange != null)
        {
            attackRange.SetOwner(ownerId); // ⭐ 여기서 SetOwner를 호출합니다.
        }
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

        PhotonView targetPV = attackRange.GetTargetPV(); 
        
        if (targetPV != null)
        {
            // ⭐ 핵심: Master Client는 공격 대상 플레이어에게 데미지 RPC를 호출합니다.
            // 공격 대상의 PhotonView ID와 몬스터의 주인 ID를 인수로 넘깁니다.
            photonView.RPC("RpcDamagePlayer", RpcTarget.All, targetPV.ViewID, ownerId);
        }
    }

    [PunRPC]
    void RpcDamagePlayer(int targetPlayerViewID, int attackerOwnerId)
    {
        // 1. 공격 대상 플레이어의 PhotonView를 찾습니다.
        PhotonView targetPV = PhotonView.Find(targetPlayerViewID);
        if (targetPV == null) return;

        // 2. 공격 대상이 로컬 플레이어인지 확인합니다.
        // 데미지 판정은 모든 클라이언트에서 실행됩니다.
        if (targetPV.Owner.ActorNumber == attackerOwnerId)
        {
            // 공격 대상이 몬스터와 같은 팀(실험대 주인)의 플레이어 -> 피해 없음
            Debug.Log($"Damage Ignored: Monster Owner ({attackerOwnerId}) hit their own team.");
            return;
        }

        // 3. 공격 대상이 상대방 플레이어일 때 피해를 입힙니다.
        // 'throwableobject'에서 참고한 패턴을 사용하여 피해를 입힙니다.
        // 이 피해는 'PlayerMovement' 스크립트에 구현된 'Hit' 함수를 호출해야 합니다.

        // 데미지 입는 대상의 PlayerMovement 스크립트를 가져옵니다.
        PlayerMovement playerMovement = targetPV.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            // PlayerMovement 스크립트의 RPC 함수인 'Hit'를 호출합니다.
            // (PlayerMovement.cs에 [PunRPC] void Hit() 함수가 구현되어 있다고 가정)
            targetPV.RPC("Hit", RpcTarget.All);
            
            Debug.Log($"------ 상대방 (Actor {targetPV.Owner.ActorNumber}) 데미지 감소! ------");
        }
        
        // 몬스터는 공격 후 파괴되는 것이 아니라, 일정 시간 후 사라지거나 계속 남아있어야 합니다.
        // PV.RPC("DestroyRPC", RpcTarget.AllBuffered); 코드는 제거합니다.
    }
}
