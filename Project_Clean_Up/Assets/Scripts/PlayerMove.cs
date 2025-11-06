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

    // ⭐ 추가: 던지기 힘 조절 변수 (Inspector에서 미세 조정)
    [Header("Throwing Settings")]
    public float throwForceMultiplier = 0.05f; // 던지는 힘의 배수

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
    
    // 던지기 로직 추가
    private void DropTrash()
    {
        if (heldTrash != null)
        {
            // 1. 레이어 복원
            heldTrash.layer = originalTrashLayer; 
            
            // 2. 부모-자식 관계 해제
            heldTrash.transform.parent = null;

            Rigidbody2D trashRb = heldTrash.GetComponent<Rigidbody2D>();
            if (trashRb != null)
            {
                trashRb.isKinematic = false;
                
                // --- ⭐ 던지기 로직 시작 ⭐ ---
                
                // 3. 현재 팔의 각속도를 가져옵니다.
                ArmRotation armRotation = holdingArm.GetComponent<ArmRotation>();
                float angularSpeed = (armRotation != null) ? armRotation.angularVelocity : 0f;
                
                // 4. 팔의 길이를 계산하여 선형 속도를 추정합니다.
                float radius = Vector3.Distance(heldTrash.transform.position, holdingArm.position);
                
                // 5. 선형 속도 (각속도 * 반지름)를 계산합니다. (Deg/s를 m/s로 변환)
                float linearSpeed = angularSpeed * Mathf.Deg2Rad * radius;
                
                // 6. 던지는 방향 (쓰레기가 원운동에서 이탈하는 접선 방향)
                Vector3 throwDirection = heldTrash.transform.position - holdingArm.position;
                Vector3 tangentialDirection = Quaternion.Euler(0, 0, 90) * throwDirection.normalized; // 90도 회전

                // 7. 계산된 속도와 배수를 사용하여 힘을 적용
                float finalThrowForce = linearSpeed * throwForceMultiplier * trashRb.mass; 
                trashRb.AddForce(tangentialDirection * finalThrowForce, ForceMode2D.Impulse);
                // --------------------------
            }
            
            // 상태 초기화
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