using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Jump (set Y velocity directly)")]
    public float jumpVelocity = 7f;
    public float doubleJumpVelocity = 10f;
    public float coyoteTime = 0.06f;

    [Header("Landing Feel")]
    public float landingMinHold = 0.22f;     // 착지 연출 최소 유지 시간
    public bool freezeHorizontalOnLanding = true;

    [Header("Landing Timing (Fallback)")]
    public float landingExitNormalized = 0.80f;
    public float landingFallbackTimeout = 0.60f;

    [Header("Hit Timing (Fallback)")]
    public float hitMinHold = 0.35f;
    public float hitExitNormalized = 0.90f;
    public float hitFallbackTimeout = 1.20f;

    [Header("World Bounds (X only)")]
    public float minX = -8f;
    public float maxX = 43f;

    public enum KillCheckMode { CenterY, FeetBelow, FullBodyBelow }
    [Header("Kill Line (A 방식)")]
    public bool killLineEnabled = true;
    public float killY = -5f;
    public KillCheckMode killCheckMode = KillCheckMode.FeetBelow;
    public float killBuffer = 0.06f;
    public bool killOnlyWhenAirborne = true;
    public float killGrace = 0.10f;
    float killBelowTimer = 0f;

    [Header("HP")]
    public int HP = 2;

    [Header("Ground Check (feet raycasts)")]
    public LayerMask groundLayer;
    public float feetOffsetX = 0.22f;
    public float feetRayLen = 0.18f;

    [Header("Ground Check (contacts)")]
    public bool useContactGrounding = true;
    int groundContacts = 0;

    [Header("Moving Platform Ride")]
    public bool ridePlatforms = true;
    public bool alwaysIdleOnPlatforms = true;
    public bool treatMovingPlatformAsGround = true;
    Transform currentPlatform = null; // 부모 Transform 저장용
    Rigidbody2D currentPlatformRB = null;
    Vector2 lastPlatformRBPos;
    Vector3 lastPlatformPos;
    Vector2 platformVelocity = Vector2.zero;
    bool onMovingPlatformGround = false;

    [Header("Pickup & Scene")]
    public string nextSceneName = "";
    public float afterPickupDelay = 0.2f;

    [Header("Pickup Debounce (애니 1회 보장)")]
    public float pickupCooldown = 0.15f;
    float _pickupBlockUntil = -1f;
    bool _pickupAnimPlayed = false;

    [Header("Hit Detection")]
    [SerializeField] LayerMask obstacleLayers;
    [SerializeField] string obstacleTag = "Obstacle";
    [SerializeField] float hitIFrame = 0.25f;
    [SerializeField] float knockbackForce = 4f;

    [Header("Screen Flash (on Hit)")]
    public bool useHitFlash = true;
    public Color hitFlashColor = new Color(1f, 0f, 0f, 0.7f);
    public float hitFlashFadeIn = 0.06f;
    public float hitFlashHold = 0.05f;
    public float hitFlashFadeOut = 0.12f;

    [Header("Failsafe")]
    public bool forceCrossFadeOnDoubleJump = true;
    public int maxJumpCount = 2;

    [Header("Game Over Hook")]
    public GameOverController gameOver;
    public bool useDeadAnimEvent = false;
    public float deadAnimMinWait = 0.15f;
    public float deadAnimTimeout = 2.5f;
    bool _gameOverFired = false;

    [Header("Debug")]
    public bool debugAnim = false;

    // --- Components
    Rigidbody2D rb;
    Collider2D col;
    Animator anim;

    // --- State
    float moveInput;
    int pendingFaceDir = 1;
    bool isDead = false;
    bool pickupLock = false;

    bool isGrounded = true;
    bool wasGrounded = true;
    bool isFalling = false;
    bool wasFalling = false;

    int jumpCount = 0;
    float lastGroundedTime = -999f;
    float landingHoldUntil = -1f;
    bool invulnHit = false;

    // 점프 직후 1~2프레임 강제 공중
    float jumpUngroundUntil = -1f;

    // --- Animator hashes
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    static readonly int JumpTrgHash = Animator.StringToHash("Jump");
    static readonly int DoubleJumpTrgHash = Animator.StringToHash("DoubleJump");
    static readonly int LandTrgHash = Animator.StringToHash("Land");
    static readonly int HitTrgHash = Animator.StringToHash("Hit");
    static readonly int DeadBoolHash = Animator.StringToHash("Dead");
    static readonly int PickupTrgHash = Animator.StringToHash("Pickup");

    // --- Animator param alias
    string[] A_Speed = { "Speed", "speed", "Move", "Velocity", "VelocityX" };
    string[] A_Grounded = { "IsGrounded", "Grounded", "Ground" };
    string[] A_Falling = { "IsFalling", "Falling" };
    string[] A_Hit = { "Hit2", "Hit" };
    string[] A_Land = { "Landing", "Land" };
    string[] A_Jump = { "Jump" };
    string[] A_DoubleJump = { "DoubleJump", "Jump2", "SkillJump" };
    string[] A_Pickup = { "Pickup", "PickUp" };
    string[] A_WalkBool = { "Walk", "IsWalking", "Running" };
    string[] A_IdleBool = { "Idle", "IsIdle" };

    // --- Animator states(폴백 후보)
    string[] S_Idle = { "Idle2", "Idle" };
    string[] S_Walk = { "Walk2", "Walk", "Run" };
    string[] S_Landing = { "Landing", "Land" };
    string[] S_Hit = { "Hit2", "Hit", "hit2" };
    string[] S_Jump2 = { "SkillJump", "DoubleJump", "Jump2" };
    string[] S_Dead = { "Dead2", "Dead" };

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");

        // 렌더 보간 권장
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!gameOver)
            gameOver = FindFirstObjectByType<GameOverController>(FindObjectsInactive.Include);
    }

    void Start()
    {
        anim.ResetTrigger(PickupTrgHash);
        anim.ResetTrigger(JumpTrgHash);
        anim.ResetTrigger(DoubleJumpTrgHash);
        anim.ResetTrigger(HitTrgHash);
        anim.SetBool(DeadBoolHash, false);

        // 시작 방향(스케일) 보정
        pendingFaceDir = Mathf.Sign(transform.localScale.x) >= 0f ? 1 : -1;

        if (debugAnim && anim != null)
        {
            Debug.Log($"[PlayerController] Animator: {anim.runtimeAnimatorController?.name}");
            LogParamPresence("Speed/Alias", A_Speed, AnimatorControllerParameterType.Float);
            LogParamPresence("Grounded/Alias", A_Grounded, AnimatorControllerParameterType.Bool);
            LogParamPresence("Falling/Alias", A_Falling, AnimatorControllerParameterType.Bool);
            LogParamPresence("Hit/Alias", A_Hit, AnimatorControllerParameterType.Trigger);
            LogParamPresence("Landing/Alias", A_Land, AnimatorControllerParameterType.Trigger);
        }
    }

    void Update()
    {
        if (isDead || pickupLock) return;

        // --- 입력만 읽는다 (트랜스폼/리짓 바꾸지 않음) ---
        moveInput = 0f;
        if (Input.GetKey(KeyCode.A)) moveInput -= 1f;
        if (Input.GetKey(KeyCode.D)) moveInput += 1f;

        // 착지 연출 중엔 수평 입력 무시
        if (freezeHorizontalOnLanding && Time.time < landingHoldUntil)
            moveInput = 0f;

        // 방향(스케일X) 결정만 해두고, 실제 반영은 FixedUpdate에서
        if (moveInput > 0.01f) pendingFaceDir = 1;
        else if (moveInput < -0.01f) pendingFaceDir = -1;

        // 점프 입력
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentPlatform != null) DetachFromPlatform();

            bool canFirst = (isGrounded || Time.time - lastGroundedTime <= coyoteTime) && jumpCount == 0;
            bool canSecond = (!isGrounded && jumpCount == 1);

            if (canFirst) DoJump(false);
            else if (canSecond) DoJump(true);
        }
    }

    void FixedUpdate()
    {
        if (isDead || pickupLock) return;

        // 변수 선언
        var s = transform.localScale;
        float abs = Mathf.Abs(s.x);

        // 4) 방향(스케일X) 반영 — FixedUpdate에서만 Transform 수정
        s.x = abs * (pendingFaceDir >= 0 ? 1f : -1f);
        transform.localScale = s;

        // 1) 수평 속도
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // 착지 연출 중엔 강제로 정지
        if (freezeHorizontalOnLanding && Time.time < landingHoldUntil)
            rb.velocity = new Vector2(0f, rb.velocity.y);

        // 2) 델타 이동 로직은 Transform Parenting 방식에서는 제거합니다.
        platformVelocity = Vector2.zero;

        // 3) 월드 경계 X 클램프
        var pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.position = pos; // Rigidbody 위치 확정
    }

    void LateUpdate()
    {
        if (isDead || pickupLock) return;

        UpdateGroundAndFalling();

        float walkSpeed = Mathf.Abs(rb.velocity.x);
        float animSpeed = walkSpeed;

        // ★ 발밑이 무빙플랫폼일 때만 Idle 고정
        if (alwaysIdleOnPlatforms && isGrounded && currentPlatform != null)
            animSpeed = 0f;

        SafeSetFloat(SpeedHash, animSpeed);
        SetFloatIfExists(A_Speed, animSpeed);

        SafeSetBool(IsGroundedHash, isGrounded);
        SetBoolIfExists(A_Grounded, isGrounded);

        SafeSetBool(IsFallingHash, isFalling);
        SetBoolIfExists(A_Falling, isFalling);

        SetBoolIfExists(A_WalkBool, isGrounded && animSpeed > 0.05f);
        SetBoolIfExists(A_IdleBool, isGrounded && animSpeed <= 0.05f && !isFalling);

        if (!wasGrounded && isGrounded)
        {
            jumpCount = 0;
            SafeSetTrigger(LandTrgHash);
            SetTriggerIfExists(A_Land);

            SafeResetTrigger(JumpTrgHash);
            SafeResetTrigger(DoubleJumpTrgHash);
            ResetTriggerIfExists(A_Jump);
            ResetTriggerIfExists(A_DoubleJump);

            landingHoldUntil = Time.time + landingMinHold;
            StartCoroutine(FallbackFromStateToGroundedIdle(S_Landing, landingExitNormalized, landingFallbackTimeout));
        }

        // isGrounded가 false인데 부모 관계가 남아있으면 해제합니다.
        if (!isGrounded && currentPlatform != null) DetachFromPlatform();

        wasGrounded = isGrounded;
        wasFalling = isFalling;

        if (killLineEnabled && IsBelowKillLineNow())
        {
            killBelowTimer += Time.deltaTime;
            if (killBelowTimer >= killGrace) { ForceKill(); return; }
        }
        else killBelowTimer = 0f;
    }

    void DoJump(bool isDouble)
    {
        if (currentPlatform != null) DetachFromPlatform();

        var v = rb.velocity;
        v.y = isDouble ? doubleJumpVelocity : jumpVelocity;
        rb.velocity = v;

        jumpUngroundUntil = Time.time + 0.08f;

        if (isDouble)
        {
            jumpCount = 2;
            SafeResetTrigger(JumpTrgHash);
            SafeSetTrigger(DoubleJumpTrgHash);
            SetTriggerIfExists(A_DoubleJump);
            if (forceCrossFadeOnDoubleJump)
                CrossFadeToFirstExisting(S_Jump2, 0.05f);
            StartCoroutine(ClearTriggerNextFrame(DoubleJumpTrgHash, A_DoubleJump));
        }
        else
        {
            jumpCount = 1;
            SafeResetTrigger(DoubleJumpTrgHash);
            SafeSetTrigger(JumpTrgHash);
            SetTriggerIfExists(A_Jump);
            ForcePlayJump(false);
            StartCoroutine(ClearTriggerNextFrame(JumpTrgHash, A_Jump));
        }

        isGrounded = false;
        isFalling = false;
        lastGroundedTime = -999f;
        landingHoldUntil = -1f;
    }

    void UpdateGroundAndFalling()
    {
        bool forceAir = Time.time < jumpUngroundUntil;

        Bounds b = col.bounds;
        Vector2 left = new Vector2(b.center.x - feetOffsetX, b.min.y + 0.02f);
        Vector2 right = new Vector2(b.center.x + feetOffsetX, b.min.y + 0.02f);

        float rayLen = Mathf.Max(feetRayLen, 0.25f);

        RaycastHit2D lh = Physics2D.Raycast(left, Vector2.down, rayLen, groundLayer);
        RaycastHit2D rh = Physics2D.Raycast(right, Vector2.down, rayLen, groundLayer);
        bool l = lh.collider != null;
        bool r = rh.collider != null;

        bool groundedByContact = (useContactGrounding && groundContacts > 0);
        bool groundedNow = (groundedByContact || l || r);
        if (forceAir) groundedNow = false;

        if (groundedNow) lastGroundedTime = Time.time;

        float vy = rb.velocity.y;
        bool fallingNow = !groundedNow && vy < -0.01f;
        if (forceAir && vy >= 0f) fallingNow = false;

        isGrounded = groundedNow;
        isFalling = fallingNow;

        // onMovingPlatformGround 플래그는 currentPlatform 유무로만 판단
        onMovingPlatformGround = currentPlatform != null;
    }

    bool IsBelowKillLineNow()
    {
        if (killOnlyWhenAirborne && isGrounded) return false;

        Bounds b = col.bounds;
        float threshold = killY - killBuffer;

        switch (killCheckMode)
        {
            case KillCheckMode.FeetBelow: return b.min.y < threshold;
            case KillCheckMode.FullBodyBelow: return b.max.y < threshold;
            default: return transform.position.y < threshold;
        }
    }

    public void ForceKill()
    {
        if (isDead) return;
        HP = 0;
        Die("ForceKill");
    }

    bool IsGroundCollider(Collider2D c)
    {
        if (!c) return false;

        bool layerOK = (groundLayer.value & (1 << c.gameObject.layer)) != 0;
        if (layerOK) return true;

        if (treatMovingPlatformAsGround)
            if (c.GetComponentInParent<MovingPlatform2D>() != null) return true;

        return false;
    }

    bool IsObstacle(Collider2D c)
    {
        if (c == null) return false;
        if (!string.IsNullOrEmpty(obstacleTag) && c.CompareTag(obstacleTag)) return true;
        if (obstacleLayers.value != 0 && ((obstacleLayers.value & (1 << c.gameObject.layer)) != 0)) return true;

        var p = c.GetComponentInParent<Transform>();
        if (p && !string.IsNullOrEmpty(obstacleTag) && p.CompareTag(obstacleTag)) return true;
        return false;
    }

    void DetachFromPlatform()
    {
        // ★★★ Transform SetParent(null) 로직 복구
        if (transform.parent != null)
            transform.SetParent(null, true);

        currentPlatform = null;
        currentPlatformRB = null;
        platformVelocity = Vector2.zero;
    }

    // ======== 충돌 처리 (Transform Parenting 방식) ========
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (IsGroundCollider(collision.collider))
        {
            groundContacts++;
            lastGroundedTime = Time.time;

            // ★★★ Parenting 로직 ★★★
            var mp = collision.collider.GetComponentInParent<MovingPlatform2D>();

            if (mp != null && currentPlatform == null)
            {
                currentPlatform = mp.transform;
                transform.SetParent(currentPlatform, true);
            }

            return;
        }

        if (pickupLock || invulnHit) return;

        if (IsObstacle(collision.collider))
        {
            Vector2 n = collision.contactCount > 0
                        ? collision.GetContact(0).normal
                        : new Vector2(-Mathf.Sign(transform.localScale.x), 0.5f);
            DoHit(1, n);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (IsGroundCollider(collision.collider))
        {
            // 플랫폼 위에 계속 있을 경우 부모 관계 유지 (안정화)
            var mp = collision.collider.GetComponentInParent<MovingPlatform2D>();

            if (mp != null && currentPlatform == null)
            {
                currentPlatform = mp.transform;
                transform.SetParent(currentPlatform, true);
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (IsGroundCollider(collision.collider))
        {
            groundContacts = Mathf.Max(0, groundContacts - 1);

            // 나가는 콜라이더가 현재 플랫폼이라면 부모 해제
            var leftRoot = collision.collider.GetComponentInParent<Transform>();
            if (currentPlatform != null && leftRoot != null && leftRoot == currentPlatform)
            {
                DetachFromPlatform();
            }
        }
    }
    // ======== 충돌 처리 끝 ========

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || pickupLock || invulnHit) return;

        if (IsObstacle(other))
        {
            Vector2 n = new Vector2(Mathf.Sign(transform.localScale.x), 0f);
            DoHit(1, -n);
        }
    }

    public void ApplyDamage(int amount = 1)
    {
        if (isDead || pickupLock || invulnHit) return;
        Vector2 n = new Vector2(-Mathf.Sign(transform.localScale.x), 0.5f);
        DoHit(amount, n);
    }

    void DoHit(int damage, Vector2 hitNormal)
    {
        if (isDead) return;

        invulnHit = true;
        HP = Mathf.Max(0, HP - damage);

        SafeResetTrigger(HitTrgHash);
        SafeResetTrigger(JumpTrgHash);
        SafeResetTrigger(DoubleJumpTrgHash);
        SafeResetTrigger(LandTrgHash);
        SafeResetTrigger(PickupTrgHash);

        ResetTriggerIfExists(A_Hit);
        ResetTriggerIfExists(A_Jump);
        ResetTriggerIfExists(A_DoubleJump);
        ResetTriggerIfExists(A_Land);
        ResetTriggerIfExists(A_Pickup);

        if (HP <= 0)
        {
            Die("HP<=0");
            return;
        }

        if (currentPlatform != null) DetachFromPlatform();

        SafeSetTrigger(HitTrgHash);
        SetTriggerIfExists(A_Hit);
        StartCoroutine(ClearTriggerNextFrame(HitTrgHash, A_Hit));

        if (useHitFlash)
        {
            var sf = ScreenFlash.Instance;
            if (sf != null) sf.Flash(hitFlashColor, hitFlashFadeIn, hitFlashHold, hitFlashFadeOut);
        }

        StartCoroutine(FallbackFromStateToGroundedIdle(S_Hit, hitExitNormalized, hitFallbackTimeout));
        StartCoroutine(ForceGroundedIdleOrWalk(minDelay: hitMinHold, timeout: hitFallbackTimeout));

        StartCoroutine(ClearHitIFrame(hitIFrame));
    }

    void Die(string reason)
    {
        if (isDead) return;
        isDead = true;

        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }
        var col2 = GetComponent<Collider2D>();
        if (col2) col2.enabled = false;

        if (anim)
        {
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.speed = 1f;
            anim.applyRootMotion = false;

            SafeResetTrigger(HitTrgHash);
            SafeResetTrigger(JumpTrgHash);
            SafeResetTrigger(DoubleJumpTrgHash);
            SafeResetTrigger(LandTrgHash);
            SafeResetTrigger(PickupTrgHash);

            ResetTriggerIfExists(A_Hit);
            ResetTriggerIfExists(A_Jump);
            ResetTriggerIfExists(A_DoubleJump);
            ResetTriggerIfExists(A_Land);
            ResetTriggerIfExists(A_Pickup);

            SafeSetBool(DeadBoolHash, true);

            CrossPlayToFirstExisting(S_Dead);
            StartCoroutine(ForceDeadStateNextFrame());
        }

        if (!useDeadAnimEvent) StartCoroutine(CoWaitDeadThenGameOver());
    }

    IEnumerator CoWaitDeadThenGameOver()
    {
        if (_gameOverFired) yield break;
        _gameOverFired = true;

        float t = 0f;
        while (t < deadAnimTimeout)
        {
            if (anim)
            {
                var info = anim.GetCurrentAnimatorStateInfo(0);
                bool inDead = IsInAnyState(info, S_Dead);
                if (inDead && info.normalizedTime >= 0.98f && t >= deadAnimMinWait)
                    break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!gameOver)
            gameOver = FindFirstObjectByType<GameOverController>(FindObjectsInactive.Include);

        if (gameOver) gameOver.TriggerGameOver();
        else Debug.LogWarning("[PlayerController] GameOverController를 찾지 못했습니다.");
    }

    public void AE_OnDeadFinished()
    {
        if (_gameOverFired) return;
        _gameOverFired = true;

        if (!gameOver)
            gameOver = FindFirstObjectByType<GameOverController>(FindObjectsInactive.Include);

        if (gameOver) gameOver.TriggerGameOver();
        else Debug.LogWarning("[PlayerController] GameOverController를 찾지 못했습니다.");
    }

    IEnumerator ForceDeadStateNextFrame()
    {
        yield return null;
        CrossPlayToFirstExisting(S_Dead);
    }

    IEnumerator ClearHitIFrame(float t)
    {
        yield return new WaitForSeconds(t);
        invulnHit = false;
    }

    public void StartPickupAndChangeScene(string sceneName, float extraDelay = 0.2f)
    {
        if (isDead || pickupLock) return;

        pickupLock = true;

        if (!_pickupAnimPlayed)
        {
            SetSingleTrigger(A_Pickup, "Pickup");
            _pickupAnimPlayed = true;
        }

        rb.velocity = Vector2.zero;
        rb.simulated = false;

        float len = GetClipLen("PickUp", 0.6f);
        StartCoroutine(LoadSceneAfter(len + extraDelay, sceneName));
    }

    public void PlayPickupOnce()
    {
        if (isDead || pickupLock) return;
        if (_pickupAnimPlayed) return;
        if (Time.time < _pickupBlockUntil) return;
        _pickupBlockUntil = Time.time + pickupCooldown;

        ResetTriggerIfExists(A_Hit);
        ResetTriggerIfExists(A_Jump);
        ResetTriggerIfExists(A_DoubleJump);
        ResetTriggerIfExists(A_Land);

        SetSingleTrigger(A_Pickup, "Pickup");
        _pickupAnimPlayed = true;
    }

    IEnumerator LoadSceneAfter(float wait, string sceneName)
    {
        yield return new WaitForSeconds(wait);
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
        {
            rb.simulated = true;
            pickupLock = false;
        }
    }

    float GetClipLen(string name, float fallback)
    {
        if (anim.runtimeAnimatorController == null) return fallback;
        foreach (var c in anim.runtimeAnimatorController.animationClips)
            if (c && c.name == name) return c.length;
        return fallback;
    }

    IEnumerator FallbackFromStateToGroundedIdle(string[] watchingStates, float exitNormalizedTime, float timeout)
    {
        yield return null;

        float t = 0f;
        while (t < timeout)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            bool inWatch = IsInAnyState(info, watchingStates);

            if (!inWatch) yield break;
            if (info.normalizedTime >= exitNormalizedTime) break;

            t += Time.deltaTime;
            yield return null;
        }

        if (isDead || pickupLock) yield break;
        if (!isGrounded) yield break;

        ForceToGroundedNow();
    }

    IEnumerator ForceGroundedIdleOrWalk(float minDelay, float timeout)
    {
        if (minDelay > 0f) yield return new WaitForSeconds(minDelay);

        float t = 0f;
        while (t < timeout)
        {
            if (isDead || pickupLock) yield break;
            if (isGrounded && !isFalling) { ForceToGroundedNow(); yield break; }
            t += Time.deltaTime;
            yield return null;
        }
        if (!isDead && !pickupLock) ForceToGroundedNow();
    }

    void ForceToGroundedNow()
    {
        float vx = Mathf.Abs(rb.velocity.x);
        string target = null;
        if (vx > 0.05f) { foreach (var s in S_Walk) if (HasState(0, s)) { target = s; break; } }
        else { foreach (var s in S_Idle) if (HasState(0, s)) { target = s; break; } }
        if (string.IsNullOrEmpty(target)) return;

        anim.CrossFadeInFixedTime(target, 0.06f, 0, 0f);
        StartCoroutine(VerifyForcedState(target));
    }

    IEnumerator VerifyForcedState(string target)
    {
        yield return null;
        var info = anim.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(target)) anim.Play(target, 0, 0f);
    }

    bool IsInAnyState(AnimatorStateInfo info, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
            if (info.IsName(names[i])) return true;
        return false;
    }

    void CrossFadeToFirstExisting(string[] states, float duration)
    {
        for (int i = 0; i < states.Length; i++)
            if (HasState(0, states[i])) { anim.CrossFadeInFixedTime(states[i], duration, 0, 0f); return; }
        if (debugAnim) Debug.LogWarning("[PlayerController] CrossFade 대상 상태를 찾지 못했어요.");
    }

    void CrossPlayToFirstExisting(string[] states)
    {
        for (int i = 0; i < states.Length; i++)
            if (HasState(0, states[i])) { anim.Play(states[i], 0, 0f); return; }
        if (debugAnim) Debug.LogWarning("[PlayerController] Play 대상 상태를 찾지 못했어요.");
    }

    bool HasState(int layer, string stateName)
    {
        return anim != null && anim.HasState(layer, Animator.StringToHash(stateName));
    }

    IEnumerator ClearTriggerNextFrame(int hash, string[] alias = null)
    {
        yield return null;
        SafeResetTrigger(hash);
        if (alias != null) ResetTriggerIfExists(alias);
    }

    void ForcePlayJump(bool isDouble)
    {
        if (isDouble) return;
        string[] firstJumpStates = { "Jump", "JumpUp", "JumpStart" };
        for (int i = 0; i < firstJumpStates.Length; i++)
        {
            string st = firstJumpStates[i];
            if (HasState(0, st))
            {
                anim.CrossFadeInFixedTime(st, 0.05f, 0, 0f);
                return;
            }
        }
    }

    void SafeSetBool(int hash, bool v) { if (anim) anim.SetBool(hash, v); }
    void SafeSetFloat(int hash, float v) { if (anim) anim.SetFloat(hash, v); }
    void SafeSetTrigger(int hash) { if (anim) anim.SetTrigger(hash); }
    void SafeResetTrigger(int hash) { if (anim) anim.ResetTrigger(hash); }

    void SetBoolIfExists(string[] names, bool v)
    {
        for (int i = 0; i < names.Length; i++)
            if (HasParam(names[i], AnimatorControllerParameterType.Bool)) anim.SetBool(names[i], v);
    }
    void SetFloatIfExists(string[] names, float v)
    {
        for (int i = 0; i < names.Length; i++)
            if (HasParam(names[i], AnimatorControllerParameterType.Float)) anim.SetFloat(names[i], v);
    }
    void SetTriggerIfExists(string[] names)
    {
        for (int i = 0; i < names.Length; i++)
            if (HasParam(names[i], AnimatorControllerParameterType.Trigger)) { anim.ResetTrigger(names[i]); anim.SetTrigger(names[i]); return; }
    }
    void ResetTriggerIfExists(string[] names)
    {
        for (int i = 0; i < names.Length; i++)
            if (HasParam(names[i], AnimatorControllerParameterType.Trigger)) anim.ResetTrigger(names[i]);
    }

    void SetSingleTrigger(string[] aliasList, string fallbackName)
    {
        for (int i = 0; i < aliasList.Length; i++)
        {
            string n = aliasList[i];
            if (HasParam(n, AnimatorControllerParameterType.Trigger))
            {
                anim.ResetTrigger(n);
                anim.SetTrigger(n);
                return;
            }
        }
        if (HasParam(fallbackName, AnimatorControllerParameterType.Trigger))
        {
            anim.ResetTrigger(fallbackName);
            anim.SetTrigger(fallbackName);
        }
    }

    bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (!anim) return false;
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].type == type && ps[i].name == name) return true;
        return false;
    }

    void LogParamPresence(string label, string[] names, AnimatorControllerParameterType type)
    {
        string found = "(none)";
        foreach (var n in names) if (HasParam(n, type)) { found = n; break; }
        Debug.Log($"[PlayerController] {label} -> {found}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var cc = GetComponent<Collider2D>();
        if (!cc) return;

        var b = cc.bounds;
        Vector2 l = new Vector2(b.center.x - feetOffsetX, b.min.y + 0.02f);
        Vector2 r = new Vector2(b.center.x + feetOffsetX, b.min.y + 0.02f);
        float rayLen = Mathf.Max(feetRayLen, 0.25f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(l, l + Vector2.down * rayLen);
        Gizmos.DrawLine(r, r + Vector2.down * rayLen);

        Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
        Gizmos.DrawLine(new Vector3(-1000f, killY, 0f), new Vector3(1000f, killY, 0f));
    }
#endif
}