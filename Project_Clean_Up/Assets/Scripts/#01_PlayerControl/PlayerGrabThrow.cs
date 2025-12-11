using UnityEngine;
using Photon.Pun; // ✨ 추가: Photon 기능을 사용하기 위해 필요합니다.

public class PlayerGrabThrow : MonoBehaviourPun
{
    // private GameManager gameManager; // 게임 매니저 참조

    public float throwPower = 7f;
    
    // 💡 PhotonView는 PlayerMovement 스크립트처럼 이미 이 컴포넌트가 붙은 GameObject에 있다고 가정합니다.
    public PhotonView PV; // 이 컴프넌트가 붙은 오브젝트의 PhotonView

    public ArmGrabSensor armLSensor;
    public ArmGrabSensor armRSensor;

    // ⭐ 추가: 던지기 힘 조절 변수 (Inspector에서 미세 조정)
    [Header("Throwing Settings")]
    public float throwForceMultiplier = 0.05f; // 던지는 힘의 배수

    // ⭐⭐ 사운드 관련 변수 추가 ⭐⭐
    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip grabSoundClip;
    public AudioClip releaseSoundClip;

    // 현재 잡고 있는 쓰레기 오브젝트
    private GameObject heldTrash = null;
    // 쓰레기가 붙잡힐 팔의 Transform
    private Transform holdingArm = null;
    
    // ⭐ 추가: 오프셋 및 레이어 변수
    private Vector3 initialLocalTrashPosition = Vector3.zero; // 로컬 위치 오프셋
    private int originalTrashLayer; // 원래 레이어를 저장
    public int ignoreCollisionLayer = 9; // 충돌 무시 레이어 번호 (유니티 에디터에서 설정)


    void Awake() 
    {
        // gameManager = FindObjectOfType<GameManager>();
        // PV를 수동으로 할당하지 않았다면 Awake에서 GetComponent로 가져와야 합니다.
        if (PV == null)
        {
            PV = GetComponent<PhotonView>();
        }

        // ⭐ AudioSource 컴포넌트 가져오기
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        // // 게임이 활성화 상태일 때만 입력 처리
        // if (gameManager == null || gameManager.IsGameActive())
        // {
        //     HandleTrashGrab();
        // }

        // ⭐ 핵심 수정: InGameManager.Instance.IsMovementAllowed()를 사용하여 게임 활성화 상태 확인
        // InGameManager.IsMovementAllowed()는 isGameActive, isCountdownActive, isGameOver 상태를 모두 고려합니다.
        if (InGameManager.Instance != null && InGameManager.Instance.IsMovementAllowed())
        {
            HandleTrashGrab();
        }
        
        // ⭐ 추가: InGameManager가 없을 경우 (예외 처리)
        if (InGameManager.Instance == null)
        {
             Debug.LogWarning("InGameManager.Instance가 씬에 없습니다. 입력 처리를 건너뜁니다.");
             return;
        }
    }

    void FixedUpdate()
    {
        // 핵심 수정: FixedUpdate가 끝날 때 로컬 위치를 강제 재설정하여 떨림 현상을 방지합니다.
        // 모든 클라이언트에서 잡고 있는 쓰레기의 위치를 부모 팔에 고정시킵니다.
        if (heldTrash != null)
        {
            heldTrash.transform.localPosition = initialLocalTrashPosition;
            // 필요하다면 회전도 고정: heldTrash.transform.localRotation = Quaternion.identity;
        }
    }
    
    public bool IsHoldingTrash()
    {
        return heldTrash != null;
    }

