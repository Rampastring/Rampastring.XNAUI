using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI.FontManagement;
using Rampastring.XNAUI.Input;
using System;
using System.Text;
using System.Text.RegularExpressions;
using TextCopy;

namespace Rampastring.XNAUI.XNAControls;

/// <summary>
/// A text input control.
/// </summary>
public class XNATextBox : XNAControl
{
    protected const int SELECTION_MARGIN = 2;
    protected const int TEXT_HORIZONTAL_MARGIN = 3;
    protected const int TEXT_VERTICAL_MARGIN = 2;
    protected const double CURSOR_SCROLL_REPEAT_TIME = 0.05;
    protected const double CURSOR_FAST_SCROLL_THRESHOLD = 20;
    protected const double BAR_ON_TIME = 0.5;
    protected const double BAR_OFF_TIME = 0.5;

    /// <summary>
    /// Creates a new text box.
    /// </summary>
    /// <param name="windowManager">The WindowManager that will be associated with this control.</param>
    public XNATextBox(WindowManager windowManager) : base(windowManager)
    {
        Height = UISettings.ActiveSettings.TextBoxDefaultHeight.GetValueOrDefault((int)FontManager.GetTextDimensions("Test String @@", FontIndex).Y + 4);
        HandledMouseInputs = MouseInputFlags.LeftMouseButton;
        HandlesDragging = true;
    }

    /// <summary>
    /// Raised when the user presses the Enter key while this text box is the
    /// selected control.
    /// </summary>
    public event EventHandler EnterPressed;

    /// <summary>
    /// Raised when the text box receives input that changes its text.
    /// </summary>
    public event EventHandler InputReceived;

    /// <summary>
    /// Raised whenever the text of the text box is changed, by the user or
    /// programmatically.
    /// </summary>
    public event EventHandler TextChanged;

    private Color? _textColor;

    /// <summary>
    /// The color of the text in the text box.
    /// </summary>
    public virtual Color TextColor
    {
        get
        {
            return _textColor ?? UISettings.ActiveSettings.AltColor;
        }
        set { _textColor = value; }
    }

    private Color? _idleBorderColor;

    /// <summary>
    /// The color of the text box border when the text box is inactive.
    /// </summary>
    public virtual Color IdleBorderColor
    {
        get
        {
            return _idleBorderColor ?? UISettings.ActiveSettings.PanelBorderColor;
        }
        set { _idleBorderColor = value; }
    }

    private Color? _activeBorderColor;

    /// <summary>
    /// The color of the text box border when the text box is selected.
    /// </summary>
    public Color ActiveBorderColor
    {
        get
        {
            return _activeBorderColor ?? UISettings.ActiveSettings.AltColor;
        }
        set { _activeBorderColor = value; }
    }

    private Color? _backColor;

    /// <summary>
    /// The color of the text box background.
    /// </summary>
    public Color BackColor
    {
        get
        {
            return _backColor ?? UISettings.ActiveSettings.BackgroundColor;
        }
        set { _backColor = value; }
    }

    private Color? _selectionColor;

    /// <summary>
    /// The color of the selection rectangle drawn behind selected text.
    /// </summary>
    public Color SelectionColor
    {
        get
        {
            return _selectionColor ?? UISettings.ActiveSettings.SelectionColor;
        }
        set { _selectionColor = value; }
    }

    /// <summary>
    /// The index of the spritefont that this textbox uses.
    /// </summary>
    public int FontIndex { get; set; }

    /// <summary>
    /// The maximum length of the text of this text box, in characters.
    /// </summary>
    public int MaximumTextLength { get; set; } = int.MaxValue;

