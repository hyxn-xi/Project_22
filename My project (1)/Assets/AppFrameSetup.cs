using UnityEngine;

public static class AppFrameSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        // vSync 1 = ����� �ֻ����� ����(60/144/240 ��)
        QualitySettings.vSyncCount = 1;

        // vSync�� ���� ȯ���� ����� ��� ĸ(60fps)
        Application.targetFrameRate = 60;

        // (����) ���� �������� ������ �ε巴��
        Time.fixedDeltaTime = 0.02f; // �⺻ 50Hz ����
        // Time.maximumDeltaTime = 0.333f; // �� ������� ���� ���� ����(����)
    }
}
