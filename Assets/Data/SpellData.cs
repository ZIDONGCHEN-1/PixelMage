using UnityEngine;

public enum SpellType
{
    Instant,        // 瞬发
    Heal,           // 回血
    TargetPosition, // 指定位置
    Blink           // 闪现
}

[CreateAssetMenu(menuName = "Spell/SpellData")]
public class SpellData : ScriptableObject
{
    public string spellName;
    public SpellType type;
    public float cooldown;
    public GameObject effectPrefab;
    public float value;            // 例如回血值，伤害值，闪现距离等
}
