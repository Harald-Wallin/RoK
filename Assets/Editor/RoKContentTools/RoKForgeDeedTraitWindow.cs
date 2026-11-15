#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class RoKForgeDeedTraitWindow : EditorWindow
{
    private const string TraitFolder = "Assets/12.Deeds/Deed-traits";

    private static readonly Regex RewardPathPattern = new(
        @"^ranks\.Array\.data\[(\d+)\]\.rewards\.Array\.data\[(\d+)\]$");

    private DeedDefinition deed;
    private int rankIndex;
    private int rewardIndex;
    private string rewardPath;
    private DeedRewardPreview originalReward;
    private GeneralTraitDefinition originalTrait;
    private EditorWindow sourceWindow;

    private GeneralTraitDefinition draft;
    private SerializedObject trackedDraft;
    private HelpBox requirements;
    private HelpBox saveError;
    private Button saveButton;

    internal static void Open(
        DeedDefinition deed,
        string rewardPath,
        EditorWindow sourceWindow)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Match match = RewardPathPattern.Match(rewardPath);

        if (!match.Success ||
            !int.TryParse(match.Groups[1].Value, out int rankIndex) ||
            !int.TryParse(match.Groups[2].Value, out int rewardIndex))
        {
            return;
        }

        DeedRewardPreview reward = GetReward(deed, rankIndex, rewardIndex);

        if (reward == null || reward.Kind != DeedRewardKind.GeneralTrait)
            return;

        RoKForgeDeedTraitWindow window =
            CreateInstance<RoKForgeDeedTraitWindow>();

        window.deed = deed;
        window.rankIndex = rankIndex;
        window.rewardIndex = rewardIndex;
        window.rewardPath = rewardPath;
        window.originalReward = reward;
        window.originalTrait = reward.GeneralTrait;
        window.sourceWindow = sourceWindow;

        window.draft =
            RoKForgeDraftSession.CreateTemporary<GeneralTraitDefinition>();

        window.draft.name = string.Empty;
        window.titleContent = new GUIContent("Create & Assign General Trait");
        window.minSize = new Vector2(550f, 440f);
        window.ShowUtility();
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        ReleaseBinding();
        root.Clear();

        if (draft == null)
        {
            root.Add(new HelpBox(
                "The trait draft is no longer available.",
                HelpBoxMessageType.Info));

            root.Add(new Button(CloseAndReturn) { text = "Close" });
            return;
        }

        root.style.paddingLeft = 12;
        root.style.paddingRight = 12;
        root.style.paddingTop = 8;
        root.style.paddingBottom = 8;

        ScrollView scroll = new();
        scroll.style.flexGrow = 1;
        scroll.style.minHeight = 0;
        root.Add(scroll);

        scroll.Add(RoKForgeGeneralTraitEditor.Create(scroll, draft, null));

        requirements = new HelpBox(string.Empty, HelpBoxMessageType.Info);
        root.Add(requirements);

        saveError = new HelpBox(string.Empty, HelpBoxMessageType.Error);
        saveError.style.display = DisplayStyle.None;
        root.Add(saveError);

        VisualElement actions = new();
        actions.style.flexDirection = FlexDirection.Row;
        actions.style.justifyContent = Justify.FlexEnd;

        actions.Add(new Button(CloseAndReturn) { text = "Cancel" });

        saveButton = new Button(SaveAndAssign) { text = "Save & Assign" };
        actions.Add(saveButton);
        root.Add(actions);

        trackedDraft = new SerializedObject(draft);
        root.TrackSerializedObjectValue(trackedDraft, _ => RefreshSaveState());
        RefreshSaveState();
    }

    private void RefreshSaveState()
    {
        string issue = GetDraftIssue();

        requirements.text = issue ?? string.Empty;
        requirements.style.display = issue == null
            ? DisplayStyle.None
            : DisplayStyle.Flex;

        saveButton.SetEnabled(issue == null);
    }

    private string GetDraftIssue()
    {
        if (draft == null)
            return "Trait-utkastet finns inte längre.";

        if (string.IsNullOrWhiteSpace(draft.DisplayName))
            return "Ange ett namn för traiten.";

        return RoKForgeGeneralTraitEditor.GetEffectIssue(draft);
    }

    private void SaveAndAssign()
    {
        saveError.style.display = DisplayStyle.None;

        if (GetDraftIssue() != null)
        {
            RefreshSaveState();
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ShowSaveError("Avsluta Play Mode innan du sparar traiten.");
            return;
        }

        if (!HasOriginalReward())
        {
            ShowSaveError(
                "Reward-raden har ändrats. Öppna formuläret igen från den aktuella raden.");
            return;
        }

        GeneralTraitDefinition saved = null;
        string savedPath = null;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create and Assign General Trait");

        try
        {
            saved = (GeneralTraitDefinition)RoKForgeDraftSession.SaveCopy(
                draft, draft.DisplayName, TraitFolder);

            savedPath = AssetDatabase.GetAssetPath(saved);

            // Import callbacks can run during SaveCopy.
            if (!HasOriginalReward())
                throw new InvalidOperationException("Reward-raden ändrades under sparningen.");

            using SerializedObject serialized = new(deed);

            SerializedProperty target =
                serialized.FindProperty(rewardPath)?.FindPropertyRelative("generalTrait");

            if (target == null || target.objectReferenceValue != originalTrait)
                throw new InvalidOperationException("Trait-länken kunde inte uppdateras.");

            target.objectReferenceValue = saved;

            if (!serialized.ApplyModifiedProperties())
                throw new InvalidOperationException("Trait-länken kunde inte sparas.");

            if (AssetDatabase.Contains(deed))
                AssetDatabase.SaveAssetIfDirty(deed);

            Undo.CollapseUndoOperations(undoGroup);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);

            if (!string.IsNullOrEmpty(savedPath) &&
                AssetDatabase.LoadMainAssetAtPath(savedPath) == saved)
            {
                AssetDatabase.DeleteAsset(savedPath);
            }

            Debug.LogException(exception);
            ShowSaveError(exception.Message);
            return;
        }

        CloseAndReturn();
    }

    private bool HasOriginalReward()
    {
        DeedRewardPreview current = GetReward(deed, rankIndex, rewardIndex);

        return current != null &&
               ReferenceEquals(current, originalReward) &&
               current.Kind == DeedRewardKind.GeneralTrait &&
               current.GeneralTrait == originalTrait;
    }

    private static DeedRewardPreview GetReward(
        DeedDefinition deed,
        int rankIndex,
        int rewardIndex)
    {
        if (deed == null || deed.Ranks == null || rankIndex >= deed.Ranks.Count)
            return null;

        IReadOnlyList<DeedRewardPreview> rewards = deed.Ranks[rankIndex]?.Rewards;

        if (rewards == null || rewardIndex >= rewards.Count)
            return null;

        return rewards[rewardIndex];
    }

    private void ShowSaveError(string message)
    {
        saveError.text = message;
        saveError.style.display = DisplayStyle.Flex;
    }

    private void CloseAndReturn()
    {
        EditorWindow previous = sourceWindow;
        Close();

        if (previous != null)
        {
            previous.Focus();
            previous.Repaint();
        }
    }

    private void ReleaseBinding()
    {
        if (trackedDraft == null)
            return;

        rootVisualElement.Unbind();
        trackedDraft.Dispose();
        trackedDraft = null;
    }

    private void OnDisable()
    {
        ReleaseBinding();
        RoKForgeDraftSession.DiscardTemporary(draft);
        draft = null;
    }
}

#endif
