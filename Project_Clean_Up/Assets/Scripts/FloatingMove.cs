using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingMove : MonoBehaviour
{
    public Vector2 initialVelocity = new Vector2(1f,0f);

    private Rigidbody2D rb; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = initialVelocity;
        }
    }
}
