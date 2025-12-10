using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class Bomb : MonoBehaviourPun
{
    public float fuseTime = 5.0f;           // 폭발까지의 시간
    public float explosionRadius = 5.0f;    // 폭발 범위
    public float explosionForce = 70f;     // 폭발이 가하는 힘

    public TextMeshPro timerText;

    private float currentTime;

    public GameObject explosionEffectPrefab;

    void Start()
    {
        Debug.Log($"[{PhotonNetwork.LocalPlayer.NickName}] 폭탄이 생성되었습니다. PV ID: {photonView.ViewID}");

        // 5초 후에 Explode 함수를 호출 (타이머)
        currentTime = fuseTime;
    }
    void Update()
    {
        // 1. 시간 감소
        currentTime -= Time.deltaTime;

        // 2. 텍스트 업데이트
        if (timerText != null)
        {
            // 남은 시간을 올림하여 정수로 표시합니다.
            timerText.text = Mathf.CeilToInt(currentTime).ToString(); 
        }

        // 3. 폭발 조건 확인
        if (currentTime <= 0)
        {
            Explode();
        }
    }

    void Explode()
    {
        // 1. 이펙트 생성 로직 (생략, 필요 시 추가)
        if (explosionEffectPrefab != null)
        {
            // 폭탄 위치에 이펙트를 생성합니다.
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            // 파티클 시스템이 재생된 후 스스로 사라지도록 설정합니다. (선택 사항)
            Destroy(effect, 2f); // 2초 뒤에 파티클 오브젝트를 제거
        }
        
        // 2. 일정 범위 내의 오브젝트 탐색
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        // 3. 탐색된 오브젝트들에게 힘 가하기
        foreach (Collider2D hit in colliders)
        {
            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                Vector2 direction = rb.position - (Vector2)transform.position;
                float distance = direction.magnitude;
                
                // 거리 감쇠 계산
                float forceMultiplier = 1.0f - Mathf.Clamp01(distance / explosionRadius);
                float finalForce = explosionForce * forceMultiplier;
                
                // 힘을 가하고, Impulse 모드로 폭발적인 힘을 줍니다.
                rb.AddForce(direction.normalized * finalForce, ForceMode2D.Impulse);
            }
        }

        // 4. 폭탄 오브젝트 제거
        Destroy(gameObject);
    }
}
