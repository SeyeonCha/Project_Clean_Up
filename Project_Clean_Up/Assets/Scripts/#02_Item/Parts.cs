// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Photon.Pun;

// public class Parts : MonoBehaviourPun
// {
//     public int partId;
//     private SpriteRenderer sr;

//     void Awake()
//     {
//         sr = GetComponent<SpriteRenderer>();
//     }

//     [PunRPC]
//     public void RpcSetPartData(int id, int spriteIndex)
//     {
//         partId = id;
//         if (sr == null) sr = GetComponent<SpriteRenderer>();
//         // sr.sprite = GameManager.Instance.partsImages[spriteIndex];
//     }

// }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Parts : MonoBehaviourPun
{
    // InGameManager의 RPC에서 직접 할당할 public 변수
    public int partId; 
    
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // ⭐ 1. InGameManager의 RPC에서 이 함수를 호출하여 Sprite를 설정하도록 합니다.
    public void SetPartSprite(int spriteIndex)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // InGameManager의 Instance를 통해 partsImages 배열에 접근합니다.
        if (InGameManager.Instance != null && InGameManager.Instance.partsImages != null && 
            spriteIndex >= 0 && spriteIndex < InGameManager.Instance.partsImages.Length)
        {
            sr.sprite = InGameManager.Instance.partsImages[spriteIndex];
        }
        else
        {
            Debug.LogError($"Parts: Sprite Index {spriteIndex}가 유효하지 않거나 InGameManager가 없습니다.");
        }
    }
}