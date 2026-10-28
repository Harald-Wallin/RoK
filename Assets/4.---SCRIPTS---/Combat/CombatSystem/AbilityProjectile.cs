using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generell runtime-projektil för actionsystemet.
///
/// Projektilens ansvar är endast:
/// - förflyttning
/// - collision
/// - välja sitt impacttarget
/// - initiera en enda impact-resolution
///
/// Den känner inte till damage, bleed, healing eller andra
/// konkreta gameplayeffekter.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class AbilityProjectile :
    MonoBehaviour
{
    [Header("Movement")]

    [SerializeField]
    [Min(0.01f)]
    private float speed = 8f;

    [SerializeField]
    [Min(0.1f)]
    private float maximumLifetime = 10f;

    [SerializeField]
    [Min(0.001f)]
    private float arrivalDistance = 0.05f;

    [Header("Collision")]

    [SerializeField]
    private LayerMask environmentCollisionLayers;

    [SerializeField]
    private bool destroyOnEnvironmentCollision = true;

    private Rigidbody2D body;
    private Collider2D ownCollider;

    private ActionExecutionContext action;

    private AbilityProjectileHitMode hitMode;

    private IReadOnlyList<AbilityEffect>
        impactEffects;

    private GameObject intendedTargetObject;
    private CharacterStats intendedTarget;

    private Vector2 destination;
    private Vector2 direction;

    private float lifetime;

    private bool isHoming;
    private bool initialized;
    private bool hasImpacted;

    private void Awake()
    {
        body =
            GetComponent<Rigidbody2D>();

        ownCollider =
            GetComponent<Collider2D>();

        body.gravityScale = 0f;
        body.freezeRotation = true;

        ownCollider.isTrigger = true;
    }

    public void Initialize(
        ActionExecutionContext executionContext,
        AbilityProjectileHitMode projectileHitMode,
        IReadOnlyList<AbilityEffect> effects,
        float movementSpeed,
        float lifetimeDuration,
        bool homing)
    {
        action =
            executionContext;

        hitMode =
            projectileHitMode;

        impactEffects =
            effects;

        speed =
            Mathf.Max(
                0.01f,
                movementSpeed
            );

        maximumLifetime =
            Mathf.Max(
                0.1f,
                lifetimeDuration
            );

        isHoming =
            homing &&
            hitMode ==
                AbilityProjectileHitMode
                    .IntendedTargetOnly;

        intendedTargetObject =
            action?.PrimaryTarget;

        intendedTarget =
            action?.PrimaryCharacterTarget;

        destination =
            ResolveInitialDestination();

        direction =
            GetDirectionTo(
                destination
            );

        RotateToDirection();

        initialized =
            action != null;
    }

    private void FixedUpdate()
    {
        if (!initialized ||
            hasImpacted)
        {
            return;
        }

        lifetime +=
            Time.fixedDeltaTime;

        if (lifetime >=
            maximumLifetime)
        {
            DestroyProjectile();
            return;
        }

        if (!TryUpdateDestination())
        {
            DestroyProjectile();
            return;
        }

        MoveProjectile();
    }

    private bool TryUpdateDestination()
    {
        if (!isHoming)
            return true;

        if (intendedTarget == null ||
            intendedTargetObject == null)
        {
            return false;
        }

        destination =
            TargetUtility.GetTargetPosition(
                intendedTargetObject
            );

        direction =
            GetDirectionTo(
                destination
            );

        RotateToDirection();

        return true;
    }

    private void MoveProjectile()
    {
        Vector2 currentPosition =
            body.position;

        float step =
            speed *
            Time.fixedDeltaTime;

        Vector2 nextPosition =
            Vector2.MoveTowards(
                currentPosition,
                destination,
                step
            );

        Vector2 movement =
            nextPosition -
            currentPosition;

        if (movement.sqrMagnitude >
            0.0001f)
        {
            direction =
                movement.normalized;

            RotateToDirection();
        }

        body.MovePosition(
            nextPosition
        );

        if (!ShouldImpactAtDestination())
            return;

        if (Vector2.Distance(
                nextPosition,
                destination) >
            arrivalDistance)
        {
            return;
        }

        HandleDestinationReached();
    }

    private bool ShouldImpactAtDestination()
    {
        return
            hitMode ==
                AbilityProjectileHitMode
                    .TargetPoint;
    }

    private void HandleDestinationReached()
    {
        Impact(
            null,
            null
        );
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!initialized ||
            hasImpacted ||
            other == null ||
            other == ownCollider)
        {
            return;
        }

        if (BelongsToCaster(other))
            return;

        CharacterStats character =
            TargetUtility.GetCharacterStats(
                other
            );

        if (character != null)
        {
            HandleCharacterCollision(
                character
            );

            return;
        }

        HandleEnvironmentCollision(
            other
        );
    }

    private bool BelongsToCaster(
        Collider2D other)
    {
        if (action?.Caster == null ||
            other == null)
        {
            return false;
        }

        CharacterStats owner =
            TargetUtility.GetCharacterStats(
                other
            );

        if (owner != null)
        {
            return owner ==
                action.Caster;
        }

        return
            other.transform.IsChildOf(
                action.Caster.transform
            );
    }

    private void HandleCharacterCollision(
        CharacterStats target)
    {
        if (target == null ||
            target == action.Caster)
        {
            return;
        }

        switch (hitMode)
        {
            case AbilityProjectileHitMode
                .IntendedTargetOnly:

                if (target != intendedTarget)
                    return;

                Impact(
                    target.gameObject,
                    target
                );
                break;

            case AbilityProjectileHitMode
                .FirstValidCharacter:

                if (!CanHitCharacter(
                        target))
                {
                    return;
                }

                Impact(
                    target.gameObject,
                    target
                );
                break;

            case AbilityProjectileHitMode
                .TargetPoint:

                /*
                 * TargetPoint-projektiler påverkas inte av
                 * character-collisions på vägen.
                 */
                break;
        }
    }

    private bool CanHitCharacter(
        CharacterStats target)
    {
        if (target == null ||
            action == null ||
            action.Caster == null ||
            target == action.Caster)
        {
            return false;
        }

        AbilityTargetingSettings settings =
            action.TargetingSettings;

        if (settings == null)
            return true;

        TargetRelation relation = TargetUtility.GetRelation(
        action.Caster,
        target
    );

        return
            settings.AllowsRelation(
                relation
            );
    }

    private void HandleEnvironmentCollision(
        Collider2D other)
    {
        if (!destroyOnEnvironmentCollision ||
            other == null)
        {
            return;
        }

        int layerBit =
            1 << other.gameObject.layer;

        if ((environmentCollisionLayers.value &
             layerBit) == 0)
        {
            return;
        }

        DestroyProjectile();
    }

    private void Impact(
        GameObject targetObject,
        CharacterStats target)
    {
        if (hasImpacted)
            return;

        hasImpacted = true;

        AbilityTargetHitResult targetResult =
            null;

        if (target != null)
        {
            targetResult =
                AbilityHitResolver.ResolveTarget(
                    action,
                    targetObject,
                    target,
                    0,
                    true
                );

            AbilityHitFeedback.Display(
                action.Caster,
                targetResult
            );

            CombatActivityUtility.Notify(
                action,
                targetResult
            );
        }

        IReadOnlyList<AbilityTargetHitResult>
            targetResults =
                targetResult != null
                    ? new[]
                    {
                        targetResult
                    }
                    : System.Array.Empty<
                        AbilityTargetHitResult
                    >();

        AbilityEffectCollectionExecutor.Execute(
            impactEffects,
            action,
            targetResults,
            AbilityEffectExecutionTiming.Deferred
        );

        DestroyProjectile();
    }

    private Vector2 ResolveInitialDestination()
    {
        if (action == null)
            return transform.position;

        if (hitMode ==
                AbilityProjectileHitMode
                    .IntendedTargetOnly &&
            intendedTargetObject != null)
        {
            return
                TargetUtility.GetTargetPosition(
                    intendedTargetObject
                );
        }

        return action.TargetPoint;
    }

    private Vector2 GetDirectionTo(
        Vector2 targetPosition)
    {
        Vector2 resolvedDirection =
            targetPosition -
            body.position;

        if (resolvedDirection.sqrMagnitude <=
            0.0001f)
        {
            resolvedDirection =
                action != null
                    ? action.Direction
                    : Vector2.down;
        }

        return resolvedDirection.normalized;
    }

    private void RotateToDirection()
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    private void DestroyProjectile()
    {
        if (gameObject != null)
        {
            Destroy(
                gameObject
            );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        speed =
            Mathf.Max(
                0.01f,
                speed
            );

        maximumLifetime =
            Mathf.Max(
                0.1f,
                maximumLifetime
            );

        arrivalDistance =
            Mathf.Max(
                0.001f,
                arrivalDistance
            );
    }
#endif
}