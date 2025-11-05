using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    Vector3 _moveVector;
    // public float movementSpeed = 5f; // 이동 속도 추가 (만약 이동도 키보드로 한다면)

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

    // ⭐ PoolInput 함수 수정: 조이스틱 대신 키보드 A/D, W/S 입력을 받습니다.
    public Vector3 PoolInput()
    {
        // "Horizontal" 축 (A 키: -1, D 키: 1)
        float h = Input.GetAxis("Horizontal"); 
        
        // "Vertical" 축 (W 키: 1, S 키: -1) (만약 이동도 사용한다면)
        float v = Input.GetAxis("Vertical"); 
        
        // 방향 벡터 생성 및 정규화
        // 2D 게임이라면 Z는 0으로 둡니다.
        Vector3 moveDir = new Vector3(h, v, 0).normalized;

        return moveDir;
    }

    // ⭐ Move 함수 수정: 입력이 있을 때만 각도 계산 및 회전
    public void Move()
    {
        // 플레이어 이동 (선택 사항: 키보드로 이동도 처리할 경우)
        // transform.position += _moveVector * movementSpeed * Time.fixedDeltaTime;

        // 회전 처리 (각도 조정)
        // _moveVector.x와 _moveVector.y 중 하나라도 0이 아니면 회전 각도를 계산합니다.
        if (_moveVector.x != 0 || _moveVector.y != 0) 
        {
            // Atan2는 벡터의 각도를 라디안으로 반환합니다. (Y축이 0도인 기준)
            // 인자 순서: Atan2(y, x) -> 여기서는 (x, y) 순서를 유지합니다.
            float rad = Mathf.Atan2(_moveVector.x, _moveVector.y);
            
            // 라디안을 각도로 변환
            float angle = (rad * 180f) / Mathf.PI;

            // Z축 회전 설정 (2D에서 일반적으로 Z축 회전을 사용)
            // 기존 코드처럼 Z축 회전 각도를 설정합니다.
            this.transform.localEulerAngles = new Vector3(0, 0, (-angle));
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