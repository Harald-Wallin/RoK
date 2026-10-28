using UnityEngine;

public class RunestoneInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private Collider2D interactionCollider;

    private RespawnPoint respawnPoint;
    private Transform player;

    void Awake()
    {
        respawnPoint = GetComponent<RespawnPoint>();

        PlayerMovement pm =
            FindFirstObjectByType<PlayerMovement>();

        if (pm != null)
            player = pm.transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector2 mouseWorld =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

            TryInteract(mouseWorld);
        }
    }

    void TryInteract(Vector2 mouseWorld)
    {
        if (interactionCollider == null)
            return;

        if (!interactionCollider.OverlapPoint(mouseWorld))
            return;

        if (player == null)
            return;

        float dist =
            Vector2.Distance(player.position, transform.position);

        if (dist > interactionRange)
            return;

        RunestoneConfirmUI.Instance.Open(this);
    }

    public void ConfirmActivation()
    {
        if (player == null)
            return;

        PlayerRespawnManager manager =
            player.GetComponent<PlayerRespawnManager>();

        if (manager == null)
            return;

        manager.SetRespawnPoint(respawnPoint);

        Debug.Log("Respawn point updated!");
    }

    public string GetRunestoneName()
    {
        if (respawnPoint == null)
            return "Runestone";

        return respawnPoint.spawnName;
    }
}