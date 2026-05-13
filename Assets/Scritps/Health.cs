using UnityEngine;
using System;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public bool IsDead => currentHealth <= 0;

    [Header("闪红效果")]
    public float flashDuration = 0.1f;
    private SpriteRenderer[] spriteRenderers;
    private Coroutine flashCoroutine;

    // 事件
    public Action OnHit;
    public Action OnDeath;
    public Action<Vector2> OnKnockback; // 击退来源方向

    public Image HealthUi;

    public bool isPlayer = false;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if(HealthUi != null)
        {
            HealthUi.fillAmount = currentHealth / maxHealth;
        }
    }

    // 新版：支持传入攻击者位置
    public void TakeDamage(float amount, Vector2 attackerPosition)
    {
        if (IsDead) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} 受到 {amount} 点伤害，剩余血量：{currentHealth}");

        FlashRed();
        OnHit?.Invoke();
        OnKnockback?.Invoke(attackerPosition);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{gameObject.name} 回复 {amount} 点生命，当前血量：{currentHealth}");
    }

    private void Die()
    {
        if (!isPlayer)
        {
            Debug.Log($"{gameObject.name} 死亡");
            OnDeath?.Invoke();
            Destroy(gameObject, 3f);
        }
        else
        {
            GetComponent<PlayerController>().LoseUi.SetActive(true);
            GetComponent<Rigidbody2D>().isKinematic = true;
            GetComponent<Collider2D>().enabled = false;
        }
    }

    private void FlashRed()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        foreach (var sr in spriteRenderers)
            sr.color = Color.red;

        yield return new WaitForSeconds(flashDuration);

        foreach (var sr in spriteRenderers)
            sr.color = Color.white;

        flashCoroutine = null;
    }
}
