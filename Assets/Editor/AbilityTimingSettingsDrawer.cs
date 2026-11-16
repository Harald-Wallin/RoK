#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AbilityTimingSettings))]
public sealed class AbilityTimingSettingsDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        using (new EditorGUI.PropertyScope(position, label, property))
        {
            float y = position.y;

            foreach (SerializedProperty field in GetVisibleFields(property))
            {
                float height = EditorGUI.GetPropertyHeight(
                    field, includeChildren: true);

                Rect row = new(position.x, y, position.width, height);
                EditorGUI.PropertyField(row, field, includeChildren: true);
                y += height + Spacing;
            }
        }
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float height = 0f;

        foreach (SerializedProperty field in GetVisibleFields(property))
        {
            height += EditorGUI.GetPropertyHeight(
                field, includeChildren: true) + Spacing;
        }

        return height;
    }

    private static IEnumerable<SerializedProperty> GetVisibleFields(
        SerializedProperty property)
    {
        SerializedProperty timingType =
            property.FindPropertyRelative("timingType");

        yield return timingType;

        switch ((ActionTimingType)timingType.enumValueIndex)
        {
            case ActionTimingType.Cast:
                yield return property.FindPropertyRelative("castDuration");
                yield return property.FindPropertyRelative("cancelOnMove");
                yield return property.FindPropertyRelative("castMovement");
                break;

            case ActionTimingType.Charge:
                yield return property.FindPropertyRelative("maximumChargeDuration");
                yield return property.FindPropertyRelative("chargeMovement");
                break;

            case ActionTimingType.Channel:
                yield return property.FindPropertyRelative("channelDuration");
                yield return property.FindPropertyRelative("channelTickInterval");
                yield return property.FindPropertyRelative("channelMovement");
                break;
        }

        SerializedProperty recovery =
            property.FindPropertyRelative("recoveryDuration");

        yield return recovery;

        if (recovery.floatValue > 0f)
            yield return property.FindPropertyRelative("recoveryMovement");
    }
}

#endif