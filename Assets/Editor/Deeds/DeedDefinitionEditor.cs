using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeedDefinition))]
public sealed class DeedDefinitionEditor : Editor
{
    private static readonly int[] RomanValues =
    {
        1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1
    };

    private static readonly string[] RomanSymbols =
    {
        "M", "CM", "D", "CD", "C", "XC", "L", "XL",
        "X", "IX", "V", "IV", "I"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "useRankObjectives",
            "objective",
            "ranks");

        SerializedProperty useRankObjectives =
            serializedObject.FindProperty("useRankObjectives");

        if (useRankObjectives != null)
            EditorGUILayout.PropertyField(useRankObjectives);

        bool hasRankObjectives = useRankObjectives?.boolValue == true;

        if (!hasRankObjectives)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("objective"));
        }

        SerializedProperty ranks = serializedObject.FindProperty("ranks");
        EditorGUILayout.PropertyField(ranks, includeChildren: true);

        for (int index = 0; index < ranks.arraySize; index++)
        {
            SerializedProperty rank = ranks.GetArrayElementAtIndex(index);
            SerializedProperty rankLabel = rank.FindPropertyRelative("rankLabel");
            string expectedLabel = ToRoman(index + 1);

            if (rankLabel.stringValue != expectedLabel)
                rankLabel.stringValue = expectedLabel;
        }

        if (!hasRankObjectives)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);

            if (ranks.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "Add a rank above to configure its rewards.",
                    MessageType.Info);
            }

            for (int index = 0; index < ranks.arraySize; index++)
            {
                SerializedProperty rank = ranks.GetArrayElementAtIndex(index);
                SerializedProperty rewards = rank.FindPropertyRelative("rewards");

                EditorGUILayout.PropertyField(
                    rewards,
                    new GUIContent($"Rank {ToRoman(index + 1)} Rewards"),
                    includeChildren: true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static string ToRoman(int number)
    {
        StringBuilder result = new();

        for (int index = 0; index < RomanValues.Length; index++)
        {
            while (number >= RomanValues[index])
            {
                result.Append(RomanSymbols[index]);
                number -= RomanValues[index];
            }
        }

        return result.ToString();
    }
}

[CustomPropertyDrawer(typeof(DeedRankDefinition))]
public sealed class DeedRankDefinitionDrawer : PropertyDrawer
{
    private static readonly string[] FieldOrder =
    {
        "rankLabel",
        "requiredTotalProgress",
        "shortDescription",
        "longDescription",
        "objective",
        "imageOverride",
        "rewards"
    };

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            Rect line = position;
            line.height = EditorGUIUtility.singleLineHeight;

            property.isExpanded = EditorGUI.Foldout(
                line, property.isExpanded, GetRankTitle(property, label), true);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;

            foreach (string fieldName in FieldOrder)
            {
                SerializedProperty field = GetVisibleField(property, fieldName);

                if (field == null)
                    continue;

                line.y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                line.height = EditorGUI.GetPropertyHeight(field, includeChildren: true);

                using (new EditorGUI.DisabledScope(fieldName == "rankLabel"))
                    EditorGUI.PropertyField(line, field, includeChildren: true);
            }

            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded)
            return height;

        foreach (string fieldName in FieldOrder)
        {
            SerializedProperty field = GetVisibleField(property, fieldName);

            if (field != null)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorGUI.GetPropertyHeight(field, includeChildren: true);
            }
        }

        return height;
    }

    private static SerializedProperty GetVisibleField(
    SerializedProperty rank,
    string fieldName)
    {
        bool hasRankObjectives = rank.serializedObject
            .FindProperty("useRankObjectives")?.boolValue == true;

        if (fieldName == "objective" && !hasRankObjectives)
            return null;

        if (fieldName == "rewards" &&
            !hasRankObjectives &&
            rank.serializedObject.targetObject is DeedDefinition)
        {
            return null;
        }

        return rank.FindPropertyRelative(fieldName);
    }

    private static GUIContent GetRankTitle(
        SerializedProperty rank,
        GUIContent fallback)
    {
        string path = rank.propertyPath;
        int start = path.LastIndexOf('[');
        int end = path.LastIndexOf(']');

        if (start >= 0 && end > start &&
            int.TryParse(path.Substring(start + 1, end - start - 1), out int index))
        {
            return new GUIContent($"Rank {index + 1}");
        }

        return fallback;
    }
}
