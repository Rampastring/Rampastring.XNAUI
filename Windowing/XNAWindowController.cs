using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Rampastring.XNAUI.Windowing;

/// <summary>
/// Interface for a control that acts as a parent for XNAUI sub-windows.
/// </summary>
public interface IWindowParentControl
{
    void AddChild(XNAControl child);
    void RemoveChild(XNAControl child);
    void AddCallback(Delegate d, params object[] args);

    event EventHandler RenderResolutionChanged;

    WindowManager WindowManager { get; }

    void SetAutoUpdateChildOrder(bool value);
}

public interface IWindow
{
    event EventHandler<InputEventArgs> LeftClick;
    event EventHandler InteractedWith;
    event EventHandler Closed;

    IWindowController WindowController { get; set; }

    EventHandler<InputEventArgs> FocusSwitchEventHandler { get; set; }
    int DrawOrder { get; set; }
    int UpdateOrder { get; set; }

    bool Visible { get; }
    bool Enabled { get; }

    void Disable();
    void CenterOnParent();
}

public interface IWindowController
{
    bool IsWindowForeground(XNAControl window);
}

/// <summary>
/// Base XNAUI window controller. Manages child windows and their draw and update order hierarchy.
/// </summary>
public class XNAWindowController : IWindowController
{
    public const int ChildWindowOrderValue = 10000;

    public XNAWindowController(IWindowParentControl windowParentControl)
    {
        WindowParentControl = windowParentControl;
    }

    protected IWindowParentControl WindowParentControl { get; }

    protected List<XNAControl> Windows { get; } = new List<XNAControl>();

    protected XNAControl ForegroundWindow { get; set; }

    public bool IsWindowForeground(XNAControl window) => ForegroundWindow == window;

    public void RegisterWindow<T>(T window) where T : XNAControl, IWindow
    {
        if (Windows.Contains(window))
            throw new InvalidOperationException("The given window is already registered in this window controller.");

        Windows.Add(window);
        window.WindowController = this;
        window.DrawOrder = ChildWindowOrderValue;
        window.UpdateOrder = ChildWindowOrderValue;
        window.LeftClick += Window_HandleFocusSwitch;
        window.InteractedWith += Window_HandleFocusSwitch;
        window.Closed += Window_Closed;
        WindowParentControl.AddChild(window);

        AddFocusSwitchHandlerToChildrenRecursive(window, window);
        window.Disable();

        // Center on next frame because child addition (and initialization) can be delayed
        // if windowParentControl is currently evaluating its children
        WindowParentControl.AddCallback(() => window.CenterOnParent());
    }

    public void UnregisterWindow(XNAControl window)
    {
        if (Windows.Remove(window))
        {
            window.DrawOrder = ChildWindowOrderValue;
            window.UpdateOrder = ChildWindowOrderValue;
            window.LeftClick -= Window_HandleFocusSwitch;
            ((IWindow)window).InteractedWith -= Window_HandleFocusSwitch;
            ((IWindow)window).Closed -= Window_Closed;
            RemoveFocusSwitchHandlerFromChildrenRecursive((IWindow)window, window);

            if (ForegroundWindow == window)
                SelectNewForegroundWindow();

            window.Kill();
            WindowParentControl.RemoveChild(window);
        }
    }

    /// <summary>
    /// Handles window focus switching.
    /// </summary>
    private void Window_HandleFocusSwitch(object sender, EventArgs e)
    {
        var window = (XNAControl)sender;

        if (ForegroundWindow != window)
        {
            WindowParentControl.SetAutoUpdateChildOrder(false);

            for (int i = 0; i < Windows.Count; i++)
            {
                if (Windows[i] != window)
                {
                    Windows[i].UpdateOrder--;
                    Windows[i].DrawOrder--;
                }
            }

            WindowParentControl.SetAutoUpdateChildOrder(true);

            ForegroundWindow = window;

            ForegroundWindow.UpdateOrder = ChildWindowOrderValue + Windows.Count;
            ForegroundWindow.DrawOrder = ChildWindowOrderValue + Windows.Count;
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        if (ForegroundWindow != sender)
            return;

        SelectNewForegroundWindow();
    }

    private void SelectNewForegroundWindow()
    {
        ForegroundWindow = null;

        int highestUpdateOrder = int.MinValue;
        XNAControl newForegroundWindow = null;

        for (int i = Windows.Count - 1; i >= 0; i--)
        {
            var window = Windows[i];
            if (!window.Visible || !window.Enabled)
                continue;

            if (window.UpdateOrder > highestUpdateOrder)
            {
                highestUpdateOrder = window.UpdateOrder;
                newForegroundWindow = window;
            }
        }

        if (newForegroundWindow != null)
        {
            ForegroundWindow = newForegroundWindow;
        }
    }

    private void AddFocusSwitchHandlerToChildrenRecursive(IWindow window, XNAControl control)
    {
        EventHandler<InputEventArgs> eventHandler = (s, e) => Window_HandleFocusSwitch(window, EventArgs.Empty);
        window.FocusSwitchEventHandler = eventHandler;

        foreach (var child in control.Children)
        {
            child.MouseLeftDown += eventHandler;
            child.LeftClick += eventHandler;
            AddFocusSwitchHandlerToChildrenRecursive(window, child);
        }
    }

    private void RemoveFocusSwitchHandlerFromChildrenRecursive(IWindow window, XNAControl control)
    {
        var eventHandler = window.FocusSwitchEventHandler;

        foreach (var child in control.Children)
        {
            child.MouseLeftDown -= eventHandler;
            child.LeftClick -= eventHandler;
            RemoveFocusSwitchHandlerFromChildrenRecursive(window, child);
        }

        window.FocusSwitchEventHandler = null;
    }

    public virtual void Clear()
    {
        var windowsCopy = new List<XNAControl>(Windows);

        foreach (var window in windowsCopy)
        {
            UnregisterWindow(window);
        }

        var properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            if (typeof(XNAWindow).IsAssignableFrom(property.PropertyType))
            {
                property.SetValue(this, null, BindingFlags.SetProperty | BindingFlags.NonPublic, null, null, null);
            }
        }

        ForegroundWindow = null;
    }
}
