using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RandomPlatformSelector : MonoBehaviour
{
    [Header("Platform References")]
    [Tooltip("랜덤으로 선택될 두 개의 발판 GameObject를 연결하세요.")]
    public GameObject platformA;
    public GameObject platformB;

    private Collider2D colliderA;
    private Collider2D colliderB;

    void Awake()
    {
        // Collider2D 컴포넌트 참조 가져오기
        if (platformA != null)
        {
            colliderA = platformA.GetComponent<Collider2D>();
        }
        if (platformB != null)
        {
            colliderB = platformB.GetComponent<Collider2D>();
        }

        // 컴포넌트가 모두 있는지 확인
        if (colliderA == null || colliderB == null)
        {
            Debug.LogError("RandomPlatformSelector: 두 발판 모두에 Collider2D 컴포넌트가 없습니다.");
            enabled = false; // 스크립트 비활성화
            return;
        }

        // 게임 시작 시 랜덤 선택 로직 실행
        SelectRandomActivePlatform();
    }

    /// <summary>
    /// 두 발판 중 하나를 랜덤으로 선택하여 콜라이더를 활성화/비활성화합니다.
    /// </summary>
    private void SelectRandomActivePlatform()
    {
        // 0 또는 1을 랜덤으로 선택
        int randomSelection = Random.Range(0, 2);

        if (randomSelection == 0)
        {
            // Case 1: A 활성화, B 비활성화
            colliderA.enabled = true;
            colliderB.enabled = false;
            Debug.Log($"Random Platform Selected: A (Enabled), B (Disabled).");
        }
        else
        {
            // Case 2: A 비활성화, B 활성화
            colliderA.enabled = false;
            colliderB.enabled = true;
            Debug.Log($"Random Platform Selected: A (Disabled), B (Enabled).");
        }

        // 참고: SpriteRenderer도 함께 켜고 끄고 싶다면 SpriteRenderer.enabled = active; 코드를 추가해야 합니다.
    }
}