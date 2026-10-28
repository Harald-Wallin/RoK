/*using UnityEngine;

public class HumanoidAI : NPCBehavior
{
    [Header("Assist Settings")]
    public float assistRadius = 10f;

    [Header("Guard Settings")]
    [SerializeField] private bool useReputationAggro = false;
    [SerializeField] private float customLeashDistance = 20f;

    protected override void Start()
    {
        base.Start();

        if (useReputationAggro)
            maxDistanceFromSpawn = customLeashDistance;
    }

    protected override void HandleDamaged(
     CharacterStats attacker)
    {
        base.HandleDamaged(
            attacker
        );

        if (!useReputationAggro)
            return;

        if (attacker is not PlayerStats playerStats)
            return;

        if (selfStats == null ||
            selfStats.faction == null)
        {
            return;
        }

        ForceAggro(
            playerStats
        );

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                assistRadius
            );

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider2D hit =
                hits[i];

            if (hit == null)
                continue;

            GuardAI guard =
                hit.GetComponentInParent<
                    GuardAI>();

            if (guard == null ||
                guard == this ||
                guard.selfStats == null)
            {
                continue;
            }

            if (guard.selfStats.faction !=
                selfStats.faction)
            {
                continue;
            }

            guard.ForceAggro(
                playerStats
            );
        }
    }

    protected virtual void TriggerCombatPhrase()
    {
    }
}*/
using UnityEngine;

/// <summary>
/// Bas för humanoid-specifik AI.
///
/// All generell NPC-logik ligger i NPCBehavior och
/// NPCReactionController.
///
/// Lägg endast funktionalitet här som verkligen ska skilja
/// humanoider från djur, monster och andra NPC-typer.
/// </summary>
public class HumanoidAI :
    NPCBehavior
{
    protected virtual void TriggerCombatPhrase()
    {
    }
}