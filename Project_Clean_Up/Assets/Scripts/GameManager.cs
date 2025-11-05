using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; 

public class GameManager : MonoBehaviour
{
    // ====== UI 및 메세지 설정 ======
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI remainingText; 
    public TextMeshProUGUI endText; 
    public GameObject retryButton;
    
    [Header("Game End Messages")]
    public string winMessage = "클리어";
    public string loseMessage = "게임 오버";

    // ====== 게임 시간 설정 ======
    public float startingTime = 60f;
    private float currentTime;
    private bool isGameOver = false;

    // ====== 쓰레기 및 클리어 조건 설정 ======
    [Header("Trash & Spawning")]
    // ⭐ 추가: 쓰레기 프리팹 (Inspector에서 연결)
    public GameObject trashPrefab;
    // ⭐ 추가: 생성할 쓰레기 개수
    public int totalTrashCount = 10;
    // ⭐ 추가: 쓰레기가 생성될 맵 범위 (월드 좌표)
    public Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(20, 10, 0));
    
    // ⭐ 남은 쓰레기 추적
    private int trashRemaining; 
    private int trashCollected = 0;

    void Start()
    {
        // 초기화
        currentTime = startingTime;
        trashRemaining = totalTrashCount; // 남은 쓰레기는 전체 개수로 시작

        // UI 숨기기
        if (retryButton != null) { retryButton.SetActive(false); }
        if (endText != null) { endText.gameObject.SetActive(false); }

        // 쓰레기 생성
        SpawnTrashObjects();

        // 초기 UI 업데이트
        UpdateUIText();
        UpdateRemainingText();
    }

    void Update()
    {
        if (isGameOver) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            // 시간 초과 시 패배
            GameOver(false); 
        }

        UpdateUIText();
    }

    // ===================================
    // ⭐ 쓰레기 생성 로직
    // ===================================

    private void SpawnTrashObjects()
    {
        if (trashPrefab == null)
        {
            Debug.LogError("Trash Prefab이 GameManager에 연결되지 않았습니다!");
            return;
        }

        for (int i = 0; i < totalTrashCount; i++)
        {
            // 맵 범위 내에서 랜덤 위치 계산
            Vector3 randomPosition = new Vector3(
                Random.Range(mapBounds.min.x, mapBounds.max.x),
                Random.Range(mapBounds.min.y, mapBounds.max.y),
                0 // 2D이므로 Z축은 0
            );
            
            // 쓰레기 생성
            Instantiate(trashPrefab, randomPosition, Quaternion.identity);
        }
    }

    // ===================================
    // ⭐ UI 업데이트 및 클리어 로직
    // ===================================

    void UpdateUIText()
    {
        if (timeText != null)
        {
            timeText.text = "Time: " + Mathf.CeilToInt(currentTime).ToString(); 
        }
    }
    
    void UpdateRemainingText()
    {
        // "Remaining: 00/10" 형식으로 업데이트
        if (remainingText != null)
        {
            remainingText.text = $"Remaining: {trashCollected}/{totalTrashCount}";
        }
    }

    // ⭐ GoalTrigger에서 호출되어 쓰레기 획득을 알리는 함수
    public void TrashCollected(GameObject trash)
    {
        if (isGameOver) return;

        trashCollected++;
        trashRemaining--;

        // 쓰레기를 씬에서 제거
        Destroy(trash);

        UpdateRemainingText();

        // ⭐ 승리 조건 확인
        if (trashCollected >= totalTrashCount)
        {
            GameOver(true); // 모든 쓰레기를 모았으므로 클리어
        }
    }

    // ===================================
    // 게임 종료 로직
    // ===================================

    public void GameOver(bool didWin)
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        UpdateUIText(); 

        // 1. 종료 메시지 표시
        if (endText != null)
        {
            endText.gameObject.SetActive(true);
            endText.text = didWin ? winMessage : loseMessage;
        }

        // 2. 버튼 표시
        if (retryButton != null)
        {
            retryButton.SetActive(true);
        }
        
        // 추가: 모든 플레이어 움직임을 멈추도록 Time.timeScale을 사용할 수도 있습니다.
        // Time.timeScale = 0f;
    }
    
    public bool IsGameActive()
    {
        return !isGameOver;
    }
    
    public void RetryGame()
    {
        // Time.timeScale = 1f; // 만약 timeScale을 0으로 설정했다면 다시 1로 설정해야 합니다.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}