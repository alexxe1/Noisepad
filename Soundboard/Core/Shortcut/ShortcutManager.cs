using SharpHook;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Soundboard.Core.Shortcut;

public class ShortcutManager : IDisposable
{
    private readonly IGlobalHook _hook;
    private readonly HashSet<KeyCode> _pressedKeys = new();
    private string? _lastShortcut = null;
    private bool _disposed;

    public static event Action<string>? ShortcutPressed;

    private static readonly HashSet<KeyCode> ModifierKeys = new()
    {
        KeyCode.VcLeftControl, KeyCode.VcRightControl,
        KeyCode.VcLeftAlt, KeyCode.VcRightAlt,
        KeyCode.VcLeftShift, KeyCode.VcRightShift
    };

    public ShortcutManager()
    {
        _hook = new SimpleGlobalHook();

        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;

        _hook.RunAsync();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        _pressedKeys.Add(e.Data.KeyCode);

        if (!ModifierKeys.Contains(e.Data.KeyCode))
        {
            string shortcut = FormatShortcut(_pressedKeys);

            if (shortcut != _lastShortcut)
            {
                _lastShortcut = shortcut;
                ShortcutPressed?.Invoke(shortcut);
            }
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        _pressedKeys.Remove(e.Data.KeyCode);

        _lastShortcut = null;
    }

    private string FormatShortcut(IEnumerable<KeyCode> keys)
    {
        var orderedKeys = keys
            .OrderBy(k => !ModifierKeys.Contains(k))
            .ThenBy(k => k.ToString())
            .Select(k => k.ToString().Replace("Vc", ""));

        return string.Join("+", orderedKeys);
    }

    public static string GetFormattedKey(KeyCode key)
    {
        return key.ToString().Replace("Vc", "");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        _hook.KeyPressed -= OnKeyPressed;
        _hook.KeyReleased -= OnKeyReleased;

        _hook.Dispose();

        GC.SuppressFinalize(this);
    }
}
