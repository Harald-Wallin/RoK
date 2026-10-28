using UnityEngine;

/// <summary>
/// Presentation-only bridge mellan Favour-systemet
/// och det generella Notification-systemet.
///
/// Objective-typerna känner inte till UI.
/// Notification-systemet känner inte till objective-typer.
/// </summary>
public sealed class FavourNotificationBridge :
    MonoBehaviour
{
    private PlayerFavourManager
        favourManager;

    private bool subscribed;

    private void Update()
    {
        /*
         * Behöver bara leta tills spelaren och dess
         * FavourManager finns.
         */
        if (!subscribed)
        {
            TrySubscribe();
        }
    }

    private void TrySubscribe()
    {
        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        favourManager =
            player.GetComponent<
                PlayerFavourManager>();

        if (favourManager == null)
            return;

        favourManager
            .FavourObjectiveProgressChanged +=
            HandleObjectiveProgressChanged;

        subscribed =
            true;
    }

    private void HandleObjectiveProgressChanged(
        FavourRuntime favour,
        FavourObjectiveRuntime objective)
    {
        if (favour == null ||
            objective == null)
        {
            return;
        }

        NotificationSpawner spawner =
            NotificationSpawner.Instance;

        if (spawner == null ||
            spawner.Database == null)
        {
            return;
        }

        NotificationData template =
            spawner.Database
                .favourObjectiveProgress;

        if (template == null)
            return;

        string message =
            BuildMessage(
                objective
            );

        spawner.Show(
            template,
            message
        );
    }

    private static string BuildMessage(
        FavourObjectiveRuntime objective)
    {
        if (objective == null)
            return string.Empty;

        if (objective.RequiredProgress > 0)
        {
            return
                objective.DisplayName +
                " " +
                objective.ProgressText;
        }

        return objective.DisplayName;
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (!subscribed ||
            favourManager == null)
        {
            return;
        }

        favourManager
            .FavourObjectiveProgressChanged -=
            HandleObjectiveProgressChanged;

        subscribed =
            false;

        favourManager =
            null;
    }
}
