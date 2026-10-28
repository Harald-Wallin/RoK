using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BaseAttackBarUI :
    MonoBehaviour
{
    [Header("Slots")]

    [SerializeField]
    private BaseAttackSlotUI
        primarySlot;

    [SerializeField]
    private BaseAttackSlotUI
        secondarySlot;

    [Header("Controls")]

    [SerializeField]
    private Button swapButton;

    private PlayerAbilityCollection
        collection;

    private CharacterActionController
        actionController;

    private IEnumerator Start()
    {
        yield return null;

        ResolveReferences();

        if (collection == null ||
            actionController == null)
        {
            Debug.LogWarning(
                $"{nameof(BaseAttackBarUI)} kunde inte hitta " +
                $"{nameof(PlayerAbilityCollection)} eller " +
                $"{nameof(CharacterActionController)}.",
                this
            );

            yield break;
        }

        primarySlot?.Initialize(
            collection,
            actionController,
            PlayerAbilityCollection
                .PrimaryBaseAttackSlotIndex
        );

        secondarySlot?.Initialize(
            collection,
            actionController,
            PlayerAbilityCollection
                .SecondaryBaseAttackSlotIndex
        );

        ConfigureSwapButton();
    }

    private void OnDestroy()
    {
        if (swapButton == null)
            return;

        swapButton.onClick
            .RemoveListener(
                HandleSwapClicked
            );
    }

    private void ResolveReferences()
    {
        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        collection =
            player.GetComponent<
                PlayerAbilityCollection>();

        actionController =
            player.GetComponent<
                CharacterActionController>();
    }

    private void ConfigureSwapButton()
    {
        if (swapButton == null)
            return;

        swapButton.onClick
            .RemoveListener(
                HandleSwapClicked
            );

        swapButton.onClick
            .AddListener(
                HandleSwapClicked
            );
    }

    private void HandleSwapClicked()
    {
        collection?.SwapBaseAttacks();
    }
}