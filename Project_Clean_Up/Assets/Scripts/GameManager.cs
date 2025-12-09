using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; 
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameManager Instance; // 싱글톤 적용
    
    // ====== Photon 설정 ======
    private PhotonView PV; // 이 오브젝트의 PhotonView
    
    // ====== UI 및 메세지 설정 ======
    public TextMeshProUGUI timeText;
    // public TextMeshProUGUI remainingText; 
    public TextMeshProUGUI endText; 
    public GameObject retryButton;
    public GameObject retryPanel;
    
    [Header("Game End Messages")]
    public string winMessage = "GAME CLEAR!";
    public string loseMessage = "GAME OVER!";

    // ====== 게임 시간 설정 (동기화 대상) ======
    public float startingTime = 60f;
    private float currentTime;
    private bool isGameOver = false;

    // 무기 관련 변수
    [Header("Trash & Spawning")]
    public GameObject trashPrefab;
    public int totalTrashCount = 5;
    public Sprite[] trashImages; // 공격체의 이미지들 <- 인스펙터

    // 부품 관련 변수
    public GameObject partPrefab; // 부품 프리팹 (Inspector에서 연결)
    // private int totalPartTypes = 4;
    public Sprite[] partsImages; // 부품 이미지 <- 4개!
    private int nextPartId = 0;

    // Drug 관련 변수
    public GameObject drugPrefab; // Drug 프리팹 (Inspector에서 연결)
    public int totalDrugCount = 2; // 생성할 Drug 개수 설정
    [Header("Drug Visuals")]
    public Color[] drugColorsForSpawning;

    [System.Serializable]
    public struct BoostSetting
    {
        public float rotationBoost;
        public float speedBoost;
        public float duration;
    }
    
    [Header("Drug Boost Settings")]
    public BoostSetting[] drugBoostSettings;

    // 장애물 관련 변수
    public GameObject obstaclePrefab; // 장애물 프리팹 (Inspector에서 연결)
    public int totalObstacleCount = 2; // 생성할 장애물 개수 설정

    public Bounds mapBounds = new Bounds(Vector3.zero, new Vector3(20, 10, 0));
    
    // ✨ 동기화 대상
    // private int trashCollected = 0; 
    //private int partsCollected = 0;
    public bool ExperimentCompleted = false;

    public GameObject experimentButton;

    // 폭탄 관련 변수
    [Header("Bomb Spawning")]
    public GameObject bombPrefab;           
    public float minBombSpawnTime = 10f;     
    public float maxBombSpawnTime = 25f;     

    public int maxBombsToSpawn = 2;
    private int bombsSpawned = 0; // ✨ 동기화 대상 (마스터만 제어)

    // ==========================================================
    // ⭐ IPunObservable 구현: 핵심 변수 동기화
    // ==========================================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 🚀 마스터 클라이언트만 전송
            stream.SendNext(currentTime);
            //stream.SendNext(trashCollected);
            stream.SendNext(isGameOver);
            stream.SendNext(bombsSpawned);
        }
        else
        {
            // 📥 다른 클라이언트들은 수신
            currentTime = (float)stream.ReceiveNext();
            //trashCollected = (int)stream.ReceiveNext();
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
        Instance = this; // 싱글톤으로. 
    }

    public void StartMasterGameLogic()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 🔥🔥
            Experiment[] tables = FindObjectsOfType<Experiment>();
            Player[] players = PhotonNetwork.PlayerList;
            int count = Mathf.Min(tables.Length, players.Length);

            for (int i = 0; i < count; i++)
            {
                int actorNumber = players[i].ActorNumber;
                tables[i].SetOwner(actorNumber); // 실험대에 플레이어 owner 설정
            }
            // 🔥🔥
            
            SpawnWeaponObjects();
            SpawnPartObjects();
            SpawnObstacleObjects();
            SpawnDrugObjects();
            StartCoroutine(SpawnBombsRoutine());
        }
    }

    void Start()
    {
        currentTime = startingTime;

        if (retryPanel != null) { retryPanel.SetActive(false); }

        // UpdateRemainingText(); // 초기 UI 업데이트
    }

    void Update()
    {
        // 마스터 클라이언트만 시간 감소 로직을 수행
        if (PhotonNetwork.IsMasterClient && !isGameOver)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                // 시간 초과 시 패배 로직은 모든 클라이언트에서 실행되어야 하므로 RPC 호출
                PV.RPC("RpcGameOver", RpcTarget.All, false); 
            }
        }
        
        // 모든 클라이언트에서 UI 업데이트
        UpdateUIText();
        // UpdateRemainingText();
    }

    // ==========================================================
    // ⭐ 장애물/부품/Drug/무기/폭탄 로직 (RPC 사용)
    // ==========================================================

    // Obstacle 생성 로직 (마스터 클라이언트 전용)
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
            PhotonNetwork.Instantiate(obstaclePrefab.name, randomPosition, Quaternion.identity);
        }
        Debug.Log($"장애물 {totalObstacleCount}개 스폰 완료.");
    }

    // 🔥🔥
    // 부품 생성 로직 (마스터 클라이언트 전용)
    private void SpawnPartObjects()
    {
        if (!PhotonNetwork.IsMasterClient || partPrefab == null)
        {
            if (partPrefab == null) Debug.LogError("Part Prefab이 GameManager에 연결되지 않았습니다!");
            return;
        }

        // ⭐ 생성한 인덱스들을 추적하여 중복되지 않게 합니다.
        // List<int> availableIndices = new List<int>();
        // 생성할 부품 개수만큼 인덱스 리스트를 채웁니다. (0, 1, 2, ...)
        // for (int i = 0; i < totalPartCount; i++)
        // {
        //     availableIndices.Add(i);
        // }

        for (nextPartId = 0; nextPartId < 2; nextPartId++) // 초기 부품 스폰 (2개)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            
            // 1. 부품 프리팹 생성 (모든 클라이언트)
            GameObject partObj = PhotonNetwork.Instantiate(partPrefab.name, randomPosition, Quaternion.identity);
            
            PhotonView partPV = partObj.GetComponent<PhotonView>();
            
            // 2. 현재 부품에 할당할 인덱스 결정
            // int partIndex = availableIndices[i]; // 순서대로 할당 (0, 1, 2)
            
            // 3. RPC를 호출하여 모든 클라이언트에게 이 부품에 어떤 이미지를 적용할지 명령
            // 인덱스를 초기화하고 동기화하기 위해 RPC를 사용합니다.
            PV.RPC("RpcSetPartSprite", RpcTarget.All, partPV.ViewID, nextPartId);
        }
        Debug.Log($"다음 스폰할 부품 id : {nextPartId}");
    }
    public void SpawnOnePart()
    {
        Vector3 randomPosition = GetRandomSpawnPosition();
        GameObject partObj = PhotonNetwork.Instantiate(partPrefab.name, randomPosition, Quaternion.identity);
        PhotonView partPV = partObj.GetComponent<PhotonView>();
        PV.RPC("RpcSetPartSprite", RpcTarget.All, partPV.ViewID, nextPartId);
        nextPartId = (nextPartId + 1) % 4; // 파츠 0 ~ 3 -> 0 ~ 3 반복 순회
    }

    [PunRPC]
    void RpcSetPartSprite(int viewID, int spriteIndex)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view == null) return;
        // 스프라이트 적용
        SpriteRenderer sr = view.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = partsImages[spriteIndex];
        }
        // 파츠에 아이디 부여
        Parts part = view.GetComponent<Parts>();
        if (part != null)
        {
            part.partId = spriteIndex;
        }
    }

    // B. 이미지 초기화 RPC 추가
    [PunRPC]
    private void RpcInitializePartVisuals(int partViewID, int imageIndex)
    {
        PhotonView partPV = PhotonView.Find(partViewID);
        if (partPV == null) return;
        
        PartVisuals visuals = partPV.GetComponent<PartVisuals>();
        
        if (visuals != null)
        {
            // 모든 클라이언트에서 수신된 인덱스로 스프라이트를 설정합니다.
            visuals.SetPartSprite(imageIndex);
        }
    }
    // 🔥🔥
    
    // Drug 생성 로직 (마스터 클라이언트 전용)
    private void SpawnDrugObjects()
    {
        if (!PhotonNetwork.IsMasterClient || drugPrefab == null)
        {
            if (drugPrefab == null) Debug.LogError("Drug Prefab이 GameManager에 연결되지 않았습니다!");
            return;
        }
        
        int settingsCount = drugBoostSettings.Length;
        int colorCount = drugColorsForSpawning.Length; // 색상 배열 크기

        for (int i = 0; i < totalDrugCount; i++)
        {
            // 설정된 개수(2개)와 색상 배열 크기를 초과하지 않도록 방지
            if (i >= settingsCount || i >= colorCount) break; 

            Vector3 randomPosition = GetRandomSpawnPosition();
            
            GameObject drugObj = PhotonNetwork.Instantiate(drugPrefab.name, randomPosition, Quaternion.identity);
            
            BoostSetting setting = drugBoostSettings[i];
            Color itemColor = drugColorsForSpawning[i]; // 해당 인덱스의 색상 가져오기
            
            Booster booster = drugObj.GetComponent<Booster>();
            if (booster != null)
            {
                booster.InitializeBoostAndColor(setting.rotationBoost, setting.speedBoost, setting.duration, itemColor);
            }
        }
        Debug.Log($"Drug {totalDrugCount}개 스폰 완료 및 색상/부스트 설정 할당.");
    }

    // 무기 생성 로직
    private void SpawnWeaponObjects()
    {
        if (!PhotonNetwork.IsMasterClient || trashPrefab == null) return;

        for (int i = 0; i < totalTrashCount; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            
            // PhotonNetwork.Instantiate를 사용하여 모든 클라이언트에서 생성
            GameObject obj = PhotonNetwork.Instantiate(trashPrefab.name, randomPosition, Quaternion.identity);

            // 🔥🔥
            PhotonView trashPV = obj.GetComponent<PhotonView>();
            PV.RPC("RpcSetTrashSprite", RpcTarget.All, trashPV.ViewID, i);
            // 이미지 변경
            // PhotonView trashPV = obj.GetComponent<PhotonView>();
            // SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            // sr.sprite = trashImages[i];
        }
    }
    [PunRPC]
    void RpcSetTrashSprite(int viewID, int spriteIndex)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view == null) return;

        SpriteRenderer sr = view.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = trashImages[spriteIndex];
        }
    }
    // 🔥🔥

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
            PhotonNetwork.Instantiate(bombPrefab.name, randomPosition, Quaternion.identity);

            bombsSpawned++;
            Debug.Log($"폭탄 스폰됨: {randomPosition}");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(
            Random.Range(mapBounds.min.x, mapBounds.max.x),
            Random.Range(mapBounds.min.y, mapBounds.max.y),
            0 
        );
    }

    // ==========================================================
    // ⭐ UI 업데이트 및 클리어 로직 (RPC 사용)
    // ==========================================================

    void UpdateUIText()
    {
        if (timeText != null)
        {
            // 시간은 Network Serialize View를 통해 동기화됨
            timeText.text = "Time: " + Mathf.CeilToInt(currentTime).ToString(); 
        }
    }
    
    // void UpdateRemainingText()
    // {
    //     // 수집된 쓰레기 개수는 Network Serialize View를 통해 동기화됨
    //     if (remainingText != null)
    //     {
    //         remainingText.text = $"Remaining: {trashCollected}/{totalTrashCount}";
    //     }
    // }

    // 🔥🔥
    // // ⭐ 쓰레기 획득 (모든 클라이언트가 RPC로 요청하고, 마스터가 동기화)
    // public void TrashCollected(GameObject trash)
    // {
    //     if (isGameOver) return;
        
    //     // 쓰레기 수집은 마스터 클라이언트에게만 알려서 동기화합니다.
    //     PV.RPC("RpcCollectTrash", RpcTarget.MasterClient, trash.GetComponent<PhotonView>().ViewID);
    // }
    // ==========================================================
    // ⭐ 부품/쓰레기 수집 완료 시 버튼 활성화 체크 (마스터 클라이언트만 실행)
    // ==========================================================
    // private void CheckButtonActivation()
    // {
    //     if (!PhotonNetwork.IsMasterClient || isGameOver) return;

    //     // 부품 수집이 모두 완료되었을 때만 버튼 활성화
    //     if (partsCollected >= totalPartCount)
    //     {
    //         // ⭐ 버튼 활성화 RPC는 한 번만 호출
    //         if (!experimentButton.activeSelf)
    //         {
    //             PV.RPC("RpcActivateExperimentButton", RpcTarget.All);
    //         }
    //     }
    // }

    // ==========================================================
    // ⭐ Experiment Complete 버튼 클릭 시 (클리어 트리거)
    // ==========================================================
    // 이 함수를 Unity Inspector에서 'experimentButton'의 OnClick() 이벤트에 연결해야 합니다.
    // public void CompleteButtonAction()
    // {
    //     if (isGameOver) return;

    //     // ⭐ 모든 클라이언트에게 게임을 클리어했음을 알리는 RPC 호출
    //     PV.RPC("RpcCompleteGame", RpcTarget.All);
    // }
    
    // [PunRPC]
    // public void RpcCollectTrash(int trashViewID)
    // {
    //     if (!PhotonNetwork.IsMasterClient) return;

    //     trashCollected++;

    //     PhotonView trashPV = PhotonView.Find(trashViewID);
    //     if (trashPV != null)
    //     {
    //         PhotonNetwork.Destroy(trashPV.gameObject);
    //     }

    //     // ⭐ 승리 조건 대신 버튼 활성화 조건 확인
    //     CheckButtonActivation();
    // }
    
    // // ⭐ 부품 획득 (RPC 사용)
    // public void PartsCollected(GameObject part)
    // {
    //     if (isGameOver) return;
    //     PV.RPC("RpcCollectPart", RpcTarget.All, part.GetComponent<PhotonView>().ViewID);
    // }
    
    // [PunRPC]
    // public void RpcCollectPart(int partViewID)
    // {
    //     if (!PhotonNetwork.IsMasterClient) return;
        
    //     partsCollected++;

    //     PhotonView partPV = PhotonView.Find(partViewID);
    //     if (partPV != null)
    //     {
    //         PhotonNetwork.Destroy(partPV.gameObject);
    //     }
        
    //     // ⭐ 승리 조건 대신 버튼 활성화 조건 확인
    //     CheckButtonActivation();
    // }

    // [PunRPC]
    // public void RpcActivateExperimentButton(int ownerId)
    // {
    //     // 모든 클라이언트에서 버튼 활성화
    //     if (experimentButton != null)
    //     {
    //         // 모든 수집이 완료되면 버튼을 활성화합니다.
    //         experimentButton.SetActive(true);
            
    //         // ⭐ 버튼이 눌리면 CompleteButtonAction()이 호출되도록 Ensure합니다.
    //         // Button btn = experimentButton.GetComponent<Button>();
    //         // if (btn != null)
    //         // {
    //         //     btn.onClick.RemoveAllListeners();
    //         //     btn.onClick.AddListener(CompleteButtonAction);
    //         // }
    //     }
    // }
    // 🔥🔥

    // ==========================================================
    // 게임 종료 로직 (RPC 사용)
    // ==========================================================

    [PunRPC]
    public void RpcGameOver(bool didWin)
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        retryPanel.SetActive(true);
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
    }
    
    public bool IsGameActive()
    {
        return !isGameOver;
    }
    
    public void RetryGame()
    {
        // ✨ 멀티플레이: 룸을 나간 후 씬을 다시 로드합니다.
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
}