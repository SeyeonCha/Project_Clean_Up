using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // ✨ 추가
using Photon.Realtime;

public class FloatingMove : MonoBehaviourPunCallbacks, IPunObservable // ✨ 상속 변경
{
    public Vector2 initialVelocity = new Vector2(1f,0f);
    public float minSpeed = 1f;       
    public float maxSpeed = 10f;
    public float kickForce = 3f;      
    public float randomAngle = 30f;   

    private Rigidbody2D rb; 
    private PhotonView PV; // ✨ 추가: PhotonView 참조
    
    // ✨ 추가: 네트워크 동기화 변수
    private Vector3 curPos; 
    private Quaternion curRot;
    public float syncRate = 10f; // 동기화 속도

    void Awake() // Start 대신 Awake 사용
    {
        PV = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        
        curPos = transform.position;
        curRot = transform.rotation;
    }

    void Start()
    {
        // ✨ 마스터 클라이언트 또는 오브젝트 소유자만 초기 속도를 설정합니다.
        // 마스터 클라이언트가 생성했으므로, 초기에는 마스터 클라이언트가 소유자입니다.
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
                // Rigidbody의 위치와 회전을 전송하는 것이 물리 동기화에 더 적합합니다.
                stream.SendNext(rb.position);
                stream.SendNext(rb.rotation);
            }
        }
        else
        {
            // 📥 비소유자(Not IsMine): 위치와 회전 수신
            curPos = (Vector3)stream.ReceiveNext();
            curRot = Quaternion.Euler(0, 0, (float)stream.ReceiveNext()); // Z축 회전만 수신
        }
    }
}