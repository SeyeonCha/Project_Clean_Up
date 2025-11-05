using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    Vector3 _moveVector;
    // public float movementSpeed = 5f; // 이동 속도 (주석 처리 유지)
    
    // ⭐ 추가: 회전 속도 변수 (Inspector에서 조절 가능)
    public float rotationSpeed = 200f; // 초당 회전할 각도 (Degree per second)

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        Move();
    }

    public void HandleInput()
    {
        _moveVector = PoolInput();
    }

    // PoolInput 함수: A/D 키 입력을 받습니다.
    public Vector3 PoolInput()
    {
        // "Horizontal" 축 (A 키: -1, D 키: 1)
        float h = Input.GetAxis("Horizontal"); 
        
        // "Vertical" 축 (W 키: 1, S 키: -1) (현재 회전에는 사용하지 않음)
        float v = Input.GetAxis("Vertical"); 
        
        // 회전을 위해 h값만 사용하고, moveDir 벡터는 그대로 유지합니다.
        Vector3 moveDir = new Vector3(h, v, 0).normalized;

        return moveDir;
    }

    // ⭐ 수정된 Move 함수: A/D 키 입력에 따라 점진적으로 회전합니다.
    public void Move()
    {
        // 1. 플레이어 이동 (주석 처리 유지)
        // transform.position += _moveVector * movementSpeed * Time.fixedDeltaTime;

        // 2. 점진적 회전 처리
        
        // Horizontal 입력 값 (-1.0 ~ 1.0)을 가져옵니다.
        float horizontalInput = _moveVector.x;
        
        // 입력이 0이 아닐 때만 회전합니다.
        if (Mathf.Abs(horizontalInput) > 0.01f) // 부동 소수점 비교를 위해 작은 값 사용
        {
            // 원하는 회전 각도를 계산합니다. (A키는 왼쪽, D키는 오른쪽)
            // 현재 2D(Z축 회전)에서 양수 Z축 회전은 반시계(왼쪽)입니다.
            // A 키 (-1) -> 양의 회전: 왼쪽 회전
            // D 키 (1) -> 음의 회전: 오른쪽 회전
            float rotateAmount = -horizontalInput * rotationSpeed * Time.fixedDeltaTime;

            // 현재 회전에 계산된 회전량을 적용하여 Z축으로 회전합니다.
            // Space.Self를 사용하여 로컬 좌표계를 기준으로 회전합니다.
            transform.Rotate(0, 0, rotateAmount, Space.Self);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            // 여기에 튕기는거 구현하면 될 듯!
        }
    }
}