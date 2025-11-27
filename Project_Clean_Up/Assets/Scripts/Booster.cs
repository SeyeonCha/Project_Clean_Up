using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Booster : MonoBehaviourPun, IPunObservable 
{
    private float rotationBoostAmount = 200f;
    private float speedBoostAmount = 5f; 
    private float boostDuration = 5f;

    // ✨ 네트워크 동기화를 위한 변수 (부스트 값 수신)
    private float networkRotBoost = 0f;
    private float networkSpeedBoost = 0f;
    private float networkDuration = 0f;
    
    // ⭐ 색상 관련 변수
    public SpriteRenderer spriteRenderer; 
    private Color currentColor = Color.white; // 현재 Drug의 색상

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        networkRotBoost = rotationBoostAmount;
        networkSpeedBoost = speedBoostAmount;
        networkDuration = boostDuration;
    }
    
    // ⭐ 외부(GameManager)에서 부스트 값과 색상을 설정하는 함수
    public void InitializeBoostAndColor(float rotAmount, float speedAmount, float duration, Color itemColor)
    {
        // 마스터 클라이언트에서만 초기화
        if (PhotonNetwork.IsMasterClient)
        {
            rotationBoostAmount = rotAmount;
            speedBoostAmount = speedAmount;
            boostDuration = duration;
            currentColor = itemColor; 
            
            // 로컬에서 즉시 색상 적용
            SetDrugColor(itemColor);
        }
    }

    // ⭐ 색상을 받아 SpriteRenderer에 적용하는 함수
    public void SetDrugColor(Color newColor)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = newColor;
            currentColor = newColor;
        }
    }
    
    // ✨ IPunObservable 구현: 부스트 값과 색상 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 🚀 마스터/소유자만 설정된 값과 색상을 전송
            stream.SendNext(rotationBoostAmount);
            stream.SendNext(speedBoostAmount);
            stream.SendNext(boostDuration);
            stream.SendNext(currentColor); // ✨ Color 전송
        }
        else
        {
            // 📥 다른 클라이언트: 수신된 값을 내부 변수에 저장
            networkRotBoost = (float)stream.ReceiveNext();
            networkSpeedBoost = (float)stream.ReceiveNext();
            networkDuration = (float)stream.ReceiveNext();
            Color receivedColor = (Color)stream.ReceiveNext(); // ✨ Color 수신

            // 부스트 값 업데이트
            rotationBoostAmount = networkRotBoost;
            speedBoostAmount = networkSpeedBoost;
            boostDuration = networkDuration;
            
            // 색상이 변경되었으면 로컬 색상 업데이트
            if (receivedColor != currentColor)
            {
                SetDrugColor(receivedColor); 
            }
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        // ... (기존 OnCollisionEnter2D 로직 유지) ...
        
        if (other.gameObject.CompareTag("Player"))
        {
            PhotonView playerPV = other.gameObject.GetComponent<PhotonView>();
            if (playerPV == null || !playerPV.IsMine) return;
            
            PlayerMovement player = other.gameObject.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.ApplyBoostItem(rotationBoostAmount, speedBoostAmount, boostDuration);
            }
            
            PhotonView itemPV = GetComponent<PhotonView>();
            if (itemPV.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}