using UnityEngine;

public static class AppFrameSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        // vSync 1 = 모니터 주사율에 동기(60/144/240 등)
        QualitySettings.vSyncCount = 1;

        // vSync가 꺼진 환경을 대비한 백업 캡(60fps)
        Application.targetFrameRate = 60;

        // (선택) 물리 보간으로 움직임 부드럽게
        Time.fixedDeltaTime = 0.02f; // 기본 50Hz 유지
        // Time.maximumDeltaTime = 0.333f; // 긴 프리즈에도 물리 폭주 방지(선택)
    }
}
