using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }
    public State currentState = State.Patrol;

    [Header("通用参数")]
    public Transform player;
    public LayerMask groundLayer;

    [Header("移动参数")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3f;

    [Header("检测范围")]
    public float detectRange = 5f;
    public float attackRange = 1.2f;

    [Header("计时")]
    public float attackCooldown = 1.5f;
    public float lostTargetDelay = 2f;

    [Header("地形检测")]
    public Transform groundCheckLeft;
    public Transform groundCheckRight;
    public Transform wallCheck;

    [SerializeField] private EnemyAnimator enemyAnimator;
    [SerializeField] private Health health;

    private Rigidbody2D rb;
    private float attackTimer = 0f;
    private float lostSightTimer = 0f;
    private bool hasAttacked = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (health != null && health.IsDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // **修复点**：受击/击退时不干扰物理
        if (enemyAnimator != null && enemyAnimator.IsInHit())
        {
            return;
        }

        attackTimer += Time.deltaTime;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInSight = distanceToPlayer < detectRange;

        switch (currentState)
        {
            case State.Idle:
                rb.velocity = Vector2.zero;
                if (playerInSight)
                {
                    Vector2 dirToPlayer = player.position - transform.position;
                    float faceDir = transform.localScale.x;

                    if (Mathf.Sign(dirToPlayer.x) != Mathf.Sign(faceDir))
                        Flip();

                    if (!IsAtEdgeOrWall())
                    {
                        lostSightTimer = 0f;
                        currentState = State.Chase;
                    }
                }
                else
                {
                    lostSightTimer += Time.deltaTime;
                    if (lostSightTimer >= lostTargetDelay)
                    {
                        lostSightTimer = 0f;
                        currentState = State.Patrol;
                    }
                }
                break;

            case State.Patrol:
                Patrol();
                if (playerInSight)
                {
                    lostSightTimer = 0f;
                    currentState = State.Chase;
                }
                break;

            case State.Chase:
                if (IsAtEdgeOrWall())
                {
                    rb.velocity = Vector2.zero;
                    currentState = State.Idle;
                    lostSightTimer = 0f;
                    break;
                }

                Chase();

                if (distanceToPlayer <= attackRange)
                {
                    hasAttacked = false;
                    currentState = State.Attack;
                }
                else if (!playerInSight)
                {
                    lostSightTimer += Time.deltaTime;
                    if (lostSightTimer >= lostTargetDelay)
                    {
                        lostSightTimer = 0f;
                        currentState = State.Patrol;
                    }
                }
                else
                {
                    lostSightTimer = 0f;
                }
                break;

            case State.Attack:
                rb.velocity = Vector2.zero;
                if (distanceToPlayer > attackRange)
                {
                    currentState = State.Chase;
                }
                else if (!hasAttacked)
                {
                    hasAttacked = true;
                    attackTimer = 0f;
                    PerformAttack();
                }
                else if (attackTimer >= attackCooldown)
                {
                    attackTimer = 0f;
                    PerformAttack();
                }
                break;
        }
    }

    void Patrol()
    {
        Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        rb.velocity = new Vector2(dir.x * patrolSpeed, rb.velocity.y);
        if (IsAtEdgeOrWall()) Flip();
    }

    void Chase()
    {
        float direction = player.position.x - transform.position.x;
        transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
        rb.velocity = new Vector2((direction > 0 ? 1 : -1) * chaseSpeed, rb.velocity.y);
    }

    void PerformAttack()
    {
        Debug.Log($"{gameObject.name} 发动攻击！");
        enemyAnimator?.PlayAttack();
    }

    bool IsAtEdgeOrWall()
    {
        bool noGroundLeft = !Physics2D.OverlapCircle(groundCheckLeft.position, 0.1f, groundLayer);
        bool noGroundRight = !Physics2D.OverlapCircle(groundCheckRight.position, 0.1f, groundLayer);
        bool wallHit = Physics2D.OverlapCircle(wallCheck.position, 0.1f, groundLayer);

        if ((transform.localScale.x < 0 && noGroundLeft) || (transform.localScale.x > 0 && noGroundRight) || wallHit)
            return true;

        return false;
    }

    void Flip()
    {
        transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (groundCheckLeft) Gizmos.DrawWireSphere(groundCheckLeft.position, 0.1f);
        if (groundCheckRight) Gizmos.DrawWireSphere(groundCheckRight.position, 0.1f);
        if (wallCheck) Gizmos.DrawWireSphere(wallCheck.position, 0.1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
