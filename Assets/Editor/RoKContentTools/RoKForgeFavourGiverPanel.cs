#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal static class RoKForgeFavourGiverPanel
{
    internal static VisualElement Create(FavourData favour)
    {
        Foldout panel = new()
        {
            text = "Favour Givers",
            value = true
        };

        panel.style.marginTop = 10;
        panel.style.marginBottom = 10;

        if (!AssetDatabase.Contains(favour))
        {
            panel.Add(new HelpBox(
                "Save the Favour to link it to a giver.",
                HelpBoxMessageType.Info));
            return panel;
        }

        Label status = new();
        status.style.whiteSpace = WhiteSpace.Normal;

        AddPicker<GameObject>(
            panel, favour, "NPC / World Object", true, LinkWorldObject, status);

        AddPicker<ItemData>(
            panel, favour, "Item", false, LinkItem, status);

        panel.Add(status);
        return panel;
    }

    private static void AddPicker<T>(
        VisualElement panel,
        FavourData favour,
        string label,
        bool allowSceneObjects,
        Func<T, FavourData, string> link,
        Label status) where T : UnityEngine.Object
    {
        ObjectField picker = new(label)
        {
            objectType = typeof(T),
            allowSceneObjects = allowSceneObjects
        };

        Button button = new(() =>
        {
            T target = picker.value as T;

            EditorApplication.delayCall += () =>
            {
                if (target == null || favour == null ||
                    !AssetDatabase.Contains(favour) ||
                    EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                try
                {
                    status.text = link(target, favour);
                }
                catch (InvalidOperationException exception)
                {
                    status.text = exception.Message;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    status.text = "Could not link the Favour. See the Console.";
                }
            };
        })
        {
            text = $"Link Favour to {label}"
        };

        button.SetEnabled(false);
        button.style.marginBottom = 6;

        picker.RegisterValueChangedCallback(evt =>
        {
            status.text = string.Empty;
            button.SetEnabled(evt.newValue != null);
        });

        panel.Add(picker);
        panel.Add(button);
    }

    private static string LinkWorldObject(GameObject target, FavourData favour)
    {
        if (EditorUtility.IsPersistent(target))
            return LinkPrefab(target, favour);

        if (!target.scene.IsValid() || !target.scene.isLoaded)
            throw new InvalidOperationException("Choose an object in an open scene.");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Link Favour Giver");

        try
        {
            GameObject owner = ConfigureGiver(target, favour, recordUndo: true);

            EditorSceneManager.MarkSceneDirty(owner.scene);
            Undo.CollapseUndoOperations(undoGroup);

            return $"Linked to {owner.name}. Save the scene or Prefab Mode.";
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
    }

    private static string LinkPrefab(GameObject prefab, FavourData favour)
    {
        string path = AssetDatabase.GetAssetPath(prefab);

        if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
            !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(path) != prefab)
        {
            throw new InvalidOperationException(
                "Choose an editable prefab root from the Project window.");
        }

        if (PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == path)
        {
            throw new InvalidOperationException(
                "This prefab is open in Prefab Mode. Pick its root from the Hierarchy.");
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        try
        {
            GameObject owner = ConfigureGiver(contents, favour, recordUndo: false);
            string ownerName = owner.name;

            PrefabUtility.SaveAsPrefabAsset(contents, path, out bool saved);

            if (!saved)
                throw new InvalidOperationException("Unity could not save the prefab.");

            return $"Linked to {ownerName} and saved the prefab.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static GameObject ConfigureGiver(
        GameObject target,
        FavourData favour,
        bool recordUndo)
    {
        InteractionTarget interaction = FindInteractionTarget(target);

        using SerializedObject interactionData = new(interaction);
        SerializedProperty ownerProperty = interactionData.FindProperty("interactionOwner");

        GameObject owner = ResolveOwner(interaction, ownerProperty);

        if (owner.scene != target.scene)
        {
            throw new InvalidOperationException(
                "The interaction owner must belong to the same scene or prefab.");
        }

        FavourGiver giver = owner.GetComponent<FavourGiver>();

        if (giver == null)
        {
            giver = recordUndo
                ? Undo.AddComponent<FavourGiver>(owner)
                : owner.AddComponent<FavourGiver>();
        }

        using SerializedObject giverData = new(giver);
        AppendFavour(giverData.FindProperty("favours"), favour);

        SerializedProperty marker = giverData.FindProperty("markerPrefab");

        if (marker.objectReferenceValue == null)
            marker.objectReferenceValue = FavourGiver.FindDefaultMarkerPrefab();

        ApplyChanges(giverData, recordUndo);

        if (ownerProperty.objectReferenceValue == null &&
            interaction.transform.parent == null)
        {
            // Keep a root interaction bound to its own object even
            // when the prefab is later instantiated under a parent.
            ownerProperty.objectReferenceValue = owner;
            ApplyChanges(interactionData, recordUndo);
        }

        return owner;
    }

    private static InteractionTarget FindInteractionTarget(GameObject target)
    {
        InteractionTarget direct = target.GetComponent<InteractionTarget>();

        if (direct != null)
            return direct;

        foreach (InteractionTarget candidate in
                 target.GetComponentsInChildren<InteractionTarget>(true))
        {
            using SerializedObject serialized = new(candidate);

            if (ResolveOwner(candidate, serialized.FindProperty("interactionOwner")) == target)
                return candidate;
        }

        throw new InvalidOperationException(
            "Choose an NPC or world object with a configured Interaction Target.");
    }

    private static GameObject ResolveOwner(
        InteractionTarget interaction,
        SerializedProperty ownerProperty)
    {
        if (ownerProperty.objectReferenceValue is GameObject configuredOwner &&
            configuredOwner != null)
        {
            return configuredOwner;
        }

        Transform parent = interaction.transform.parent;
        return parent != null ? parent.gameObject : interaction.gameObject;
    }

    private static string LinkItem(ItemData item, FavourData favour)
    {
        if (!AssetDatabase.Contains(item))
            throw new InvalidOperationException("Choose a saved ItemData asset.");

        using SerializedObject serialized = new(item);
        SerializedProperty settings = serialized.FindProperty("favourUse");

        AppendFavour(settings.FindPropertyRelative("favours"), favour);
        settings.FindPropertyRelative("enabled").boolValue = true;

        if (serialized.ApplyModifiedProperties())
            AssetDatabase.SaveAssetIfDirty(item);

        return $"Linked to {item.DisplayName}. Favour Interaction is enabled.";
    }

    private static void AppendFavour(SerializedProperty favours, FavourData favour)
    {
        if (favours == null || !favours.isArray)
            throw new InvalidOperationException("The giver has no Favour list.");

        for (int i = 0; i < favours.arraySize; i++)
        {
            if (favours.GetArrayElementAtIndex(i).objectReferenceValue == favour)
                return;
        }

        int index = favours.arraySize;
        favours.arraySize++;
        favours.GetArrayElementAtIndex(index).objectReferenceValue = favour;
    }

    private static void ApplyChanges(SerializedObject serialized, bool recordUndo)
    {
        if (recordUndo)
            serialized.ApplyModifiedProperties();
        else
            serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

#endif
