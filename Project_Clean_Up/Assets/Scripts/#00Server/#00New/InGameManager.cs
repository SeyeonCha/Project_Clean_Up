using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon; // Hashtable 사용

public class InGameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static InGameManager Instance;
    
    // ==========================================================
    // ⭐ UI Panels
    // ==========================================================
    [Header("Game Panels")]
    public GameObject howToPlayPanel; // 1. 초기 활성화
    public GameObject inGamePanel;    // 2. 게임 중 활성화
    public GameObject victoryPanel;   // 3. 승리 시 활성화
    public GameObject losePanel;      // 4. 패배 시 활성화
    public GameObject timesUpPanel;   // 5. 시간 초과 시 활성화

    [Header("In Game UI")]
    public TMP_Text timeText;
    public TMP_Text countdownText; // 게임 시작 전 5초 카운트다운
    public Button readyButton;     // How To Play Panel의 Ready 버튼
    
    [Header("Scene & Time Settings")]
    public string lobbySceneName = "LobbyScene"; // 로비 씬 이름
    public float timeLimit = 60f; // 제한 시간 (Inspector 설정 가능)

    // ==========================================================
    // ⭐ 스폰 설정
    // ==========================================================
    [Header("Spawn Settings")]
    public GameObject playerPrefab; // 플레이어 프리팹 (Inspector에서 연결)
    public Vector3 ownerSpawnPoint = new Vector3(-5f, 0f, 0f); // Master Client 스폰 위치
    public Vector3 player2SpawnPoint = new Vector3(5f, 0f, 0f); // Player 2 스폰 위치
    
    // ==========================================================
    // ⭐ 오브젝트 생성 설정 (기존 GameManager 참고)
    // ==========================================================
    [Header("Object Spawning")]
    public GameObject trashPrefab;
    public GameObject drugPrefab;
    public GameObject obstaclePrefab;
    public GameObject bombPrefab;           

    public int totalTrashCount = 5;
    public int totalDrugCount = 2; 
    public int totalObstacleCount = 2; 

    public float minBombSpawnTime = 10f;     
    public float maxBombSpawnTime = 25f;     
    public int maxBombsToSpawn = 2;
    
    public Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(20, 10, 0)); // 스폰 영역
    
    // ==========================================================
    // ⭐ Private & Network Variables
    // ==========================================================
    private PhotonView PV;
    private float currentTime;
    private bool isGameActive = false;
    private bool isGameOver = false;
    private int readyPlayers = 0; // 준비된 플레이어 수 카운트
    private int bombsSpawned = 0; // 마스터 클라이언트만 제어
    private Coroutine countdownCoroutine;
    
    private const string READY_KEY = "IsReadyForGame"; 
    private const int GAME_START_COUNTDOWN = 5;

    // ==========================================================
    // 초기화 및 루프
    // ==========================================================
    void Awake()
    {
        PV = GetComponent<PhotonView>();
        Instance = this;
        
        // 룸에 있는 플레이어가 2명이 아닐 경우 (예: 테스트 중) 오류 방지
        if (PhotonNetwork.CurrentRoom.PlayerCount != 2)
        {
            Debug.LogWarning("현재 2인 플레이가 아닙니다. 게임 로직에 오류가 발생할 수 있습니다.");
        }
    }

    void Start()
    {
        // 1. 초기 UI 설정: HowToPlayPanel만 활성화
        howToPlayPanel.SetActive(true);
        inGamePanel.SetActive(false);
        victoryPanel.SetActive(false);
        losePanel.SetActive(false);
        timesUpPanel.SetActive(false);
        
        // 2. 시간 초기화
        currentTime = timeLimit;

        // 3. Ready 상태 초기화 (Lobby에서 설정한 Ready 상태와 다름)
        SetLocalPlayerReadyState(false);
    }

    void Update()
    {
        // 1. 마스터 클라이언트만 시간 감소 로직을 수행
        if (PhotonNetwork.IsMasterClient && isGameActive && !isGameOver)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                // 시간 초과 시 모든 클라이언트에게 RPC 호출
                PV.RPC("RpcTimeIsUp", RpcTarget.All);
            }
        }
        
        // 2. 모든 클라이언트에서 UI 업데이트 (시간은 동기화된 값 사용)
        UpdateUIText();
    }
    
    // UI 텍스트 업데이트
    void UpdateUIText()
    {
        if (timeText != null)
        {
            timeText.text = "Time: " + Mathf.CeilToInt(currentTime).ToString(); 
        }
    }

    // ==========================================================
    // ⭐ Ready 및 게임 시작 로직
    // ==========================================================
    
    // Ready 버튼 클릭 시
    public void OnReadyButtonClicked()
    {
        // Ready 상태 토글 대신, 한 번 누르면 Ready 상태가 되는 것으로 가정
        readyButton.interactable = false;
        SetLocalPlayerReadyState(true);
    }
    
    // 로컬 플레이어 Ready 상태를 Custom Property로 설정
    private void SetLocalPlayerReadyState(bool isReady)
    {
        ExitGames.Client.Photon.Hashtable props = new() { { READY_KEY, isReady } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // 플레이어 속성 변경 시 (Ready 상태 변경 감지)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey(READY_KEY))
        {
            CheckAllPlayersReady();
        }
    }

    // 모든 플레이어가 Ready 상태인지 확인
    private void CheckAllPlayersReady()
    {
        if (isGameActive || PhotonNetwork.PlayerList.Length != 2) return;

        int readyCount = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isReadyValue;
            if (p.CustomProperties.TryGetValue(READY_KEY, out isReadyValue) && (bool)isReadyValue)
            {
                readyCount++;
            }
        }

        if (readyCount == 2)
        {
            // 모든 플레이어가 준비 완료. Master Client만 카운트다운 시작 RPC 호출
            if (PhotonNetwork.IsMasterClient && countdownCoroutine == null)
            {
                countdownCoroutine = StartCoroutine(MasterStartCountdownRoutine());
            }
        }
    }
    
    // 마스터 클라이언트에서 실행되는 카운트다운 코루틴
    private IEnumerator MasterStartCountdownRoutine()
    {
        // 모든 클라이언트에게 카운트다운 시작 알림
        PV.RPC("RpcUpdateCountdownText", RpcTarget.All, GAME_START_COUNTDOWN);

        for (int i = GAME_START_COUNTDOWN; i > 0; i--)
        {
            yield return new WaitForSeconds(1f);
            PV.RPC("RpcUpdateCountdownText", RpcTarget.All, i - 1);
        }
        
        // 카운트다운 완료 후 게임 시작
        PV.RPC("RpcStartGame", RpcTarget.All);
        countdownCoroutine = null;
    }

    // 모든 클라이언트에서 카운트다운 텍스트 업데이트
    [PunRPC]
    public void RpcUpdateCountdownText(int remainingTime)
    {
        howToPlayPanel.SetActive(true); // 카운트다운 중에는 HowToPlayPanel 유지
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            if (remainingTime > 0)
            {
                countdownText.text = $"Game Starts in {remainingTime}...";
            }
            else
            {
                countdownText.text = "GO!";
            }
        }
    }
    
    // 게임 시작 (모든 클라이언트)
    [PunRPC]
    public void RpcStartGame()
    {
        isGameActive = true;
        
        // UI 전환
        howToPlayPanel.SetActive(false);
        inGamePanel.SetActive(true);
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        // 플레이어 스폰
        SpawnPlayer();
        
        // 마스터 클라이언트만 게임 오브젝트 생성 로직 시작
        if (PhotonNetwork.IsMasterClient)
        {
            StartMasterGameLogic();
        }
    }
    
    // ==========================================================
    // ⭐ 스폰 및 오브젝트 생성 로직 (Master Client Only)
    // ==========================================================

    private void SpawnPlayer()
    {
        Vector3 spawnPosition;
        
        // 액터 넘버가 1인 플레이어가 Master Client입니다.
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            spawnPosition = ownerSpawnPoint;
        }
        else
        {
            spawnPosition = player2SpawnPoint;
        }

        if (playerPrefab != null)
        {
            // NetworkManager의 Spawn() 로직을 대체
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
            Debug.Log($"Player spawned at: {spawnPosition}");
        }
        else
        {
            Debug.LogError("Player Prefab이 InGameManager에 연결되지 않았습니다!");
        }
    }
    
    public void StartMasterGameLogic()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        SpawnWeaponObjects();
        SpawnObstacleObjects();
        SpawnDrugObjects();
        StartCoroutine(SpawnBombsRoutine());
    }

    // 무기 생성 로직 (Trash)
    private void SpawnWeaponObjects()
    {
        if (trashPrefab == null) return;

        for (int i = 0; i < totalTrashCount; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            PhotonNetwork.Instantiate(trashPrefab.name, randomPosition, Quaternion.identity);
        }
        Debug.Log($"Trash {totalTrashCount}개 스폰 완료.");
    }
    
    // 장애물 생성 로직
    private void SpawnObstacleObjects()
    {
        if (obstaclePrefab == null) return;

        for (int i = 0; i < totalObstacleCount; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            PhotonNetwork.Instantiate(obstaclePrefab.name, randomPosition, Quaternion.identity);
        }
        Debug.Log($"장애물 {totalObstacleCount}개 스폰 완료.");
    }
    
    // Drug 생성 로직
    private void SpawnDrugObjects()
    {
        if (drugPrefab == null) return;
        
        for (int i = 0; i < totalDrugCount; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            // Drug는 기존 GameManager처럼 특정 설정을 필요로 할 수 있지만, 초안에서는 일단 생성만
            PhotonNetwork.Instantiate(drugPrefab.name, randomPosition, Quaternion.identity);
        }
        Debug.Log($"Drug {totalDrugCount}개 스폰 완료.");
    }

    // 폭탄 생성 로직
    private IEnumerator SpawnBombsRoutine()
    {
        if (bombPrefab == null) yield break;
        
        while (isGameActive && bombsSpawned < maxBombsToSpawn)
        {
            float waitTime = Random.Range(minBombSpawnTime, maxBombSpawnTime);
            yield return new WaitForSeconds(waitTime);

            if (!isGameActive) break; 
            
            Vector3 randomPosition = GetRandomSpawnPosition();
            PhotonNetwork.Instantiate(bombPrefab.name, randomPosition, Quaternion.identity);

            bombsSpawned++;
            Debug.Log($"폭탄 스폰됨: {randomPosition}");
        }
    }
    
    // 랜덤 스폰 위치
    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(
            Random.Range(mapBounds.min.x, mapBounds.max.x),
            Random.Range(mapBounds.min.y, mapBounds.max.y),
            0 
        );
    }
    
    // ==========================================================
    // ⭐ 게임 종료 로직
    // ==========================================================

    // 플레이어 스크립트에서 호출되어야 함 (Master Client에게 RPC 요청)
    public void PlayerDied(Player deadPlayer)
    {
        if (isGameOver) return;
        
        // 마스터 클라이언트에게 게임 종료 처리를 요청
        PV.RPC("RpcHandlePlayerDeath", RpcTarget.MasterClient, deadPlayer.ActorNumber);
    }

    [PunRPC]
    public void RpcHandlePlayerDeath(int deadPlayerActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || isGameOver) return;
        
        Player winner = null;
        Player loser = null;

        // 승자와 패자 결정
        foreach(Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == deadPlayerActorNumber)
            {
                loser = p;
            }
            else
            {
                winner = p;
            }
        }

        // 모든 클라이언트에게 종료 결과 전송
        PV.RPC("RpcGameOver", RpcTarget.All, winner.ActorNumber, loser.ActorNumber);
    }

    // 시간 초과 시 (Master Client의 Update에서 호출)
    [PunRPC]
    public void RpcTimeIsUp()
    {
        if (isGameOver) return;
        isGameOver = true;
        isGameActive = false;
        
        // UI 전환: Times Up
        inGamePanel.SetActive(false);
        timesUpPanel.SetActive(true);
        Debug.Log("시간 초과! 무승부 처리");
    }

    // 게임 종료 RPC (모든 클라이언트에서 실행)
    [PunRPC]
    public void RpcGameOver(int winnerActorNumber, int loserActorNumber)
    {
        if (isGameOver) return;
        isGameOver = true;
        isGameActive = false;
        
        inGamePanel.SetActive(false);
        
        if (PhotonNetwork.LocalPlayer.ActorNumber == winnerActorNumber)
        {
            // 로컬 플레이어가 승자
            victoryPanel.SetActive(true);
            Debug.Log("승리!");
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == loserActorNumber)
        {
            // 로컬 플레이어가 패자
            losePanel.SetActive(true);
            Debug.Log("패배!");
        }
    }
    
    // ==========================================================
    // ⭐ 포스트 게임 액션
    // ==========================================================

    // Victory/Lose/TimesUp Panel의 Lobby 버튼 클릭 시
    public void OnLobbyButtonClicked()
    {
        // 1. 방 나가기
        PhotonNetwork.LeaveRoom();
        
        // 2. 로비 씬 로드는 OnLeftRoom 콜백에서 처리됩니다.
    }
    
    // 방을 나간 후 로비 씬 로드
    public override void OnLeftRoom()
    {
        // 이 함수는 JoinRoomPanel의 Exit 버튼 (방 나가기)과 포스트 게임의 Lobby 버튼 모두에서 호출됩니다.
        if (isGameOver || !isGameActive) // 게임이 끝났거나, InGamePanel이 활성화되기 전이라면
        {
            // Lobby Scene으로 전환
            PhotonNetwork.LoadLevel(lobbySceneName);
        }
        else
        {
            // 게임 도중에 나간 경우 (예상치 못한 경우), 일단 Lobby로 복귀
            PhotonNetwork.LoadLevel(lobbySceneName);
        }
    }

    // ==========================================================
    // ⭐ IPunObservable 구현: 핵심 변수 동기화
    // ==========================================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 🚀 마스터 클라이언트만 전송
            stream.SendNext(currentTime);
            stream.SendNext(isGameActive);
            stream.SendNext(isGameOver);
            stream.SendNext(bombsSpawned);
        }
        else
        {
            // 📥 다른 클라이언트들은 수신
            currentTime = (float)stream.ReceiveNext();
            isGameActive = (bool)stream.ReceiveNext();
            isGameOver = (bool)stream.ReceiveNext();
            bombsSpawned = (int)stream.ReceiveNext();
        }
    }
}