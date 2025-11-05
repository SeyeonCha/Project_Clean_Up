using UnityEngine;

public class ArmGrabSensor : MonoBehaviour
{
    private PlayerMove playerMove;
    
    // 현재 이 팔에 닿아있는 쓰레기 오브젝트 (최대 1개만 잡는다고 가정)
    [HideInInspector] public GameObject currentTouchingTrash = null;

    void Awake()
    {
        playerMove = transform.parent.GetComponent<PlayerMove>();
        if (playerMove == null)
        {
            Debug.LogError("PlayerMove script not found on the parent object.");
        }
    }

    // Arm 콜라이더가 Trash 태그를 가진 오브젝트와 닿기 시작할 때
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trash") && playerMove.IsHoldingTrash() == false)
        {
            currentTouchingTrash = other.gameObject;
            Debug.Log($"{gameObject.name}이 Trash에 닿음: {currentTouchingTrash.name}");
        }
    }

    // Arm 콜라이더가 Trash 태그를 가진 오브젝트에서 떨어질 때
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Trash"))
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