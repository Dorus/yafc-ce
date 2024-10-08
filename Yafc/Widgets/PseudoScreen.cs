﻿using System;
using System.Numerics;
using SDL2;
using Yafc.UI;

namespace Yafc;

// Pseudo screen is not an actual screen, it is a panel shown in the middle of the main screen
public abstract class PseudoScreen : IKeyboardFocus {
    public readonly ImGui contents;
    protected readonly float width;
    protected bool opened;
    /// <summary>
    /// If not <see langword="null"/>, called after the panel is closed to let the inheriting object perform required (cleanup) actions.
    /// </summary>
    protected Action? cleanupCallback;

    protected PseudoScreen(float width = 40f) {
        this.width = width;
        contents = new ImGui(Build, ImGuiUtils.DefaultScreenPadding) {
            boxColor = SchemeColor.PureBackground
        };
    }

    public virtual void Open() => opened = true;
    public abstract void Build(ImGui gui);

    public void Build(ImGui gui, Vector2 screenSize) {
        if (gui.isBuilding) {
            var contentSize = contents.CalculateState(width, gui.pixelsPerUnit);
            var position = (screenSize - contentSize) / 2;
            Rect rect = new Rect(position, contentSize);
            gui.DrawPanel(rect, contents);
            gui.DrawRectangle(rect, SchemeColor.None, RectangleBorder.Full);
        }
    }

    protected void BuildHeader(ImGui gui, string? text, bool closeButton = true) {
        gui.BuildText(text, new TextBlockDisplayStyle(Font.header, Alignment: RectAlignment.Middle));
        if (closeButton) {
            Rect closeButtonRect = new Rect(width - 3f, 0f, 3f, 2f);
            if (gui.isBuilding) {
                bool isOver = gui.IsMouseOver(closeButtonRect);
                Rect closeButtonCenter = Rect.Square(closeButtonRect.Center, 1f);
                gui.DrawIcon(closeButtonCenter, Icon.Close, isOver ? SchemeColor.ErrorText : SchemeColor.BackgroundText);
            }
            if (gui.BuildButton(closeButtonRect, SchemeColor.None, SchemeColor.Error)) {
                Close();
            }
        }
    }

    /// <summary>Closes the pseudo screen</summary>
    /// <remarks>NOTE: Do not abuse this virtual function for cleaning up, instead use <see cref="cleanupCallback"/>.</remarks>
    protected virtual void Close() {
        cleanupCallback?.Invoke();

        opened = false;
        _ = InputSystem.Instance.SetDefaultKeyboardFocus(null);
        InputSystem.Instance.SetKeyboardFocus(null);
        MainScreen.Instance.ClosePseudoScreen(this);
    }

    public void Rebuild() => contents.Rebuild();

    public virtual bool KeyDown(SDL.SDL_Keysym key) {
        if (key.scancode == SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE) {
            Close();
        }
        if (key.scancode is SDL.SDL_Scancode.SDL_SCANCODE_RETURN or SDL.SDL_Scancode.SDL_SCANCODE_RETURN2 or SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER) {
            ReturnPressed();
        }

        return true;
    }

    protected virtual void ReturnPressed() { }

    public virtual bool TextInput(string input) => true;

    public virtual bool KeyUp(SDL.SDL_Keysym key) => true;

    public virtual void FocusChanged(bool focused) { }
    public virtual void Activated() => Rebuild();
}

/// <summary>
/// Represents a panel that can generate a result. (But doesn't have to, if the user selects a close or cancel button.)
/// </summary>
/// <typeparam name="T">The type of result the panel can generate.</typeparam>
public abstract class PseudoScreenWithResult<T>(float width = 40f) : PseudoScreen(width) {
    /// <summary>
    /// If not <see langword="null"/>, called after the panel is closed. The parameters are <c>hasResult</c> and <c>result</c>: If a result is available, the first parameter will
    /// be <see langword="true"/>, and the second parameter will have the result. The result may be <see langword="null"/>, depending on the kind of panel that was displayed.
    /// </summary>
    /// <remarks>Note that the <see cref="PseudoScreen.cleanupCallback"/> will be called after invoking this callback.</remarks>
    protected Action<bool, T?>? completionCallback;

    protected void CloseWithResult(T? result) {
        completionCallback?.Invoke(true, result);
        base.Close();
    }

    protected override void Close() {
        completionCallback?.Invoke(false, default);
        base.Close();
    }
}
