using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack, Evade }
    public State currentState = State.Patrol;

    [Header("目标与地形")]
    public Transform player;
    public LayerMask groundLayer;

    [Header("移动参数")]
    public float patrolSpeed = 2f;
    public float evadeSpeed = 5f;

    [Header("检测范围")]
    public float detectRange = 8f;
    public float attackRange = 6f;
    public float safeDistance = 2.5f;

    [Header("瞬移参数")]
    public float blinkOffset = 2f;

    [Header("后方检测点")]
    public Transform groundCheckBack;
    public Transform wallCheckBack;

    [Header("前方检测点")]
    public Transform groundCheckFront;
    public Transform wallCheckFront;

    [Header("瞬移目标点检测")]
    public Transform blinkGroundCheck;
    public Transform blinkWallCheck;

    [Header("冷却")]
    public float attackCooldown = 2f;
    public float lostTargetDelay = 2f;

    [SerializeField] private EnemyAnimator enemyAnimator;
    [SerializeField] private Health health;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;

    private Rigidbody2D rb;
    private float attackTimer = 0f;
    private float lostSightTimer = 0f;
    private bool hasAttacked = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();

        if (enemyAnimator != null)
        {
            enemyAnimator.onShoot = () =>
            {
                if (projectilePrefab != null && shootPoint != null)
                {
                    GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
                    float faceDir = Mathf.Sign(transform.localScale.x);
                    proj.GetComponent<Rigidbody2D>().velocity = new Vector2(faceDir, 0f) * 6f;

                    Vector3 scale = proj.transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * faceDir;
                    proj.transform.localScale = scale;

                    Destroy(proj, 5f);
                }
            };
        }
    }

    void Update()
    {
        if (health != null && health.IsDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (enemyAnimator != null && enemyAnimator.IsInHit())
            return;

        attackTimer += Time.deltaTime;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool playerInSight = distanceToPlayer < detectRange;

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                if (playerInSight)
                {
                    lostSightTimer = 0f;
                    currentState = State.Attack;
                }
                break;

            case State.Attack:
                FacePlayer();

                if (distanceToPlayer > attackRange)
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

                    if (distanceToPlayer < safeDistance)
                    {
                        if (IsBackBlocked())
                        {
                            BlinkBehindPlayer();
                        }
                        else
                        {
                            currentState = State.Evade;
                        }
                    }

                    if (!hasAttacked)
                    {
                        hasAttacked = true;
                        attackTimer = 0f;
                        PlayRangedAttack();
                    }
                    else if (attackTimer >= attackCooldown)
                    {
                        attackTimer = 0f;
                        PlayRangedAttack();
                    }
                }
                break;

            case State.Evade:
                if (IsBackBlocked())
                {
                    rb.velocity = Vector2.zero;
                    BlinkBehindPlayer();
                    currentState = State.Attack;
                    break;
                }

                EvadeBack();

                if (Vector2.Distance(transform.position, player.position) >= safeDistance * 1.2f)
                {
                    rb.velocity = Vector2.zero;
                    currentState = State.Attack;
                }
                break;
        }
    }

    void Patrol()
    {
        bool noGroundAhead = groundCheckFront != null && !Physics2D.OverlapCircle(groundCheckFront.position, 0.1f, groundLayer);
        bool wallAhead = wallCheckFront != null && Physics2D.OverlapCircle(wallCheckFront.position, 0.1f, groundLayer);

        if (noGroundAhead || wallAhead)
        {
            Flip();
        }

        rb.velocity = new Vector2(transform.localScale.x * patrolSpeed, rb.velocity.y);
    }

    void FacePlayer()
    {
        float direction = player.position.x - transform.position.x;
        transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
    }

    void PlayRangedAttack()
    {
        enemyAnimator?.PlayAttack();
    }

    void EvadeBack()
    {
        int dir = (int)-Mathf.Sign(transform.localScale.x);
        rb.velocity = new Vector2(dir * evadeSpeed, rb.velocity.y);
    }

    bool IsBackBlocked()
    {
        bool noGround = !Physics2D.OverlapCircle(blinkGroundCheck.position, 0.1f, groundLayer);
        bool hasWall = Physics2D.OverlapCircle(blinkWallCheck.position, 0.1f, groundLayer);
        return noGround || hasWall;
    }

    void BlinkBehindPlayer()
    {
        if (blinkGroundCheck == null || blinkWallCheck == null)
        {
            Debug.LogWarning("瞬移检测点未设置！");
            return;
        }

        bool hasGround = Physics2D.OverlapCircle(blinkGroundCheck.position, 0.1f, groundLayer);
        bool hitWall = Physics2D.OverlapCircle(blinkWallCheck.position, 0.1f, groundLayer);

        if (hasGround && !hitWall)
        {
            float faceDir = Mathf.Sign(transform.position.x - player.position.x);
            Vector3 blinkPos = new Vector3(player.position.x + faceDir * blinkOffset, transform.position.y, transform.position.z);
            transform.position = blinkPos;
            Debug.Log($"{gameObject.name} 瞬移到玩家背后！");
        }
        else
        {
            Debug.Log($"{gameObject.name} 瞬移失败：目标点不安全");
        }
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, safeDistance);

        Gizmos.color = Color.red;
        if (groundCheckBack) Gizmos.DrawWireSphere(groundCheckBack.position, 0.1f);
        if (wallCheckBack) Gizmos.DrawWireSphere(wallCheckBack.position, 0.1f);

        Gizmos.color = Color.green;
        if (blinkGroundCheck) Gizmos.DrawWireSphere(blinkGroundCheck.position, 0.1f);

        Gizmos.color = Color.magenta;
        if (blinkWallCheck) Gizmos.DrawWireSphere(blinkWallCheck.position, 0.1f);

        Gizmos.color = Color.blue;
        if (groundCheckFront) Gizmos.DrawWireSphere(groundCheckFront.position, 0.1f);
        if (wallCheckFront) Gizmos.DrawWireSphere(wallCheckFront.position, 0.1f);
    }
}
