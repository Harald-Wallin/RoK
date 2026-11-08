
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RoKContentStudioDiagnosticsPanel
{
    private RoKDiagnosticReport report;

    private ScriptableObject inspectedAsset;

    private string search = string.Empty;

    private Vector2 issueScroll;

    private bool showInfo = true;
    private bool showWarnings = true;
    private bool showErrors = true;

    // =====================================================
    // DRAW
    // =====================================================

    public void Draw(
        Action<ScriptableObject> openAsset,
        bool twoColumns = false)
    {
        EditorGUILayout.HelpBox(
            "Analyze all Content Studio assets.\n\n" +
            "The scan is read-only. " +
            "It does not modify gameplay data.",
            MessageType.Info
        );

        if (GUILayout.Button(
                "Run Content Audit",
                GUILayout.Height(32)))
        {
            report =
                RoKContentStudioDiagnostics.Scan();

            GUI.changed = true;
        }

        if (report == null)
        {
            EditorGUILayout.HelpBox(
                "Click Run Content Audit to begin.",
                MessageType.None
            );

            return;
        }

        RoKForgeFormLayout.DrawColumns(
        twoColumns,
        () =>
        {
            DrawSummary();
            EditorGUILayout.Space(12);
            DrawIssueList(openAsset);
        },
        () => DrawDependencies(openAsset));
        }

    // =====================================================
    // SUMMARY
    // =====================================================

    private void DrawSummary()
    {
        Section("Project Overview");

        EditorGUILayout.LabelField(
            "Scanned Assets",
            report.Assets.Count.ToString()
        );

        EditorGUILayout.LabelField(
            "Errors",
            report.Count(
                RoKDiagnosticSeverity.Error
            ).ToString()
        );

        EditorGUILayout.LabelField(
            "Warnings",
            report.Count(
                RoKDiagnosticSeverity.Warning
            ).ToString()
        );

        EditorGUILayout.LabelField(
            "Information",
            report.Count(
                RoKDiagnosticSeverity.Info
            ).ToString()
        );

        EditorGUILayout.HelpBox(
            "Errors: trasiga eller saknade referenser.\n" +
            "Warnings: misstänkta konfigurationer.\n" +
            "Info: saker som kan vara avsiktliga.\n\n" +
            "Resultatet är en ögonblicksbild. " +
            "Kör om analysen efter ändringar.",
            MessageType.None
        );
    }

    // =====================================================
    // ISSUES
    // =====================================================

    private void DrawIssueList(
        Action<ScriptableObject> openAsset)
    {
        Section("Validation Results");

        search =
            EditorGUILayout.TextField(
                "Search",
                search
            );

        EditorGUILayout.BeginHorizontal();

        showErrors =
            GUILayout.Toggle(
                showErrors,
                "Errors"
            );

        showWarnings =
            GUILayout.Toggle(
                showWarnings,
                "Warnings"
            );

        showInfo =
            GUILayout.Toggle(
                showInfo,
                "Info"
            );

        EditorGUILayout.EndHorizontal();

        issueScroll =
            EditorGUILayout.BeginScrollView(
                issueScroll,
                GUILayout.MaxHeight(320)
            );

        int visibleCount = 0;

        foreach (RoKDiagnosticIssue issue
                 in report.Issues)
        {
            if (!ShouldShow(issue))
                continue;

            visibleCount++;

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );

            EditorGUILayout.LabelField(
                $"{issue.Severity}: " +
                $"{issue.Asset.name}",
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                issue.Message,
                EditorStyles.wordWrappedLabel
            );

            if (GUILayout.Button(
                    "Inspect Asset",
                    GUILayout.Width(110)))
            {
                inspectedAsset = issue.Asset;

                ScriptableObject captured =
                    issue.Asset;

                EditorApplication.delayCall +=
                    () => openAsset?.Invoke(captured);
            }

            EditorGUILayout.EndVertical();
        }

        if (visibleCount == 0)
        {
            EditorGUILayout.HelpBox(
                "No matching issues.",
                MessageType.Info
            );
        }

        EditorGUILayout.EndScrollView();
    }

    private bool ShouldShow(
        RoKDiagnosticIssue issue)
    {
        if (issue == null ||
            issue.Asset == null)
        {
            return false;
        }

        switch (issue.Severity)
        {
            case RoKDiagnosticSeverity.Error:
                if (!showErrors) return false;
                break;

            case RoKDiagnosticSeverity.Warning:
                if (!showWarnings) return false;
                break;

            case RoKDiagnosticSeverity.Info:
                if (!showInfo) return false;
                break;
        }

        if (string.IsNullOrWhiteSpace(search))
            return true;

        return
            issue.Asset.name.IndexOf(
                search,
                StringComparison.OrdinalIgnoreCase
            ) >= 0 ||
            issue.Message.IndexOf(
                search,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }

    // =====================================================
    // DEPENDENCY INSPECTION
    // =====================================================

    private void DrawDependencies(
        Action<ScriptableObject> openAsset)
    {
        Section("Dependency Inspector");

        inspectedAsset =
            (ScriptableObject)
            EditorGUILayout.ObjectField(
                "Inspect Asset",
                inspectedAsset,
                typeof(ScriptableObject),
                false
            );

        if (GUILayout.Button(
                "Use Project Selection"))
        {
            inspectedAsset =
                Selection.activeObject
                    as ScriptableObject;
        }

        if (inspectedAsset == null)
        {
            EditorGUILayout.HelpBox(
                "Select an asset to inspect its references.",
                MessageType.None
            );

            return;
        }

        if (!report.References.ContainsKey(
                inspectedAsset))
        {
            EditorGUILayout.HelpBox(
                "This asset is not included in the " +
                "current Content Studio audit.\n\n" +
                "Run a new audit if the asset was " +
                "created after the previous scan.",
                MessageType.Warning
            );

            return;
        }

        DrawAssetLinks(
            "References (Uses)",
            report.GetReferences(inspectedAsset),
            openAsset
        );

        EditorGUILayout.Space(8);

        DrawAssetLinks(
            "Used By",
            report.GetUsers(inspectedAsset),
            openAsset
        );
    }

    // =====================================================
    // ASSET LINKS
    // =====================================================

    private void DrawAssetLinks(
        string title,
        IReadOnlyList<ScriptableObject> assets,
        Action<ScriptableObject> openAsset)
    {
        EditorGUILayout.LabelField(
            $"{title} ({assets.Count})",
            EditorStyles.boldLabel
        );

        if (assets.Count == 0)
        {
            EditorGUILayout.LabelField(
                "No references found.",
                EditorStyles.miniLabel
            );

            return;
        }

        foreach (ScriptableObject asset in assets)
        {
            if (asset == null)
                continue;

            EditorGUILayout.BeginHorizontal(
                EditorStyles.helpBox
            );

            EditorGUILayout.LabelField(
                asset.name
            );

            if (GUILayout.Button(
                    "Open",
                    GUILayout.Width(55)))
            {
                ScriptableObject captured =
                    asset;

                EditorApplication.delayCall +=
                    () => openAsset?.Invoke(captured);
            }

            if (GUILayout.Button(
                    "Ping",
                    GUILayout.Width(55)))
            {
                EditorGUIUtility.PingObject(
                    asset
                );
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    // =====================================================
    // PRESENTATION
    // =====================================================

    private static void Section(string title)
    {
        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );
    }
}

#endif

