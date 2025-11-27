using UnityEngine;
using Photon.Pun;

public class PartVisuals : MonoBehaviourPun, IPunObservable
{
    // ⭐ Inspector에서 부품 이미지들을 연결합니다. (예: 3개)
    public Sprite[] partSprites; 
    public SpriteRenderer spriteRenderer;

    private int currentSpriteIndex = -1; // 현재 부품이 사용하는 이미지 인덱스

    void Awake()
    {
        // SpriteRenderer 컴포넌트 자동 참조
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // ⭐ 네트워크로 받은 인덱스를 로컬에서 적용하는 함수
    public void SetPartSprite(int index)
    {
        if (index >= 0 && index < partSprites.Length && spriteRenderer != null)
        {
            spriteRenderer.sprite = partSprites[index];
            currentSpriteIndex = index;
        }
    }

    // IPunObservable 구현: 이미지 인덱스를 동기화합니다.
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 🚀 마스터/소유자만 현재 인덱스를 전송
            stream.SendNext(currentSpriteIndex);
        }
        else
        {
            // 📥 다른 클라이언트는 인덱스를 수신하여 적용
            int receivedIndex = (int)stream.ReceiveNext();
            if (receivedIndex != currentSpriteIndex)
            {
                SetPartSprite(receivedIndex); // 수신 즉시 스프라이트 업데이트
            }
        }
    }
}