using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public class Scene001aIntroDirector_UsingTypingWithPopup : MonoBehaviour
{
    [Header("Dialogue (existing TypingDialogueWithPopup)")]
    public TypingDialogueWithPopup dialogue;  // ���� ��ũ��Ʈ
    [TextArea] public string line1 = "���� �� �������� ������ �� ����. ���� �ٽ� ���ƿԾ�.";
    [TextArea] public string line2 = "..������ �� ���ܸ��� ������� �� �� ����. ������ �ʹ� ���� ����.";
    public KeyCode advanceKey = KeyCode.F;    // ���� Ű

    [Header("Scene Transition")]
    public string nextSceneName = "GameScene-1a";

    [Header("Camera Focus")]
    public Camera cam;                 // ���� Camera.main
    public Transform dadFocus;         // �ƺ� Ŭ����� ����Ʈ(�� ������Ʈ)
    public Transform momFocus;         // ���� Ŭ����� ����Ʈ(�� ������Ʈ)
    public float zoomInTime = 0.6f;
    public float holdTime = 0.6f;
    public float zoomOutTime = 0.6f;
    [Tooltip("���� ī�޶�: �������� �� �����")]
    public float orthoZoomSize = 2.5f;
    [Tooltip("���� ī�޶�: �������� �� �����")]
    public float perspectiveFOV = 30f;

    [Header("Optional")]
    [Tooltip("���� �� ��Ȱ��ȭ�� ī�޶� �ȷο�/Cinemachine ������Ʈ")]
    public GameObject cameraFollowToDisable;

    // internal
    Vector3 camPos0; float ortho0; float fov0;
    bool followWasActive;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("[IntroDirector] Camera�� �ʿ��մϴ�."); enabled = false; return; }

        camPos0 = cam.transform.position;
        if (cam.orthographic) ortho0 = cam.orthographicSize; else fov0 = cam.fieldOfView;

        if (!dialogue)
        {
            Debug.LogError("[IntroDirector] TypingDialogueWithPopup ������ �ʿ��մϴ�.");
            enabled = false; return;
        }

        // �� TypingDialogueWithPopup�� Start���� lines�� �ٷ� �����Ƿ�,
        //    Awake���� ���� ������ �θ� ���� ���� ���� �����մϴ�.
        dialogue.lines = new string[] { line1, line2 };
        dialogue.nextKey = advanceKey;
        dialogue.nextSceneName = nextSceneName;

        // �� �������� �˾�/ī�޶����� ���� �ʵ���(���� MoveCameraTo�� EndSequence�� ȣ����)
        dialogue.popupObject = null;
        dialogue.dimBackground = null;
        dialogue.cameraTransform = null;
        dialogue.cameraTarget = null;

        if (cameraFollowToDisable)
        {
            followWasActive = cameraFollowToDisable.activeSelf;
            cameraFollowToDisable.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (cameraFollowToDisable)
            cameraFollowToDisable.SetActive(followWasActive);
    }

    void Start()
    {
        // TypingDialogueWithPopup�� Start���� �ڵ����� 1��° ���� ��� �����մϴ�.
        // �츮�� �ؽ�Ʈ�� "������" ǥ�õǴ� ������ ������ ī�޶� ������ �����մϴ�.
        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // 1) ù ��° ���� ������ ǥ�õ� ������ ���
        yield return StartCoroutine(WaitUntilTextFullyEquals(dialogue.dialogueText, line1));
        // �ƺ� Ŭ�����(���߿� ����ڰ� ���� �ٷ� �ѱ�� ��� �ߴ�)
        if (dadFocus) yield return StartCoroutine(ZoomWithInterrupt(line1, dadFocus));

        // 2) �� ��° ���� ������ ǥ�õ� ������ ���
        yield return StartCoroutine(WaitUntilTextFullyEquals(dialogue.dialogueText, line2));
        // ���� Ŭ�����(���������� ���߿� ���� �ٲ�� �ߴ�)
        if (momFocus) yield return StartCoroutine(ZoomWithInterrupt(line2, momFocus));

        // ���� �帧: ����ڰ� F�� ������ ���� �ѱ��
        // TypingDialogueWithPopup�� EndSequence()�� ȣ���ϰ�
        // nextSceneName("GameScene-1a")�� ��ȯ�˴ϴ�.
    }

    IEnumerator WaitUntilTextFullyEquals(TMP_Text label, string target)
    {
        if (!label) yield break;
        // ����ڰ� F�� ��ŵ�ϵ� Ÿ������ ������, "��Ȯ�� ���� ���ڿ�"�� �� �������� ���
        while (label.text != target)
            yield return null;
    }

    IEnumerator ZoomWithInterrupt(string watchingLine, Transform focus)
    {
        if (!cam) yield break;

        // �غ�
        Vector3 startPos = cam.transform.position;
        Vector3 targetPos = new Vector3(focus.position.x, focus.position.y, startPos.z);
        float fromSize = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
        float toSize = cam.orthographic ? orthoZoomSize : perspectiveFOV;

        // IN
        float t = 0f;
        while (t < zoomInTime)
        {
            // ���� �ٲ�� ��� �ߴ�
            if (dialogue.dialogueText && dialogue.dialogueText.text != watchingLine)
                yield break;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / zoomInTime);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, a);
            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(fromSize, toSize, a);
            else cam.fieldOfView = Mathf.Lerp(fromSize, toSize, a);
            yield return null;
        }
        cam.transform.position = targetPos;
        if (cam.orthographic) cam.orthographicSize = toSize; else cam.fieldOfView = toSize;

        // HOLD
        float h = 0f;
        while (h < holdTime)
        {
            if (dialogue.dialogueText && dialogue.dialogueText.text != watchingLine)
                yield break;
            h += Time.deltaTime;
            yield return null;
        }

        // OUT
        t = 0f;
        while (t < zoomOutTime)
        {
            if (dialogue.dialogueText && dialogue.dialogueText.text != watchingLine)
                yield break;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / zoomOutTime);
            cam.transform.position = Vector3.Lerp(targetPos, camPos0, a);
            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(toSize, ortho0, a);
            else cam.fieldOfView = Mathf.Lerp(toSize, fov0, a);
            yield return null;
        }
        RestoreCamera();
    }

    void RestoreCamera()
    {
        if (!cam) return;
        cam.transform.position = camPos0;
        if (cam.orthographic) cam.orthographicSize = ortho0;
        else cam.fieldOfView = fov0;
    }
}
