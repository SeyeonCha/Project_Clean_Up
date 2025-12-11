using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// 이 스크립트는 몬스터의 공격 범위 콜라이더에 붙어 있어야 합니다.
public class AttackRange : MonoBehaviour
{
    private int monsterOwnerId; // 몬스터의 주인 ActorNumber
    private PhotonView targetPlayerPV = null; // 현재 범위 내의 상대방 플레이어 PV
    private SpriteRenderer sr;

    public bool OpponentInside => targetPlayerPV != null; // 몬스터가 공격할 수 있는 상태인지 반환

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void SetOwner(int id)
    {
        monsterOwnerId = id;
        float transparency = 0.4f;

        // ⭐ 핵심 수정: 로컬 플레이어의 액터 넘버와 비교하여 색상 설정
        if (sr != null)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == monsterOwnerId)
            {
                // 로컬 플레이어의 몬스터: 아군 표시 색상 (예: 녹색/흰색)
                sr.color = Color.green;
                sr.color = new Color(0f, 1f, 0f, transparency);
            }
            else
            {
                // 상대방의 몬스터: 적군 표시 색상 (예: 빨간색/파란색)
                sr.color = Color.red;
                sr.color = new Color(1f, 0f, 0f, transparency);
            }
        }
    }

    // ⭐ 몬스터 스크립트에서 호출되는 함수 (문제 해결)
    public PhotonView GetTargetPV()
    {
        return targetPlayerPV;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PhotonView playerPV = other.GetComponent<PhotonView>();
        if (playerPV == null) return;
        
        // 범위 내 플레이어가 몬스터의 주인과 다를 때 (상대방일 때) 저장
        if (playerPV.Owner.ActorNumber != monsterOwnerId)
        {
            targetPlayerPV = playerPV;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        PhotonView playerPV = other.GetComponent<PhotonView>();
        if (playerPV == null) return;
        
        // 범위 밖으로 나간 플레이어가 저장된 타겟이라면 제거
        if (playerPV == targetPlayerPV)
        {
            targetPlayerPV = null;
        }
    }
}