using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    public float damage = 10f;
    public float range = 1f;
    public Vector2 offset = Vector2.zero;
    public string[] targetTags = { "Player" };
    public LayerMask targetLayer;

    public void PerformAttack()
    {
        Vector2 scaleDir = transform.lossyScale;
        Vector2 flippedOffset = new Vector2(offset.x * Mathf.Sign(scaleDir.x), offset.y);
        Vector2 origin = (Vector2)transform.position + flippedOffset;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, targetLayer);

        foreach (var hit in hits)
        {
            foreach (var tag in targetTags)
            {
                if (hit.CompareTag(tag))
                {
                    Health health = hit.GetComponent<Health>();
                    if (health != null)
                    {
                        health.TakeDamage(damage, transform.position);
                        Debug.Log($"命中 {hit.name}，造成 {damage} 点伤害");
                        return;
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 scaleDir = transform.lossyScale;
        Vector2 flippedOffset = new Vector2(offset.x * Mathf.Sign(scaleDir.x), offset.y);
        Vector2 origin = (Vector2)transform.position + flippedOffset;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, range);
    }
}
