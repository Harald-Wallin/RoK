using System.Collections;
using UnityEngine;

/// <summary>
/// Central runtime-service för NPC-respawn.
///
/// En NPC som får respawna registrerar sin död här innan
/// CharacterStats förstör den gamla instansen.
///
/// Systemet skapar då en inaktiv kopia av NPC:n och aktiverar
/// den igen vid dess ursprungliga spawnposition när
/// respawn-timern löpt ut.
///
/// Ingen separat MobSpawner eller prefab-reference behövs.
/// </summary>
[DisallowMultipleComponent]
public sealed class NPCRespawnSystem :
    MonoBehaviour
{
    private static NPCRespawnSystem instance;

    public static NPCRespawnSystem Instance =>
        GetOrCreateInstance();

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Registrerar en NPC för framtida respawn.
    ///
    /// Måste anropas medan original-NPC:n fortfarande existerar,
    /// eftersom dess kompletta GameObject används som template
    /// för den nya instansen.
    /// </summary>
    public static void ScheduleRespawn(
        NPCBehavior source)
    {
        if (source == null)
            return;

        if (!source.CanRespawn)
            return;

        GetOrCreateInstance()
            .CreateRespawnRequest(
                source
            );
    }

    private void CreateRespawnRequest(
        NPCBehavior source)
    {
        if (source == null)
            return;

        Vector3 spawnPosition =
            source.SpawnPosition;

        Quaternion spawnRotation =
            source.SpawnRotation;

        Transform originalParent =
            source.transform.parent;

        float respawnTime =
            source.RespawnTime;

        /*
         * Vi klonar NPC:n medan originalet fortfarande är vid liv
         * som GameObject.
         *
         * Instansen placeras direkt vid sin riktiga spawnpunkt.
         * Den inaktiveras omedelbart innan nästa frame.
         *
         * Eftersom CharacterStats.Awake återställer HP får den
         * nya instansen ett rent runtime-state.
         */
        GameObject respawnInstance =
            Instantiate(
                source.gameObject,
                spawnPosition,
                spawnRotation,
                originalParent
            );

        respawnInstance.name =
            source.gameObject.name;

        NPCBehavior respawnBehavior =
            respawnInstance.GetComponent<
                NPCBehavior>();

        if (respawnBehavior == null)
        {
            Debug.LogError(
                $"NPCRespawnSystem: " +
                $"{source.name} saknar NPCBehavior.",
                source
            );

            Destroy(
                respawnInstance
            );

            return;
        }

        /*
         * Instansen ska inte existera aktivt i världen medan
         * respawn-timern räknar.
         */
        respawnInstance.SetActive(
            false
        );

        StartCoroutine(
            RespawnAfterDelay(
                respawnInstance,
                respawnBehavior,
                spawnPosition,
                spawnRotation,
                respawnTime
            )
        );
    }

    private IEnumerator RespawnAfterDelay(
        GameObject respawnInstance,
        NPCBehavior respawnBehavior,
        Vector3 spawnPosition,
        Quaternion spawnRotation,
        float respawnTime)
    {
        if (respawnTime > 0f)
        {
            yield return new WaitForSeconds(
                respawnTime
            );
        }
        else
        {
            /*
             * Även 0 sekunders respawn väntar åtminstone en frame.
             *
             * Det förhindrar att den nya NPC:n blir aktiv innan
             * CharacterStats hunnit avsluta den gamla NPC:ns
             * death-pipeline.
             */
            yield return null;
        }

        if (respawnInstance == null ||
            respawnBehavior == null)
        {
            yield break;
        }

        /*
         * Något kan teoretiskt ha flyttat den inaktiva instansen,
         * så spawntransformen återställs precis innan aktivering.
         */
        respawnInstance.transform
            .SetPositionAndRotation(
                spawnPosition,
                spawnRotation
            );

        /*
         * Proximity-aggro får en kort grace-period.
         * Damage/ForceAggro påverkas inte av detta.
         */
        respawnBehavior
            .PrepareForRespawn();

        respawnInstance.SetActive(
            true
        );
    }

    private static NPCRespawnSystem
        GetOrCreateInstance()
    {
        if (instance != null)
            return instance;

        instance =
            FindFirstObjectByType<
                NPCRespawnSystem>();

        if (instance != null)
            return instance;

        GameObject systemObject =
            new GameObject(
                "[NPC Respawn System]"
            );

        instance =
            systemObject.AddComponent<
                NPCRespawnSystem>();

        return instance;
    }
}
