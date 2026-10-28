using UnityEngine;
public class GuardAI : HumanoidAI
{
    protected override void TriggerCombatPhrase()
    {
        // Guard-specifika fraser
        //Debug.Log($"{name}: For the city!");
    }

    public void ForceAggro(PlayerStats playerStats)
    {
        // Use base implementation to ensure proper subscription to player death and shared logic
        base.ForceAggro(playerStats);

        // Guard specific combat phrase
        TriggerCombatPhrase();
    }
}

