using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// 실험대에 붙을 스크립트

public class Experiment : MonoBehaviourPun
{
    private GameManager gameManager;

    // ----- 필드
    [SerializeField]
    private int ownerId; // 0, 1로 설정해두기
    public int requiredParts = 3; // 해당 실험대가 필요로 하는 총 파츠 개수
    public GameObject experimentButton; // 이 실험대의 완성버튼

    public CompletingMonster monster;

    private int partsCollected = 0; // 각 실험대가 모은 파츠 개수 저장. 

    private enum TableState {collecting, completed} // 각 실험대의 상태
    private TableState state = TableState.collecting;

    void Start()
    {
        if (experimentButton != null)
        {
            experimentButton.SetActive(false);
        }
            
        monster = GetComponent<CompletingMonster>();

        
        // 씬에서 GameManager를 찾습니다.
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager 스크립트를 찾을 수 없습니다! 게임이 정상 작동하지 않을 수 있습니다.");
        }
    }

    
    // 부품이 실험대에 들어왔는지 감지
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("parts")) return;
        if (!gameManager.IsGameActive()) return;

        Parts p = other.gameObject.GetComponent<Parts>();
        if (p!= null && p.ownerId == ownerId)
        {
            Debug.Log($"[Experiment {ownerId}] Correct part collected!");
            // gameManager.PartsCollected(other.gameObject);
            photonView.RPC("RpcCollectPart", RpcTarget.MasterClient, other.GetComponent<PhotonView>().ViewID);
        }
        else{
            Debug.Log($"[Experiment {ownerId}] Wrong part!");
        }
        
    }

    [PunRPC]
    public void RpcCollectPart(int partViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!gameManager.IsGameActive()) return;

        // 수집 파츠 개수 +1
        partsCollected++; 
        
        // 등록된 파츠 파괴 & 동기화
        PhotonView partPV = PhotonView.Find(partViewID);
        if (partPV != null) PhotonNetwork.Destroy(partPV.gameObject);

        Debug.Log($"[Experiment {ownerId}] {partsCollected}/3 parts collected");

        if (partsCollected >= requiredParts)
        {
            photonView.RPC("RpcActivateButton", RpcTarget.All);
        }
        
    }

    [PunRPC]
    public void RpcActivateButton()
    {
        Debug.Log($"Experiment {ownerId} completed! Button active");
        // 모든 클라이언트에서 버튼 활성화
        if (experimentButton != null)
        {
            // 모든 수집이 완료되면 버튼을 활성화합니다.
            experimentButton.SetActive(true);
        }
    }
    public void CompleteExperiment()
    {
        photonView.RPC("RpcCompleteExperiment", RpcTarget.All);

    }
    [PunRPC]
    private void RpcCompleteExperiment()
    {
        state = TableState.completed;
        monster.MonsterGenerate();
        Debug.Log($"🧪 Experiment {ownerId} Completed & Monster Spawned (Synced)");
    }


    
}
