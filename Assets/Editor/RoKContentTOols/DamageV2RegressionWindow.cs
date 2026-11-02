
#if UNITY_EDITOR

using System;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Read-only gameplay regression tester for Damage V2.
///
/// Uses real CharacterStats and DamageCalculator.
///
/// Temporary EffectDefinitions are created in memory.
/// No ScriptableObject assets are saved or modified.
///
/// No HP damage is applied.
/// Unity's random state is preserved.
/// </summary>
public sealed class DamageV2RegressionWindow :
    EditorWindow
{
    // =====================================================
    // CONFIGURATION
    // =====================================================

    [SerializeField]
    private CharacterStats attacker;

    [SerializeField]
    private CharacterStats defender;

    [SerializeField, Min(1)]
    private int baseDamage = 1000;

    [SerializeField]
    private int randomSeed = 12345;

    private string report =
        "Enter Play Mode and select a defender.";

    private Vector2 scrollPosition;

    // =====================================================
    // MENU
    // =====================================================

    [MenuItem(
        "RöK/Content Tools/Damage V2/Regression Tester"
    )]
    private static void Open()
    {
        DamageV2RegressionWindow window =
            GetWindow<DamageV2RegressionWindow>();

        window.titleContent =
            new GUIContent(
                "Damage V2 Tests",
                "Damage System V2 regression tests"
            );

        window.minSize =
            new Vector2(440f, 400f);

        window.Show();
    }

    // =====================================================
    // GUI
    // =====================================================

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Damage System V2",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Tests the real damage calculation " +
            "without applying damage to characters.\n\n" +
            "Temporary damage effects are activated " +
            "and removed automatically.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode before running tests.",
                MessageType.Warning
            );

            return;
        }

        if (attacker == null)
        {
            attacker =
                PlayerReference.Player;
        }

        attacker =
            (CharacterStats)
            EditorGUILayout.ObjectField(
                new GUIContent(
                    "Attacker",
                    "Character whose outgoing damage " +
                    "will be tested.\n\n" +
                    "Usually the Player."
                ),
                attacker,
                typeof(CharacterStats),
                true
            );

        defender =
            (CharacterStats)
            EditorGUILayout.ObjectField(
                new GUIContent(
                    "Defender",
                    "Character receiving the calculated " +
                    "damage.\n\n" +
                    "Use a Wolf with CreatureTypes " +
                    "Wolf | Beast to test creature bonuses."
                ),
                defender,
                typeof(CharacterStats),
                true
            );

        baseDamage =
            EditorGUILayout.IntField(
                new GUIContent(
                    "Base Damage",
                    "Raw input value before variation, " +
                    "armor, resistance and block.\n\n" +
                    "A large value makes percentage " +
                    "differences easier to observe.\n\n" +
                    "Does not change actual HP."
                ),
                baseDamage
            );

        baseDamage = Mathf.Max(
            1,
            baseDamage
        );

        randomSeed =
            EditorGUILayout.IntField(
                new GUIContent(
                    "Random Seed",
                    "Each comparison uses the same " +
                    "random sequence.\n\n" +
                    "This prevents damage variation " +
                    "from hiding the effect being tested."
                ),
                randomSeed
            );

        EditorGUILayout.Space();

        if (defender != null)
        {
            EditorGUILayout.LabelField(
                "Target Classification",
                defender.CreatureTypes.ToString()
            );

            CreatureIdentity identity =
                defender.GetComponent<
                    CreatureIdentity>();

            if (identity != null &&
                identity.Definition != null)
            {
                EditorGUILayout.LabelField(
                    "Creature Definition",
                    identity.Definition.DisplayName
                );
            }
        }

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(
            attacker == null ||
            defender == null ||
            attacker == defender
        );

        if (GUILayout.Button(
                "Run Damage V2 Regression",
                GUILayout.Height(35f)))
        {
            RunRegression();
        }

        EditorGUI.EndDisabledGroup();

        if (attacker != null &&
            attacker == defender)
        {
            EditorGUILayout.HelpBox(
                "Use two different characters " +
                "for this regression.",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Test Results",
            EditorStyles.boldLabel
        );

        scrollPosition =
            EditorGUILayout.BeginScrollView(
                scrollPosition
            );

        EditorGUILayout.TextArea(
            report,
            GUILayout.ExpandHeight(true)
        );

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button(
                "Copy Report"))
        {
            EditorGUIUtility.systemCopyBuffer =
                report;
        }
    }

    // =====================================================
    // REGRESSION
    // =====================================================

    private void RunRegression()
    {
        if (!Application.isPlaying ||
            attacker == null ||
            defender == null ||
            attacker == defender)
        {
            return;
        }

        StringBuilder output = new();

        output.AppendLine(
            "=== RöK DAMAGE V2 REGRESSION ==="
        );

        output.AppendLine(
            $"Attacker: {attacker.DisplayName}"
        );

        output.AppendLine(
            $"Defender: {defender.DisplayName}"
        );

        output.AppendLine(
            $"Creature Types: " +
            $"{defender.CreatureTypes}"
        );

        output.AppendLine();

        UnityEngine.Random.State originalState =
            UnityEngine.Random.state;

        CreatureDamageBonusEffectDefinition
            creatureEffect = null;

        DamageTypeBonusEffectDefinition
            typeEffect = null;

        DamageResistanceEffectDefinition
            resistanceEffect = null;

        try
        {
            // =============================================
            // TEST 1: PHYSICAL LEGACY COMPATIBILITY
            // =============================================

            DamageResult legacy =
                SampleLegacyPhysical();

            DamageResult explicitPhysical =
                Sample(DamageType.Physical);

            bool physicalMatches =
                legacy.damage ==
                explicitPhysical.damage &&
                legacy.isBlocked ==
                explicitPhysical.isBlocked &&
                legacy.blockedAmount ==
                explicitPhysical.blockedAmount;

            output.AppendLine(
                physicalMatches
                    ? "[PASS] Legacy Physical compatibility"
                    : "[FAIL] Legacy Physical compatibility"
            );

            output.AppendLine(
                $"  Legacy: {legacy.damage}"
            );

            output.AppendLine(
                $"  Explicit: {explicitPhysical.damage}"
            );

            output.AppendLine();

            // =============================================
            // TEST 2: CREATURE BONUS
            // =============================================

            creatureEffect =
                CreateEffect<
                    CreatureDamageBonusEffectDefinition>(
                    so =>
                    {
                        so.FindProperty("targetTypes")
                            .intValue =
                            (int)CreatureType.Beast;

                        so.FindProperty("bonus")
                            .floatValue = 0.50f;
                    }
                );

            if ((defender.CreatureTypes &
                 CreatureType.Beast) != 0)
            {
                TestModifier(
                    output,
                    "Creature Damage +50% vs Beast",
                    creatureEffect,
                    attacker,
                    DamageType.Physical,
                    true
                );
            }
            else
            {
                output.AppendLine(
                    "[SKIP] Creature Damage: " +
                    "Target is not classified as Beast."
                );

                output.AppendLine();
            }

            // =============================================
            // TEST 3: DAMAGE TYPE BONUS
            // =============================================

            typeEffect =
                CreateEffect<
                    DamageTypeBonusEffectDefinition>(
                    so =>
                    {
                        so.FindProperty("damageType")
                            .enumValueIndex =
                            (int)DamageType.Fire;

                        so.FindProperty("bonus")
                            .floatValue = 0.50f;
                    }
                );

            TestModifier(
                output,
                "Fire Damage +50%",
                typeEffect,
                attacker,
                DamageType.Fire,
                true
            );

            // =============================================
            // TEST 4: DAMAGE RESISTANCE
            // =============================================

            resistanceEffect =
                CreateEffect<
                    DamageResistanceEffectDefinition>(
                    so =>
                    {
                        so.FindProperty("damageType")
                            .enumValueIndex =
                            (int)DamageType.Fire;

                        so.FindProperty("resistance")
                            .floatValue = 0.25f;
                    }
                );

            TestModifier(
                output,
                "Fire Resistance +25%",
                resistanceEffect,
                defender,
                DamageType.Fire,
                false
            );

            // =============================================
            // CHECK ACTUAL HP
            // =============================================

            output.AppendLine(
                "No TakeDamage() calls were made."
            );

            output.AppendLine(
                "Combat state, contribution and " +
                "loot were not exercised."
            );
        }
        catch (Exception exception)
        {
            output.AppendLine(
                "[ERROR] " + exception
            );

            Debug.LogException(
                exception
            );
        }
        finally
        {
            UnityEngine.Random.state =
                originalState;

            DestroyTemporary(creatureEffect);
            DestroyTemporary(typeEffect);
            DestroyTemporary(resistanceEffect);
        }

        report = output.ToString();

        Debug.Log(report);

        Repaint();
    }

    // =====================================================
    // TEST EFFECT APPLICATION
    // =====================================================

    private void TestModifier(
        StringBuilder output,
        string testName,
        EffectDefinition definition,
        CharacterStats owner,
        DamageType damageType,
        bool expectIncrease)
    {
        DamageResult baseline =
            Sample(damageType);

        CharacterEffectController controller =
            CharacterEffectController.GetOrCreate(
                owner
            );

        if (controller == null)
        {
            output.AppendLine(
                $"[FAIL] {testName}: " +
                "Missing effect controller."
            );

            return;
        }

        EffectHandle handle = null;

        int modifiedDamage = 0;

        bool activated = false;

        try
        {
            handle = controller.Activate(
                new EffectDefinition[]
                {
                    definition
                },
                DamageSourceContext
                    .FromDirectSource(attacker),
                ModifierSourceType.Buff,
                1
            );

            activated =
                handle != null &&
                handle.IsActive;

            if (activated)
            {
                modifiedDamage =
                    Sample(damageType).damage;
            }
        }
        finally
        {
            if (handle != null)
            {
                controller.Deactivate(
                    handle
                );
            }
        }

        DamageResult restored =
            Sample(damageType);

        bool cleanupCorrect =
            restored.damage ==
            baseline.damage;

        bool directionCorrect =
            expectIncrease
                ? modifiedDamage > baseline.damage
                : modifiedDamage < baseline.damage;

        bool inconclusive =
            baseline.damage <= 0;

        string status;

        if (!activated || !cleanupCorrect)
        {
            status = "FAIL";
        }
        else if (inconclusive)
        {
            status = "SKIP";
        }
        else if (directionCorrect)
        {
            status = "PASS";
        }
        else
        {
            status = "CHECK";
        }

        output.AppendLine(
            $"[{status}] {testName}"
        );

        output.AppendLine(
            $"  Baseline: {baseline.damage}"
        );

        output.AppendLine(
            $"  Modified: {modifiedDamage}"
        );

        output.AppendLine(
            $"  Restored: {restored.damage}"
        );

        output.AppendLine(
            $"  Cleanup: " +
            (cleanupCorrect ? "PASS" : "FAIL")
        );

        if (status == "CHECK")
        {
            output.AppendLine(
                "  Inspect existing resistance, " +
                "effect configuration and " +
                "damage rounding."
            );
        }

        if (status == "SKIP")
        {
            output.AppendLine(
                "  Baseline damage was zero. " +
                "Choose a non-immune target."
            );
        }

        output.AppendLine();
    }

    // =====================================================
    // DETERMINISTIC DAMAGE SAMPLES
    // =====================================================

    private DamageResult Sample(
        DamageType damageType)
    {
        UnityEngine.Random.InitState(
            randomSeed
        );

        return DamageCalculator
            .CalculateDamageAfterSuccessfulHit(
                baseDamage,
                0f,
                1f,
                attacker,
                defender,
                false,
                damageType,
                DamageSourceContext
                    .FromDirectSource(attacker)
            );
    }

    private DamageResult SampleLegacyPhysical()
    {
        UnityEngine.Random.InitState(
            randomSeed
        );

        return DamageCalculator
            .CalculateDamageAfterSuccessfulHit(
                baseDamage,
                0f,
                1f,
                attacker,
                defender,
                false
            );
    }

    // =====================================================
    // TEMPORARY EFFECT FACTORY
    // =====================================================

    private static T CreateEffect<T>(
        Action<SerializedObject> configure)
        where T : EffectDefinition
    {
        T definition =
            ScriptableObject.CreateInstance<T>();

        definition.hideFlags =
            HideFlags.HideAndDontSave;

        SerializedObject serialized =
            new SerializedObject(definition);

        configure(serialized);

        serialized.ApplyModifiedPropertiesWithoutUndo();

        return definition;
    }

    private static void DestroyTemporary(
        EffectDefinition definition)
    {
        if (definition != null)
        {
            DestroyImmediate(
                definition
            );
        }
    }
}

#endif

