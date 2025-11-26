using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Experiment : MonoBehaviour
{
    private GameManager gameManager;

    void Start()
    {
        // 씬에서 GameManager를 찾습니다.
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager 스크립트를 찾을 수 없습니다! 게임이 정상 작동하지 않을 수 있습니다.");
        }
    }
    void Update()
    {
        if (gameManager.ExperimentCompleted)
        {
            DestroyAllChildren();
        }
    }
    private void DestroyAllChildren()
    {
        // 자식 오브젝트를 순회합니다.
        // `GetChild(i)`를 사용하면, 오브젝트를 파괴할 때마다 인덱스가 바뀌어 문제가 생길 수 있으므로,
        // 파괴할 오브젝트들을 리스트에 먼저 담아두고 순회하는 것이 가장 안전합니다.
        
        // 1. 파괴할 자식 오브젝트들을 리스트에 담습니다.
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        // 2. 리스트에 담긴 자식 오브젝트들을 파괴합니다.
        foreach (GameObject child in childrenToDestroy)
        {
            Destroy(child);
        }

        Debug.Log($"실험 완료 상태이므로, {this.name}의 **모든 자식 오브젝트**가 파괴되었습니다.");
    }
    // 부품이 실험대에 들어왔는지 감지
    void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.CompareTag("parts") && gameManager != null)
        {
            // 닿은 오브젝트가 부품 태그를 가지고 있다면,
            // GameManager에게 부품이 수집되었음을 알립니다.
            gameManager.PartsCollected(other.gameObject);

            //파츠가 실험대 위에 고정되도록 
            other.transform.SetParent(this.transform);
            other.transform.localPosition = Vector3.zero;
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;  // 물리 영향 제거
            }
             // 콜라이더 비활성화 
            Collider2D col = other.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
            Debug.Log($"{other.name} 파츠가 실험대 위에 고정됨");
        }
    }
}
