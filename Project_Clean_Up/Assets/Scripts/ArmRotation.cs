using UnityEngine;

public class ArmRotation : MonoBehaviour
{
    public float rotationSpeed = 10f; 

    void Update()
    {
        RotateArmTowardsMouse();
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
}