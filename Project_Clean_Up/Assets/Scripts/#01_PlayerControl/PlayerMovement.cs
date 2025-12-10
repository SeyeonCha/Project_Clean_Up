using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    public Rigidbody2D rb;
    // public Animator AN;
    public SpriteRenderer SR;
    public PhotonView PV;
    public TMP_Text NickNameText;
    public Image HealthImage;

    Vector3 curPos;
    Quaternion curRot;
    
    // 🔥🔥
    // private GameManager gameManager; // 게임 매니저 참조
    
    // private Rigidbody2D rb; 

    // 발차기 관련 변수들
    private bool isTouchingWall = false;
    // private bool isKicked = false;
    public float kickForce = 5.0f;

    // 몸통 회전 관련 변수들
    private float h; // 키 인풋을 받을 변수
    public float rotationSpeed = 300f; // 초당 회전할 각도
    private float originalRotationSpeed;

    // 부스트 아이템 관련 변수들
    private bool itemApplying = false; // 아이템 적용중인지 여부 
    private float boostEndTime;

    // 조작감 향상을 위한 코요테 타이머 & 입력 버퍼링 타이머 구현
    public float coyoteTimeDuration = 0.3f; // 킥 후에도 여유 주기
    public float inputBufferDuration = 0.3f; // 킥 전에도 여유 주기

    public float wallFriction = 0.5f;

    private float coyoteTimer;
    private float inputBufferTimer;

    
    private PlayerGrabThrow Grabber;

    private Vector2 lastWallNormal = Vector2.up;

    public Vector3 CurPos { get => curPos; set => curPos = value; }

    private void Awake()
    {
        NickNameText.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;
        NickNameText.color = PV.IsMine ? Color.green : Color.red;

        // 🔥🔥
        // gameManager = FindObjectOfType<GameManager>();
        
        rb = GetComponent<Rigidbody2D>();
        Grabber = GetComponent<PlayerGrabThrow>();

        originalRotationSpeed = rotationSpeed; // 초기 회전 속도
        
        rb.velocity = new Vector2(0f,0f); // 초기 이동 속도 : 0 -> 정지 상태에서 시작
    }

    private void Update()
    {
        if (PV.IsMine)
        {
            // 게임이 활성화 상태일 때만 입력 처리
            // 🔥🔥
            // if (gameManager == null || gameManager.IsGameActive())
            // {
            if (InGameManager.Instance != null && InGameManager.Instance.IsMovementAllowed())
            {
                // a,d키 인풋 받기
                GetInput();

                // 벽 충돌 중 스페이스 감지 
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    inputBufferTimer = inputBufferDuration; // 인풋버퍼 타이머 시작
                    Kick();
                }
                else
                {
                    inputBufferTimer -= Time.deltaTime;
                }
            }
        }
        // IsMine이 아닌 것들은 부드럽게 위치 동기화
        else
        {
            // 위치 동기화
            if ((transform.position - curPos).sqrMagnitude >= 100) transform.position = curPos;
            else transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);
            
            // 회전 동기화
            transform.rotation = Quaternion.Lerp(transform.rotation, curRot, Time.deltaTime * 10);
        }
    }

    private void GetInput()
    {
        h = Input.GetAxis("Horizontal"); // a키 : -1, d키 : 1, 때면 0
    }

    private void FixedUpdate()
    {
        // 🔥🔥
        // if (gameManager == null || gameManager.IsGameActive())
        // {
        // ⭐ 4. 수정: InGameManager.Instance.IsMovementAllowed()를 사용하여 움직임 가능 여부 체크
        if (InGameManager.Instance != null && InGameManager.Instance.IsMovementAllowed())
        {
            Rotate(); // 인풋받은 h와 기본 설정한 rotationSpeed로 플레이어 회전시키기
            if (!isTouchingWall) // 벽에서 떨어지면 코요테 타이머 시간 감소
            {
                coyoteTimer -= Time.fixedDeltaTime;
                
            }

            if ((coyoteTimer > 0 || isTouchingWall) && inputBufferTimer > 0) // 벽에 닿고 스페이스바 눌렀으면 Kick 실행
            {
                KickWall(); // 기본설정한 힘 (kickForce) 로 플레이어 추진시키기
                inputBufferTimer = 0f; // 버퍼 타이머 초기화
            }
        }
        else
        {
            // 게임 오버 시 물리 움직임을 멈춤
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    // 🔥🔥
    public void Kick()
    {
        rb.AddForce(transform.up*1f, ForceMode2D.Impulse);
    }
    // 🔥🔥

    public void KickWall()
    {
        rb.AddForce(transform.up*kickForce, ForceMode2D.Impulse);
        // isKicked = false;
        coyoteTimer = 0f; // 코요테 시간 바로 종료
        isTouchingWall = false;

        transform.position += (Vector3)lastWallNormal * 0.05f;
        Debug.Log("Kick!");
    }

    public void Rotate()
    {
        if (Mathf.Abs(h) > 0.01f)
        {
            float currentRotationSpeed = rotationSpeed;
            if (Grabber.IsHoldingTrash()) { currentRotationSpeed *= 0.8f; }

            float rotateAmount = -h * currentRotationSpeed * Time.fixedDeltaTime;
            transform.Rotate(0, 0, rotateAmount, Space.Self);
        }
    }

    void OnCollisionEnter2D(Collision2D other) // other : 감지 된 충돌체
    {
        if (other.gameObject.CompareTag("Wall")) // 충돌체가 벽이면, 
        {
            isTouchingWall = true;
            Debug.Log("벽 충돌 시작 ---");

            if (!itemApplying)
            {
                rb.velocity = new Vector2(rb.velocity.x*-0.1f,rb.velocity.y*-0.1f);
            }
        }
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            // ... (속도 0 강제 로직 유지) ...
            
            // 💡 법선 벡터 저장: 충돌 해소 로직에 사용하기 위해 저장합니다.
            if (other.contacts.Length > 0)
            {
                lastWallNormal = other.contacts[0].normal;
            }

            isTouchingWall = true;
            coyoteTimer = coyoteTimeDuration;
        }
    }

    void  OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = false;
            Debug.Log("벽 충돌 종료 ---");

            coyoteTimer = coyoteTimeDuration; // 코요테 타이머 시작
        }
    }
    public void ApplyBoostItem(float rAmount, float speedAmount, float duration)
    {
        if (Time.time < boostEndTime)
        {
            boostEndTime += duration;
            return;
        }
        boostEndTime = Time.time + duration;

        // 회전 속도 일시적 증가
        rotationSpeed = originalRotationSpeed + rAmount;

        // 이동 속도 증가
        rb.velocity *= speedAmount;

        itemApplying = true;
        Debug.Log("부스트 시작");

        StartCoroutine(BoostTimer());
    }

    IEnumerator BoostTimer()
    {
        // 부스트 종료 시간까지 대기
        while (Time.time < boostEndTime)
        {
            yield return null; // 매 프레임 대기
        }

        // 부스트 해제
        rotationSpeed = originalRotationSpeed;
        itemApplying = false;
        Debug.Log("속도 부스트 종료");
    }

    // 플레이어 데미지 입을 때 불러올 함수
    [PunRPC]
    public void Hit()
    {
        if (PV.IsMine)
        {
            if (HealthImage != null)
            {
                HealthImage.fillAmount -= 0.1f;
            }
            
            if (HealthImage != null && HealthImage.fillAmount <= 0)
            {
                if (InGameManager.Instance != null)
                {
                    InGameManager.Instance.PlayerDied(PV.Owner); 
                }
            }
        }
    }

    // [PunRPC]
    // void DestroyRPC() => Destroy(gameObject);

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(HealthImage.fillAmount);
        }
        else
        {
            CurPos = (Vector3)stream.ReceiveNext();
            curRot = (Quaternion)stream.ReceiveNext();
            HealthImage.fillAmount = (float)stream.ReceiveNext();
        }
    }
}
