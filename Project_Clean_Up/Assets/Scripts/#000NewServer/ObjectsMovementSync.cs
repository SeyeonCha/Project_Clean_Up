using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TrashMovementSync : MonoBehaviourPunCallbacks, IPunObservable
{
    // ⭐ 중요: 이 컴포넌트가 붙은 GameObject는 PhotonNetwork.Instantiate로 생성되어야 합니다.
    private Vector3 curPos; // 수신된 위치
    private Quaternion curRot; // 수신된 회전
    private Rigidbody2D rb;
    private PhotonView PV;
    
    // 네트워크 동기화 속도 (Lerp 계수)
    public float syncRate = 10f; 

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();

        // 초기 위치와 회전을 현재 값으로 설정
        curPos = transform.position;
        curRot = transform.rotation;
        
        // Rigidbody가 없으면 Photon Transform View를 대신 사용해야 합니다.
        if (rb == null)
        {
            Debug.LogWarning("Trash object is missing a Rigidbody2D. Manual sync will be based on Transform.");
        }
    }

    private void Update()
    {
        if (PV.IsMine)
        {
            // 소유자(IsMine)는 물리 시뮬레이션을 담당하며 네트워크로 위치를 전송합니다.
            // 별도의 로직은 필요 없습니다.
        }
        else
        {
            // 소유자가 아닌 클라이언트들은 수신된 데이터로 부드럽게 위치를 보간합니다.
            
            // 1. 위치 동기화
            // 거리가 너무 멀면 즉시 위치를 조정하여 튕기는 현상(teleporting)을 방지합니다.
            if ((transform.position - curPos).sqrMagnitude >= 100)
            {
                transform.position = curPos;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * syncRate);
            }
            
            // 2. 회전 동기화
            transform.rotation = Quaternion.Lerp(transform.rotation, curRot, Time.deltaTime * syncRate);
        }
    }

    // ⭐ IPunObservable 구현: 위치와 회전 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 🚀 소유자(IsMine): 위치와 회전 전송
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 📥 비소유자(Not IsMine): 위치와 회전 수신
            curPos = (Vector3)stream.ReceiveNext();
            curRot = (Quaternion)stream.ReceiveNext();
        }
    }
}