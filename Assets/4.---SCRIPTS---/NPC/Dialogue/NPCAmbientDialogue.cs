using System.Collections.Generic;
using UnityEngine;

public class NPCAmbientDialogue : MonoBehaviour
{
    
    [Header("Dialogue Tables")]
    public List<NPCDialogueTable> dialogueTables = new();


    [Header("Combat Dialogue")]
    public NPCDialogueTable combatDialogueTable;
    CharacterStats stats;
    bool isInCombat;
    float combatExitTimer = 0f;
    [SerializeField] float combatExitDelay = 5f;


    [Header("Trigger Settings")]
    public float triggerRadius = 3f;
    [Range(0f, 1f)]
    public float speakChance = 0.3f;
    public float cooldown = 20f;

    [Tooltip("0 = exact front only, -1 = full circle")]
    [Range(-1f, 1f)]
    public float forwardThreshold = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugSpeak = false;

    float lastSpeakTime = -999f;
    float lastCombatSpeakTime = -999f;


    Transform player;
    Vendor vendor;
    PlayerReputationManager reputationManager;
    public GameObject dialogueBubblePrefab;
    private NPCBehavior ai;
    private NPCMovement movement;

    void Awake()
    {
        ai = GetComponent<NPCBehavior>();
        movement = GetComponent<NPCMovement>();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        ai = GetComponent<NPCBehavior>();
        movement = GetComponent<NPCMovement>();

        vendor = GetComponent<Vendor>();

        reputationManager = FindFirstObjectByType<PlayerReputationManager>();

        if (playerObj != null)
            player = playerObj.transform;

        stats = GetComponent<CharacterStats>();

        if (stats != null)
        {
            stats.OnDamagedBy += OnDamaged;
        }
    }

    void Update()
    {
        if (player == null)
            return;

        // Uppdatera combat-state från AI
        if (ai != null)
            isInCombat = ai.IsInCombat;

        // =========================
        // IDLE SPEAK
        // =========================
        if (!isInCombat)
        {
            if (Time.time < lastSpeakTime + cooldown)
                return;

            float distance = Vector2.Distance(transform.position, player.position);
            if (distance > triggerRadius)
                return;

            if (!IsPlayerInFront())
                return;

            TrySpeak();

            // =========================
            // INTERACT (Vendor)
            // =========================
            if (vendor != null && player != null)
            {
                /*float*/distance = Vector2.Distance(transform.position, player.position);

                if (distance <= triggerRadius)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (vendor.CanTrade(reputationManager))
                        {
                            //Debug.Log("Opening Vendor UI...");
                        }
                        else
                        {
                            Debug.Log("Vendor refuses to trade.");
                        }
                    }
                }
            }
        }
    }

    bool aiIsAggro()
    {
        if (ai == null)
            return false;

        return ai.IsInCombat;
    }

    bool IsPlayerInFront()
    {
        if (movement == null)
            return true;

        Vector2 directionToPlayer =
            (player.position - transform.position).normalized;

        Vector2 npcForward =
            movement.CurrentFacingDirection.normalized;

        float dot =
            Vector2.Dot(npcForward, directionToPlayer);

        return dot >= forwardThreshold;
    }

    void TrySpeak()
    {
        // Proceed with probability `speakChance` (e.g. 0.3 = 30%)
        float r = Random.value;
        if (debugSpeak)
            Debug.Log($"NPCAmbientDialogue.TrySpeak: rand={r:F3}, chance={speakChance:F3}");

        if (r >= speakChance)
        {
            if (debugSpeak)
                Debug.Log("NPCAmbientDialogue: skipped by probability");
            return;
        }

        var line = GetValidDialogueLine();
        if (line == null)
            return;

        ShowDialogue(line);

        lastSpeakTime = Time.time;
    }

    void OnDamaged(CharacterStats attacker)
    {
        isInCombat = true;
        combatExitTimer = 0f;

        TryCombatSpeak();
    }

    void TryCombatSpeak()
    {
        if (combatDialogueTable == null)
            return;

        if (combatDialogueTable.lines.Count == 0)
            return;

        if (Time.time < lastCombatSpeakTime + cooldown)
            return;

        var line = combatDialogueTable.lines[
            Random.Range(0, combatDialogueTable.lines.Count)];

        ShowDialogue(line);

        lastCombatSpeakTime = Time.time;
    }

    void ShowDialogue(DialogueLine line)
    {
        if (dialogueBubblePrefab == null)
            return;

        GameObject bubble = Instantiate(dialogueBubblePrefab);

        NPCDialogueBubble bubbleScript =
            bubble.GetComponent<NPCDialogueBubble>();

        if (bubbleScript == null)
        {
            Debug.LogError("NPCDialogueBubble script missing!");
            return;
        }

        bubbleScript.Initialize(transform, line);
    }

    DialogueLine GetValidDialogueLine()
    {
        List<DialogueLine> validLines = new();

        foreach (var table in dialogueTables)
        {
            if (!IsTableValid(table))
                continue;

            validLines.AddRange(table.lines);
        }

        if (validLines.Count == 0)
            return null;

        return validLines[Random.Range(0, validLines.Count)];
    }

    bool IsTableValid(NPCDialogueTable table)
    {
        if (table.faction == null)
            return true;

        if (reputationManager == null)
            return false;

        var rep = reputationManager.GetReputation(table.faction);

        int level = rep != null ? rep.level : 0;

        return level >= table.minReputationLevel;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        if (!Application.isPlaying || ai == null || movement == null)
            return;

        Vector2 forward = movement.CurrentFacingDirection.normalized;

        float angle = Mathf.Acos(forwardThreshold) * Mathf.Rad2Deg;

        int segments = 30;
        float step = (angle * 2f) / segments;

        Vector3 previousPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -angle + (step * i);

            Vector3 dir = Quaternion.Euler(0, 0, currentAngle) * forward;
            Vector3 point = transform.position + dir * triggerRadius;

            if (i > 0)
                Gizmos.DrawLine(previousPoint, point);

            previousPoint = point;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position,
            transform.position + (Vector3)(forward * triggerRadius));
    }
}
