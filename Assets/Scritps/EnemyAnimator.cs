using UnityEngine;
using System;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private EnemyAI enemyAI;
    private Rigidbody2D rb;
    private Health health;

    private bool isHit = false;
    private bool isKnockback = false;

    [Header("击退参数")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.1f;

    // 公共事件：供动画事件调用
    public Action onShoot;

    void Start()
    {
        animator = GetComponent<Animator>();
        enemyAI = GetComponentInParent<EnemyAI>();
        rb = GetComponentInParent<Rigidbody2D>();
        health = GetComponentInParent<Health>();

        if (health != null)
        {
            health.OnHit += PlayHit;
            health.OnDeath += PlayDeath;
            health.OnKnockback += ApplyKnockback;
        }
    }

    void Update()
    {
        bool isMoving = !isHit && !isKnockback && Mathf.Abs(rb.velocity.x) > 0.1f &&
                        (enemyAI == null || enemyAI.currentState != EnemyAI.State.Attack);
        animator.SetBool("isMoving", isMoving);
    }

    public void PlayAttack()
    {
        animator.SetTrigger("attack");
    }

    public void PlayHit()
    {
        if (!health.IsDead)
        {
            isHit = true;
            animator.SetTrigger("hit");
            Invoke(nameof(EndHit), 0.3f);
        }
    }

    void EndHit()
    {
        isHit = false;
    }

    public void PlayDeath()
    {
        animator.SetTrigger("dead");
    }

    public bool IsInHit()
    {
        return isHit || isKnockback;
    }

    void ApplyKnockback(Vector2 attackerPos)
    {
        if (isKnockback || health.IsDead) return;

        isKnockback = true;
        Vector2 dir = (rb.transform.position - (Vector3)attackerPos).normalized;
        Vector2 knockDir = new Vector2(Mathf.Sign(dir.x), 1f).normalized;

        rb.velocity = Vector2.zero;
        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        Invoke(nameof(EndKnockback), knockbackDuration);
    }

    void EndKnockback()
    {
        isKnockback = false;
    }

    // 动画事件调用此方法触发发射逻辑
    public void ShootProjectile()
    {
        onShoot?.Invoke();
    }
}
