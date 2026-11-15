#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeGeneralTraitEditor
{
    internal static VisualElement Create(
        ScrollView workspace,
        GeneralTraitDefinition trait,
        Action<ScriptableObject> openAsset,
        Action onIdentityChanged = null)
    {
        var picker = new RoKForgePassiveEffectList.ExistingEffectPicker();

        VisualElement left = new();
        left.Add(RoKForgeIdentityHeader.Create(trait, onIdentityChanged));

        left.Add(new IMGUIContainer(() =>
        {
            if (trait == null)
                return;

            using SerializedObject serialized = new(trait);
            serialized.Update();

            using (new EditorGUI.DisabledScope(
                EditorApplication.isPlayingOrWillChangePlaymode))
            {
                RoKForgeEditorFields.Group(
                    serialized, "GENERAL", "description");
            }

            serialized.ApplyModifiedProperties();
        }));

        IMGUIContainer right = new(() =>
        {
            if (trait == null)
                return;

            using SerializedObject serialized = new(trait);
            serialized.Update();

            using (new EditorGUI.DisabledScope(
                EditorApplication.isPlayingOrWillChangePlaymode))
            {
                RoKForgePassiveEffectList.Draw(
                    serialized, "effects", "PASSIVE EFFECTS", openAsset);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(
                    "ADD EXISTING EFFECT", EditorStyles.boldLabel);

                picker.Draw(serialized, "effects", openAsset);
            }

            serialized.ApplyModifiedProperties();
        });

        VisualElement form = RoKForgeFormLayout.CreateColumns(
            workspace, left, right);
        form.style.marginTop = 10;

        return form;
    }

    internal static string GetEffectIssue(GeneralTraitDefinition trait)
    {
        if (trait.Effects.Count == 0)
            return "Lägg till minst en passiv effekt.";

        for (int index = 0; index < trait.Effects.Count; index++)
        {
            if (trait.Effects[index] == null)
                return $"Effektplats {index + 1} är tom. Fyll i den eller ta bort den.";
        }

        return null;
    }
}

#endif
