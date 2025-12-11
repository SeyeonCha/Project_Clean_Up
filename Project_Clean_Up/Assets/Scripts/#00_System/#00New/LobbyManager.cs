using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    // --- UI Panels ---
    [Header("Lobby Panels")]
    public GameObject LobbyPanel;           // ⭐ #01_LobbyPanel (이전의 JoinGamePanel)
    public GameObject JoinRoomPanel;        // ⭐ #02_JoinRoomPanel (기존의 로비 UI)
    
    // --- Common UI ---
    [Header("Common UI")]
    public TMP_Text instructionText;
    public TMP_Text roomNameText; 
    
    // --- Lobby Panel #01 (Room Selection) ---
    [Header("Lobby Panel Controls")]
    public TMP_Text playerNameText;         // LoginManager에서 옮겨옴
    
    // Create/Search Sub-Panels 
    public GameObject CreateRoomPanel;
    public TMP_InputField CreateRoomNameInput;

    public GameObject SearchRoomPanel;
    public TMP_InputField SearchRoomNameInput;

    // --- Join Room Panel #02 (Game Ready) ---
    [Header("Join Room Panel Controls")]
    public Button exitButton;
    public Button readyButton;
    public Button startButton;
    public GameObject playerUI_1; 
    public TMP_Text nicknameText_1;
    public TMP_Text readyStateText_1;
    public GameObject playerUI_2; 
    public TMP_Text nicknameText_2;
    public TMP_Text readyStateText_2;

    // --- Private Variables ---
    private const string READY_KEY = "IsReady"; 
    private const int MAX_PLAYERS = 2;
    private bool isReady = false; 
    private Coroutine countdownCoroutine;
    private const int START_COUNTDOWN_TIME = 3;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";
    private PhotonView PV;

    // ==========================================================
    // 초기화 및 시작
    // ==========================================================

    void Start()
    {
        if (!PhotonNetwork.AutomaticallySyncScene)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            Debug.Log("PhotonNetwork.AutomaticallySyncScene set to TRUE.");
        }

        PV = GetComponent<PhotonView>();
        if (PV == null)
        {
            Debug.LogError("LobbyManager requires a PhotonView component on the same GameObject.");
        }
        
        // 씬 로드 후 초기 UI 설정
        InitializeLobbyUI();
    }

    private void InitializeLobbyUI()
    {
        // 1. 초기 패널 상태: #01_LobbyPanel 활성화
        LobbyPanel.SetActive(true);
        JoinRoomPanel.SetActive(false);
        CreateRoomPanel.SetActive(false);
        SearchRoomPanel.SetActive(false);

        instructionText.text = "Select a room action.";

        // 2. 플레이어 이름 출력 (LoginManager에서 설정된 닉네임 사용)
        if (playerNameText != null && !string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
        {
            playerNameText.text = $"Hello,\n{PhotonNetwork.LocalPlayer.NickName}";
        }
        
        // 로비 UI 요소 초기화 (JoinRoomPanel 요소들은 방 참가 후에 설정됨)
        startButton.gameObject.SetActive(false);
        SetLocalPlayerReady(false); // 로컬 Ready 상태 초기화
    }

    // ==========================================================
    // ⭐ Lobby Panel #01 액션 함수 (방 선택)
    // ==========================================================
    
    // Quick Match 버튼
    public void OnQuickMatchClicked()
    {
        instructionText.text = "Searching for a quick match room...";
        PhotonNetwork.JoinOrCreateRoom("QuickRoom", new RoomOptions { MaxPlayers = 2 }, null);
    }

    // Create Room 버튼
    public void OnCreateRoomClicked()
    {
        CreateRoomPanel.SetActive(true);
        instructionText.text = "";
        CreateRoomNameInput.ActivateInputField(); 
    }
    
    // Search Room 버튼
    public void OnSearchRoomClicked()
    {
        SearchRoomPanel.SetActive(true);
        instructionText.text = "";
        SearchRoomNameInput.ActivateInputField(); 
    }

    // Confirm 버튼 (CreateRoomPanel)
    public void OnCreateRoomConfirmed()
    {
        string roomName = CreateRoomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            instructionText.text = "Please enter a room name.";
            return;
        }
        
        CreateRoomPanel.SetActive(false);
        instructionText.text = $"Creating room '{roomName}'...";
        
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = MAX_PLAYERS }, null);
    }

    // Confirm 버튼 (SearchRoomPanel)
    public void OnSearchRoomConfirmed()
    {
        string roomName = SearchRoomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            instructionText.text = "Please enter a room name to join.";
            return;
        }
        
        // SearchRoomPanel.SetActive(false); // 실패 시 패널을 유지하기 위해 제거
        instructionText.text = $"Attempting to join room '{roomName}'...";
        
        PhotonNetwork.JoinRoom(roomName);
    }
    
    // Cancel 버튼 (Create/Search Panel)
    public void OnCancelButtonClicked()
    {
        CreateRoomPanel.SetActive(false);
        SearchRoomPanel.SetActive(false);
        LobbyPanel.SetActive(true); // LobbyPanel로 복귀
        instructionText.text = "Select a room action.";
    }

    // 게임 완전 종료 함수 (#01_LobbyPanel의 Exit 버튼에 연결)
    public void OnGameExitClicked()
    {
        instructionText.text = "Exiting game...";
        
        // Photon 서버와의 연결을 끊습니다.
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        // 애플리케이션 종료
        Application.Quit();

        #if UNITY_EDITOR
        // 에디터에서 실행 중일 경우 플레이 모드 중지
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // ==========================================================
    // ⭐ Join Room Panel #02 액션 함수 (로비 상태)
    // ==========================================================

    // 1. Exit 버튼 (방 나가기)
    public void OnExitButtonClicked()
    {
        // 게임 시작 카운트 중이라면 취소
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        // 방 나가기
        PhotonNetwork.LeaveRoom();
    }

    // 2. Ready 버튼
    public void OnReadyButtonClicked()
    {
        // 로컬 플레이어의 Ready 상태 토글
        isReady = !isReady;
        SetLocalPlayerReady(isReady);
        
        // 카운트 중 Ready를 취소하면 게임 시작 취소
        if (!isReady && countdownCoroutine != null)
        {
            CancelGameStart();
        }
    }
    
    // 3. Start 버튼 (Master Client Only)
    public void OnStartButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            // [English Instruction]
            instructionText.text = "Only the Room Owner can start the game.";
            return;
        }

        Player[] players = PhotonNetwork.PlayerList;

        // 1. 플레이어 수 체크
        if (players.Length < MAX_PLAYERS)
        {
            // [English Instruction]
            instructionText.text = $"Not enough players. Needs {MAX_PLAYERS} players to start.";
            return;
        }

        // 2. 모든 플레이어 Ready 상태 체크
        foreach (Player p in players)
        {
            object isReadyValue;
            bool isPlayerReady = p.CustomProperties.TryGetValue(READY_KEY, out isReadyValue) && (bool)isReadyValue;

            if (!isPlayerReady)
            {
                // [English Instruction]
                instructionText.text = $"All players must be ready to start the game.";
                return;
            }
        }

        // 3. 조건 충족 -> 카운트다운 시작
        if (countdownCoroutine == null)
        {
            countdownCoroutine = StartCoroutine(StartGameCountdown());
        }
        else
        {
            // 이미 카운트다운 중이라면 무시
        }
    }

    private void UpdateLobbyUI()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        // Player List (Sorted by Actor Number to ensure Owner is always #1)
        Player[] players = PhotonNetwork.PlayerList;
        Player owner = PhotonNetwork.MasterClient;
        Player otherPlayer = null;

        // Find the other player (if exists)
        foreach (Player p in players)
        {
            if (p != owner)
            {
                otherPlayer = p;
                break;
            }
        }
        
        // --- Player UI 1 (Owner) ---
        DisplayPlayerInfo(nicknameText_1, readyStateText_1, owner);

        // --- Player UI 2 (Other Player) ---
        if (otherPlayer != null)
        {
            playerUI_2.SetActive(true);
            DisplayPlayerInfo(nicknameText_2, readyStateText_2, otherPlayer);
        }
        else
        {
            // 방에 한 명만 있는 경우
            playerUI_2.SetActive(false);
            instructionText.text = "Waiting for another player...";
        }
    }

    private void DisplayPlayerInfo(TMP_Text nickname, TMP_Text readyStateText, Player player)
    {
        nickname.text = player.NickName;

        // Custom Property에서 Ready 상태 가져오기
        object isReadyValue;
        bool isPlayerReady = player.CustomProperties.TryGetValue(READY_KEY, out isReadyValue) && (bool)isReadyValue;

        if (isPlayerReady)
        {
            // [English Instruction]
            readyStateText.text = "READY!";
        }
        else
        {
            // [English Instruction]
            readyStateText.text = "NOT READY";
        }
    }

    // ==========================================================
    // 상태 동기화 및 카운트다운 로직
    // ==========================================================

    private void SetLocalPlayerReady(bool readyState)
    {
        isReady = readyState;
        
        // Custom Property를 통해 Ready 상태를 네트워크로 동기화
        ExitGames.Client.Photon.Hashtable props = new() { { READY_KEY, readyState } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
    
    private void CancelGameStart()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            // [English Instruction]
            instructionText.text = "Game start cancelled by a player.";
        }
    }

    private IEnumerator StartGameCountdown()
    {
        // ⭐ Master Client만 이 코루틴을 실행합니다.
        
        // 1. 초기 메시지 설정 (RPC로 모든 클라이언트에게 알림)
        PV.RPC("RpcUpdateCountdown", RpcTarget.All, START_COUNTDOWN_TIME);
        
        // Master Client만 카운트다운을 실행하고 LoadLevel을 호출합니다.
        for (int i = START_COUNTDOWN_TIME; i > 0; i--)
        {
            // ⭐ 1초 대기
            yield return new WaitForSeconds(1f);

            // 2. 매 초마다 모든 플레이어가 여전히 Ready 상태인지 확인
            if (!CheckAllPlayersReadyStatus())
            {
                // Ready 상태 취소 시, 모든 클라이언트에게 카운트다운 취소 알림
                PV.RPC("RpcCancelCountdown", RpcTarget.All);
                yield break; 
            }
            
            // 3. 남은 시간 RPC로 모든 클라이언트에게 전송
            PV.RPC("RpcUpdateCountdown", RpcTarget.All, i - 1);
        }

        // 4. 카운트다운 완료 후 게임 시작 (Master Client만 씬 로드)
        if (PhotonNetwork.IsMasterClient)
        {
            // 모든 클라이언트에게 게임 시작 메시지 알림
            PV.RPC("RpcNotifyGameStart", RpcTarget.All);

            // PhotonNetwork.LoadLevel 호출
            PhotonNetwork.LoadLevel(gameSceneName);
        }
        
        countdownCoroutine = null;
    }

    private bool CheckAllPlayersReadyStatus()
    {
         foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isReadyValue;
            bool isPlayerReady = p.CustomProperties.TryGetValue(READY_KEY, out isReadyValue) && (bool)isReadyValue;
            if (!isPlayerReady)
            {
                return false;
            }
        }
        return true;
    }

    [PunRPC]
    public void RpcUpdateCountdown(int remainingTime)
    {
        if (remainingTime > 0)
        {
            instructionText.text = $"Game starting in {remainingTime} seconds...";
        }
        else
        {
            // 카운트다운이 0이 되었으나, Master Client의 LoadLevel이 아직 실행되지 않았을 경우의 메시지
            instructionText.text = "Game starting now...";
        }
    }

    // 2. 카운트다운 취소 RPC
    [PunRPC]
    public void RpcCancelCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        // [English Instruction]
        instructionText.text = "Game start cancelled by a player.";
    }

    // 3. 게임 시작 알림 RPC (선택 사항: LoadLevel 직전에 깔끔한 메시지 출력용)
    [PunRPC]
    public void RpcNotifyGameStart()
    {
        instructionText.text = "Loading Game...";
    }

    // ==========================================================
    // Photon 콜백 함수 (네트워크 상태 변화)
    // ==========================================================

    // 룸 생성 또는 참가 성공 시 (LobbyPanel -> JoinRoomPanel 전환)
    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        
        // ⭐ 핵심 수정: #01_LobbyPanel 닫고 #02_JoinRoomPanel 활성화
        LobbyPanel.SetActive(false);
        CreateRoomPanel.SetActive(false);
        SearchRoomPanel.SetActive(false);
        JoinRoomPanel.SetActive(true); 

        // JoinRoomPanel UI 초기 설정
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        if (roomNameText != null && PhotonNetwork.CurrentRoom != null)
        {
            roomNameText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}";
        }
        
        instructionText.text = "Room joined successfully. Waiting for players to ready up.";
        UpdateLobbyUI();
    }
    
    // 룸 참가 실패 시 (SearchRoomPanel에서 발생 가능)
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Failed to join room: {message}");
        
        if (SearchRoomPanel.activeSelf)
        {
            instructionText.text = $"Failed to join room: The room does not exist or is full. ({message})";
            // SearchRoomPanel은 그대로 유지
        }
        else
        {
            instructionText.text = $"Failed to join room: {message}. Try Quick Match.";
            LobbyPanel.SetActive(true); // LobbyPanel로 복귀
            CreateRoomPanel.SetActive(false);
            SearchRoomPanel.SetActive(false);
        }
    }
    
    // 방을 나갔을 때 (Exit 버튼)
    public override void OnLeftRoom()
    {
        instructionText.text = "Leaving room, returning to Lobby Panel...";
        
        // 방을 나간 후 LobbyPanel (#01_LobbyPanel)로 복귀
        JoinRoomPanel.SetActive(false);
        LobbyPanel.SetActive(true); 

        // 모든 상태 초기화
        isReady = false;
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        instructionText.text = "Left the room. Select a new action.";
        
        // 씬 로드 대신 씬의 상태만 전환
    }

    // 플레이어의 Custom Property (Ready 상태) 변경 시
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey(READY_KEY))
        {
            UpdateLobbyUI(); // Ready 상태가 변경되었으므로 UI 갱신

            // 카운트다운 중 Ready 상태가 변경되었고, Ready가 false로 바뀌었다면 취소
            if (countdownCoroutine != null)
            {
                object isReadyValue;
                bool isReady = changedProps.TryGetValue(READY_KEY, out isReadyValue) && (bool)isReadyValue;

                if (!isReady)
                {
                    CancelGameStart();
                }
                // 만약 모든 플레이어가 Ready 상태였다가 해제되었다면, Start 버튼을 눌렀을 때만 취소되도록 할 수 있지만,
                // 여기서는 안전하게 Ready가 풀리면 무조건 취소하도록 합니다.
            }
            
            // Master Client는 Ready 상태 변경 시마다 게임 시작 조건 체크
            if (PhotonNetwork.IsMasterClient)
            {
                 // 모든 플레이어가 Ready 상태가 되었을 때, Start 버튼이 눌리지 않아도 게임 시작 카운트가 자동 시작될 수도 있습니다.
                 // (요청에는 없으므로 Start 버튼을 눌러야만 시작하도록 유지)
            }
        }
    }

    // 다른 플레이어가 방에 들어오거나 나갔을 때
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateLobbyUI();
        instructionText.text = $"{newPlayer.NickName} has entered the room.";
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateLobbyUI();
        instructionText.text = $"{otherPlayer.NickName} has left the room.";
        
        // 플레이어가 나가면 카운트다운 취소
        if (countdownCoroutine != null)
        {
            CancelGameStart();
        }
    }

    // Master Client가 변경되었을 때 (새로운 Master Client는 Start 버튼 활성화)
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        // [English Instruction]
        instructionText.text = $"{newMasterClient.NickName} is the new Room Owner.";
    }
}