    // 쓰레기 집기/놓기 네트워크 처리 로직 (로컬 플레이어만 입력 처리 및 RPC 호출)
    private void HandleTrashGrab()
    {
        // ✨ 로컬 플레이어(IsMine)만 입력 처리
        if (PV == null || !PV.IsMine) return;

        if (Input.GetMouseButton(0) && heldTrash == null)
        {
            GameObject trashToGrab = null;
            int armIndex = -1; // 0: 왼쪽 팔 (LSensor), 1: 오른쪽 팔 (RSensor)

            if (armLSensor != null && armLSensor.currentTouchingTrash != null)
            {
                trashToGrab = armLSensor.currentTouchingTrash;
                armIndex = 0;
            }
            else if (armRSensor != null && armRSensor.currentTouchingTrash != null)
            {
                trashToGrab = armRSensor.currentTouchingTrash;
                armIndex = 1;
            }

            if (trashToGrab != null)
            {
                PhotonView trashPV = trashToGrab.GetComponent<PhotonView>();
                if (trashPV != null)
                {
                    // 1. 쓰레기의 소유권을 현재 플레이어에게 요청합니다. (물리 시뮬레이션을 위해)
                    if (!trashPV.IsMine)
                    {
                        trashPV.RequestOwnership();
                    }

                    // ⭐⭐ 핵심 수정: 잡기 사운드 RPC 호출
                    PV.RPC("RpcPlayGrabSound", RpcTarget.All);
                    
                    // 2. RPC를 호출하여 모든 클라이언트에서 잡는 로직을 실행합니다.
                    PV.RPC("RpcGrabTrash", RpcTarget.All, trashPV.ViewID, armIndex);
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (heldTrash != null)
            {
                // ⭐⭐ 핵심 수정: 놓기 사운드 RPC 호출
                PV.RPC("RpcPlayReleaseSound", RpcTarget.All);

                // RPC를 호출하여 모든 클라이언트에서 놓는 로직을 실행합니다.
                PV.RPC("RpcDropTrash", RpcTarget.All);
            }
        }
    }

    // 🔗 RPC 수신: 쓰레기 잡기
    [PunRPC]
    private void RpcGrabTrash(int trashViewID, int armIndex)
    {
        PhotonView trashPV = PhotonView.Find(trashViewID);
        if (trashPV == null) return;

        // armIndex에 따라 잡을 팔을 결정합니다.
        Transform armTransform = (armIndex == 0) ? armLSensor.transform : armRSensor.transform;

        // 로컬 로직 실행
        GrabTrashLogic(trashPV.gameObject, armTransform);
    }

    // 1️⃣ 로컬 잡기 로직 (GrabTrash -> GrabTrashLogic으로 이름 변경)
    private void GrabTrashLogic(GameObject trashObject, Transform armTransform)
    {
        if (heldTrash != null) return;

        heldTrash = trashObject;
        holdingArm = armTransform;
        if (heldTrash.CompareTag("Trash"))
        {
            heldTrash.GetComponent<ThrowableObject>().SetHeld(); // 잡은 쓰레기 오브젝트의 상태를 held로 변경해주기\
        }
        
        Rigidbody2D trashRb = heldTrash.GetComponent<Rigidbody2D>();
        if (trashRb != null)
        {
            trashRb.isKinematic = true;
        }

        // ⭐ 핵심: 쓰레기의 원래 레이어 저장 및 레이어 변경 (충돌 무시)
        Debug.Log($"의심 레이어 지점 1 : {heldTrash.layer}");
        originalTrashLayer = heldTrash.layer;
        heldTrash.layer = ignoreCollisionLayer;


        // 오프셋 계산 및 저장
        Vector3 desiredLocalPosition = holdingArm.InverseTransformPoint(heldTrash.transform.position);
        initialLocalTrashPosition = desiredLocalPosition;

        // 부모-자식 관계 설정
        heldTrash.transform.parent = holdingArm;

        // 계산된 로컬 위치 설정
        heldTrash.transform.localPosition = initialLocalTrashPosition;

        // 잡는 순간 회전도 부모와 같게 맞춥니다.
        heldTrash.transform.localRotation = Quaternion.identity;

        Debug.Log($"Trash {trashObject.name} 잡기 성공! 팔: {holdingArm.name}");
    }
    
    // 🔗 RPC 수신: 쓰레기 놓기
    [PunRPC]
    private void RpcDropTrash()
    {
        // 로컬 로직 실행
        DropTrashLogic();
    }

    // 2️⃣ 로컬 놓기 로직 (DropTrash -> DropTrashLogic으로 이름 변경)
    private void DropTrashLogic()
    {
        if (heldTrash != null)
        {
            // 1. 레이어 복원

            Debug.Log($"의심 레이어 지점 2 : {originalTrashLayer}");
            heldTrash.layer = originalTrashLayer; 
            
            // 2. 부모-자식 관계 해제
            heldTrash.transform.parent = null;
            if (heldTrash.CompareTag("Trash"))
            {
                heldTrash.GetComponent<ThrowableObject>().SetThrown();
                Debug.Log("쓰레기 던져짐!!");
            }

            Rigidbody2D trashRb = heldTrash.GetComponent<Rigidbody2D>();
            if (trashRb != null)
            {
                trashRb.isKinematic = false;
                
                // --- ⭐ 던지기 로직 (소유자만 물리 적용) ⭐ ---
                PhotonView trashPV = heldTrash.GetComponent<PhotonView>();

                // 쓰레기의 소유권을 가진 플레이어(물리 시뮬레이션을 담당하는 플레이어)만 힘을 적용합니다.
                if (trashPV != null && trashPV.IsMine)
                {
                    // 3. 현재 팔의 각속도를 가져옵니다.
                    ArmRotation armRotation = holdingArm.GetComponent<ArmRotation>();
                    float angularSpeed = (armRotation != null) ? armRotation.angularVelocity : 0f;
                    
                    // 4. 팔의 길이를 계산하여 선형 속도를 추정합니다.
                    float radius = Vector3.Distance(heldTrash.transform.position, holdingArm.position);
                    
                    // 5. 선형 속도 (각속도 * 반지름)를 계산합니다. 
                    float linearSpeed = angularSpeed * Mathf.Deg2Rad * radius;
                    
                    // 6. 던지는 방향 (접선 방향)
                    Vector3 throwDirection = heldTrash.transform.position - holdingArm.position;
                    Vector3 tangentialDirection = Quaternion.Euler(0, 0, 90) * throwDirection.normalized;

                    // 7. 계산된 속도와 배수를 사용하여 힘을 적용
                    // float finalThrowForce = linearSpeed * throwForceMultiplier * trashRb.mass; 
                    float finalThrowForce = linearSpeed * throwForceMultiplier * 10f; 
                    trashRb.AddForce(tangentialDirection * finalThrowForce, ForceMode2D.Impulse);
                }
                // --------------------------
            }
            
            // 상태 초기화 (모든 클라이언트에서 실행)
            heldTrash = null;
            holdingArm = null;
            initialLocalTrashPosition = Vector3.zero;

            // 로컬 센서 상태 초기화
            if (armLSensor != null) armLSensor.currentTouchingTrash = null;
            if (armRSensor != null) armRSensor.currentTouchingTrash = null;
            
            Debug.Log("Trash 놓기");
        }
    }

    // 🔗 RPC 수신: 잡기 사운드 재생
    [PunRPC]
    private void RpcPlayGrabSound()
    {
        if (audioSource != null && grabSoundClip != null)
        {
            audioSource.PlayOneShot(grabSoundClip);
        }
    }

    // 🔗 RPC 수신: 놓기 사운드 재생
    [PunRPC]
    private void RpcPlayReleaseSound()
    {
        if (audioSource != null && releaseSoundClip != null)
        {
            audioSource.PlayOneShot(releaseSoundClip);
        }
    }
}