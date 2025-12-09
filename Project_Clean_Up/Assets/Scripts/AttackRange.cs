using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AttackRange : MonoBehaviourPun
{
    [SerializeField] private Monster monster;
    
    public bool OpponentInside = false;

    public int ownerId; // Photon ActorNumber 저장

    public void SetOwner(int id)
    {
        ownerId = id;
        Debug.Log($"AttackRange OwnerId : {ownerId}");
        bool isMine = (ownerId == PhotonNetwork.LocalPlayer.ActorNumber);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = isMine ? Color.green : Color.red;
            c.a = 0.25f; 

            sr.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        // 충돌한 플레이어의 pv 가져오기
        PhotonView pv = other.GetComponent<PhotonView>();

        // 충돌한 플레이어가 자신이 아니면
        if (pv.Owner.ActorNumber != monster.ownerId) 
        {
            OpponentInside = true;
        }
        
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv.Owner.ActorNumber != monster.ownerId)
        {
            OpponentInside = false;
        }
        
    }
}
