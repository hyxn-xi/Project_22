using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StageClearSequence : MonoBehaviour
{
    [Header("Camera / Zoom (LevelBounds 기준)")]
    public Camera cam;
    public Collider2D levelBounds;
    public float zoomDuration = 1.2f;
    public float zoomMargin = 1.05f;
    public AnimationCurve zoomEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Disable During Zoom (팔로우/시네머신 등)")]
    public Behaviour[] cameraBehavioursToDisable;

    [Header("Radial (UI)")]
    public RadialReveal radial;
    public float radialDuration = 0.9f;

    [Header("Background source (밝은 배경)")]
    public SpriteRenderer brightBg;            // BG_Bright의 SpriteRenderer
    public Texture nextBackgroundTexture;      // 비워두면 brightBg.sprite.texture 자동

    [Header("Pickup Animation (중복 방지용 감지)")]
    [Tooltip("감지할 상태 이름 후보들(레이어 무관, 하나라도 맞으면 '이미 재생 중'으로 간주)")]
    public string[] pickupStateCandidates = { "PickUp", "Pickup" };
    [Tooltip("감지 그레이스(초). 이 시간 동안 '이미 픽업 상태로 진입했는지'를 먼저 기다린다.")]
    public float pickupEnterGrace = 0.25f;
    [Tooltip("픽업 상태가 감지되지 않으면 마지막에 1회 CrossFade로 강제 진입할지")]
    public bool autoPlayPickupIfNotEntered = false;
    [Tooltip("autoPlay 시 사용할 레이어 인덱스(보통 0). -1이면 0을 사용")]
    public int autoPlayLayerIndex = -1;
    [Tooltip("픽업 끝에서 살짝 더 기다리는 시간(초)")]
    public float pickupEndHold = 0.05f;

    [Header("Scene")]
    public string nextSceneName = "";
    public float holdAfterReveal = 3f;

    [Header("Debug")]
    public bool verboseLog = true;

    Transform _player;
    Vector3? _fixedCenter;

    // freeze 복구용
    PlayerController _pc;
    Rigidbody2D _rb;
    bool _pcWasEnabled;
    bool _rbWasKinematic;
    RigidbodyConstraints2D _rbPrevConstraints;

    bool _running; // Begin 중복 방지

    void OnValidate()
    {
        if (!cam) cam = Camera.main;
    }

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!levelBounds) levelBounds = FindFirstObjectByType<Collider2D>(FindObjectsInactive.Include);
        if (!radial) radial = FindFirstObjectByType<RadialReveal>(FindObjectsInactive.Include);

        if (!nextBackgroundTexture)
        {
            if (!brightBg)
            {
                var go = GameObject.Find("BG_Bright");
                if (go) brightBg = go.GetComponent<SpriteRenderer>();
            }
            if (brightBg && brightBg.sprite) nextBackgroundTexture = brightBg.sprite.texture;
        }

        if (radial)
        {
            if (nextBackgroundTexture) radial.SetTexture(nextBackgroundTexture);
            radial.gameObject.SetActive(false);
        }

        if (verboseLog)
            Debug.Log($"[SCS] Awake cam={(cam ? cam.name : "NULL")}, bounds={levelBounds}, radial={radial}, nextTex={(nextBackgroundTexture ? nextBackgroundTexture.name : "NULL")}");
    }

    // === 외부 호출 ===
    public void Begin(Transform player, string nextScene, float hold)
    {
        if (_running) return;  // 중복 방지
        _running = true;

        _player = player;
        _fixedCenter = null;
        if (!string.IsNullOrEmpty(nextScene)) nextSceneName = nextScene;
        holdAfterReveal = hold;
        StartCoroutine(Co_Run());
    }
    public void Begin(Transform player) => Begin(player, nextSceneName, holdAfterReveal);
    public void Begin(Vector3 playerWorldPos)
    {
        if (_running) return;  // 중복 방지
        _running = true;

        _player = null;
        _fixedCenter = playerWorldPos;
        StartCoroutine(Co_Run());
    }

    IEnumerator Co_Run()
    {
        // 보정
        if (!_player && !_fixedCenter.HasValue)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc) _player = pc.transform;
        }
        if (!cam) cam = Camera.main;
        if (!radial)
        {
            radial = FindFirstObjectByType<RadialReveal>(FindObjectsInactive.Include);
            if (!radial) { Debug.LogError("[SCS] RadialReveal가 없습니다."); yield break; }
        }
        if (!nextBackgroundTexture && brightBg && brightBg.sprite)
            nextBackgroundTexture = brightBg.sprite.texture;
        if (nextBackgroundTexture) radial.SetTexture(nextBackgroundTexture);

        // ---------- 1) 즉시 플레이어 고정 → 픽업 애니 '중복 없이' 완료까지 대기 ----------
        if (_player) FreezePlayer(true);
        if (_player) yield return PlayPickupAndWait();   // ※ 트리거는 건드리지 않음

        // ---------- 2) 팔로우/시네머신 끄고, 경계 기준 패닝+줌 ----------
        SetBehaviours(cameraBehavioursToDisable, false);
        yield return ZoomAndCenterToBounds();

        // ---------- 3) 원형 퍼짐 (플레이어 중심, 코너까지 채움) ----------
        Vector3 centerWorld = _player ? _player.position :
                              _fixedCenter ?? (levelBounds ? levelBounds.bounds.center : Vector3.zero);

        radial.ConfigureForCamera(cam, centerWorld); // Aspect/MaxRadius/Center 세팅
        if (verboseLog)
        {
            var uv = cam.WorldToViewportPoint(centerWorld);
            Debug.Log($"[SCS] Reveal center world={centerWorld}  uv={uv} cam={cam.name}");
        }

        radial.gameObject.SetActive(true);
        yield return RevealRoutineFollow(radial, radialDuration); // 퍼짐 중 Center 보정

        // ---------- 4) 잠깐 유지 후 씬 전환 ----------
        yield return new WaitForSeconds(holdAfterReveal);
        SetBehaviours(cameraBehavioursToDisable, true);

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            FreezePlayer(false); // 다음 씬이 없다면 원복
    }

    // === 픽업 애니를 "중복 없이" 기다리는 구간 ===
    IEnumerator PlayPickupAndWait()
    {
        var anim = _player ? _player.GetComponent<Animator>() : null;
        if (!anim) { yield return new WaitForSeconds(0.5f); yield break; }

        int foundLayer;
        bool EnteredPickupAnyLayer()
        {
            for (int layer = 0; layer < anim.layerCount; layer++)
            {
                var cur = anim.GetCurrentAnimatorStateInfo(layer);
                var next = anim.GetNextAnimatorStateInfo(layer);
                if (IsPickupName(cur) || IsPickupName(next)) { foundLayer = layer; return true; }
            }
            foundLayer = -1;
            return false;
        }
        bool IsPickupName(AnimatorStateInfo info)
        {
            if (pickupStateCandidates == null) return false;
            for (int i = 0; i < pickupStateCandidates.Length; i++)
            {
                string n = pickupStateCandidates[i];
                if (!string.IsNullOrEmpty(n) && info.IsName(n)) return true;
            }
            return false;
        }

        // 1) grace 동안 '이미 진입했는지' 먼저 기다림
        float t = 0f;
        while (t < pickupEnterGrace)
        {
            if (EnteredPickupAnyLayer())
            {
                if (verboseLog) Debug.Log($"[SCS] Pickup detected (layer {foundLayer}). Waiting to finish.");
                break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        // 2) 아직도 감지 안 되었고, 자동 재생 옵션이 켜진 경우에만 1회 CrossFade
        if (!EnteredPickupAnyLayer() && autoPlayPickupIfNotEntered)
        {
            int layer = (autoPlayLayerIndex >= 0 ? autoPlayLayerIndex : 0);
            string nameToPlay = (pickupStateCandidates != null && pickupStateCandidates.Length > 0)
                                ? pickupStateCandidates[0] : "PickUp";
            if (verboseLog) Debug.Log($"[SCS] Pickup NOT detected. CrossFade once → {nameToPlay} (layer {layer})");
            anim.CrossFadeInFixedTime(nameToPlay, 0.05f, layer, 0f);
        }

        // 3) 활성화될 때까지 짧게 대기(안전망)
        float sec = 0f;
        while (sec < 0.25f && !EnteredPickupAnyLayer())
        {
            sec += Time.deltaTime;
            yield return null;
        }

        // 4) 활성화되면 끝까지 대기
        float clipLen = TryGetClipLength(anim, pickupStateCandidates, 0.6f);
        float elapsed = 0f;
        while (elapsed < clipLen)
        {
            bool finished = false;
            for (int layer = 0; layer < anim.layerCount; layer++)
            {
                var info = anim.GetCurrentAnimatorStateInfo(layer);
                if (IsPickupName(info) && info.normalizedTime >= 0.99f) { finished = true; break; }
            }
            if (finished) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (pickupEndHold > 0f) yield return new WaitForSeconds(pickupEndHold);
    }

    float TryGetClipLength(Animator a, string[] names, float fallback)
    {
        if (!a || a.runtimeAnimatorController == null) return fallback;
        foreach (var c in a.runtimeAnimatorController.animationClips)
        {
            if (!c) continue;
            for (int i = 0; i < names.Length; i++)
                if (!string.IsNullOrEmpty(names[i]) && c.name == names[i]) return c.length;
        }
        return fallback;
    }

    void FreezePlayer(bool freeze)
    {
        if (!_player) return;

        if (!_pc) _pc = _player.GetComponent<PlayerController>();
        if (!_rb) _rb = _player.GetComponent<Rigidbody2D>();

        if (freeze)
        {
            if (_pc) { _pcWasEnabled = _pc.enabled; _pc.enabled = false; }
            if (_rb)
            {
                _rbWasKinematic = _rb.isKinematic;
                _rbPrevConstraints = _rb.constraints;

                _rb.velocity = Vector2.zero;
                _rb.isKinematic = true;                           // 힘 제거
                _rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }
        else
        {
            if (_pc) _pc.enabled = _pcWasEnabled;
            if (_rb)
            {
                _rb.isKinematic = _rbWasKinematic;
                _rb.constraints = _rbPrevConstraints;
            }
        }
    }

    void SetBehaviours(Behaviour[] arr, bool on)
    {
        if (arr == null) return;
        foreach (var b in arr) if (b) b.enabled = on;
    }

    IEnumerator ZoomAndCenterToBounds()
    {
        if (!cam || !levelBounds) yield break;
        Bounds b = levelBounds.bounds;

        Vector3 startP = cam.transform.position;
        Vector3 targetP = new Vector3(b.center.x, b.center.y, startP.z);

        float startS = cam.orthographicSize;
        float targetS = CalcOrthoSizeToFit(b, zoomMargin);

        float t = 0f;
        while (t < zoomDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / zoomDuration);
            float e = zoomEase.Evaluate(k);
            cam.transform.position = Vector3.Lerp(startP, targetP, e);
            cam.orthographicSize = Mathf.Lerp(startS, targetS, e);
            yield return null;
        }
        cam.transform.position = targetP;
        cam.orthographicSize = targetS;
    }

    float CalcOrthoSizeToFit(Bounds b, float margin)
    {
        float halfH = b.extents.y * margin;
        float halfW = b.extents.x * margin / cam.aspect;
        return Mathf.Max(halfH, halfW);
    }

    // 퍼짐 동안에도 매 프레임 플레이어 중심/반경 보정
    IEnumerator RevealRoutineFollow(RadialReveal r, float duration)
    {
        float t = 0f;
        r.SetProgress(0f);

        while (t < duration)
        {
            t += Time.deltaTime;

            Vector3 worldCenter = _player ? _player.position :
                                  _fixedCenter ?? (levelBounds ? levelBounds.bounds.center : Vector3.zero);

            var uv = cam.WorldToViewportPoint(worldCenter);
            r.ConfigureForCamera(cam, uv);  // Center/Aspect/MaxRadius 동시 반영(uv버전)

            r.SetProgress(Mathf.Clamp01(t / duration));
            yield return null;
        }
        r.SetProgress(1f);
    }
}
