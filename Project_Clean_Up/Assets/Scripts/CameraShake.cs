using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake instance;
    private Vector3 originalPos;

    void Awake()
    {
        instance = this;
        originalPos = transform.localPosition;
    }

    public static void Shake(float duration, float magnitude)
    {
        instance.StartCoroutine(instance.ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float time = 0f;

        while (time < duration)
        {
            Vector3 randomPoint = originalPos + Random.insideUnitSphere * magnitude;
            transform.localPosition = randomPoint;

            time += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
