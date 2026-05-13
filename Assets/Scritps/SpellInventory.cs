using UnityEngine;

public class SpellInventory : MonoBehaviour
{
    public SpellData[] equippedSpells = new SpellData[3];

    public SpellData GetSpell(int index)
    {
        if (index >= 0 && index < equippedSpells.Length)
            return equippedSpells[index];
        return null;
    }
}
