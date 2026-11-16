using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrialSpawnPoint : MonoBehaviour
{
    [SerializeField, HideInInspector] private TrialStageDefinition stage;
    [SerializeField, HideInInspector] private string objectiveId;
    [SerializeField] private CharacterStats prefab;

    public TrialStageDefinition Stage => stage;
    public string ObjectiveId => objectiveId;
    public CharacterStats Prefab => prefab;
    public TrialZone Zone => GetComponentInParent<TrialZone>(true);

#if UNITY_EDITOR
    public void AssignObjective(TrialStageDefinition definition, string id)
    {
        stage = definition;
        objectiveId = id;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 0.18f);
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.up * 0.35f);
    }
#endif
}
