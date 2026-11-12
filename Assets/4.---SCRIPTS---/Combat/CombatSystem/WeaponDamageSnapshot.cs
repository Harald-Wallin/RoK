using UnityEngine;

internal readonly struct WeaponDamageSnapshot
{
    private readonly float minimum;
    private readonly float maximum;

    internal bool HasRange { get; }

    internal float Average => minimum + (maximum - minimum) * 0.5f;

    private WeaponDamageSnapshot(float first, float second)
    {
        minimum = Mathf.Max(0f, Mathf.Min(first, second));
        maximum = Mathf.Max(0f, Mathf.Max(first, second));
        HasRange = true;
    }

    internal float Roll()
    {
        return minimum == maximum ? minimum : Random.Range(minimum, maximum);
    }

    internal static WeaponDamageSnapshot Capture(CharacterStats caster)
    {
        if (caster == null)
            return default;

        ItemData weapon;
        object equipmentSource = null;

        if (caster is PlayerStats player)
        {
            EquipmentManager equipment = EquipmentManager.Instance;

            EquipmentSlotUI slot = equipment != null
                ? equipment.GetEquippedWeaponSlot(player)
                : null;

            weapon = slot != null ? slot.GetEquippedItem() : null;
            equipmentSource = slot;
        }
        else
        {
            HumanoidEquipment equipment = caster.GetComponent<HumanoidEquipment>();
            weapon = equipment != null ? equipment.weapon : null;
        }

        if (weapon == null || !weapon.TryGetWeaponDamageRange(out Vector2Int range))
            return default;

        float minimum = caster.GetStatValueBreakdown(
            StatType.WeaponDamage, range.x, equipmentSource).FinalValue;

        float maximum = caster.GetStatValueBreakdown(
            StatType.WeaponDamage, range.y, equipmentSource).FinalValue;

        return new WeaponDamageSnapshot(minimum, maximum);
    }

    internal static float GetPreviewValue(CharacterStats caster)
    {
        if (caster == null)
            return 0f;

        WeaponDamageSnapshot snapshot = Capture(caster);

        return snapshot.HasRange
            ? snapshot.Average
            : caster.GetStat(StatType.WeaponDamage);
    }
}
