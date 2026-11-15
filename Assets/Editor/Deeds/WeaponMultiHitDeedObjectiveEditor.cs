using UnityEditor;

[CustomEditor(typeof(WeaponMultiHitDeedObjectiveDefinition))]
public sealed class WeaponMultiHitDeedObjectiveEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "requiredAbility",
            "requiredAbilities");

        SerializedProperty requireSpecificAbilities =
            serializedObject.FindProperty("requireSpecificAbilities");

        if (requireSpecificAbilities.boolValue)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("requiredAbilities"),
                includeChildren: true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
