using UnityEngine;

internal readonly struct WeaponDamageSnapshot
{
    private readonly float minimum;
    private readonly float maximum;

    internal bool HasRange { get; }

    internal float Average => minimum + (maximum - minimum) * 0.5f;

    internal WeaponFamilyDefinition WeaponFamily { get; }

    private WeaponDamageSnapshot(
        float first,
        float second,
        WeaponFamilyDefinition weaponFamily,
        bool hasRange = true)
    {
        minimum = Mathf.Max(0f, Mathf.Min(first, second));
        maximum = Mathf.Max(0f, Mathf.Max(first, second));
        WeaponFamily = weaponFamily;
        HasRange = hasRange;
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

        if (weapon == null)
            return default;

        WeaponFamilyDefinition family = weapon.WeaponFamily;

        if (!weapon.TryGetWeaponDamageRange(out Vector2Int range))
            return new WeaponDamageSnapshot(0f, 0f, family, hasRange: false);

        float minimum = caster.GetStatValueBreakdown(
            StatType.WeaponDamage, range.x, equipmentSource).FinalValue;

        float maximum = caster.GetStatValueBreakdown(
            StatType.WeaponDamage, range.y, equipmentSource).FinalValue;

        return new WeaponDamageSnapshot(minimum, maximum, family);
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
