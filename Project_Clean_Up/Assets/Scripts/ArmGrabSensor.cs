using UnityEngine;

public class ArmGrabSensor : MonoBehaviour
{
    private PlayerGrabThrow playerGrabThrow; // ✨ 변수명 명확화
    
    // 현재 이 팔에 닿아있는 쓰레기 오브젝트 (최대 1개만 잡는다고 가정)
    [HideInInspector] public GameObject currentTouchingTrash = null;

    void Awake()
    {
        // 부모에서 PlayerGrabThrow 스크립트 참조
        playerGrabThrow = transform.parent.GetComponent<PlayerGrabThrow>();
        if (playerGrabThrow == null)
        {
            Debug.LogError("PlayerGrabThrow script not found on the parent object.");
        }
    }

    // Arm 콜라이더가 Trash 태그를 가진 오브젝트와 닿기 시작할 때
    void OnTriggerEnter2D(Collider2D other)
    {
        // 쓰레기 태그이고, 아직 플레이어가 아무것도 잡고 있지 않을 때만 감지 상태를 갱신
        if ((other.CompareTag("Trash") || other.CompareTag("parts")) && playerGrabThrow.IsHoldingTrash() == false)
        {
            currentTouchingTrash = other.gameObject;
            Debug.Log($"{gameObject.name}이 Trash에 닿음: {currentTouchingTrash.name}");
        }
    }

    // Arm 콜라이더가 Trash 태그를 가진 오브젝트에서 떨어질 때
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Trash") || other.CompareTag("parts"))
        {
            // 닿아있던 쓰레기가 맞는지 확인 후 해제
            if (other.gameObject == currentTouchingTrash)
            {
                currentTouchingTrash = null;
                Debug.Log($"{gameObject.name}이 Trash에서 떨어짐");
            }
        }
    }
}