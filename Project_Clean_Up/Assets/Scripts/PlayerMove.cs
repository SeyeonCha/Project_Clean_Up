using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private GameManager gameManager; // 게임 매니저 참조
    public Vector2 initialVelocity = new Vector2(0f,0f);
    
    private Rigidbody2D rb; 

    Vector3 _moveVector;
    public float rotationSpeed = 200f; // 초당 회전할 각도 (Degree per second)

    public ArmGrabSensor armLSensor;
    public ArmGrabSensor armRSensor;

    // 현재 잡고 있는 쓰레기 오브젝트
    private GameObject heldTrash = null;
    // 쓰레기가 붙잡힐 팔의 Transform
    private Transform holdingArm = null;
    
    // ⭐ 추가: 오프셋 및 레이어 변수
    private Vector3 initialLocalTrashPosition = Vector3.zero; // 로컬 위치 오프셋
    private int originalTrashLayer; // 원래 레이어를 저장
    public int ignoreCollisionLayer = 9; // 충돌 무시 레이어 번호 (유니티 에디터에서 설정)

    private bool isTouchWall = false; // 벽 접촉 상태 플래그


    void Awake() 
    {
        rb = GetComponent<Rigidbody2D>();

        gameManager = FindObjectOfType<GameManager>();

        if (rb != null)
        {
            rb.velocity = initialVelocity;
        }
    }

    void Update()
    {
        // 게임이 활성화 상태일 때만 입력 처리
        if (gameManager == null || gameManager.IsGameActive())
        {
            HandleInput();

            if (isTouchWall && Input.GetKeyDown(KeyCode.Space))
            {
                KickWall();
            }

            HandleTrashGrab();
        }
    }

    void FixedUpdate()
    {
        // 게임이 활성화 상태일 때만 움직임 처리, 아니면 정지
        if (gameManager == null || gameManager.IsGameActive())
        {
            Move();
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

        // ⭐ 핵심 수정: FixedUpdate가 끝날 때 로컬 위치를 강제 재설정하여 떨림 현상을 방지합니다.
        if (heldTrash != null)
        {
            heldTrash.transform.localPosition = initialLocalTrashPosition;
        }
    }

    public void HandleInput()
    {
        _moveVector = PoolInput();
    }

    public Vector3 PoolInput()
    {
        float h = Input.GetAxis("Horizontal"); 
        float v = Input.GetAxis("Vertical"); 
        
        Vector3 moveDir = new Vector3(h, v, 0).normalized;

        return moveDir;
    }

    public void Move()
    {
        float horizontalInput = _moveVector.x;

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            float currentRotationSpeed = rotationSpeed;
            if (heldTrash != null) { currentRotationSpeed *= 0.5f; }

            float rotateAmount = -horizontalInput * currentRotationSpeed * Time.fixedDeltaTime;
            transform.Rotate(0, 0, rotateAmount, Space.Self);
        }
    }
    
    public void KickWall()
    {
        rb.AddForce(transform.up * 4.0f, ForceMode2D.Impulse);
    }
    
    public bool IsHoldingTrash()
    {
        return heldTrash != null;
    }

    // 쓰레기 집기/놓기 로직
    private void HandleTrashGrab()
    {
        if (Input.GetMouseButton(0) && heldTrash == null)
        {
            if (armLSensor != null && armLSensor.currentTouchingTrash != null)
            {
                GrabTrash(armLSensor.currentTouchingTrash, armLSensor.transform);
            }
            else if (armRSensor != null && armRSensor.currentTouchingTrash != null)
            {
                GrabTrash(armRSensor.currentTouchingTrash, armRSensor.transform);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            DropTrash();
        }
    }

    private void GrabTrash(GameObject trashObject, Transform armTransform)
    {
        if (heldTrash != null) return;
        
        heldTrash = trashObject;
        holdingArm = armTransform;

        Rigidbody2D trashRb = heldTrash.GetComponent<Rigidbody2D>();
        if (trashRb != null)
        {
            trashRb.isKinematic = true; 
        }

        // ⭐ 핵심: 쓰레기의 원래 레이어 저장 및 레이어 변경 (충돌 무시)
        originalTrashLayer = heldTrash.layer;
        heldTrash.layer = ignoreCollisionLayer; 

        // 오프셋 계산 및 저장
        // InverseTransformPoint를 사용하여 월드 좌표를 로컬 좌표로 정확히 변환
        Vector3 desiredLocalPosition = holdingArm.InverseTransformPoint(heldTrash.transform.position);
        initialLocalTrashPosition = desiredLocalPosition;

        // 부모-자식 관계 설정
        heldTrash.transform.parent = holdingArm;
        
        // 계산된 로컬 위치 설정
        heldTrash.transform.localPosition = initialLocalTrashPosition; 
        
        Debug.Log($"Trash {trashObject.name} 잡기 성공! 팔: {holdingArm.name}");
    }

    private void DropTrash()
    {
        if (heldTrash != null)
        {
            // ⭐ 핵심: 쓰레기의 레이어를 원래 레이어로 복원
            heldTrash.layer = originalTrashLayer; 
            
            // 부모-자식 관계 해제
            heldTrash.transform.parent = null;

            Rigidbody2D trashRb = heldTrash.GetComponent<Rigidbody2D>();
            if (trashRb != null)
            {
                trashRb.isKinematic = false;
                trashRb.velocity = rb.velocity; 
            }
            
            // 상태 및 오프셋 초기화
            heldTrash = null;
            holdingArm = null;
            initialLocalTrashPosition = Vector3.zero;

            if (armLSensor != null) armLSensor.currentTouchingTrash = null;
            if (armRSensor != null) armRSensor.currentTouchingTrash = null;
            
            Debug.Log("Trash 놓기");
        }
    }
    

    // 벽 충돌 로직
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            isTouchWall = true;
            Debug.Log("벽 충돌");
        }
    }

    // 벽 충돌 로직
    void OnCollisionStay2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            isTouchWall = true;
            Debug.Log("벽 충돌");
        }
    }
    void  OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            isTouchWall = false;
        }
    }
}