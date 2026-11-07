
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RoKContentStudioEffectRegistryPanel
{

    public void PrepareForTalent(TalentData talent)
    {
        if (talent == null ||
            !AssetDatabase.Contains(talent))
            return;

        kind = RoKRegistryEffectKind.PassiveEffect;
        passiveAttachment = PassiveAttachment.Talent;

        targetTalent = talent;
        targetBuff = null;

        selectedType = null;
        assetName = string.Empty;
        search = string.Empty;
    }


    // =====================================================
    // STATE
    // =====================================================

    private enum PassiveAttachment
    {
        Standalone,
        Buff,
        Talent
    }

    private RoKRegistryEffectKind kind =
        RoKRegistryEffectKind.PassiveEffect;

    private PassiveAttachment passiveAttachment =
        PassiveAttachment.Standalone;

    private RoKRegisteredEffectType selectedType;

    private string search = string.Empty;
    private string assetName = string.Empty;

    private AbilityData targetAbility;
    private BuffDefinition targetBuff;
    private TalentData targetTalent;

    private BuffDefinition applyBuffDefinition;

    private AbilityEffectTargetMode targetMode =
        AbilityEffectTargetMode.EachAffectedTarget;

    private AbilityEffectExecutionTiming executionTiming =
        AbilityEffectExecutionTiming.Immediate;

    private bool requiresCharacterTarget = true;

    private string helpText =
        "Select an effect type to configure its creation.";

    // =====================================================
    // DRAW
    // =====================================================

    public void Draw(
        Action<ScriptableObject> openAsset,
        Action refreshLibrary)
    {
        EditorGUILayout.HelpBox(
            "Dynamic Effect Registry\n\n" +
            "Discovers all asset-creatable AbilityEffect " +
            "and EffectDefinition classes automatically.\n\n" +
            "No manual type registration required.",
            MessageType.Info
        );

        RoKRegistryEffectKind previousKind =
            kind;

        kind =
            (RoKRegistryEffectKind)
            GUILayout.Toolbar(
                (int)kind,
                new[]
                {
                    "Ability Actions",
                    "Passive Effects"
                }
            );

        if (kind != previousKind)
        {
            selectedType = null;
            assetName = string.Empty;
            search = string.Empty;
        }

        EditorGUILayout.Space(8);

        DrawTypeSelector();

        if (selectedType == null)
            return;

        EditorGUILayout.Space(10);

        DrawCreationSettings();

        EditorGUILayout.Space(10);

        DrawAttachmentSettings();

        EditorGUILayout.Space(10);

        DrawCreateButton(
            openAsset,
            refreshLibrary
        );

        EditorGUILayout.Space(12);

        EditorGUILayout.LabelField(
            "Context Help",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            helpText,
            MessageType.Info
        );
    }

    // =====================================================
    // TYPE SELECTOR
    // =====================================================

    private void DrawTypeSelector()
    {
        EditorGUILayout.LabelField(
            "Effect Type",
            EditorStyles.boldLabel
        );

        search =
            EditorGUILayout.TextField(
                new GUIContent(
                    "Search",
                    "Filter available effect classes."
                ),
                search
            );

        IReadOnlyList<RoKRegisteredEffectType> all =
            RoKContentStudioEffectRegistry.GetTypes(kind);

        List<RoKRegisteredEffectType> matches =
            new();

        foreach (RoKRegisteredEffectType entry in all)
        {
            if (string.IsNullOrWhiteSpace(search) ||
                entry.DisplayName.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0 ||
                entry.Type.Name.IndexOf(
                    search,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0 ||
                (
                    !string.IsNullOrEmpty(entry.MenuPath) &&
                    entry.MenuPath.IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                ))
            {
                matches.Add(entry);
            }
        }

        EditorGUILayout.LabelField(
            $"Available Types: {matches.Count} / {all.Count}",
            EditorStyles.miniLabel
        );

        if (matches.Count == 0)
        {
            selectedType = null;

            EditorGUILayout.HelpBox(
                "No matching effect types found.",
                MessageType.Warning
            );

            return;
        }

        string[] names =
            new string[matches.Count];

        int currentIndex = -1;

        for (int i = 0; i < matches.Count; i++)
        {
            names[i] =
                $"{matches[i].DisplayName} " +
                $"[{matches[i].Type.Name}]";

            if (selectedType != null &&
                matches[i].Type == selectedType.Type)
            {
                currentIndex = i;
            }
        }

        int nextIndex =
            EditorGUILayout.Popup(
                "Type",
                Mathf.Max(0, currentIndex),
                names
            );

        nextIndex =
            Mathf.Clamp(
                nextIndex,
                0,
                matches.Count - 1
            );

        if (currentIndex < 0 ||
            nextIndex != currentIndex)
        {
            SelectType(
                matches[nextIndex]
            );
        }

        EditorGUILayout.LabelField(
            selectedType.Type.FullName,
            EditorStyles.miniLabel
        );

        if (!string.IsNullOrWhiteSpace(
                selectedType.MenuPath))
        {
            EditorGUILayout.LabelField(
                $"Menu: {selectedType.MenuPath}",
                EditorStyles.miniLabel
            );
        }
    }

    // =====================================================
    // TYPE SELECTION
    // =====================================================

    private void SelectType(
        RoKRegisteredEffectType type)
    {
        selectedType = type;

        assetName =
            "New " + type.DisplayName;

        targetMode =
            AbilityEffectTargetMode.EachAffectedTarget;

        executionTiming =
            AbilityEffectExecutionTiming.Immediate;

        requiresCharacterTarget = true;

        applyBuffDefinition = null;

        // Reuse existing known action defaults,
        // but do not depend on templates for discovery.
        if (kind == RoKRegistryEffectKind.AbilityAction)
        {
            foreach (RoKActionTemplate template
                     in RoKContentStudioActionFactory.Templates)
            {
                if (template.ActionType != type.Type)
                    continue;

                targetMode =
                    template.DefaultTarget;

                requiresCharacterTarget =
                    template.RequiresCharacterTarget;

                break;
            }
        }

        helpText =
            kind == RoKRegistryEffectKind.AbilityAction
                ? "AbilityEffect executes through the " +
                  "existing action pipeline.\n\n" +
                  "Configure targeting and execution timing " +
                  "before linking it to gameplay."
                : "EffectDefinition contains reusable " +
                  "gameplay behavior.\n\n" +
                  "Duration and stacking belong to " +
                  "the owning Buff or Talent system.";
    }

    // =====================================================
    // CREATION SETTINGS
    // =====================================================

    private void DrawCreationSettings()
    {
        EditorGUILayout.LabelField(
            "New Asset",
            EditorStyles.boldLabel
        );

        assetName =
            EditorGUILayout.TextField(
                new GUIContent(
                    "Asset Name",
                    "The name of the new ScriptableObject."
                ),
                assetName
            );

        if (kind != RoKRegistryEffectKind.AbilityAction)
            return;

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Action Configuration",
            EditorStyles.boldLabel
        );

        targetMode =
            (AbilityEffectTargetMode)
            EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Target Mode",
                    "Who receives the effect?\n\n" +
                    "EachAffectedTarget: every selected target.\n" +
                    "Caster: the character using the ability.\n" +
                    "OncePerAction: once per execution."
                ),
                targetMode
            );

        executionTiming =
            (AbilityEffectExecutionTiming)
            EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Execution Timing",
                    "Immediate: action execution.\n\n" +
                    "Deferred: triggered later, " +
                    "for example by projectile impact."
                ),
                executionTiming
            );

        requiresCharacterTarget =
            EditorGUILayout.Toggle(
                new GUIContent(
                    "Requires Character",
                    "Whether execution requires " +
                    "a CharacterStats target."
                ),
                requiresCharacterTarget
            );

        if (typeof(ApplyBuffDefinitionEffect)
                .IsAssignableFrom(selectedType.Type))
        {
            applyBuffDefinition =
                (BuffDefinition)
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Buff Definition",
                        "BuffDefinition applied by this action."
                    ),
                    applyBuffDefinition,
                    typeof(BuffDefinition),
                    false
                );
        }
    }

    // =====================================================
    // ATTACHMENT SETTINGS
    // =====================================================

    private void DrawAttachmentSettings()
    {
        EditorGUILayout.LabelField(
            "Attachment",
            EditorStyles.boldLabel
        );

        if (kind == RoKRegistryEffectKind.AbilityAction)
        {
            targetAbility =
                (AbilityData)
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Attach To Ability",
                        "Optional.\n\n" +
                        "If selected, the new action is " +
                        "automatically added to " +
                        "AbilityData.effects."
                    ),
                    targetAbility,
                    typeof(AbilityData),
                    false
                );

            return;
        }

        passiveAttachment =
            (PassiveAttachment)
            EditorGUILayout.EnumPopup(
                "Attach To",
                passiveAttachment
            );

        switch (passiveAttachment)
        {
            case PassiveAttachment.Buff:

                targetBuff =
                    (BuffDefinition)
                    EditorGUILayout.ObjectField(
                        "Buff Definition",
                        targetBuff,
                        typeof(BuffDefinition),
                        false
                    );

                break;

            case PassiveAttachment.Talent:

                targetTalent =
                    (TalentData)
                    EditorGUILayout.ObjectField(
                        "Talent Data",
                        targetTalent,
                        typeof(TalentData),
                        false
                    );

                break;
        }
    }

    // =====================================================
    // CREATE BUTTON
    // =====================================================

    private void DrawCreateButton(
        Action<ScriptableObject> openAsset,
        Action refreshLibrary)
    {
        ScriptableObject owner =
            GetSelectedOwner();

        bool attachmentMissing =
            kind == RoKRegistryEffectKind.PassiveEffect &&
            passiveAttachment != PassiveAttachment.Standalone &&
            owner == null;

        bool missingBuff =
            kind == RoKRegistryEffectKind.AbilityAction &&
            typeof(ApplyBuffDefinitionEffect)
                .IsAssignableFrom(selectedType.Type) &&
            applyBuffDefinition == null;

        if (attachmentMissing)
        {
            EditorGUILayout.HelpBox(
                "Select the Buff or Talent to attach to.",
                MessageType.Warning
            );
        }

        if (missingBuff)
        {
            EditorGUILayout.HelpBox(
                "Select a BuffDefinition for the " +
                "ApplyBuffDefinitionEffect.",
                MessageType.Warning
            );
        }

        if (kind == RoKRegistryEffectKind.AbilityAction &&
            executionTiming ==
                AbilityEffectExecutionTiming.Deferred)
        {
            EditorGUILayout.HelpBox(
                "Deferred actions require a compatible " +
                "executor such as a projectile.\n\n" +
                "Creating this action does not automatically " +
                "configure an executor.",
                MessageType.Info
            );
        }

        bool valid =
            selectedType != null &&
            !string.IsNullOrWhiteSpace(assetName) &&
            !attachmentMissing &&
            !missingBuff &&
            !EditorApplication.isPlayingOrWillChangePlaymode;

        using (new EditorGUI.DisabledScope(!valid))
        {
            string label =
                owner != null
                    ? "Create & Attach Effect"
                    : "Create Effect Asset";

            if (GUILayout.Button(
                    label,
                    GUILayout.Height(34)))
            {
                QueueCreate(
                    owner,
                    openAsset,
                    refreshLibrary
                );
            }
        }

        EditorGUILayout.HelpBox(
            "After creation, the new asset opens " +
            "in Content Studio for configuration.\n\n" +
            "The registry does not assume values for " +
            "new effect-specific fields.\n\n" +
            "Existing gameplay systems remain unchanged.",
            MessageType.None
        );
    }

    // =====================================================
    // RESOLVE OWNER
    // =====================================================

    private ScriptableObject GetSelectedOwner()
    {
        if (kind == RoKRegistryEffectKind.AbilityAction)
        {
            return targetAbility;
        }

        return passiveAttachment switch
        {
            PassiveAttachment.Buff => targetBuff,
            PassiveAttachment.Talent => targetTalent,
            _ => null
        };
    }

    // =====================================================
    // DEFERRED CREATION
    // =====================================================

    private void QueueCreate(
        ScriptableObject owner,
        Action<ScriptableObject> openAsset,
        Action refreshLibrary)
    {
        Type capturedType =
            selectedType.Type;

        string capturedName =
            assetName.Trim();

        AbilityEffectTargetMode capturedTargetMode =
            targetMode;

        AbilityEffectExecutionTiming capturedTiming =
            executionTiming;

        bool capturedRequiresCharacter =
            requiresCharacterTarget;

        BuffDefinition capturedBuff =
            applyBuffDefinition;

        EditorApplication.delayCall += () =>
        {
            try
            {
                ScriptableObject created =
                    RoKContentStudioEffectRegistry.Create(
                        capturedType,
                        capturedName,
                        owner,
                        capturedTargetMode,
                        capturedTiming,
                        capturedRequiresCharacter,
                        capturedBuff
                    );

                refreshLibrary?.Invoke();

                openAsset?.Invoke(created);

                Selection.activeObject =
                    created;

                EditorGUIUtility.PingObject(
                    created
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception
                );

                EditorUtility.DisplayDialog(
                    "Effect Creation Failed",
                    exception.Message,
                    "OK"
                );
            }
        };
    }
}

#endif

