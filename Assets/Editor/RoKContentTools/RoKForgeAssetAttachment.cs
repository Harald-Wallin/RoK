#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

internal static class RoKForgeAssetAttachment
{
    internal static ScriptableObject CreateAndAttach(
        ScriptableObject owner,
        string propertyName,
        Type assetType,
        string assetName,
        string folder,
        string undoLabel)
    {
        if (owner == null || !AssetDatabase.Contains(owner))
            throw new InvalidOperationException("Save the parent asset first.");

        using SerializedObject serialized = new(owner);
        FindAttachmentProperty(serialized, propertyName);

        ScriptableObject temporary = null;
        string createdPath = null;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoLabel);

        try
        {
            temporary = RoKForgeDraftSession.CreateTemporary(assetType);

            ScriptableObject created = RoKForgeDraftSession.SaveCopy(
                temporary, assetName, folder);

            createdPath = AssetDatabase.GetAssetPath(created);

            serialized.Update();
            SerializedProperty reference =
                FindAttachmentProperty(serialized, propertyName);

            if (reference.isArray)
            {
                int index = reference.arraySize;
                reference.arraySize++;
                reference = reference.GetArrayElementAtIndex(index);
            }

            if (reference.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException("The field cannot hold an asset reference.");

            reference.objectReferenceValue = created;

            if (reference.objectReferenceValue != created ||
                !serialized.ApplyModifiedProperties())
            {
                throw new InvalidOperationException("The asset could not be attached.");
            }

            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(undoGroup);

            return created;
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);

            if (!string.IsNullOrEmpty(createdPath) &&
                AssetDatabase.LoadMainAssetAtPath(createdPath) != null)
            {
                AssetDatabase.DeleteAsset(createdPath);
            }

            throw;
        }
        finally
        {
            RoKForgeDraftSession.DiscardTemporary(temporary);
        }
    }

    private static SerializedProperty FindAttachmentProperty(
        SerializedObject serialized,
        string propertyName)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null ||
            (!property.isArray &&
             property.propertyType != SerializedPropertyType.ObjectReference))
        {
            throw new InvalidOperationException($"Invalid attachment field: {propertyName}.");
        }

        if (!property.isArray && property.objectReferenceValue != null)
            throw new InvalidOperationException("This field already has a linked asset.");

        return property;
    }
}

#endif
