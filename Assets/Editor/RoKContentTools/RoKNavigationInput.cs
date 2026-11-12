
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Browser-like navigation input for The Forge.
///
/// Handles:
/// - Backspace: Back
/// - Alt + Left: Back
/// - Alt + Right: Forward
/// - Mouse XButton1: Back
/// - Mouse XButton2: Forward
///
/// Keyboard shortcuts never override text editing.
///
/// Does not own navigation history.
/// </summary>
public sealed class RoKForgeNavigationInput : IDisposable
{
    private VisualElement root;
    private EditorWindow owner;

    private Action navigateBack;
    private Action navigateForward;

#if UNITY_EDITOR_WIN

    private const int VK_XBUTTON1 = 0x05;
    private const int VK_XBUTTON2 = 0x06;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(
        int virtualKey
    );

    private bool backWasDown;
    private bool forwardWasDown;

#endif

    // =====================================================
    // BIND
    // =====================================================

    public void Bind(
        EditorWindow window,
        VisualElement rootElement,
        Action back,
        Action forward)
    {
        Dispose();

        owner = window;
        root = rootElement;

        navigateBack = back;
        navigateForward = forward;

        if (root == null)
            return;

        // Capture keyboard shortcuts before child
        // UI controls process them.

        root.RegisterCallback<KeyDownEvent>(
            OnKeyDown,
            TrickleDown.TrickleDown
        );

#if UNITY_EDITOR_WIN

        // Unity Editor does not reliably forward
        // extra mouse buttons through UI Toolkit.
        //
        // Poll only the two side mouse buttons,
        // and only respond inside The Forge.

        EditorApplication.update += PollMouseButtons;

#else

        // Fallback for macOS/Linux where extra
        // mouse button events are delivered.

        root.RegisterCallback<MouseDownEvent>(
            OnMouseDown,
            TrickleDown.TrickleDown
        );

#endif
    }

    // =====================================================
    // KEYBOARD SHORTCUTS
    // =====================================================

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt == null || !evt.altKey || IsEditingText())
            return;

        bool back = evt.keyCode == KeyCode.LeftArrow;
        bool forward = evt.keyCode == KeyCode.RightArrow;

        if (!back && !forward)
            return;

        evt.StopImmediatePropagation();
        evt.PreventDefault();

        if (back)
            navigateBack?.Invoke();
        else
            navigateForward?.Invoke();
    }

    // =====================================================
    // TEXT FIELD PROTECTION
    // =====================================================

    private bool IsEditingText()
    {
        if (root == null)
            return false;

        VisualElement focused =
            root.focusController?.focusedElement
                as VisualElement;

        while (focused != null)
        {
            if (focused is TextField ||
                focused is IntegerField ||
                focused is FloatField ||
                focused is DoubleField ||
                focused is LongField ||
                focused is IMGUIContainer)
            {
                return true;
            }

            focused = focused.parent;
        }

        return false;
    }

    // =====================================================
    // WINDOWS MOUSE SIDE BUTTONS
    // =====================================================

#if UNITY_EDITOR_WIN

    private void PollMouseButtons()
    {
        if (owner == null)
            return;

        // Only navigate when The Forge is focused
        // AND the cursor is inside its window.
        //
        // This prevents interference with other
        // Unity windows and external applications.

        bool active =
            EditorWindow.focusedWindow == owner &&
            EditorWindow.mouseOverWindow == owner;

        if (!active)
        {
            backWasDown = false;
            forwardWasDown = false;
            return;
        }

        bool backDown =
            (GetAsyncKeyState(VK_XBUTTON1) &
                0x8000) != 0;

        bool forwardDown =
            (GetAsyncKeyState(VK_XBUTTON2) &
                0x8000) != 0;

        // Only navigate once per physical press.

        if (backDown && !backWasDown)
        {
            navigateBack?.Invoke();
        }
        else if (forwardDown && !forwardWasDown)
        {
            navigateForward?.Invoke();
        }

        backWasDown = backDown;
        forwardWasDown = forwardDown;
    }

#else

    // =====================================================
    // NON-WINDOWS MOUSE FALLBACK
    // =====================================================

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt == null)
            return;

        // Standard extra mouse button indices.
        // Availability depends on Unity/platform.

        if (evt.button != 3 && evt.button != 4)
            return;

        evt.StopImmediatePropagation();
        evt.PreventDefault();

        if (evt.button == 3)
            navigateBack?.Invoke();
        else
            navigateForward?.Invoke();
    }

#endif

    // =====================================================
    // CLEANUP
    // =====================================================

    public void Dispose()
    {
        if (root != null)
        {
            root.UnregisterCallback<KeyDownEvent>(
                OnKeyDown,
                TrickleDown.TrickleDown
            );

#if !UNITY_EDITOR_WIN

            root.UnregisterCallback<MouseDownEvent>(
                OnMouseDown,
                TrickleDown.TrickleDown
            );

#endif
        }

#if UNITY_EDITOR_WIN

        EditorApplication.update -= PollMouseButtons;

        backWasDown = false;
        forwardWasDown = false;

#endif

        root = null;
        owner = null;

        navigateBack = null;
        navigateForward = null;
    }
}

#endif

