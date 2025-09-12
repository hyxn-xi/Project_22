using UnityEngine;

public class SceneSwitcher : MonoBehaviour
{
    [Header("Scene Settings")]
    public string targetSceneName;
    public bool useFade = true;
    public float fadeDuration = 1.5f; // 원하는 페이드 시간 설정

    public void SwitchScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[SceneSwitcher] 씬 이름이 설정되지 않았습니다.");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[SceneSwitcher] SceneTransitionManager 인스턴스를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"[SceneSwitcher] 씬 전환 요청: {targetSceneName}, 페이드 사용: {useFade}, 페이드 시간: {fadeDuration}");
        SceneTransitionManager.Instance.StartSceneTransition(targetSceneName, fadeDuration, useFade); // ✅ 올바른 파라미터 순서
    }
}
