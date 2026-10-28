using UnityEngine;

[RequireComponent(typeof(FavourGiver))]
public sealed class FavourDebugTester :
    MonoBehaviour
{
    [SerializeField]
    private KeyCode interactKey =
        KeyCode.K;

    [SerializeField]
    private KeyCode acceptFirstFavourKey =
        KeyCode.L;

    [SerializeField]
    private KeyCode turnInFirstFavourKey =
        KeyCode.O;

    private FavourGiver giver;

    private void Awake()
    {
        giver =
            GetComponent<FavourGiver>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(
                interactKey))
        {
            //giver.Interact();

            Debug.Log(
                $"Interacted with favour giver: " +
                $"{giver.GiverName}",
                this
            );
        }

        if (Input.GetKeyDown(
                acceptFirstFavourKey))
        {
            if (giver.Favours.Count == 0)
                return;

            bool accepted =
                giver.TryAccept(
                    giver.Favours[0]
                );

            Debug.Log(
                accepted
                    ? "Favour accepted."
                    : "Favour could not be accepted.",
                this
            );
        }

        if (Input.GetKeyDown(
                turnInFirstFavourKey))
        {
            if (giver.Favours.Count == 0)
                return;

            bool turnedIn =
                giver.TryTurnIn(
                    giver.Favours[0]
                );

            Debug.Log(
                turnedIn
                    ? "Favour turned in."
                    : "Favour is not ready to turn in.",
                this
            );
        }
    }
}
