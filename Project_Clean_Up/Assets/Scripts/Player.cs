using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private Rigidbody2D rigid2D; 

    Vector3 _moveVector;
    public float rotationSpeed = 200f; // 초당 회전할 각도 (Degree per second)
    public float moveSpeed = 500f; // 속력

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
        rigid2D.AddForce(-transform.right * moveSpeed, ForceMode2D.Force);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            // 여기에 튕기는거 구현하면 될 듯!
        }
    }
}