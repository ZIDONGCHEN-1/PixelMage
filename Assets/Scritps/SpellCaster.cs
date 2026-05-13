using UnityEngine;
using UnityEngine.UI;

public class SpellCaster : MonoBehaviour
{
    public Transform castPoint;
    public Transform HealPoint;
    private PlayerAnimator animator;
    private SpellInventory inventory;
    private float[] cooldownTimers;

    public Image[] cooldownImages;


    void Start()
    {
        animator = GetComponentInChildren<PlayerAnimator>();
        inventory = GetComponent<SpellInventory>();
        cooldownTimers = new float[3];
    }

    void Update()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] > 0)
            {
                cooldownTimers[i] -= Time.deltaTime;
                cooldownTimers[i] = Mathf.Max(cooldownTimers[i], 0);
            }

            if (inventory.GetSpell(i) != null && cooldownImages[i] != null)
            {
                float cooldown = inventory.GetSpell(i).cooldown;
                cooldownImages[i].fillAmount = cooldown > 0 ? cooldownTimers[i] / cooldown : 0;
            }
        }


        if (Input.GetKeyDown(KeyCode.Q)) CastSpell(0);
        if (Input.GetKeyDown(KeyCode.E)) CastSpell(1);
        if (Input.GetKeyDown(KeyCode.R)) CastSpell(2);

        if(Input.GetKeyDown(KeyCode.F))
        {
            animator.PlayMeleeAttack();
        }
    }

     public void CastSpell(int index)
    {
        SpellData spell = inventory.GetSpell(index);
        if (spell == null) return;

        if (cooldownTimers[index] > 0f)
        {
            Debug.Log($"技能 {spell.spellName} 冷却中，还需 {cooldownTimers[index]:F1} 秒");
            return;
        }

        animator.PlayCastSpell();
        cooldownTimers[index] = spell.cooldown;

        animator.onCastSpellEffect = () =>
        {
            switch (spell.type)
            {
                case SpellType.Instant:
                    GameObject fireball = Instantiate(spell.effectPrefab, castPoint.position, Quaternion.identity);
                    Vector2 dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
                    FireBall fb = fireball.GetComponent<FireBall>();
                    if (fb != null) fb.SetDirection(dir);
                    break;

                case SpellType.Heal:
                    GetComponent<Health>()?.Heal(spell.value);
                    GameObject heal = Instantiate(spell.effectPrefab, HealPoint.position, Quaternion.identity , transform);
                    Destroy(heal, 4f);
                    break;

                case SpellType.TargetPosition:
                    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 10f);
                    Transform nearestEnemy = null;
                    float minDist = float.MaxValue;

                    foreach (var hit in hits)
                    {
                        if (hit.CompareTag("Enemy"))
                        {
                            float dist = Vector2.Distance(transform.position, hit.transform.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                nearestEnemy = hit.transform;
                            }
                        }
                    }

                    if (nearestEnemy != null)
                    {
                        Instantiate(spell.effectPrefab, nearestEnemy.position, Quaternion.identity);
                    }
                    else
                    {
                        Debug.Log("没有找到敌人，技能释放失败");
                    }
                    break;

                case SpellType.Blink:
                    Vector3 blinkDir = transform.localScale.x > 0 ? Vector3.right : Vector3.left;
                    transform.position += blinkDir * spell.value;
                    break;
            }

            animator.onCastSpellEffect = null;
        };
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = 10f;
        return Camera.main.ScreenToWorldPoint(mouseScreen);
    }
}
