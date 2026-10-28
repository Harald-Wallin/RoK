using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EntityIdentity))]
public sealed class ExploreZone :
    MonoBehaviour
{
    private EntityIdentity identity;

    private void Awake()
    {
        identity =
            GetComponent<EntityIdentity>();
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (other == null ||
            identity == null)
        {
            return;
        }

        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        /*
         * Spelarens collider kan sitta på ett child-object,
         * därför kontrollerar vi både collider-objektet och
         * dess parents.
         */
        PlayerStats enteringPlayer =
            other.GetComponentInParent<
                PlayerStats>();

        if (enteringPlayer == null ||
            enteringPlayer != player)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                identity.Id))
        {
            return;
        }

        ExplorationEvents
            .RaiseZoneEntered(
                identity.Id
            );
    }
}
