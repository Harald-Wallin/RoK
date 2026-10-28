using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Ward Capacity"
)]
public class WardCapacityEffect : AbilityEffect
{
    public int wardsPerPoint = 2;

    public override void Apply(
        CharacterStats caster,
        CharacterStats target
    )
    {
    }
}
