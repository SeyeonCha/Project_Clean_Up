using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Booster : MonoBehaviour
{
    // 속도 증가량
    public float rotationBoostAmount = 200f;
    public float speedBoostAmount = 5f; 

    
    // 부스트 효과가 지속될 시간 (예: 5초)
    public float boostDuration = 5f;

    void OnCollisionEnter2D(Collision2D other)
    {
        // 닿은 오브젝트의 태그가 "Player" 면
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerMovement player = other.gameObject.GetComponent<PlayerMovement>();

            if (player != null)
            {
                Debug.Log("아이템에서 플레이어 찾음");
                player.ApplyBoostItem(rotationBoostAmount, speedBoostAmount, boostDuration);
                Destroy(gameObject); // 약물은 제거
            }
        }
    }

}
