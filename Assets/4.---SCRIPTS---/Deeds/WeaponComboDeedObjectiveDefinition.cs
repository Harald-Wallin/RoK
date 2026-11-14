using UnityEngine;

[CreateAssetMenu(menuName = "RöK/Deeds/Objectives/Weapon Combo Hit")]
public sealed class WeaponComboDeedObjectiveDefinition : DeedObjectiveDefinition
{
    [SerializeField] private WeaponFamilyDefinition weaponFamily;

    [SerializeField, Min(1)]
    private int requiredHitNumber = 3;

    public override DeedObjectiveRuntime CreateRuntime()
    {
        return new Runtime(weaponFamily, Mathf.Max(1, requiredHitNumber));
    }

    private sealed class Runtime : DeedObjectiveRuntime
    {
        private readonly WeaponFamilyDefinition weaponFamily;
        private readonly int requiredHitNumber;

        public Runtime(WeaponFamilyDefinition weaponFamily, int requiredHitNumber)
        {
            this.weaponFamily = weaponFamily;
            this.requiredHitNumber = requiredHitNumber;
        }

        public override void Receive(DeedProgressEvent progressEvent)
        {
            if (progressEvent is DeedWeaponComboHitEvent hit &&
                weaponFamily != null &&
                hit.WeaponFamily == weaponFamily &&
                hit.LandedHits == requiredHitNumber)
            {
                AddProgress(1);
            }
        }
    }
}
