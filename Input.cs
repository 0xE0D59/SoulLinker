using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared;
using SharpDX;

namespace SoulLinker;

/// <summary>
/// Credits to Totalschaden for his input code.
/// </summary>
public static class Input
{
    public const int MouseeventfLeftdown = 0x02;
    public const int MouseeventfLeftup = 0x04;
    public const int MouseeventfRightdown = 0x0008;
    public const int MouseeventfRightup = 0x0010;
    
    private const int KeyeventfExtendedkey = 0x0001;
    private const int KeyeventfKeyup = 0x0002;
    private const string CoroutineKeyPress = "KeyPress";
    private static Coroutine _keyboardCoroutine;
    
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    public static void SetCursorPos(Vector2 vec)
    {
        SetCursorPos((int)vec.X, (int)vec.Y);
    }
    
    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    public static void LeftMouseDown()
    {
        mouse_event(MouseeventfLeftdown, 0, 0, 0, 0);
    }

    public static void LeftMouseUp()
    {
        mouse_event(MouseeventfLeftup, 0, 0, 0, 0);
    }

    public static void RightMouseDown()
    {
        mouse_event(MouseeventfRightdown, 0, 0, 0, 0);
    }

    public static void RightMouseUp()
    {
        mouse_event(MouseeventfRightup, 0, 0, 0, 0);
    }

    public static IEnumerator LeftClick()
    {
        LeftMouseDown();
        yield return new WaitTime(40);
        LeftMouseUp();
        yield return new WaitTime(100);
    }
        
    public static IEnumerator RightClick()
    {
        RightMouseDown();
        yield return new WaitTime(40);
        RightMouseUp();
        yield return new WaitTime(100);
    }
    
    [DllImport("user32.dll")]
    private static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool BlockInput(bool fBlockIt);

    public static void KeyDown(Keys key)
    {
        keybd_event((byte) key, 0, KeyeventfExtendedkey | 0, 0);
    }

    public static void KeyUp(Keys key)
    {
        keybd_event((byte) key, 0, KeyeventfExtendedkey | KeyeventfKeyup, 0); //0x7F
    }
    
    public static void KeyPress(Keys key)
    {
        _keyboardCoroutine = new Coroutine(KeyPressRoutine(key), SoulLinker.Instance, CoroutineKeyPress);
        Core.ParallelRunner.Run(_keyboardCoroutine);
    }
    
    private static IEnumerator KeyPressRoutine(Keys key)
    {
        KeyDown(key);
        yield return new WaitTime(20);
        KeyUp(key);
    }
}