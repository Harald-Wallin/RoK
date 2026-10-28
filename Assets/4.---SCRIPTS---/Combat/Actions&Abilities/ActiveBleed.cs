using UnityEngine;

public sealed class ActiveBleed :
    ActiveBuff
{
    private readonly BleedEffect effect;

    private readonly DamageSourceContext
        damageSource;

    private float tickTimer;

    public ActiveBleed(
        BleedEffect effect,
        DamageSourceContext damageSource)
    {
        this.effect = effect;
        this.damageSource = damageSource;

        sourceEffect = effect;
        duration = effect.duration;
    }

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        elapsed += deltaTime;
        tickTimer += deltaTime;

        while (tickTimer >=
               effect.tickInterval)
        {
            tickTimer -=
                effect.tickInterval;

            if (target == null ||
                target.currentHP <= 0)
            {
                return;
            }

            Vector3 position =
                target.effectPoint != null
                    ? target.effectPoint.position
                    : target.transform.position;

            BleedVFXSpawner.Instance?.Spawn(
                position
            );

            CombatResolver.DealRawDamage(
                damageSource,
                target,
                effect.damagePerTick
            );
        }
    }
}