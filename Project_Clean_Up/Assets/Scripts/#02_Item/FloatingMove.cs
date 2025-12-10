using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; 

public class FloatingMove : MonoBehaviourPunCallbacks, IPunObservable 
{
    public Vector2 initialVelocity = new Vector2(1f,0f);
    public float minSpeed = 1f;       
    public float maxSpeed = 10f;
    public float kickForce = 3f;      
    public float randomAngle = 30f;   

    private Rigidbody2D rb; 
    private PhotonView PV; 
    
    // ✨ 추가: 네트워크 동기화 변수
    private Vector3 curPos; 
    private Quaternion curRot;
    public float syncRate = 10f; 

    void Awake() 
    {
        PV = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        
        curPos = transform.position;
        curRot = transform.rotation;
    }

    void Start()
    {
        if (PV.IsMine && rb != null)
        {
            rb.velocity = initialVelocity;
        }
    }

    void Update()
    {
        if (PV.IsMine)
        {
            // ⭐ 소유자(IsMine): 물리 시뮬레이션 및 힘 조절 로직 실행
            
            if (rb == null) return;

            float speed = rb.velocity.magnitude;

            if (speed < minSpeed)
            {
                // 약간 랜덤한 방향으로 힘 주기
                Vector2 randomDir = Quaternion.Euler(0, 0, Random.Range(-randomAngle, randomAngle)) * rb.velocity.normalized;

                if (rb.velocity == Vector2.zero)  
                    randomDir = Random.insideUnitCircle.normalized;

                rb.AddForce(randomDir * kickForce, ForceMode2D.Impulse);
            }
            else if (speed > maxSpeed)
            {
                Vector2 direction = rb.velocity.normalized;
                rb.velocity = direction * maxSpeed;
            }
        }
        else
        {
            // ⭐ 비소유자(Not IsMine): 네트워크 데이터로 보간하여 동기화
            
            // 1. 위치 동기화 (사용자 요청 로직과 동일)
            if ((transform.position - curPos).sqrMagnitude >= 100)
            {
                transform.position = curPos;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * syncRate);
            }
            
            // 2. 회전 동기화
            transform.rotation = Quaternion.Lerp(transform.rotation, curRot, Time.deltaTime * syncRate);
        }
    }
    
    // ⭐ IPunObservable 구현: 위치와 회전 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 🚀 소유자(IsMine): 위치와 회전 전송
            if (rb != null)
            {
                // Vector2 (rb.position)와 float (rb.rotation)을 전송합니다.
                stream.SendNext(rb.position);
                stream.SendNext(rb.rotation);
            }
        }
        else
        {
            // 📥 비소유자(Not IsMine): 위치와 회전 수신
            // ⭐ 수정: 전송된 Vector2를 Vector2로 정확히 수신합니다.
            Vector2 networkPosition2D = (Vector2)stream.ReceiveNext();
            curPos = networkPosition2D; // Vector2를 Vector3 curPos에 할당 (Z=0으로 자동 변환)

            // float으로 회전 수신 후 Quaternion에 적용
            float networkRotationZ = (float)stream.ReceiveNext(); 
            curRot = Quaternion.Euler(0, 0, networkRotationZ);
        }
    }
}