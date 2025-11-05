using UnityEngine;

public class GoalTrigger : MonoBehaviour
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

    // ⭐ 쓰레기가 골대에 들어왔는지 감지합니다.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trash") && gameManager != null)
        {
            // 닿은 오브젝트가 쓰레기 태그를 가지고 있다면,
            // GameManager에게 쓰레기가 수집되었음을 알립니다.
            gameManager.TrashCollected(other.gameObject);
        }
    }
}