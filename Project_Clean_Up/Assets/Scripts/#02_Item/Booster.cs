using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Booster : MonoBehaviourPun 
{
    private float rotationBoostAmount = 200f;
    private float speedBoostAmount = 5f; 
    private float boostDuration = 5f;

    // ⭐ 사운드 관련 변수 추가
    [Header("Item Sounds")]
    public AudioSource audioSource;         // 소리 재생을 위한 AudioSource 컴포넌트
    public AudioClip collectSoundClip;
    
    // ⭐ 색상 관련 변수
    public SpriteRenderer spriteRenderer; 
    private Color currentColor = Color.white; // 현재 Drug의 색상

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // ⭐ AudioSource 컴포넌트 가져오기
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    // ⭐ 외부(InGameManager)에서 부스트 값과 색상을 설정하는 RPC 함수
    // 이 RPC는 InGameManager.SpawnDrugObjects()에서 Drug 생성 직후 호출되어
    // 모든 클라이언트에서 Drug의 속성과 색상을 초기화합니다.
    [PunRPC]
    public void RpcInitializeBoostAndColor(float rotBoost, float spdBoost, float dur, float r, float g, float b)
    {
        // 1. 색상 적용 (모든 클라이언트에서 실행됨)
        Color itemColor = new Color(r, g, b);
        SetDrugColor(itemColor); 
        
        // 2. 부스트 설정 적용 (모든 클라이언트에서 실행됨)
        rotationBoostAmount = rotBoost;
        speedBoostAmount = spdBoost;
        boostDuration = dur;
        
        Debug.Log("Booster 초기화 RPC 수신 및 색상/부스트 설정 완료.");
    }

    // ⭐ 색상을 받아 SpriteRenderer에 적용하는 함수
    public void SetDrugColor(Color newColor)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColor;
            currentColor = newColor; // 로컬 변수 업데이트
        }
    }
    
    // ==========================================================
    // ⭐ 충돌 및 Master Client에게 파괴 위임 로직
    // ==========================================================
    void OnCollisionEnter2D(Collision2D other)
    {
        // 충돌 대상이 플레이어가 아니면 리턴
        if (!other.gameObject.CompareTag("Player")) return;

        PhotonView playerPV = other.gameObject.GetComponent<PhotonView>();
        
        // ⭐ 핵심: 이 플레이어 오브젝트가 로컬 플레이어 (나)의 것인지 확인
        if (playerPV == null || !playerPV.IsMine) return; 

        // 1. 로컬에서 즉시 부스트 효과 적용 (딜레이 없이)
        PlayerMovement player = other.gameObject.GetComponent<PlayerMovement>();
        if (player != null)
        {
            Debug.Log("Drug 효과 로컬 적용: " + PhotonNetwork.LocalPlayer.NickName);
            player.ApplyBoostItem(rotationBoostAmount, speedBoostAmount, boostDuration);

            if (audioSource != null && collectSoundClip != null)
            {
                // PlayOneShot을 사용하여 다른 소리와 겹쳐 재생될 수 있도록 합니다.
                audioSource.PlayOneShot(collectSoundClip);
            }
        }
        
        // 2. Master Client에게 이 Drug 오브젝트를 파괴하도록 요청 (RPC 호출)
        // Drug 오브젝트의 PhotonView를 사용하여 RPC를 호출합니다.
        photonView.RPC("RpcRequestDestroyDrug", RpcTarget.MasterClient);
        
        // 3. Drug를 먹은 로컬 플레이어 화면에서 Drug 오브젝트 즉시 비활성화 (선택 사항: 시각적 피드백)
        // 파괴는 Master Client가 처리하지만, 로컬에서 즉시 안보이게 처리하여 먹은 듯한 느낌을 줍니다.
        gameObject.SetActive(false); 
    }

    // ==========================================================
    // ⭐ Master Client 전용: 파괴 요청 처리 RPC
    // ==========================================================
    [PunRPC]
    public void RpcRequestDestroyDrug()
    {
        // 이 RPC는 Master Client에게만 도달합니다 (RpcTarget.MasterClient).
        
        if (PhotonNetwork.IsMasterClient)
        {
            // Master Client는 PhotonNetwork.Destroy를 실행할 권한이 있습니다.
            Debug.Log("Master Client: 클라이언트로부터 Drug 파괴 요청 수신, 파괴 실행.");
            PhotonNetwork.Destroy(gameObject);
        }
        // Master Client가 아닌 경우, 이 코드는 실행되지 않습니다.
    }
}