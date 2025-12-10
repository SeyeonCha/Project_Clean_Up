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
    public GameObject stopPanel;

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
    
    // ⭐ Drug 관련 세부 설정 (GameManager에서 통합됨)
    [System.Serializable]
    public struct BoostSetting
    {
        public float rotationBoost;
        public float speedBoost;
        public float duration;
    }
    [Header("Drug Boost Settings")]
    public BoostSetting[] drugBoostSettings;
    [Header("Drug Visuals")]
    public Color[] drugColorsForSpawning;

    // 장애물 변수
    public int totalObstacleCount = 2; 

    [Header("Bomb Spawning")]
    public float minBombSpawnTime = 10f;     
    public float maxBombSpawnTime = 25f;     
    public int maxBombsToSpawn = 2;
    
    public Bounds mapBounds = new(Vector3.zero, new Vector3(20, 10, 0)); // 스폰 영역
    
    // ==========================================================
    // ⭐ Private & Network Variables
    // ==========================================================
    private PhotonView PV;
    private float currentTime;
    private bool isGameActive = false;
    private bool isGameOver = false;
    private bool isCountdownActive = false;
    private int bombsSpawned = 0; // 마스터 클라이언트만 제어
    private Coroutine countdownCoroutine;
    private bool isPaused = false;
    
    private const string READY_KEY = "IsReadyForGame"; 
    private const int GAME_START_COUNTDOWN = 5;

    // ==========================================================
    // ⭐ IPunObservable 구현: 핵심 변수 동기화
    // ==========================================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentTime);
            stream.SendNext(isGameActive);
            stream.SendNext(isGameOver);
            stream.SendNext(bombsSpawned);
        }
        else
        {
            currentTime = (float)stream.ReceiveNext();
            isGameActive = (bool)stream.ReceiveNext();
            isGameOver = (bool)stream.ReceiveNext();
            bombsSpawned = (int)stream.ReceiveNext();
        }
    }

    // ==========================================================
    // 초기화 및 루프
    // ==========================================================
    void Awake()
    {
        PV = GetComponent<PhotonView>();
        Instance = this;
    }

    public void StartMasterGameLogic()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnWeaponObjects();
            SpawnObstacleObjects();
            SpawnDrugObjects();
            StartCoroutine(SpawnBombsRoutine());
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

        // // ⭐ 개선: 2인 플레이 확인은 Start에서 (CurrentRoom 정보가 안정적일 때)
        // if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.MaxPlayers != 2)
        // {
        //      Debug.LogWarning("This room is configured for non-2 player max count.");
        // }
    }

    void Update()
    {
        // 1. 마스터 클라이언트만 시간 감소 로직을 수행
        if (PhotonNetwork.IsMasterClient && isGameActive && !isCountdownActive && !isGameOver)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                PV.RPC("RpcTimeIsUp", RpcTarget.All);
            }
        }
        
        // 2. 모든 클라이언트에서 UI 업데이트 (시간은 동기화된 값 사용)
        UpdateUIText();

        if (Input.GetKeyDown(KeyCode.Escape) && isGameActive && !isGameOver)
        {
            ToggleStopPanel();
        }
    }

    // ⭐ PlayerMovement.cs에서 사용될 핵심 메서드 (GameManager.IsGameActive() 대체)
    public bool IsMovementAllowed()
    {
        // 게임 활성화 중이고, 카운트다운 중이 아니며, 게임 오버가 아닐 때, 그리고 일시 정지 상태가 아닐 때만 허용
        return isGameActive && !isCountdownActive && !isGameOver && !isPaused;
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

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "Waiting for other player to ready up...";
        }

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
        PV.RPC("RpcUpdateCountdownText", RpcTarget.All, GAME_START_COUNTDOWN);
        
        for (int i = GAME_START_COUNTDOWN; i > 0; i--)
        {
            yield return new WaitForSeconds(1f);
            PV.RPC("RpcUpdateCountdownText", RpcTarget.All, i - 1);
        }

        yield return new WaitForSeconds(1f); // 0초(GO!)가 표시되는 시간을 1초 보장
        
        PV.RPC("RpcStartGame", RpcTarget.All);
        countdownCoroutine = null;
    }

    // 모든 클라이언트에서 카운트다운 텍스트 업데이트
    [PunRPC]
    public void RpcUpdateCountdownText(int remainingTime)
    {
        isCountdownActive = true;

        howToPlayPanel.SetActive(false); 
        inGamePanel.SetActive(true);
        
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
        isCountdownActive = false;
        
        // UI 전환
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
        if (!PhotonNetwork.IsMasterClient || obstaclePrefab == null)
        {
            if (obstaclePrefab == null) Debug.LogError("Obstacle Prefab이 GameManager에 연결되지 않았습니다!");
            return;
        }

        for (int i = 0; i < totalObstacleCount; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            
            // PhotonNetwork.Instantiate를 사용하여 모든 클라이언트에서 장애물 생성
            PhotonNetwork.Instantiate(obstaclePrefab.name, randomPosition, Quaternion.identity);
        }
        Debug.Log($"장애물 {totalObstacleCount}개 스폰 완료.");
    }
    
    // Drug 생성 로직
    private void SpawnDrugObjects()
    {
        if (!PhotonNetwork.IsMasterClient || drugPrefab == null)
        {
            if (drugPrefab == null) Debug.LogError("Drug Prefab이 GameManager에 연결되지 않았습니다!");
            return;
        }
        
        int settingsCount = drugBoostSettings.Length;
        int colorCount = drugColorsForSpawning.Length; // ✨ 색상 배열 크기

        for (int i = 0; i < totalDrugCount; i++)
        {
            // 설정된 개수(2개)와 색상 배열 크기를 초과하지 않도록 방지
            if (i >= settingsCount || i >= colorCount) break; 

            Vector3 randomPosition = GetRandomSpawnPosition();
            
            GameObject drugObj = PhotonNetwork.Instantiate(drugPrefab.name, randomPosition, Quaternion.identity);
            
            BoostSetting setting = drugBoostSettings[i];
            Color itemColor = drugColorsForSpawning[i]; 
            
            PhotonView drugPV = drugObj.GetComponent<PhotonView>();
        
            if (drugPV != null)
            {
                // RPC를 통해 모든 클라이언트에게 색상 및 부스트 정보를 전달합니다.
                // Color 타입은 RPC 인수로 바로 전달할 수 없으므로, R, G, B 값을 float로 분리하여 전달합니다.
                drugPV.RPC("RpcInitializeBoostAndColor", RpcTarget.All,
                    setting.rotationBoost, 
                    setting.speedBoost, 
                    setting.duration,
                    itemColor.r, // Color.r
                    itemColor.g, // Color.g
                    itemColor.b  // Color.b
                );
            }
        }
        Debug.Log($"Drug {totalDrugCount}개 스폰 완료 및 색상/부스트 설정 할당.");
    }

    // 폭탄 생성 로직
    private IEnumerator SpawnBombsRoutine()
    {
        if (!PhotonNetwork.IsMasterClient || bombPrefab == null) yield break;
        
        while (!isGameOver && bombsSpawned < maxBombsToSpawn)
        {
            float waitTime = Random.Range(minBombSpawnTime, maxBombSpawnTime);
            yield return new WaitForSeconds(waitTime);

            if (isGameOver) break; 
            
            Vector3 randomPosition = GetRandomSpawnPosition();
            
            // ✨ PhotonNetwork.Instantiate를 사용하여 모든 클라이언트에서 생성
            GameObject bombObj = PhotonNetwork.Instantiate(bombPrefab.name, randomPosition, Quaternion.identity);

            // ⭐ 디버그 로그 추가: 객체 생성 성공 여부 확인
            if (bombObj != null)
            {
                 Debug.Log($"Master Client: 폭탄 생성 성공 ({bombObj.name}) at {randomPosition}");
            }
            else
            {
                 Debug.LogError("Master Client: PhotonNetwork.Instantiate가 null을 반환했습니다. Prefab 이름을 확인하세요.");
            }

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
    
    // ⭐ 새로 추가: 상대방 퇴장 시 승리 처리
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        
        // ⭐ 개선: 게임 활성화 상태 여부와 관계없이 처리
        // 1. 카운트다운 중이거나,
        if (isCountdownActive || isGameActive)
        {
            // 2. 다른 플레이어가 나갔는데 방에 1명만 남은 경우 (남은 플레이어가 승자)
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                // 현재 마스터 클라이언트(혹은 새로운 마스터)만 판정 및 RPC 호출
                if (PhotonNetwork.IsMasterClient)
                {
                    Player winner = PhotonNetwork.LocalPlayer; // 남은 사람이 승자
                    PV.RPC("RpcHandleOpponentExit", RpcTarget.All, winner.ActorNumber);
                }
            }
        }
        else if (!isGameActive)
        {
            // 게임 시작 전에 나간 경우 (로비 씬으로의 자동 복귀 없음)
            Debug.Log("Player left during the setup phase.");
        }
    }

    // ⭐ OnMasterClientSwitched 콜백 추가 (Master Client가 나가면 승자 판정 기회 제공)
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // 이전 Master Client가 나간 후 새로운 Master Client가 되었을 때
        if (isGameActive && !isGameOver)
        {
            // 방에 한 명만 남았다면 (퇴장 처리 로직이 OnPlayerLeftRoom에서 곧 실행되거나 이미 실행됨)
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                // 새로운 Master Client가 승자 판정을 담당합니다.
                Player winner = PhotonNetwork.LocalPlayer;
                PV.RPC("RpcHandleOpponentExit", RpcTarget.All, winner.ActorNumber);
            }
        }
    }

    [PunRPC]
    public void RpcHandleOpponentExit(int winnerActorNumber)
    {
        if (isGameOver) return;
        
        isGameOver = true;
        isGameActive = false;
        inGamePanel.SetActive(false);
        
        // UI 전환: 승자만 Victory Panel 활성화
        if (PhotonNetwork.LocalPlayer.ActorNumber == winnerActorNumber)
        {
            victoryPanel.SetActive(true);
        }
        else
        {
            // 이 코드는 비마스터 클라이언트가 승리자가 되어야 함을 알게 되었을 때 실행됩니다.
            // 실제로 이 코드는 비승리자(나간 사람)에게는 도달하지 않으므로, 
            // 남은 플레이어가 승리 패널을 켜는 것이 핵심입니다.
            Debug.Log("Game ended due to opponent leaving.");
        }
        // 나간 플레이어가 패배 패널을 볼 일은 없으므로, 승리 패널 활성화만 신경씁니다.
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
    // ⭐ 정지 패널 (Stop Panel) 로직
    // ==========================================================

    // ESC 키 입력 시 호출되어 패널을 토글하고 상태를 업데이트합니다.
    public void ToggleStopPanel()
    {
        if (stopPanel == null || !isGameActive || isGameOver || isCountdownActive) return;

        // 상태 토글
        isPaused = !isPaused; 

        // UI 활성화/비활성화
        stopPanel.SetActive(isPaused);
        
        // ⭐ 마스터 클라이언트만 시간을 멈춥니다.
        // 비마스터 클라이언트는 어차피 currentTime을 동기화 받으므로, 이 코드는 UI에만 영향을 줍니다.
        // 하지만 게임의 핵심 상태이므로 마스터 클라이언트에서 제어합니다.
        if (PhotonNetwork.IsMasterClient)
        {
            // 시간을 멈추거나 다시 시작하는 추가 로직이 필요하다면 여기에 작성 (현재는 isPaused로 Update에서 제어)
        }
    }

    // "게임 계속하기" 버튼 클릭 시 연결
    public void OnResumeButtonClicked()
    {
        if (isPaused)
        {
            ToggleStopPanel(); // 정지 상태를 해제합니다.
        }
    }

    // "로비로 돌아가기" 버튼 클릭 시 연결
    public void OnLobbyButtonClickedFromPause()
    {
        // 기존의 OnLobbyButtonClicked 함수를 재사용합니다.
        OnLobbyButtonClicked(); 
    }
}