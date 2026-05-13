using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    [Header("Knockback")]
    public float knockbackForce = 6f;
    public float knockbackDuration = 0.2f;

    [Header("Dash")]
    public float shortDashSpeed = 14f;
    public float shortDashDuration = 0.15f;

    public float chargedDashSpeed = 22f;
    public float chargedDashDuration = 0.22f;

    public float dashCooldown = 0.8f;

    [Tooltip("按住多久后，松开会触发蓄力冲刺；不足这个时间则是短按冲刺")]
    public float chargeThreshold = 0.35f;

    [Tooltip("蓄力时全局时间缩放，比如 0.2 就是 20% 速度")]
    public float chargeSlowTimeScale = 0.2f;

    [Tooltip("为了避免物理太卡，慢动作时同步调低 fixedDeltaTime")]
    public float baseFixedDeltaTime = 0.02f;

    [Header("Mobile Control")]
    public VirtualJoystick moveJoystick;   // 手机摇杆
    public bool useJoystickPriority = true; // 摇杆有输入时优先使用摇杆

    [Header("Animation")]
    public Animator animator;

    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;
    private float verticalInput;

    public bool isKnockback = false;
    public bool canMove = true;

    private bool isDashing = false;
    public bool isChargingDash = false;
    private bool dashOnCooldown = false;

    private float dashHoldTimer = 0f;
    private Vector2 lastFacingDir = Vector2.right;
    private float originalGravityScale;

    public bool IsGrounded => isGrounded;
    public float MoveInput => moveInput;
    public Vector2 Velocity => rb.velocity;

    public SpriteRenderer PlayerMat;

    public GameObject WinUi;
    public GameObject LoseUi;

    // ====== 新增：兼容手机 UI 按钮 ======
    private bool jumpRequest = false;
    private bool dashButtonHeld = false;
    private bool dashButtonDown = false;
    private bool dashButtonUp = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;

        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.OnKnockback += ApplyKnockback;
        }

        if (baseFixedDeltaTime <= 0f)
            baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    void OnDestroy()
    {
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.OnKnockback -= ApplyKnockback;
        }

        ResetTimeScale();
    }

    void Update()
    {
        if (transform.position.y < -20f)
        {
            if (LoseUi != null && !LoseUi.activeSelf)
            {
                LoseUi.SetActive(true);
                canMove = false;
            }
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        HandleInput();
        HandleJump();
        HandleDashInput();
        Flip();
        UpdateAnimator();

        // 一帧结束清掉“单次触发”按钮
        dashButtonDown = false;
        dashButtonUp = false;
    }

    void FixedUpdate()
    {
        if (!isKnockback && !isDashing)
        {
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        }
    }

    void HandleInput()
    {
        if (!canMove || isKnockback || isDashing || isChargingDash)
        {
            moveInput = 0f;
            verticalInput = 0f;
            return;
        }

        // 键盘输入
        float keyboardH = Input.GetAxisRaw("Horizontal");
        float keyboardV = Input.GetAxisRaw("Vertical");

        // 摇杆输入
        float joystickH = 0f;
        float joystickV = 0f;

        if (moveJoystick != null)
        {
            joystickH = moveJoystick.Horizontal;
            joystickV = moveJoystick.Vertical;
        }

        // 输入合并策略
        bool joystickActive = moveJoystick != null && moveJoystick.HasInput;

        if (useJoystickPriority && joystickActive)
        {
            moveInput = joystickH;
            verticalInput = joystickV;
        }
        else
        {
            // 没摇杆输入就走键盘；如果你想两者混合，这里也能改
            moveInput = Mathf.Abs(keyboardH) > 0.01f ? keyboardH : joystickH;
            verticalInput = Mathf.Abs(keyboardV) > 0.01f ? keyboardV : joystickV;
        }

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            lastFacingDir = new Vector2(Mathf.Sign(moveInput), 0f);
        }
    }

    void HandleJump()
    {
        bool keyboardJump = Input.GetButtonDown("Jump");

        if (isGrounded && (keyboardJump || jumpRequest) && canMove && !isKnockback && !isDashing && !isChargingDash)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        jumpRequest = false;
    }

    void HandleDashInput()
    {
        bool keyboardDashHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool keyboardDashDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        bool keyboardDashUp = Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift);

        bool dashHeld = keyboardDashHeld || dashButtonHeld;
        bool dashDown = keyboardDashDown || dashButtonDown;
        bool dashUp = keyboardDashUp || dashButtonUp;

        // 1. 已经在蓄力中
        if (isChargingDash)
        {
            if (dashHeld)
            {
                dashHoldTimer += Time.unscaledDeltaTime;
            }

            if (dashUp)
            {
                ReleaseDash();
                Debug.Log("释放冲刺");
            }

            return;
        }

        // 2. 不在蓄力状态时，再判断能不能开始冲刺
        if (!canMove || isKnockback || isDashing || dashOnCooldown)
        {
            return;
        }

        // 3. 按下冲刺，开始蓄力
        if (dashDown)
        {
            StartDashCharge();
        }
    }

    void StartDashCharge()
    {
        isChargingDash = true;
        dashHoldTimer = 0f;
        canMove = false;
        rb.velocity = Vector2.zero;

        SetSlowMotion(true);

        if (animator != null)
        {
            animator.SetBool("IsChargingDash", true);
        }
    }

    void ReleaseDash()
    {
        isChargingDash = false;
        canMove = true;

        SetSlowMotion(false);

        if (animator != null)
        {
            animator.SetBool("IsChargingDash", false);
        }

        Vector2 dashDir = GetDashDirection();
        bool useChargedDash = dashHoldTimer >= chargeThreshold;

        if (useChargedDash)
        {
            StartCoroutine(DashCoroutine(dashDir, chargedDashSpeed, chargedDashDuration, true));
        }
        else
        {
            StartCoroutine(DashCoroutine(dashDir, shortDashSpeed, shortDashDuration, false));
        }
    }

    Vector2 GetDashDirection()
    {
        // 键盘方向
        float keyboardH = Input.GetAxisRaw("Horizontal");
        float keyboardV = Input.GetAxisRaw("Vertical");

        // 摇杆方向
        float joystickH = 0f;
        float joystickV = 0f;
        bool joystickActive = false;

        if (moveJoystick != null)
        {
            joystickH = moveJoystick.Horizontal;
            joystickV = moveJoystick.Vertical;
            joystickActive = moveJoystick.HasInput;
        }

        float h;
        float v;

        if (useJoystickPriority && joystickActive)
        {
            h = joystickH;
            v = joystickV;
        }
        else
        {
            h = Mathf.Abs(keyboardH) > 0.01f ? keyboardH : joystickH;
            v = Mathf.Abs(keyboardV) > 0.01f ? keyboardV : joystickV;
        }

        Vector2 inputDir = new Vector2(h, v);

        // 没按方向时给默认方向
        if (inputDir.sqrMagnitude < 0.001f)
        {
            inputDir = lastFacingDir;
        }

        if (isGrounded)
        {
            // 地面限制方向：左、右、上、左上、右上
            if (inputDir.y < 0)
                inputDir.y = 0;

            if (inputDir.sqrMagnitude < 0.001f)
            {
                inputDir = lastFacingDir;
            }
        }

        inputDir.Normalize();

        if (inputDir == Vector2.zero)
        {
            inputDir = lastFacingDir.normalized;
        }

        return inputDir;
    }

    IEnumerator DashCoroutine(Vector2 dashDir, float dashSpeed, float dashDuration, bool charged)
    {
        isDashing = true;
        dashOnCooldown = true;
        canMove = false;

        float savedGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        if (dashDir.x != 0)
        {
            lastFacingDir = new Vector2(Mathf.Sign(dashDir.x), 0f);
        }

        if (animator != null)
        {
            animator.SetTrigger(charged ? "ChargedDash" : "ShortDash");
            animator.SetBool("IsDashing", true);
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.velocity = dashDir * dashSpeed;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.gravityScale = savedGravity;
        rb.velocity = Vector2.zero;

        isDashing = false;
        canMove = true;

        if (animator != null)
        {
            animator.SetBool("IsDashing", false);
        }

        yield return new WaitForSeconds(dashCooldown);
        dashOnCooldown = false;
    }

    void SetSlowMotion(bool enable)
    {
        if (enable)
        {
            Time.timeScale = chargeSlowTimeScale;
            Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
        }
        else
        {
            ResetTimeScale();
        }
    }

    void ResetTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
    }

    void Flip()
    {
        if (isDashing || isChargingDash) return;

        if (moveInput > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", rb.velocity.y);
    }

    void ApplyKnockback(Vector2 attackerPos)
    {
        if (isKnockback) return;

        // 被击退时，打断蓄力和慢动作
        if (isChargingDash)
        {
            isChargingDash = false;
            ResetTimeScale();

            if (animator != null)
            {
                animator.SetBool("IsChargingDash", false);
            }
        }

        isKnockback = true;
        canMove = false;

        Vector2 dir = (transform.position - (Vector3)attackerPos).normalized;
        Vector2 knockDir = new Vector2(Mathf.Sign(dir.x), 1f).normalized;

        rb.velocity = Vector2.zero;
        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        Invoke(nameof(EndKnockback), knockbackDuration);
    }

    void EndKnockback()
    {
        isKnockback = false;
        canMove = true;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    public void RestartGame()
    {
        ResetTimeScale();
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void MainMenu()
    {
        ResetTimeScale();
        SceneManager.LoadScene(0);
    }

    // =========================
    // 给手机 UI 按钮绑定的方法
    // =========================

    // 跳跃按钮 OnClick 绑定这个
    public void MobileJump()
    {
        jumpRequest = true;
    }

    // 冲刺按钮按下（EventTrigger PointerDown）
    public void MobileDashDown()
    {
        dashButtonHeld = true;
        dashButtonDown = true;
    }

    // 冲刺按钮松开（EventTrigger PointerUp）
    public void MobileDashUp()
    {
        dashButtonHeld = false;
        dashButtonUp = true;
    }
}