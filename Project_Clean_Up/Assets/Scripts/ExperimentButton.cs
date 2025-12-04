using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentButton : MonoBehaviour
{
    private GameManager gameManager;

    [SerializeField]
    private Sprite button_unpressed; 
    [SerializeField]
    private Sprite button_pressed; 

    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        // 씬에서 GameManager를 찾습니다.
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager 스크립트를 찾을 수 없습니다! 게임이 정상 작동하지 않을 수 있습니다.");
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = button_unpressed;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.CompareTag("Player") && gameManager != null)
        {
            // 이미지 교체. 
            spriteRenderer.sprite = button_pressed;
            gameManager.ExperimentCompleted = true;
            Debug.Log($"실험 완성 버튼 눌림");
        }
    }
}
