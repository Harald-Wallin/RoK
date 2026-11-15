#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DeedRewardPreview))]
public sealed class DeedRewardPreviewDrawer : PropertyDrawer
{
    private static readonly string[] Fields =
    {
        "kind", "displayName", "icon", "ability",
        "item", "generalTrait", "amount"
    };

    public override void OnGUI(
    Rect position,
    SerializedProperty property,
    GUIContent label)
    {
        bool showCreate = CanShowCreate(property);

        using (new EditorGUI.PropertyScope(position, label, property))
        {
            Rect line = position;
            line.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.Foldout(
                line, property.isExpanded, label, true);

            if (!property.isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (string fieldName in Fields)
                {
                    SerializedProperty field =
                        property.FindPropertyRelative(fieldName);

                    if (field == null)
                        continue;

                    AdvanceLine(ref line,
                        EditorGUI.GetPropertyHeight(field, includeChildren: true));

                    if (fieldName == "generalTrait" &&
                        EditorWindow.focusedWindow is RoKContentStudioWindow forge)
                    {
                        RoKForgeLinkedField.Draw(
                            line,
                            field,
                            new GUIContent(
                                field.displayName,
                                "Double-click to open this trait in The Forge."),
                            forge.ShowAsset);
                    }
                    else
                    {
                        EditorGUI.PropertyField(line, field, includeChildren: true);
                    }

                    if (fieldName == "generalTrait" && showCreate)
                    {
                        AdvanceLine(ref line, EditorGUIUtility.singleLineHeight);
                        DrawCreateButton(line, property);
                    }
                }
            }
        }
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded)
            return height;

        float spacing = EditorGUIUtility.standardVerticalSpacing;

        foreach (string fieldName in Fields)
        {
            SerializedProperty field = property.FindPropertyRelative(fieldName);

            if (field != null)
                height += spacing + EditorGUI.GetPropertyHeight(field, true);
        }

        if (CanShowCreate(property))
            height += spacing + EditorGUIUtility.singleLineHeight;

        return height;
    }

    private static bool CanShowCreate(SerializedProperty property)
    {
        SerializedObject serialized = property.serializedObject;

        return !serialized.isEditingMultipleObjects &&
               serialized.targetObject is DeedDefinition &&
               property.FindPropertyRelative("generalTrait") != null &&
               property.FindPropertyRelative("kind")?.intValue ==
                   (int)DeedRewardKind.GeneralTrait;
    }

    private static void DrawCreateButton(
        Rect line,
        SerializedProperty property)
    {
        using (new EditorGUI.DisabledScope(
            EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (!GUI.Button(
                EditorGUI.IndentedRect(line), "Create & Assign General Trait"))
            {
                return;
            }
        }

        DeedDefinition deed =
            (DeedDefinition)property.serializedObject.targetObject;

        string rewardPath = property.propertyPath;
        EditorWindow sourceWindow = EditorWindow.focusedWindow;

        EditorApplication.delayCall += () =>
            RoKForgeDeedTraitWindow.Open(deed, rewardPath, sourceWindow);
    }

    private static void AdvanceLine(ref Rect line, float height)
    {
        line.y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
        line.height = height;
    }
}

#endif
