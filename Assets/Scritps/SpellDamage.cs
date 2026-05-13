using UnityEngine;

public class SpellDamage : MonoBehaviour
{
    public float damage = 10f;
    public bool destroyOnHit = true;
    public string[] targetTags = { "Enemy" }; // 支持多种目标类型

    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查目标标签
        bool isValidTarget = false;
        foreach (var tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                isValidTarget = true;
                break;
            }
        }

        if (!isValidTarget) return;

        // 查找 Health 组件并造成伤害
        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage,transform.position);

            if (destroyOnHit)
                Destroy(gameObject);
        }
    }
}