    /// <summary>
    /// The text on the text box.
    /// </summary>
    public override string Text
    {
        get
        {
            return text;
        }

        set
        {
            text = value ?? throw new InvalidOperationException("XNATextBox text cannot be set to null.");
            InputPosition = 0;
            TextStartPosition = 0;
            UnselectText();

            if (text.Length > MaximumTextLength)
                text = text.Substring(0, MaximumTextLength);

            TextEndPosition = text.Length;

            while (!TextFitsBox())
            {
                TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);

                if (TextEndPosition <= TextStartPosition)
                {
                    TextEndPosition = TextStartPosition;
                    break;
                }
            }

            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private string text = string.Empty;
    private string savedText = string.Empty;

    /// <summary>
    /// The input character index inside the textbox text.
    /// </summary>
    public int InputPosition { get; set; }

    private int selectionStartPosition = -1;

    /// <summary>
    /// Start position of current text selection. -1 for none.
    /// </summary>
    public int SelectionStartPosition
    {
        get => selectionStartPosition;
        [Obsolete("Use SetSelection() method instead")]
        set => selectionStartPosition = value;
    }

    private int selectionEndPosition = -1;
    /// <summary>
    /// End position of current text selection. -1 for none.
    /// </summary>
    public int SelectionEndPosition
    {
        get => selectionEndPosition;
        [Obsolete("Use SetSelection() method instead")]
        set => selectionEndPosition = value;
    }

    /// <summary>
    /// Calculates and returns the length of the currently selected piece of text.
    /// </summary>
    public int SelectionLength => Math.Max(0, SelectionEndPosition - SelectionStartPosition);

    /// <summary>
    /// Checks whether the current text selection is valid.
    /// </summary>
    public bool IsValidSelection() => SelectionStartPosition > -1 && SelectionEndPosition > 0 && SelectionStartPosition < text.Length && SelectionEndPosition <= text.Length && SelectionEndPosition > SelectionStartPosition;

    /// <summary>
    /// Sets the text selection range.
    /// </summary>
    /// <param name="start">The new value of <see cref="SelectionStartPosition"/>.</param>
    /// <param name="end">The new value of <see cref="SelectionEndPosition"/>.</param>
    public void SetSelection(int start, int end)
    {
        if (start < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Selection start position is out of range.");
        }

        if (end < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "Selection end position is out of range.");
        }

        if ((start == -1) != (end == -1))
        {
            throw new ArgumentException("Only one of start or end is -1. Both must be -1 or both must be valid positions.");
        }

        if (start != -1 && end != -1 && end < start)
        {
            Logger.Log("Selection end position must be greater than or equal to start position. Unselecting text.");

#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif

            selectionStartPosition = -1;
            selectionEndPosition = -1;

            return;
        }

        if (end != -1 && end > text.Length)
        {
            Logger.Log("Selection end position is out of range. Unselecting text.");

#if DEBUG
            System.Diagnostics.Debugger.Break();
#endif

            selectionStartPosition = -1;
            selectionEndPosition = -1;

            return;
        }

        selectionStartPosition = start;
        selectionEndPosition = end;
    }

    /// <summary>
    /// Unselects current text selection.
    /// </summary>
    public void UnselectText() => SetSelection(start: -1, end: -1);

    /// <summary>
    /// The start character index of the visible part of the text string.
    /// </summary>
    public int TextStartPosition { get; set; }

    /// <summary>
    /// The end character index of the visible part of the text string.
    /// </summary>
    public int TextEndPosition { get; set; }

    /// <summary>
    /// Can be set to disable IME (Input Method Editor) support for this control.
    /// </summary>
    public bool IMEDisabled { get; set; }

    // TODO PreviousControl and NextControl should be implemented at XNAControl level,
    // but we currently lack a generic way to handle input in a selected control
    // (without having all controls subscribe to Keyboard.OnKeyPressed, which could
    // have bad effects on performance)

    /// <summary>
    /// The control to switch selection state to when
    /// the user presses Tab while holding Shift.
    /// See also <see cref="NextControl"/>.
    /// </summary>
    public XNAControl PreviousControl { get; set; }

    /// <summary>
    /// The control to switch selection state to when
    /// the user presses Tab without holding Shift.
    /// See also <see cref="PreviousControl"/>.
    /// </summary>
    public XNAControl NextControl { get; set; }

    protected TimeSpan barTimer = TimeSpan.Zero;

    private TimeSpan scrollKeyTime = TimeSpan.Zero;
    private TimeSpan timeSinceLastScroll = TimeSpan.Zero;
    private bool isScrollingQuickly = false;

    private Point mouseDownPosition;
    private int mouseDownCharacterIndex;
    private bool isMouseLocked;
    private DateTime lastMouseScrollTime = DateTime.MinValue;

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        if (key == nameof(MaximumTextLength))
        {
            MaximumTextLength = Conversions.IntFromString(value, MaximumTextLength);
            return;
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Initializes the text box.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

#if !XNA
        Game.Window.TextInput += Window_TextInput;
#else
        KeyboardEventInput.CharEntered += KeyboardEventInput_CharEntered;
#endif
        Keyboard.OnKeyDown += Keyboard_OnKeyDown;

        InitializeIME();
    }

    private void InitializeIME()
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
        {
            WindowManager.IMEHandler.RegisterXNATextBox(this, HandleCharInput);

            TextChanged += (sender, e) =>
            {
                WindowManager.IMEHandler.OnTextChanged(this);
            };
        }
    }

    private void DeinitializeIME()
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
            WindowManager.IMEHandler.KillXNATextBox(this);
    }

    public override void Kill()
    {
#if !XNA
        Game.Window.TextInput -= Window_TextInput;
#else
        KeyboardEventInput.CharEntered -= KeyboardEventInput_CharEntered;
#endif
        Keyboard.OnKeyDown -= Keyboard_OnKeyDown;

        DeinitializeIME();

        base.Kill();
    }

#if XNA
    private void KeyboardEventInput_CharEntered(object sender, KeyboardEventArgs e)
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
        {
            if (WindowManager.IMEHandler.HandleCharInput(this, e.Character))
                return;
        }

        HandleCharInput(e.Character);
    }
