using UnityEngine;

public class FactionDiscoverySource : MonoBehaviour
{
    [SerializeField]
    private float discoveryRadius = 8f;

    private CharacterStats stats;

    private bool discovered;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    void Update()
    {
        if (discovered)
            return;

        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        float distance =
            Vector2.Distance(
                transform.position,
                player.transform.position
            );

        if (distance > discoveryRadius)
            return;

        if (stats == null)
            return;

        if (stats.faction == null)
            return;

        PlayerReputationManager repManager =
            player.GetComponent<PlayerReputationManager>();

        if (repManager == null)
            return;

        repManager.DiscoverFaction(
            stats.faction
        );

        discovered = true;
    }
}
