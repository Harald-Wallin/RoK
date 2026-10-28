using UnityEngine;

[RequireComponent(typeof(Vendor))]
public class VendorRightClickInteract : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f; // max avstånd till spelaren
    [SerializeField] private Collider2D interactionCollider;

    private Vendor vendor;
    private Transform player;

    private void Awake()
    {
        vendor = GetComponent<Vendor>();

        if (interactionCollider == null)
            Debug.LogError("VendorRightClickInteract: InteractionCollider not assigned!");

        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
            player = pm.transform;
        else
            Debug.LogError("VendorRightClickInteract: PlayerMovement not found!");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) // högerklick
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            TryInteract(mouseWorld);
        }
    }

    private void TryInteract(Vector2 mouseWorld)
    {
        if (vendor == null || player == null || interactionCollider == null)
            return;

        // Kolla mus över trigger
        if (!interactionCollider.OverlapPoint(mouseWorld))
            return;

        float distance = Vector2.Distance(player.position, transform.position);
        if (distance > interactionRange)
            return;

        NPCReactionController reaction = vendor.GetComponent<NPCReactionController>();

        if (reaction != null)
        {
            if (reaction.BlocksInteraction(
                PlayerReference.Player))
            {
                return;
            }
        }

        //Debug.Log("Högerklick: Öppnar Vendor UI!");
        vendor.OpenVendor();
    }
}