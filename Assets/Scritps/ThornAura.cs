using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThornAura : MonoBehaviour
{
    public float damage = 5f;
    public float interval = 0.5f;
    public float duration = 3f;
    public float radius = 2f;
    public LayerMask targetLayer;
    public string targetTag = "Enemy";

    private float timer = 0f;
    private float lifeTimer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        lifeTimer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0f;
            ApplyDamage();
        }

        if (lifeTimer >= duration)
        {
            Destroy(gameObject);
        }
    }

    void ApplyDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag(targetTag))
            {
                Health health = hit.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage, transform.position);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
