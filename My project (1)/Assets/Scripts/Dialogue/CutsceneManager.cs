using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] VideoPlayer videoPlayer;
    [SerializeField] string nextSceneName;

    void Start()
    {
        Debug.Log("컷신 씬에서 SceneTransitionManager.Instance 상태: " + (SceneTransitionManager.Instance == null ? "NULL" : "OK"));
        
        // 씬 시작 시 페이드인
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartFadeIn();
        }
        else
        {
            Debug.LogWarning("[CutsceneManager] SceneTransitionManager 인스턴스를 찾을 수 없습니다.");
        }

        // 비디오 종료 이벤트 연결
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        // 비디오 끝나면 페이드아웃 후 씬 전환
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartSceneTransition(nextSceneName);
        }
        else
        {
            Debug.LogError("[CutsceneManager] SceneTransitionManager 인스턴스를 찾을 수 없습니다.");
            SceneManager.LoadScene(nextSceneName); // 예외 처리: 페이드 없이 전환
        }
    }
}
