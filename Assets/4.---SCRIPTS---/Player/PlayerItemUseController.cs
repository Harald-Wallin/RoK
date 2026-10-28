using UnityEngine;

/// <summary>
/// Central ingång för generella spelarinitierade item-use actions.
///
/// UI:t frågar controllern om itemet har en use-action som ska
/// ta prioritet över traditionella fallback-actions såsom equip.
///
/// Vendor/sell är däremot UI-context och hanteras därför innan
/// denna controller anropas.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerItemUseController :
    MonoBehaviour
{
    public bool TryUseItem(
        ItemData item)
    {
        if (item == null)
            return false;

        /*
         * ---------------------------------------------------------
         * FAVOUR INTERACTION
         * ---------------------------------------------------------
         *
         * Favour-interaktion är en item-capability och är helt
         * separat från ItemType.
         *
         * Om itemet har denna capability konsumerar den
         * högerklicket. Ett equippable item med Favour-interaktion
         * auto-equippas därför inte via samma högerklick.
         */
        if (item.HasFavourInteraction)
        {
            FavourWindow window =
                FavourWindow.Instance;

            if (window == null)
            {
                Debug.LogWarning(
                    $"Kan inte använda Favour-interaktionen på " +
                    $"'{item.itemName}': FavourWindow saknas.",
                    this
                );

                return true;
            }

            window.Open(
                item
            );

            return true;
        }

        return false;
    }
}