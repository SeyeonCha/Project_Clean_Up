// ButtonSoundPlayer.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class ButtonSoundPlayer : MonoBehaviour
{
    public AudioClip clickSound; // 버튼 클릭 소리
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D UI 사운드
        audioSource.playOnAwake = false; // 자동 재생 금지

        // 씬에 있는 모든 Button을 찾아서 OnClick 이벤트를 자동으로 연결
        Button[] buttons = FindObjectsOfType<Button>(true); // (true) 비활성화된 버튼도 포함
        foreach (Button btn in buttons)
        {
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    // 버튼이 클릭될 때마다 이 함수가 호출됨
    void PlayClickSound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}