using System.Collections;
using UnityEngine;

public sealed class ActionBarUI :
    MonoBehaviour
{
    [SerializeField]
    private ActionSlot[] slots;

    private PlayerAbilityCollection
        collection;

    private IEnumerator Start()
    {
        yield return null;

        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            yield break;

        collection =
            player.GetComponent<
                PlayerAbilityCollection>();

        if (collection == null)
        {
            Debug.LogWarning(
                $"{nameof(ActionBarUI)} kunde inte hitta " +
                $"{nameof(PlayerAbilityCollection)}.",
                this
            );

            yield break;
        }

        AbilityData[] abilities =
            collection
                .GetEquippedAbilities();

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            ActionSlot slot =
                slots[i];

            if (slot == null)
                continue;

            AbilityData ability =
                abilities != null &&
                i < abilities.Length
                    ? abilities[i]
                    : null;

            slot.Initialize(
                collection,
                ability,
                i
            );
        }
    }
}