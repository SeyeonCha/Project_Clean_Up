using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// 쓰레기 객체에 붙음
public class ThrowableObject : MonoBehaviour
{
    public PhotonView PV;
    // private int ignoreCollisionLayer = 9;
    private const int trashLayer = 7; 

    private int ignoreFirstCollision = 0;
    
    public enum State {
        Idle, // 다른 물체와의 충돌 이후 상태 (공격성 X), 물리 충돌 X
        Held, // 플레이어 손에 잡힌 상태
        Thrown // 플레이어가 던짐 ~ 다른 물체와 충돌 사이의 상태 (공격성 O, 물리 충돌 가능)
    }
    public State currentState = State.Idle;
    private Rigidbody2D rb;
    private Collider2D col;

    public float idleDrag = 7f; // idle 일 때 공기저항 값
    public float thrownDrag = 0f;

    private Color originalColor;
    private SpriteRenderer sr;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); 
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        Debug.Log($"trash layer : {gameObject.layer}");

        SetIdle();
    }

    private void Update()
    {
        // // 속도 기반 Idle 전환
        // if (currentState == State.Thrown)
        // {
        //     if (rb.velocity.magnitude <= 0.2f)
        //     {
        //         Debug.Log("속도 낮아짐 → Idle 전환");
        //         ignoreFirstCollision = 0; 
        //         SetIdle();
        //     }
        // }
        // Debug.Log($"currentStae : {currentState}, isTrigger : {col.isTrigger}, isKinematic : {rb.isKinematic}");
    }
    public void SetIdle() // 공격 불가 상태
    {
        currentState = State.Idle;
        
        // 물리 OK
        // col.isTrigger = false;
        rb.isKinematic = false;
        col.isTrigger = true;
        // rb.isKinematic = true;

        // 높은 공기저항 
        rb.drag = idleDrag;
        rb.angularDrag = idleDrag;

        // 색깔 초기화.
        sr.color = originalColor;

        // 충돌횟수 초기화
        ignoreFirstCollision = 0;

        Debug.Log($"Idle로 돌아옴 : 레이어 : {gameObject.layer}");

        gameObject.layer = trashLayer; // 레이어 돌려놓기

    }
    public void SetHeld()
    {
        currentState = State.Held;
        // col.isTrigger = true;
        // rb.isKinematic = true;
        Debug.Log($"setHeld : 레이어 : {gameObject.layer}");
        
    }
    public void SetThrown()
    {
        currentState = State.Thrown;
        // 레이어 돌려놓기
        
        Debug.Log($"setThrown : 레이어 : {gameObject.layer}");
        gameObject.layer = trashLayer; // 레이어 돌려놓기

        // 물리 충돌 ok
        col.isTrigger = false;
        rb.isKinematic = false;

        // 공기저항 0
        rb.drag = 0f;
        rb.angularDrag = 0f;

        // 색깔 빨갛게
        sr.color = Color.red;

        Debug.Log($"쓰레기 던져짐 in ThrowableObject -- {ignoreFirstCollision}");
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (currentState != State.Thrown) return;

        if (other.gameObject.CompareTag("Player"))
        {
            
            
            // **주의: 데미지 처리는 보통 마스터 클라이언트에서 처리 후 RPC로 동기화하지만,
            // 현재 로직을 최대한 유지하기 위해 IsMine 조건을 포함합니다.**
            

            // SetIdle();
            if (ignoreFirstCollision == 0)
            {
                ignoreFirstCollision++;
                return;
            }
            else
            {
                PhotonView playerPV = other.gameObject.GetComponent<PhotonView>();
                if (playerPV != null) 
                {
                    playerPV.RPC("Hit", RpcTarget.All);
                    
                }
                SetIdle(); // 충돌 후 Idle 상태로 전환.
                return;
            }
        }

        Debug.Log($"onCollision : 레이어 : {gameObject.layer}");
        gameObject.layer = trashLayer; // 레이어 돌려놓기

        
    }
}
