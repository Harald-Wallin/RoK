using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Spawn VFX"
)]
public sealed class SpawnVFXEffect :
    AbilityEffect
{
    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private EffectSpawnPositionMode positionMode =
        EffectSpawnPositionMode.Target;

    [SerializeField]
    private Vector3 positionOffset;

    [SerializeField]
    private bool rotateWithActionDirection;

    [SerializeField]
    private bool parentToTarget;

    [SerializeField]
    [Min(0f)]
    private float lifetime = 2f;

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            prefab == null)
        {
            return;
        }

        Vector3 position =
            ResolvePosition(
                context
            ) +
            positionOffset;

        Quaternion rotation =
            ResolveRotation(
                context
            );

        Transform parent =
            ResolveParent(
                context
            );

        GameObject instance =
            Object.Instantiate(
                prefab,
                position,
                rotation,
                parent
            );

        if (instance == null)
            return;

        if (lifetime > 0f)
        {
            Object.Destroy(
                instance,
                lifetime
            );
        }
    }

    private Vector3 ResolvePosition(
        AbilityEffectExecutionContext context)
    {
        switch (positionMode)
        {
            case EffectSpawnPositionMode.Origin:
                return context.Origin;

            case EffectSpawnPositionMode.TargetPoint:
                return context.TargetPoint;

            case EffectSpawnPositionMode.Target:
                return GetCharacterEffectPosition(
                    context.Target
                );

            case EffectSpawnPositionMode.Caster:
                return GetCharacterEffectPosition(
                    context.Caster
                );

            case EffectSpawnPositionMode.PrimaryTarget:
                CharacterStats primary =
                    context.Action
                        .PrimaryCharacterTarget;

                return GetCharacterEffectPosition(
                    primary
                );

            default:
                return context.TargetPoint;
        }
    }

    private Quaternion ResolveRotation(
        AbilityEffectExecutionContext context)
    {
        if (!rotateWithActionDirection)
            return Quaternion.identity;

        Vector2 direction =
            context.Direction;

        if (direction.sqrMagnitude <= 0.0001f)
            return Quaternion.identity;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        return Quaternion.Euler(
            0f,
            0f,
            angle
        );
    }

    private Transform ResolveParent(
        AbilityEffectExecutionContext context)
    {
        if (!parentToTarget)
            return null;

        if (context.Target != null)
            return context.Target.transform;

        return null;
    }

    private static Vector3
        GetCharacterEffectPosition(
            CharacterStats character)
    {
        if (character == null)
            return Vector3.zero;

        if (character.effectPoint != null)
        {
            return character
                .effectPoint
                .position;
        }

        return character.transform.position;
    }
}
