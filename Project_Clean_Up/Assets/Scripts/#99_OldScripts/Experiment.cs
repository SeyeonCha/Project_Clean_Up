using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

// 실험대에 붙을 스크립트
public class Experiment : MonoBehaviourPun
{
    public int ownerId = -1; // 현재 Desk의 주인 플레이어 ActorNumber

    // ----- 필드
    [SerializeField]
    public int requiredParts = 4; // 해당 실험대가 필요로 하는 총 파츠 개수
    public GameObject experimentButton; // 이 실험대의 완성버튼

    [Header("UI Reference")]
    public TMP_Text partsCountText;

    private CompletingMonster monster;

    private enum TableState {collecting, completed} // 각 실험대의 상태
    private TableState state = TableState.collecting;

    public HashSet<int> collectedPartIds = new HashSet<int>();

    // ⭐ SetOwner 함수 수정: InGameManager에서 Desk를 생성할 때 호출됩니다.
    // Desk는 생성될 때 해당 플레이어의 소유로 설정되어야 합니다.
    public void SetOwner(Player player)
    {
        ownerId = player.ActorNumber;

        // 1. Desk의 소유권을 해당 플레이어에게 변경
        photonView.TransferOwnership(player);

        // 2. ⭐ 핵심: Master Client는 모든 클라이언트에게 색상을 설정하라고 명령하는 RPC를 전송합니다.
        // 이 RPC는 모든 클라이언트가 실행하며, 자신의 Desk와 상대방 Desk를 구분하여 색상을 적용합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RpcSetDeskColor", RpcTarget.All, player.ActorNumber);
        }
    }
    
    void Start()
    {
        if (experimentButton != null)
        {
            experimentButton.SetActive(false);
        }
            
        monster = GetComponent<CompletingMonster>();

        if (InGameManager.Instance == null)
        {
            Debug.LogError("InGameManager 인스턴스를 찾을 수 없습니다! 게임이 정상 작동하지 않을 수 있습니다.");
        }
        
        // 초기 텍스트 설정 (RPC가 오기 전까지의 안전 장치)
        if (partsCountText != null)
        {
            partsCountText.text = "0 / " + requiredParts.ToString();
        }
    }

    
    // 부품이 실험대에 들어왔는지 감지
    void OnTriggerEnter2D(Collider2D other)
    {
        // ⭐ 로컬 플레이어의 Desk가 아니거나, 게임이 활성화 상태가 아니면 리턴
        if (photonView.Owner != PhotonNetwork.LocalPlayer) return;
        if (InGameManager.Instance == null || !InGameManager.Instance.IsMovementAllowed()) return;
        
        if (!other.CompareTag("parts")) return;

        Parts p = other.gameObject.GetComponent<Parts>();
        if (p!= null)
        {
            Debug.Log($"파츠 {p.partId}가 로컬 플레이어의 Desk에 수집됨!");
            
            // ⭐ Master Client에게 파츠 수집 및 파괴를 요청합니다.
            // RpcTarget.MasterClient 대신, 이 Desk의 PV를 사용하여 Master Client에게 요청합니다.
            photonView.RPC("RpcCollectPart", RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID, p.partId);
        }
    }

    [PunRPC]
    public void RpcCollectPart(int partViewID, int partId)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (InGameManager.Instance == null || InGameManager.Instance.IsMovementAllowed() == false) return;

        if (collectedPartIds.Add(partId))
        {
            Debug.Log($"[Experiment {ownerId}] 파츠 {partId} 수집됨 (Unique count: {collectedPartIds.Count})");

            PhotonView PV = PhotonView.Find(partViewID);
            if (PV != null) PhotonNetwork.Destroy(PV.gameObject);
            
            if (InGameManager.Instance != null)
            {
                InGameManager.Instance.PartCollected(); 
            }
            
            // ⭐ 수집 후 모든 클라이언트에게 새로운 카운트를 전달합니다.
            photonView.RPC("RpcUpdatePartsText", RpcTarget.All, collectedPartIds.Count); 

            if (collectedPartIds.Count >= requiredParts)
            {
                photonView.RPC("RpcActivateButton", RpcTarget.All);
            }
        }
        else
        {
            Debug.Log($"[Experiment {ownerId}] 파츠 {partId}는 이미 있음 → 무시");
        }
    }

    // ⭐ 3. 텍스트 카운트를 업데이트하는 RPC 함수 추가
    [PunRPC]
    public void RpcUpdatePartsText(int currentCount)
    {
        if (partsCountText != null)
        {
            partsCountText.text = currentCount.ToString() + " / " + requiredParts.ToString();
        }
    }

    // ⭐ 4. Desk 색상 설정 RPC 함수 수정: 텍스트 색상도 함께 설정
    [PunRPC]
    public void RpcSetDeskColor(int deskOwnerActorNumber)
    {
        bool isMine = (deskOwnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        
        // Desk SpriteRenderer 색상 설정
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // 로컬 플레이어의 Desk는 노란색, 상대방의 Desk는 흰색
        Color deskColor = isMine ? new Color(1f, 1f, 0.4f) : Color.white; 
        
        if (sr != null)
        {
            sr.color = deskColor;
        }

        // ⭐ 텍스트 색상 설정
        // 배경색과 대비되도록 (노란색 배경이더라도 잘 보이도록 검은색으로 통일하는 것이 일반적입니다.)
        Color textColor = isMine ? new Color(1f, 1f, 0.4f) : Color.black; 
        if (partsCountText != null)
        {
            partsCountText.color = textColor;
        }
        
        Debug.Log($"Desk color set for Owner: {deskOwnerActorNumber} (IsMine: {isMine})");
    }

    [PunRPC]
    public void RpcActivateButton()
    {
        // 모든 클라이언트에서 버튼 활성화
        if (experimentButton != null)
        {
            experimentButton.SetActive(true);
        }
    }
    
    // 완성 버튼 클릭 시 호출될 함수 (UI 연결)
    public void CompleteExperiment()
    {
        // ⭐ 로컬 플레이어가 버튼을 눌렀는지 확인 (해당 Desk의 소유자만 가능)
        if (photonView.Owner != PhotonNetwork.LocalPlayer)
        {
            Debug.LogWarning("자신의 Desk가 아닙니다. 실험을 완료할 수 없습니다.");
            return;
        }

        photonView.RPC("RpcCompleteExperiment", RpcTarget.All);
    }
    
    [PunRPC]
    private void RpcCompleteExperiment() // 활성화된 버튼이 소유 플레이어와 충돌하면 완성. 
    {
        if (state == TableState.completed) return;

        state = TableState.completed;
        if (monster != null)
        {
            // CompletingMonster 스크립트에 몬스터 생성 로직이 있다고 가정
            monster.MonsterGenerate(); 
        }
        Debug.Log($"🧪 Experiment {ownerId} Completed & Monster Spawned (Synced)");
        
        // 버튼 비활성화 (한 번만 완료되도록)
        if (experimentButton != null)
        {
            experimentButton.SetActive(false);
        }
    }
}