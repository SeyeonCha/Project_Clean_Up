using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI 네임스페이스는 SpriteRenderer와 관련 없으므로, 제거해도 무방하나 일단 유지
using Photon.Pun;

public class ExperimentButton : MonoBehaviourPun
{
    // private GameManager gameManager; // ⭐ 제거: GameManager 참조 제거
    
    [SerializeField] private Sprite button_unpressed; 
    [SerializeField] private Sprite button_pressed;
    
    // ⭐ Experiment Desk 참조 (Inspector에서 연결되어야 함)
    public Experiment experimentTable; 

    private SpriteRenderer spriteRenderer;

    private bool isPressed = false;
    
    void Start()
    {
        // ⭐ GameManager 참조 로직 제거
        if (InGameManager.Instance == null)
        {
            Debug.LogError("InGameManager 인스턴스를 찾을 수 없습니다! 게임이 정상 작동하지 않을 수 있습니다.");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        // 버튼은 Experiment 스크립트에서 제어되므로, 초기 상태는 해당 스크립트가 관리
        if (spriteRenderer != null && button_unpressed != null)
        {
            spriteRenderer.sprite = button_unpressed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) {
            // Debug.Log("DEBUG: Tag Not Player"); // 너무 자주 찍힐 수 있음
            return;
        }
        if (isPressed) {
            Debug.Log("DEBUG: Button is already pressed.");
            return;
        }
        
        // 게임 활성화 상태 확인
        if (InGameManager.Instance == null || !InGameManager.Instance.IsMovementAllowed()) {
            Debug.Log("DEBUG: Game is paused or inactive.");
            return;
        }

        PhotonView playerPV = other.GetComponent<PhotonView>();
        if (playerPV == null) {
            Debug.LogError("DEBUG: Player PV is null!");
            return;
        }

        // 1. 충돌한 플레이어가 로컬 플레이어인지 확인
        if (!playerPV.IsMine) {
            Debug.Log("DEBUG: Not my local player (Ignore).");
            return; 
        }
        
        // 2. Desk 소유자 확인
        if (experimentTable == null) {
            Debug.LogError("DEBUG: ExperimentTable is not linked!");
            return;
        }

        int localActorId = playerPV.Owner.ActorNumber;
        int deskOwnerId = experimentTable.ownerId;
        
        // ⭐⭐ 핵심 확인: 소유자가 일치하는가? ⭐⭐
        if (localActorId == deskOwnerId) 
        {
            Debug.Log($"SUCCESS! Local Player ({localActorId}) is Desk Owner. Calling RPC.");
            photonView.RPC("RpcPressButton", RpcTarget.All);
        }
        else
        {
            Debug.Log($"DEBUG: Failed. Local Player ({localActorId}) is NOT Desk Owner ({deskOwnerId}).");
        }
    }
    
    [PunRPC]
    public void RpcPressButton()
    {
        // 이 RPC는 모든 클라이언트에서 실행되어야 하므로, isPressed 체크를 먼저 수행합니다.
        if (isPressed) return;

        isPressed = true;
        
        // 1. 시각적 피드백 동기화
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = button_pressed;
        }

        // 2. 실험 완료 로직 실행 (Experiment 스크립트의 함수를 호출하여 네트워크 동기화)
        if (experimentTable != null)
        {
            experimentTable.CompleteExperiment();
        }
        
        // 3. 버튼 오브젝트를 비활성화하여 중복 사용을 완전히 방지합니다.
        gameObject.SetActive(false); 
        
        Debug.Log($"🔵 Experiment Button Pressed & Synced Across Network! (Completed by Desk {experimentTable.ownerId})");
    }

}