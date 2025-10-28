using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlternatingPlatformManager : MonoBehaviour
{
    [Header("Group A (빨간색 알약 쌍)")]
    public GameObject[] groupA;

    [Header("Group B (파란색 알약 쌍)")]
    public GameObject[] groupB;

    [Header("Timing Settings")]
    [Tooltip("각 그룹이 활성화/비활성화되는 주기 (초 단위)")]
    public float switchInterval = 2.0f;

    [Header("Fade Settings")]
    [Tooltip("서서히 켜지고 꺼지는 데 걸리는 시간 (초)")]
    public float fadeDuration = 0.5f;

    private bool isGroupAActive = true;

    // 모든 SpriteRenderer 컴포넌트를 미리 저장할 리스트
    private List<SpriteRenderer> renderersA = new List<SpriteRenderer>();
    private List<SpriteRenderer> renderersB = new List<SpriteRenderer>();

    // ★★★ Collider2D 컴포넌트 리스트 추가 ★★★
    private List<Collider2D> collidersA = new List<Collider2D>();
    private List<Collider2D> collidersB = new List<Collider2D>();

    // 현재 진행 중인 페이드 코루틴을 저장할 리스트
    private List<Coroutine> currentFades = new List<Coroutine>();


    void Start()
    {
        // 1. 모든 컴포넌트 미리 가져오기
        CacheComponents(groupA, renderersA, collidersA);
        CacheComponents(groupB, renderersB, collidersB);

        // 2. 초기 상태 설정: A를 켜고 (Alpha=1, Collider=On), B를 끕니다 (Alpha=0, Collider=Off).
        SetGroupState(renderersA, collidersA, 1f, true);
        SetGroupState(renderersB, collidersB, 0f, false);

        // 3. 주기적 전환 코루틴 시작
        StartCoroutine(SwitchPlatformsPeriodically());
    }

    // 초기화 시 컴포넌트를 캐싱하는 헬퍼 함수
    private void CacheComponents(GameObject[] group, List<SpriteRenderer> rendererList, List<Collider2D> colliderList)
    {
        foreach (GameObject obj in group)
        {
            if (obj != null)
            {
                // SpriteRenderer와 Collider2D를 찾습니다.
                SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
                Collider2D collider = obj.GetComponent<Collider2D>();

                if (renderer != null) rendererList.Add(renderer);
                if (collider != null) colliderList.Add(collider);

                // 자식 오브젝트의 컴포넌트도 필요하다면 GetComponentsInChildren을 사용하세요.
            }
        }
    }

    // 그룹의 초기 알파 및 콜라이더 상태를 설정하는 헬퍼 함수
    private void SetGroupState(List<SpriteRenderer> rendererList, List<Collider2D> colliderList, float alpha, bool colliderActive)
    {
        foreach (SpriteRenderer renderer in rendererList)
        {
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }
        foreach (Collider2D collider in colliderList)
        {
            if (collider != null)
            {
                collider.enabled = colliderActive;
            }
        }
    }


    IEnumerator SwitchPlatformsPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(switchInterval);

            StopAllFades();

            // 상태 전환 로직
            if (isGroupAActive)
            {
                // A 서서히 꺼짐 (Fade Out), B 서서히 켜짐 (Fade In)
                FadeGroup(renderersA, collidersA, 0f, fadeDuration, false); // A -> 0, Collider Off
                FadeGroup(renderersB, collidersB, 1f, fadeDuration, true);  // B -> 1, Collider On
                isGroupAActive = false;
            }
            else
            {
                // A 서서히 켜짐 (Fade In), B 서서히 꺼짐 (Fade Out)
                FadeGroup(renderersA, collidersA, 1f, fadeDuration, true);  // A -> 1, Collider On
                FadeGroup(renderersB, collidersB, 0f, fadeDuration, false); // B -> 0, Collider Off
                isGroupAActive = true;
            }
        }
    }

    // 그룹에 페이드 효과를 적용
    private void FadeGroup(List<SpriteRenderer> rendererList, List<Collider2D> colliderList, float targetAlpha, float duration, bool targetColliderActive)
    {
        // 켜지는 그룹일 경우: 페이드 시작과 동시에 콜라이더를 고/GameObject를 고, 알파를 높입니다.
        if (targetAlpha > 0f)
        {
            // 켜지는 순간 콜라이더 활성화
            foreach (Collider2D collider in colliderList)
                if (collider != null) collider.enabled = true;

            // 옵션: GameObject 자체도 활성화 (씬에 따라 필요할 수 있음)
            // if (rendererList.Count > 0 && rendererList[0] != null) rendererList[0].gameObject.SetActive(true);
        }

        for (int i = 0; i < rendererList.Count; i++)
        {
            if (rendererList[i] != null)
            {
                // 페이드 코루틴 시작
                Coroutine fadeCo = StartCoroutine(FadeToAlpha(rendererList[i], colliderList[i], targetAlpha, duration, targetColliderActive));
                currentFades.Add(fadeCo);
            }
        }
    }

    // 모든 현재 페이드 코루틴을 멈추는 함수
    private void StopAllFades()
    {
        foreach (Coroutine co in currentFades)
        {
            if (co != null)
            {
                StopCoroutine(co);
            }
        }
        currentFades.Clear();
    }


    /// <summary>
    /// SpriteRenderer의 알파 값을 지정된 시간 동안 부드럽게 변경하고, 완료 시 콜라이더를 끕니다.
    /// </summary>
    IEnumerator FadeToAlpha(SpriteRenderer renderer, Collider2D collider, float targetAlpha, float duration, bool targetColliderActive)
    {
        float startAlpha = renderer.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            Color color = renderer.color;
            color.a = newAlpha;
            renderer.color = color;

            yield return null;
        }

        // 1. 최종 값 보장
        Color finalColor = renderer.color;
        finalColor.a = targetAlpha;
        renderer.color = finalColor;

        // 2. 최종 상태 설정 (알파가 0이 되었을 때만 콜라이더를 비활성화)
        if (targetAlpha <= 0f)
        {
            if (collider != null) collider.enabled = false;

            // 옵션: GameObject 자체도 비활성화 (씬에 따라 필요할 수 있음)
            // if (renderer != null) renderer.gameObject.SetActive(false);
        }
    }
}