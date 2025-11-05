using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private Rigidbody2D rigid2D; 

    Vector3 _moveVector;
    public float rotationSpeed = 200f; // 초당 회전할 각도 (Degree per second)
    public float shootForce = 500f; // 속력

    void Awake() 
    {
        rigid2D = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleInput();
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
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
    public void Shoot() 
    {
        // transform.up은 현재 오브젝트가 바라보는 앞쪽 방향 벡터입니다.
        // AddForce에 Time.deltaTime을 곱하면 의도치 않은 결과를 낳을 수 있으므로 제거했습니다.
        // AddForce는 이미 프레임 속도와 관계없이 작동하도록 설계되어 있습니다.
        rigid2D.AddForce(transform.up * shootForce, ForceMode2D.Force);
        
        // 참고: ForceMode2D.Impulse는 짧은 순간에 힘을 가해 즉각적인 속도 변화를 만듭니다.
        // 만약 지속적인 가속을 원하면 ForceMode2D.Force를 사용하세요.
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            // 여기에 튕기는거 구현하면 될 듯!
        }
    }
}