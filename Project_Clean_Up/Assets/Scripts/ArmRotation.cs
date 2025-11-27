using UnityEngine;
using Photon.Pun; // ✨ 추가

public class ArmRotation : MonoBehaviourPunCallbacks, IPunObservable // ✨ 상속 변경
{
    public float rotationSpeed = 10f;
    
    // ⭐ 추가: 현재 팔의 각속도 (초당 회전 각도)
    [HideInInspector] public float angularVelocity = 0f;
    
    // ⭐ 추가: PhotonView 참조 및 네트워크 동기화 변수
    private PhotonView PV;
    private Quaternion curRot; // 네트워크로 수신된 회전 값

    private float previousAngle = 0f;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
        curRot = transform.rotation; // 초기 회전 값 설정
    }

    void Update()
    {
        if (PV.IsMine) // ✨ 로컬 플레이어만 마우스 입력 처리
        {
            RotateArmTowardsMouse();
            MeasureAngularVelocity();
        }
        else // ✨ 다른 플레이어는 네트워크 데이터로 동기화
        {
            // 수신된 회전 값으로 부드럽게 보간
            transform.rotation = Quaternion.Lerp(transform.rotation, curRot, Time.deltaTime * 10);
        }
    }

    private void RotateArmTowardsMouse()
    {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        Vector3 directionToMouse = mouseWorldPosition - transform.position;

        // 1. 기본 각도 계산 (라디안을 디그리(Degree)로 변환)
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        angle += 180f;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    
    // ⭐ 추가: 팔의 회전 속도(각속도)를 측정합니다.
    private void MeasureAngularVelocity()
    {
        // 현재 Z축 회전 각도를 가져옵니다.
        float currentAngle = transform.rotation.eulerAngles.z;

        // 각도 차이 계산 (360도 경계를 넘는 경우 처리)
        float angleDifference = Mathf.DeltaAngle(previousAngle, currentAngle);

        // 초당 각속도 계산 (Time.deltaTime으로 나눠 줍니다)
        if (Time.deltaTime > 0)
        {
            angularVelocity = angleDifference / Time.deltaTime;
        }
        else
        {
            angularVelocity = 0f;
        }

        previousAngle = currentAngle;
    }

    // ✨ IPunObservable 구현: 회전 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 🚀 로컬 플레이어: 현재 회전을 전송
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 📥 다른 플레이어: 수신된 회전 저장
            curRot = (Quaternion)stream.ReceiveNext();
        }
    }
}