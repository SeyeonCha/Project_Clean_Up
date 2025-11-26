using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingMove : MonoBehaviour
{
    public Vector2 initialVelocity = new Vector2(1f,0f);
    public float minSpeed = 1f;       // 최소 속도 기준
    public float maxSpeed = 10f;
    public float kickForce = 3f;      // 부족할 때 밀어줄 힘
    public float randomAngle = 30f;   // 밀어줄 방향의 랜덤 범위

    private Rigidbody2D rb; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = initialVelocity;
        }
    }
    void Update()
    {
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
            
            // 방향 * 최대 속도 = 새로운 속도 벡터
            rb.velocity = direction * maxSpeed;
        }
    }
}
