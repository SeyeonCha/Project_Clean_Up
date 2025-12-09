using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Parts : MonoBehaviourPun
{
    public int partId;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    [PunRPC]
    public void RpcSetPartData(int id, int spriteIndex)
    {
        partId = id;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.sprite = GameManager.Instance.partsImages[spriteIndex];
    }

}
