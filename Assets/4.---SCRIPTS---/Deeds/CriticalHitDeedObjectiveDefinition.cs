using UnityEngine;

[CreateAssetMenu(menuName = "RöK/Deeds/Objectives/Critical Hit")]
public sealed class CriticalHitDeedObjectiveDefinition : DeedObjectiveDefinition
{
    [SerializeField]
    [Tooltip("Leave empty to count critical hits from any source used by the player.")]
    private WeaponFamilyDefinition weaponFamily;

    public override DeedObjectiveRuntime CreateRuntime()
    {
        return new Runtime(weaponFamily);
    }

    private sealed class Runtime : DeedObjectiveRuntime
    {
        private readonly WeaponFamilyDefinition weaponFamily;

        public Runtime(WeaponFamilyDefinition weaponFamily)
        {
            this.weaponFamily = weaponFamily;
        }

        public override void Receive(DeedProgressEvent progressEvent)
        {
            if (progressEvent is not DeedCriticalHitEvent hit)
                return;

            if (weaponFamily != null && hit.WeaponFamily != weaponFamily)
                return;

            AddProgress(1);
        }
    }
}
