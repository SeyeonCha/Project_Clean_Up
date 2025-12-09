using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ExperimentButton : MonoBehaviourPun
{
    private GameManager gameManager;

    [SerializeField] private Sprite button_unpressed; 
    [SerializeField] private Sprite button_pressed;
    
    public Experiment experimentTable;

    private SpriteRenderer spriteRenderer;

    private bool isPressed = false;
    
    void Start()
    {
        // 씬에서 GameManager를 찾습니다.
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager 스크립트를 찾을 수 없습니다! 게임이 정상 작동하지 않을 수 있습니다.");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = button_unpressed;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isPressed) return; // 중복 입력 방지!

        photonView.RPC("RpcPressButton", RpcTarget.All);
        
    }
    [PunRPC]
    public void RpcPressButton()
    {
        if (isPressed) return;

        isPressed = true;
        spriteRenderer.sprite = button_pressed;

        // gameManager.ExperimentCompleted = true;
        experimentTable.CompleteExperiment();
        Debug.Log($"🔵 Experiment Button Pressed & Synced Across Network!");
    }

}