#else
    private void Window_TextInput(object sender, TextInputEventArgs e)
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
        {
            if (WindowManager.IMEHandler.HandleCharInput(this, e.Character))
                return;
        }

        HandleCharInput(e.Character);
    }
#endif

    private int GetNextCharacterBoundary(int position)
    {
        if (position >= text.Length)
            return text.Length;
        if (position < 0)
            return 0;
        if (position < text.Length - 1 && char.IsHighSurrogate(text[position]) && char.IsLowSurrogate(text[position + 1]))
            return position + 2;
        return position + 1;
    }

    private int GetPreviousCharacterBoundary(int position)
    {
        if (position <= 0)
            return 0;
        if (position > text.Length)
            position = text.Length;
        if (position > 1 && char.IsLowSurrogate(text[position - 1]) && char.IsHighSurrogate(text[position - 2]))
            return position - 2;
        return position - 1;
    }

    private int EnsureCharacterBoundary(int position)
    {
        if (position <= 0 || position >= text.Length)
            return position;
        if (char.IsLowSurrogate(text[position]) && position > 0 && char.IsHighSurrogate(text[position - 1]))
            return position - 1;
        return position;
    }

    private void HandleCharInput(char character)
    {
        if (WindowManager.SelectedControl != this || !Enabled || !Parent.Enabled || !WindowManager.HasFocus)
            return;

        // Don't allow control characters (asc 0-31 + 127)
        if (char.IsControl(character))
            return;

        // Don't allow typing characters that don't exist in the spritefont
        if (Renderer.GetSafeString(character.ToString(), FontIndex) != character.ToString())
        {
            InputReceived?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!AllowCharacterInput(character))
        {
            InputReceived?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!IsValidSelection())
        {
            if (Text.Length >= MaximumTextLength)
            {
                InputReceived?.Invoke(this, EventArgs.Empty);
                return;
            }

            InputPosition = EnsureCharacterBoundary(InputPosition);

            text = text.Insert(InputPosition, character.ToString());
            InputPosition++;

            if (InputPosition > TextEndPosition)
            {
                TextEndPosition = InputPosition;

                while (!TextFitsBox())
                    TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
            }

            while (TextFitsBox() && TextEndPosition < text.Length)
            {
                TextEndPosition = GetNextCharacterBoundary(TextEndPosition);
            }

            if (!TextFitsBox())
            {
                TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);
            }
        }
        else
        {
            text = text.Substring(0, SelectionStartPosition) + character.ToString() + text.Substring(SelectionEndPosition);
            InputPosition = SelectionStartPosition + 1;
            UnselectText();

            TextStartPosition = Math.Min(TextStartPosition, text.Length);
            TextEndPosition = Math.Min(TextEndPosition, text.Length);

            if (TextStartPosition > 0 && TextFitsBox())
            {
                while (TextFitsBox() && TextStartPosition > 0)
                {
                    TextStartPosition = GetPreviousCharacterBoundary(TextStartPosition);
                }

                if (TextStartPosition > 0)
                    TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
            }
        }

        TextChanged?.Invoke(this, EventArgs.Empty);

        barTimer = TimeSpan.Zero;

        InputReceived?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Determines if the user is allowed to type a specific character into the textbox.
    /// </summary>
    /// <param name="character">The character.</param>
    protected virtual bool AllowCharacterInput(char character)
    {
        // Allow all characters by default
        return true;
    }

    private void Keyboard_OnKeyDown(object sender, KeyPressEventArgs e)
    {
        if (WindowManager.SelectedControl != this || !Enabled || !Parent.Enabled || !WindowManager.HasFocus)
            return;

        e.Handled = HandleKeyPress(e.PressedKey);
    }

    /// <summary>
    /// Handles a key press while the text box is the selected control.
    /// Can be overridden in derived classes to handle additional key presses.
    /// Returns true if the key was handled.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    protected virtual bool HandleKeyPress(Keys key)
    {
        switch (key)
        {
            case Keys.Home:
                if (text.Length != 0)
                {
                    if (Keyboard.IsShiftHeldDown())
                    {
                        if (!IsValidSelection())
                        {
                            SetSelection(start: 0, end: InputPosition);
                        }
                        else
                        {
                            SetSelection(start: 0, end: SelectionEndPosition);
                        }
                    }
                    else
                    {
                        UnselectText();
                    }

                    TextStartPosition = 0;
                    TextEndPosition = 0;
                    InputPosition = 0;

                    while (TextEndPosition < text.Length)
                    {
                        int nextPos = GetNextCharacterBoundary(TextEndPosition);
                        if (nextPos > text.Length)
                            break;

                        TextEndPosition = nextPos;

                        if (!TextFitsBox())
                        {
                            TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);
                            break;
                        }
                    }
                }

                return true;
            case Keys.End:
                TextEndPosition = text.Length;

                if (Keyboard.IsShiftHeldDown())
                {
                    if (!IsValidSelection())
                        SetSelection(start: InputPosition, end: text.Length);
                    else
                        SetSelection(start: SelectionStartPosition, end: text.Length);
                }
                else
                {
                    UnselectText();
                }

                InputPosition = text.Length;
                TextStartPosition = 0;

                while (!TextFitsBox())
                {
                    TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
                }

                return true;
            case Keys.X:
                if (!Keyboard.IsCtrlHeldDown())
                    break;

                if (!IsValidSelection())
                    break;

                ClipboardService.SetText(text.Substring(SelectionStartPosition, SelectionLength));
                int newInputPosition = SelectionStartPosition;
                Text = text.Substring(0, SelectionStartPosition) + text.Substring(SelectionEndPosition);
                InputPosition = newInputPosition;
                if (TextEndPosition < InputPosition)
                {
                    TextEndPosition = InputPosition;
                    while (!TextFitsBox())
                        TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
                }

                InputReceived?.Invoke(this, EventArgs.Empty);

                return true;
            case Keys.V:
                if (!Keyboard.IsCtrlHeldDown())
                    break;

                string clipboardText = ClipboardService.GetText();
                if (clipboardText == null)
                    return true;

                // Replace newlines with spaces, invalid font chars with ?
                // https://stackoverflow.com/questions/238002/replace-line-breaks-in-a-string-c-sharp
                string textToAdd = Regex.Replace(clipboardText, @"\r\n?|\n", " ");
                textToAdd = Renderer.GetSafeString(textToAdd, FontIndex);

                // Trim pasted text to fit MaximumTextLength
                string fullText = text.Substring(0, InputPosition) + textToAdd + text.Substring(InputPosition);
                if (fullText.Length > MaximumTextLength)
                {
                    int availableSpace = MaximumTextLength - (text.Length - (IsValidSelection() ? SelectionLength : 0));
                    textToAdd = textToAdd.Substring(0, Math.Min(textToAdd.Length, Math.Max(0, availableSpace)));
                }

                if (IsValidSelection())
                {
                    text = text.Substring(0, SelectionStartPosition) + textToAdd + text.Substring(SelectionEndPosition);

                    InputPosition = SelectionStartPosition + textToAdd.Length;
                    UnselectText();

                    if (TextEndPosition < InputPosition)
                        TextEndPosition = InputPosition;
                    else
                        TextEndPosition = Math.Min(TextEndPosition, text.Length);

                    TextStartPosition = Math.Min(TextStartPosition, text.Length);
                    if (TextStartPosition < TextEndPosition)
                        TextStartPosition = Math.Max(TextEndPosition - 1, 0);

                    // Replacing part of the string with another string might open up space for displaying more characters.
                    // For correct behaviour, we need to look for them at both the beginning and end of the string.
                    if (text.Length > 0)
                    {
                        if (TextFitsBox())
                        {
                            // If the text fits, then show more characters from the beginning.
                            while (TextFitsBox() && TextStartPosition > 0)
                                TextStartPosition = GetPreviousCharacterBoundary(TextStartPosition);

                            // If the start position is not at the beginning in this case, we have over-scrolled by one character.
                            if (TextStartPosition > 0)
                            {
                                TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
                            }
                            else
                            {
                                // If we have not overscrolled, we reached the beginning of the string.
                                // See if we could show more of the string in the end, too.
                                while (TextEndPosition < text.Length && TextFitsBox())
                                    TextEndPosition = GetNextCharacterBoundary(TextEndPosition);

                                if (!TextFitsBox())
                                    TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);
                            }
                        }
                        else
                        {
                            // If the text does not fit, most likely the replacement operation added to the overall string length
                            // and now the text is too long. Cut it from the beginning as much as necessary.
                            while (!TextFitsBox())
                                TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
                        }
                    }
                }
                else
                {
                    text = text.Substring(0, InputPosition) + textToAdd + text.Substring(InputPosition);
                    InputPosition = InputPosition + textToAdd.Length;

                    if (TextEndPosition < InputPosition)
                    {
                        TextEndPosition = InputPosition;

                        // If we have to display more characters at the end for the input position to be visible,
                        // then check whether we should hide characters from the front.
                        while (!TextFitsBox())
                            TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
                    }

                    // Since we added text to the string, display more of the string at the end - as much as possible.
                    // Avoid displaying more than possible, though.
                    bool scrolled = false;
                    while (true)
                    {
                        if (TextFitsBox())
                        {
                            if (TextEndPosition < text.Length)
                            {
                                scrolled = true;
                                TextEndPosition = GetNextCharacterBoundary(TextEndPosition);
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (scrolled)
                                TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);

                            break;
                        }
                    }
                }

                TextChanged?.Invoke(this, EventArgs.Empty); // we are changing text not Text, so invoke TextChanged
                InputReceived?.Invoke(this, EventArgs.Empty);

                return true;
            case Keys.C:
                if (!Keyboard.IsCtrlHeldDown())
                    break;

                if (!IsValidSelection())
                    break;

                ClipboardService.SetText(text.Substring(SelectionStartPosition, SelectionLength));

                return true;
            case Keys.A:
                if (!Keyboard.IsCtrlHeldDown())
                    break;

                SetSelection(start: 0, end: text.Length);
                return true;
            case Keys.Enter:
                if (!IMEDisabled && WindowManager.IMEHandler != null)
                {
                    if (WindowManager.IMEHandler.HandleEnterKey(this))
                        return true;
                }

                EnterPressed?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Escape:
                if (!IMEDisabled && WindowManager.IMEHandler != null)
                {
                    if (WindowManager.IMEHandler.HandleEscapeKey(this))
                        return true;
                }

                UnselectText();
                InputPosition = 0;
                Text = string.Empty;
                InputReceived?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Tab:
                UnselectText();

                if (Keyboard.IsShiftHeldDown())
                {
                    if (PreviousControl != null)
                        WindowManager.SelectedControl = PreviousControl;
                }
                else if (NextControl != null)
                {
                    WindowManager.SelectedControl = NextControl;
                }

                return true;
        }

        return false;
    }

    private bool TextFitsBox()
    {
        if (string.IsNullOrEmpty(text))
            return true;

        int safeStart = EnsureCharacterBoundary(TextStartPosition);
        int safeEnd = EnsureCharacterBoundary(TextEndPosition);

        if (safeStart >= safeEnd)
            return true;

        return FontManager.GetTextDimensions(
                    text.Substring(safeStart, safeEnd - safeStart),
                    FontIndex).X < Width - TEXT_HORIZONTAL_MARGIN * 2;
    }

    protected void ResetMouseTracking()
    {
        isMouseLocked = false;
        mouseDownCharacterIndex = 0;
    }

    private void UpdateCursorState()
    {
        if (isMouseLocked)
        {
            if (!InputEnabled || !Cursor.LeftDown)
            {
                isMouseLocked = false;
                return;
            }

            Point cursorPoint = GetCursorPoint();
            if (cursorPoint == mouseDownPosition)
            {
                return;
            }

            if (!IsActive)
            {
                // The user has moved the cursor outside of the text box.
                // Most likely, this means that they want to scroll the text to select a wider part of it.
                // Check if we can scroll the view.
                // However, don't scroll too often.

                bool shorterInterval = cursorPoint.X < -CURSOR_FAST_SCROLL_THRESHOLD || cursorPoint.X > Width + CURSOR_FAST_SCROLL_THRESHOLD;

                if ((DateTime.Now - lastMouseScrollTime).TotalSeconds > CURSOR_SCROLL_REPEAT_TIME / (shorterInterval ? 2 : 1))
                {
                    lastMouseScrollTime = DateTime.Now;

                    if (cursorPoint.X <= 0 && TextStartPosition > 0)
                    {
                        TextStartPosition = GetPreviousCharacterBoundary(TextStartPosition);

                        while (!TextFitsBox())
                            TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);
                    }
                    else if (cursorPoint.X >= Width && TextEndPosition < Text.Length)
                    {
                        TextEndPosition = GetNextCharacterBoundary(TextEndPosition);

                        while (!TextFitsBox())
                            TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
                    }
                }
            }

            // Determine where the user has moved the cursor and select characters accordingly.
            // Default to last position as it is where we will end up if the cursor is all the way on the edge of the visible text.
            int newPosition = TextEndPosition;

            var tb = new StringBuilder();

            for (int i = TextStartPosition; i < TextEndPosition;)
            {
                int nextBoundary = GetNextCharacterBoundary(i);
                if (nextBoundary > TextEndPosition)
                    break;

                tb.Append(text.Substring(i, nextBoundary - i));

                if (FontManager.GetTextDimensions(tb.ToString(), FontIndex).X +
                    TEXT_HORIZONTAL_MARGIN > cursorPoint.X)
                {
                    newPosition = i;
                    break;
                }

                i = nextBoundary;
            }

            int smaller = Math.Min(mouseDownCharacterIndex, newPosition);
            int larger = Math.Max(mouseDownCharacterIndex, newPosition);
            InputPosition = newPosition;
            SetSelection(start: smaller, end: larger);
        }
        else
        {
            if (Cursor.LeftPressedDown)
            {
                UnselectText();

                if (IsActive)
                {
                    isMouseLocked = true;
                    mouseDownPosition = GetCursorPoint();

                    int inputPosition = TextEndPosition;

                    var tb = new StringBuilder();

                    for (int i = TextStartPosition; i < TextEndPosition;)
                    {
                        int nextBoundary = GetNextCharacterBoundary(i);
                        if (nextBoundary > TextEndPosition)
                            break;

                        tb.Append(text.Substring(i, nextBoundary - i));

                        if (FontManager.GetTextDimensions(tb.ToString(), FontIndex).X +
                            TEXT_HORIZONTAL_MARGIN > mouseDownPosition.X)
                        {
                            inputPosition = i;
                            break;
                        }

                        i = nextBoundary;
                    }

                    InputPosition = Math.Max(0, inputPosition);
                    mouseDownCharacterIndex = InputPosition;
                    barTimer = TimeSpan.Zero;
                }
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        barTimer += gameTime.ElapsedGameTime;

        if (barTimer > TimeSpan.FromSeconds(BAR_ON_TIME + BAR_OFF_TIME))
            barTimer -= TimeSpan.FromSeconds(BAR_ON_TIME + BAR_OFF_TIME);

        if (WindowManager.SelectedControl == this)
        {
            if (Keyboard.IsKeyHeldDown(Keys.Left))
            {
                HandleScrollKeyDown(gameTime, ScrollLeft);
            }
            else if (Keyboard.IsKeyHeldDown(Keys.Right))
            {
                HandleScrollKeyDown(gameTime, ScrollRight);
            }
            else if (Keyboard.IsKeyHeldDown(Keys.Delete))
            {
                HandleScrollKeyDown(gameTime, DeleteCharacter);
            }
            else if (Keyboard.IsKeyHeldDown(Keys.Back))
            {
                HandleScrollKeyDown(gameTime, Backspace);
            }
            else
            {
                isScrollingQuickly = false;
                timeSinceLastScroll = TimeSpan.Zero;
                scrollKeyTime = TimeSpan.Zero;
            }
        }

        UpdateCursorState();
    }

    private int HowManyCharactersToScrollLeft()
    {
        if (!Keyboard.IsCtrlHeldDown())
            return GetPreviousCharacterBoundary(InputPosition) - InputPosition;

        if (InputPosition < 2)
            return InputPosition;

        int targetPos = InputPosition;

        // Take as many spaces from the beginning as we can find.
        while (targetPos > 0)
        {
            int prevPos = GetPreviousCharacterBoundary(targetPos);
            if (text[prevPos] != ' ')
                break;

            targetPos = prevPos;
        }

        // Now do the opposite - we want to take a word, so accept all characters that are not spaces.
        while (targetPos > 0)
        {
            int prevPos = GetPreviousCharacterBoundary(targetPos);
            if (text[prevPos] == ' ')
                break;

            targetPos = prevPos;
        }

        return InputPosition - targetPos;
    }

    private void ScrollLeft()
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
        {
            if (WindowManager.IMEHandler.HandleScrollLeftKey(this))
                return;
        }

        if (IsValidSelection())
        {
            if (Keyboard.IsShiftHeldDown())
            {
                int howMany = HowManyCharactersToScrollLeft();

                if (InputPosition == 0)
                    return;

                InputPosition = Math.Max(0, InputPosition - howMany);

                if (InputPosition < SelectionStartPosition)
                {
                    SetSelection(start: InputPosition, end: SelectionEndPosition);
                }
                else if (InputPosition < SelectionEndPosition)
                {
                    SetSelection(start: SelectionStartPosition, end: InputPosition);
                }
            }
            else
            {
                InputPosition = SelectionStartPosition;
                UnselectText();
            }
        }
        else
        {
            int howMany = HowManyCharactersToScrollLeft();

            if (InputPosition == 0)
                return;

            int oldPos = InputPosition;

            if (Keyboard.IsCtrlHeldDown())
            {
                InputPosition = Math.Max(0, InputPosition - howMany);
            }
            else
            {
                InputPosition = GetPreviousCharacterBoundary(InputPosition);
            }

            if (Keyboard.IsShiftHeldDown())
            {
                SetSelection(start: InputPosition, end: InputPosition + howMany);
            }
        }

        if (InputPosition < TextStartPosition)
        {
            TextStartPosition = InputPosition;

            while (!TextFitsBox())
                TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);
        }
    }

    private int HowManyCharactersToScrollRight()
    {
        if (!Keyboard.IsCtrlHeldDown())
            return GetNextCharacterBoundary(InputPosition) - InputPosition;

        if (InputPosition >= text.Length - 1)
            return text.Length - InputPosition;

        int targetPos = InputPosition;

        // This has opposite operation order from scrolling to the left.
        // Windows text boxes act the same.

        // We want to take a word, so accept all characters that are not spaces.
        while (targetPos < text.Length)
        {
            if (text[targetPos] == ' ')
                break;

            targetPos = GetNextCharacterBoundary(targetPos);
        }

        // Take as many spaces from the end as we can find.
        while (targetPos < text.Length)
        {
            if (text[targetPos] != ' ')
                break;

            targetPos = GetNextCharacterBoundary(targetPos);
        }

        return targetPos - InputPosition;
    }

    private void ScrollRight()
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
        {
            if (WindowManager.IMEHandler.HandleScrollRightKey(this))
                return;
        }

        if (IsValidSelection())
        {
            if (Keyboard.IsShiftHeldDown())
            {
                int howMany = HowManyCharactersToScrollRight();

                if (InputPosition >= text.Length)
                    return;

                InputPosition = Math.Min(text.Length, InputPosition + howMany);

                if (InputPosition > SelectionEndPosition)
                {
                    SetSelection(start: SelectionStartPosition, end: InputPosition);
                }
                else if (InputPosition > SelectionStartPosition)
                {
                    SetSelection(start: InputPosition, end: SelectionEndPosition);
                }
            }
            else
            {
                InputPosition = SelectionEndPosition;
                UnselectText();
            }
        }
        else
        {
            int howMany = HowManyCharactersToScrollRight();

            if (InputPosition >= text.Length)
                return;

            int oldPos = InputPosition;

            if (Keyboard.IsCtrlHeldDown())
            {
                InputPosition = Math.Min(text.Length, InputPosition + howMany);
            }
            else
            {
                InputPosition = GetNextCharacterBoundary(InputPosition);
            }

            if (Keyboard.IsShiftHeldDown())
            {
                SetSelection(start: InputPosition - howMany, end: InputPosition);
            }
        }

        if (InputPosition > TextEndPosition)
        {
            TextEndPosition = InputPosition;

            while (!TextFitsBox())
            {
                TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
            }
        }
    }

    private void DeleteSelection()
    {
        if (IsValidSelection())
        {
            text = text.Remove(SelectionStartPosition, SelectionLength);

            InputPosition = SelectionStartPosition;
            UnselectText();

            if (TextEndPosition > text.Length)
            {
                int width = TextEndPosition - text.Length;
                TextStartPosition = Math.Max(0, TextStartPosition - width);
                TextEndPosition = text.Length;

                if (!TextFitsBox())
                    TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
            }

            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DeleteCharacter()
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
        {
            if (WindowManager.IMEHandler.HandleDeleteKey(this))
                return;
        }

        if (IsValidSelection())
        {
            DeleteSelection();
        }
        else if (text.Length > InputPosition)
        {
            InputPosition = EnsureCharacterBoundary(InputPosition);
            int nextBoundary = GetNextCharacterBoundary(InputPosition);
            int charsToDelete = nextBoundary - InputPosition;
            text = text.Remove(InputPosition, charsToDelete);

            // Adjust TextEndPosition if it's beyond the new text length
            if (TextEndPosition > text.Length)
                TextEndPosition = text.Length;

            // If we have space to show more text, expand the visible range
            if (TextStartPosition > 0 && TextFitsBox())
            {
                while (TextStartPosition > 0 && TextFitsBox())
                {
                    TextStartPosition = GetPreviousCharacterBoundary(TextStartPosition);
                }
                // If we went too far, step forward
                if (!TextFitsBox())
                    TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
            }
            // If we still don't fit after trying to expand backward, try expanding forward
            else
            {
                while (TextEndPosition < text.Length && TextFitsBox())
                {
                    int nextPos = GetNextCharacterBoundary(TextEndPosition);
                    if (nextPos > text.Length) break;
                    TextEndPosition = nextPos;
                }
                // If we went too far, step back
                if (!TextFitsBox())
                    TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);
            }

            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        InputReceived?.Invoke(this, EventArgs.Empty);
    }

    private void Backspace()
    {
        if (!IMEDisabled && WindowManager.IMEHandler != null)
        {
            if (WindowManager.IMEHandler.HandleBackspaceKey(this))
                return;
        }

        if (IsValidSelection())
        {
            DeleteSelection();
        }
        else if (text.Length > 0 && InputPosition > 0)
        {
            InputPosition = EnsureCharacterBoundary(InputPosition);
            int prevBoundary = GetPreviousCharacterBoundary(InputPosition);
            int charsToDelete = InputPosition - prevBoundary;
            text = text.Remove(prevBoundary, charsToDelete);
            InputPosition = prevBoundary;

            if (TextEndPosition > text.Length)
                TextEndPosition = text.Length;

            // If carat is now before the visible range, adjust TextStartPosition
            if (InputPosition < TextStartPosition)
            {
                TextStartPosition = InputPosition;
                // Recalculate TextEndPosition to fit as much text as possible
                while (TextEndPosition < text.Length && TextFitsBox())
                {
                    int nextPos = GetNextCharacterBoundary(TextEndPosition);
                    if (nextPos > text.Length) break;
                    TextEndPosition = nextPos;
                }
                // If we went too far, step back
                if (!TextFitsBox())
                    TextEndPosition = GetPreviousCharacterBoundary(TextEndPosition);
            }
            // If we have space to show more text, expand the visible range
            else if (TextStartPosition > 0 && TextFitsBox())
            {
                while (TextStartPosition > 0 && TextFitsBox())
                    TextStartPosition = GetPreviousCharacterBoundary(TextStartPosition);

                // If we went too far, step forward
                if (!TextFitsBox())
                    TextStartPosition = GetNextCharacterBoundary(TextStartPosition);
            }

            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        InputReceived?.Invoke(this, EventArgs.Empty);
    }

    public override void OnSelectedChanged()
    {
        // Note: IMEHandler.OnSelectedChanged() should be called even if this.IMEDisabled holds true
        WindowManager.IMEHandler?.OnSelectedChanged(this);

        if (WindowManager.SelectedControl != this)
            UnselectText();

        base.OnSelectedChanged();
    }

    private void HandleScrollKeyDown(GameTime gameTime, Action action)
    {
        if (scrollKeyTime.Equals(TimeSpan.Zero))
            action();

        scrollKeyTime += gameTime.ElapsedGameTime;

        if (isScrollingQuickly)
        {
            timeSinceLastScroll += gameTime.ElapsedGameTime;

            if (timeSinceLastScroll > TimeSpan.FromSeconds(XNAUIConstants.KEYBOARD_SCROLL_REPEAT_TIME))
            {
                timeSinceLastScroll = TimeSpan.Zero;
                action();
            }
        }

        if (scrollKeyTime > TimeSpan.FromSeconds(XNAUIConstants.KEYBOARD_FAST_SCROLL_TRIGGER_TIME) && !isScrollingQuickly)
        {
            isScrollingQuickly = true;
            timeSinceLastScroll = TimeSpan.Zero;
        }

        barTimer = TimeSpan.Zero;
    }

    public override void Draw(GameTime gameTime)
    {
        FillControlArea(BackColor);

        if (WindowManager.SelectedControl == this && Enabled && WindowManager.HasFocus)
            DrawRectangle(new Rectangle(0, 0, Width, Height), ActiveBorderColor);
        else
            DrawRectangle(new Rectangle(0, 0, Width, Height), IdleBorderColor);

        if (WindowManager.SelectedControl == this && Enabled && IsValidSelection())
        {
            int selectionStartX = TEXT_HORIZONTAL_MARGIN;
            int selectionWidth;

            int safeSelectionStart = EnsureCharacterBoundary(SelectionStartPosition);
            int safeSelectionEnd = EnsureCharacterBoundary(SelectionEndPosition);
            int safeTextStart = EnsureCharacterBoundary(TextStartPosition);
            int safeTextEnd = EnsureCharacterBoundary(TextEndPosition);

            if (safeSelectionStart > safeTextStart)
            {
                string textBeforeSelection = Text.Substring(safeTextStart, safeSelectionStart - safeTextStart);
                selectionStartX = (int)FontManager.GetTextDimensions(textBeforeSelection, FontIndex).X + TEXT_HORIZONTAL_MARGIN;
            }

            if (safeSelectionEnd > safeTextEnd)
            {
                selectionWidth = Width - selectionStartX - 1;
            }
            else
            {
                int startIndex = safeTextStart > safeSelectionStart ? safeTextStart : safeSelectionStart;
                int selectionDrawnLength = safeSelectionEnd - startIndex;
                string selectedText = Text.Substring(startIndex, selectionDrawnLength);
                selectionWidth = (int)FontManager.GetTextDimensions(selectedText, FontIndex).X + 1; // +1 due to shadow
            }

            FillRectangle(new Rectangle(selectionStartX, SELECTION_MARGIN, selectionWidth, Height - (SELECTION_MARGIN * 2)), SelectionColor);
        }

        int safeStartPos = EnsureCharacterBoundary(TextStartPosition);
        int safeEndPos = EnsureCharacterBoundary(TextEndPosition);

        if (safeStartPos < safeEndPos)
        {
            DrawStringWithShadow(Text.Substring(safeStartPos, safeEndPos - safeStartPos),
                FontIndex, new Vector2(TEXT_HORIZONTAL_MARGIN, TEXT_VERTICAL_MARGIN), TextColor);
        }

        if (WindowManager.SelectedControl == this && Enabled && WindowManager.HasFocus)
        {
            int safeInputPos = EnsureCharacterBoundary(InputPosition);
            if (safeInputPos >= safeStartPos)
            {
                int barLocationX = TEXT_HORIZONTAL_MARGIN;

                if (safeInputPos > safeStartPos)
                {
                    string inputText = Text.Substring(safeStartPos, safeInputPos - safeStartPos);
                    barLocationX += (int)FontManager.GetTextDimensions(inputText, FontIndex).X;
                }

                if (!IMEDisabled && WindowManager.IMEHandler != null)
                {
                    if (WindowManager.IMEHandler.GetDrawCompositionText(this, out string composition, out int compositionCursorPosition))
                    {
                        DrawString(composition, FontIndex, new(barLocationX, TEXT_VERTICAL_MARGIN), Color.Orange);
                        Vector2 measStr = FontManager.GetTextDimensions(composition.Substring(0, compositionCursorPosition), FontIndex);
                        barLocationX += (int)measStr.X;
                    }
                }

                if (barTimer.TotalSeconds < BAR_ON_TIME && !IsValidSelection())
                {
                    FillRectangle(new Rectangle(barLocationX, 2, 1, Height - 4), Color.White);
                }
            }
        }

        base.Draw(gameTime);
    }
}