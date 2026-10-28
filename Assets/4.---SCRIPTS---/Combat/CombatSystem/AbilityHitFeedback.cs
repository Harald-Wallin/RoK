/// <summary>
/// Presentation av misslyckade ability-träffar.
///
/// Denna klass innehåller ingen hitlogik. Den presenterar endast
/// ett redan avgjort AbilityTargetHitResult.
/// </summary>
public static class AbilityHitFeedback
{
    public static void Display(
        CharacterStats caster,
        AbilityTargetHitResult result)
    {
        if (result == null ||
            result.Target == null ||
            result.WasSuccessful)
        {
            return;
        }

        DamageResult damageResult =
            new DamageResult
            {
                damage = 0,
                isMiss = result.IsMiss,
                isEvaded = result.IsEvaded,
                isCrit = false,
                isBlocked = false,
                blockedAmount = 0
            };

        result.Target.TakeDamage(
            damageResult,
            caster
        );
    }
}
