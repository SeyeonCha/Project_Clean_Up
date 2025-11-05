using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public Vector2 initialVelocity = new Vector2(1f,0f);
    
    private Rigidbody2D rb; 

    Vector3 _moveVector;
    public float rotationSpeed = 200f; // 초당 회전할 각도 (Degree per second)
    // public float moveSpeed = 500f; // 속력

    public ArmGrabSensor armLSensor;
    public ArmGrabSensor armRSensor;

    // 현재 잡고 있는 쓰레기 오브젝트
    private GameObject heldTrash = null;
    // 쓰레기가 붙잡힐 팔의 Transform
    private Transform holdingArm = null;

    private bool isTouchWall = false;

    void Awake() 
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = initialVelocity;
        }
    }

    void Update()
    {
        HandleInput();
        
        if (isTouchWall && Input.GetKeyDown(KeyCode.Space))
        {
            KickWall();
        }
        
        // 쓰레기 집기/놓기 처리
        HandleTrashGrab();
    }

    void FixedUpdate()
    {
        Move();
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
            float rotateAmount = -horizontalInput * rotationSpeed * Time.fixedDeltaTime;
            transform.Rotate(0, 0, rotateAmount, Space.Self);
        }
    }

    public void KickWall()
    {
        rb.AddForce(transform.up * 2.0f, ForceMode2D.Impulse); 
    }

    // 현재 쓰레기를 잡고 있는지 확인하는 Public 함수
    public bool IsHoldingTrash()
    {
        return heldTrash != null;
    }

    // 쓰레기 집기/놓기 로직
    private void HandleTrashGrab()
    {
        // 마우스 왼쪽 버튼을 누르고 있는 동안
        if (Input.GetMouseButton(0))
        {
            // 아직 아무것도 잡고 있지 않을 때만 잡기를 시도합니다.
            if (heldTrash == null)
            {
                // 1. ArmL 센서가 쓰레기에 닿았는지 확인
                if (armLSensor.currentTouchingTrash != null)
                {
                    GrabTrash(armLSensor.currentTouchingTrash, armLSensor.transform);
                }
                // 2. ArmR 센서가 쓰레기에 닿았는지 확인
                else if (armRSensor.currentTouchingTrash != null)
                {
                    GrabTrash(armRSensor.currentTouchingTrash, armRSensor.transform);
                }
            }
        }
        // 마우스 왼쪽 버튼을 뗐을 때 (잡고 있던 것이 있다면 놓습니다)
        else if (Input.GetMouseButtonUp(0))
        {
            DropTrash();
        }
    }

    private void GrabTrash(GameObject trashObject, Transform armTransform)
    {
        heldTrash = trashObject;
        holdingArm = armTransform;

        // 1. Rigidbody2D 물리 해제 (플레이어와 함께 움직이도록)
        Rigidbody2D trashRb = heldTrash.GetComponent<Rigidbody2D>();
        if (trashRb != null)
        {
            trashRb.isKinematic = true; // 물리 엔진의 영향을 받지 않도록 설정
        }

        // 2. 부모-자식 관계 설정 (팔의 위치를 따라가도록)
        // 쓰레기가 Arm의 자식이 됩니다.
        heldTrash.transform.parent = holdingArm;
        
        heldTrash.transform.position = holdingArm.position;
        heldTrash.transform.rotation = holdingArm.rotation;

        Debug.Log($"Trash {trashObject.name} 잡기 성공! 팔: {holdingArm.name}");
    }

    private void DropTrash()
    {
        if (heldTrash != null)
        {
            // 1. 부모-자식 관계 해제
            heldTrash.transform.parent = null;

            // 2. Rigidbody2D 활성화 (물리 적용 재개)
            Rigidbody2D trashRb = heldTrash.GetComponent<Rigidbody2D>();
            if (trashRb != null)
            {
                trashRb.isKinematic = false;
                // 놓는 순간 플레이어의 속도를 쓰레기에게 적용하여 던지는 느낌을 줄 수 있습니다.
                trashRb.velocity = rb.velocity; 
            }
            
            // 3. 상태 초기화
            heldTrash = null;
            holdingArm = null;

            // Arm Sensor의 닿음 정보도 초기화 (필요하다면)
            armLSensor.currentTouchingTrash = null;
            armRSensor.currentTouchingTrash = null;
            
            Debug.Log("Trash 놓기");
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Wall"))
        {
            // 벽 충돌 처리
            isTouchWall = true;
            Debug.Log("벽 충돌");
        }
    }
}