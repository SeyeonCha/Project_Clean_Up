using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class LoginManager : MonoBehaviourPunCallbacks
{
    [Header("UI Objects")]
    public GameObject LogoText;             // "LogoText" 오브젝트
    public TMP_Text instructionText;        // "instructiontext" 텍스트 컴포넌트
    public GameObject EnterPanel;           // "#01_EnterPanel" (닉네임 입력 및 연결)
    public TMP_InputField NickNameInput;    // 닉네임 입력 인풋
    public GameObject ConnectButton;    

    [Header("Scene Settings")]
    public string lobbySceneName = "LobbyScene";

    // ==========================================================
    // 초기화 및 시작
    // ==========================================================

    void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // 1. 초기 UI 상태 설정: LogoText, instructionText만 활성화
        LogoText.SetActive(true);
        instructionText.text = "Press Spacebar to Start"; 
        EnterPanel.SetActive(false);
    }

    // ==========================================================
    // 업데이트 및 입력 처리
    // ==========================================================
    void Update()
    {
        // 스페이스바 입력 감지 (로고 화면에서만)
        if (LogoText.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            ShowEnterPanel();
        }
    }

    private void ShowEnterPanel()
    {
        LogoText.SetActive(false);
        instructionText.text = "";
        EnterPanel.SetActive(true);
    }

    // ==========================================================
    // 액션 버튼 함수 (Main Panel: JoinGamePanel)
    // ==========================================================

    // 1. Connect 버튼 (EnterPanel)
    public void OnConnectButtonClicked()
    {
        if (string.IsNullOrEmpty(NickNameInput.text))
        {
            instructionText.text = "Please enter your nickname.";
            return;
        }

        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        
        instructionText.text = "connecting server...";
        ConnectButton.SetActive(false); 
        
        PhotonNetwork.ConnectUsingSettings();
    }

    // ==========================================================
    // Photon 콜백 함수
    // ==========================================================

    public override void OnConnectedToMaster()
    {
        Debug.Log($"Server Connected. NickName: {PhotonNetwork.LocalPlayer.NickName}");
        
        EnterPanel.SetActive(false);
        LogoText.SetActive(false);
        instructionText.text = "";

        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log($"Attempting to load scene: {lobbySceneName}"); 
            PhotonNetwork.LoadLevel(lobbySceneName);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from server: {cause}");
        instructionText.text = $"Disconnected. Reason: {cause}. Press SPACE to try again.";
        
        // 연결 끊김 시 초기 상태로 복구
        ConnectButton.SetActive(true);
        LogoText.SetActive(true);
        EnterPanel.SetActive(false);
    }
}