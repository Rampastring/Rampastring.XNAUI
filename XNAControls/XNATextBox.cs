using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI.Input;
using System;
using System.Text;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A text input control.
    /// </summary>
    public class XNATextBox : XNAControl
    {
        protected const int TEXT_HORIZONTAL_MARGIN = 3;
        protected const int TEXT_VERTICAL_MARGIN = 2;
        protected const double SCROLL_REPEAT_TIME = 0.03;
        protected const double FAST_SCROLL_TRIGGER_TIME = 0.4;
        protected const double BAR_ON_TIME = 0.5;
        protected const double BAR_OFF_TIME = 0.5;

        /// <summary>
        /// Creates a new XNATextBox.
        /// </summary>
        /// <param name="windowManager">The WindowManager that will be associated with this control.</param>
        public XNATextBox(WindowManager windowManager) : base(windowManager)
        {
            IdleBorderColor = UISettings.PanelBorderColor;
            ActiveBorderColor = UISettings.AltColor;
            TextColor = UISettings.AltColor;
            BackColor = UISettings.BackgroundColor;
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

        public virtual Color TextColor { get; set; }

        /// <summary>
        /// The color of the text box border when the text box is inactive.
        /// </summary>
        public Color IdleBorderColor { get; set; }

        /// <summary>
        /// The color of the text box border when the text box is selected.
        /// </summary>
        public Color ActiveBorderColor { get; set; }

        /// <summary>
        /// The color of the text box background.
        /// </summary>
        public Color BackColor { get; set; }

        /// <summary>
        /// The index of the spritefont that this textbox uses.
        /// </summary>
        public int FontIndex { get; set; }

        private int _maximumTextLength = int.MaxValue;

        /// <summary>
        /// The maximum length of the text of this text box, in characters.
        /// </summary>
        public int MaximumTextLength
        {
            get { return _maximumTextLength; }
            set { _maximumTextLength = value; }
        }

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
                text = value;
                InputPosition = 0;
                TextStartPosition = 0;

                if (text.Length > MaximumTextLength)
                    text = text.Substring(0, MaximumTextLength);

                TextEndPosition = text.Length;

                while (!TextFitsBox())
                {
                    TextEndPosition--;

                    if (TextEndPosition < TextStartPosition)
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

        /// <summary>
        /// The start character index of the visible part of the text string.
        /// </summary>
        public int TextStartPosition { get; set; }

        /// <summary>
        /// The end character index of the visible part of the text string.
        /// </summary>
        public int TextEndPosition { get; set; }

        protected TimeSpan barTimer = TimeSpan.Zero;

        private TimeSpan scrollKeyTime = TimeSpan.Zero;
        private TimeSpan timeSinceLastScroll = TimeSpan.Zero;
        private bool isScrollingQuickly = false;

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
            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        }

#if XNA
        private void KeyboardEventInput_CharEntered(object sender, KeyboardEventArgs e)
        {
            HandleCharInput(e.Character);
        }
#else
        private void Window_TextInput(object sender, TextInputEventArgs e)
        {
            HandleCharInput(e.Character);
        }
#endif

        private void HandleCharInput(char character)
        {
            if (WindowManager.SelectedControl != this || !Enabled || !Parent.Enabled || !WindowManager.HasFocus)
                return;

            switch (character)
            {
                /*/ There are a bunch of keys that are detected as text input on
                 * Windows builds of MonoGame, but not on WindowsGL or Linux builds of MonoGame.
                 * We already handle these keys (enter, tab, backspace, escape) by other means,
                 * so we don't want to handle them also as text input on Windows to avoid 
                 * potentially harmful extra triggering of the InputReceived event.
                 * So, we detect that input here and return on these keys.
                /*/
                case '\r':      // Enter / return
                case '\x0009':  // Tab
                case '\b':      // Backspace
                case '\x001b':  // ESC
                    return;
                default:
                    if (text.Length == MaximumTextLength)
                        break;

                    // Don't allow typing characters that don't exist in the spritefont
                    if (Renderer.GetSafeString(character.ToString(), FontIndex) != character.ToString())
                        break;

                    if (!AllowCharacterInput(character))
                        break;

                    text = text.Insert(InputPosition, character.ToString());
                    InputPosition++;

                    if (TextEndPosition == text.Length - 1 ||
                        InputPosition > TextEndPosition)
                    {
                        TextEndPosition++;

                        while (!TextFitsBox())
                        {
                            TextStartPosition++;
                        }
                    }

                    break;
            }

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

        private void Keyboard_OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            if (WindowManager.SelectedControl != this || !Enabled || !Parent.Enabled || !WindowManager.HasFocus)
                return;

            switch (e.PressedKey)
            {
                case Keys.Home:
                    if (text.Length == 0)
                        return;

                    TextStartPosition = 0;
                    TextEndPosition = 0;
                    InputPosition = 0;

                    while (true)
                    {
                        if (TextEndPosition < text.Length)
                        {
                            TextEndPosition++;

                            if (!TextFitsBox())
                            {
                                TextEndPosition--;
                                break;
                            }

                            continue;
                        }

                        break;
                    }

                    break;
                case Keys.End:
                    TextEndPosition = text.Length;
                    InputPosition = text.Length;
                    TextStartPosition = 0;

                    while (true)
                    {
                        if (!TextFitsBox())
                        {
                            TextStartPosition++;
                            continue;
                        }

                        break;
                    }

                    break;
                case Keys.X:
                    if (!Keyboard.IsCtrlHeldDown())
                        break;

                    if (!string.IsNullOrEmpty(text))
                    {
                        System.Windows.Forms.Clipboard.SetText(text);
                        Text = string.Empty;
                        InputReceived?.Invoke(this, EventArgs.Empty);
                    }

                    break;
                case Keys.V:
                    if (!Keyboard.IsCtrlHeldDown())
                        break;

                    Text = Text + Renderer.GetSafeString(System.Windows.Forms.Clipboard.GetText(), FontIndex);
                    InputReceived?.Invoke(this, EventArgs.Empty);

                    goto case Keys.End;
                case Keys.C:
                    if (!Keyboard.IsCtrlHeldDown())
                        break;

                    if (!string.IsNullOrEmpty(text))
                        System.Windows.Forms.Clipboard.SetText(text);

                    break;
                case Keys.Enter:
                    EnterPressed?.Invoke(this, EventArgs.Empty);
                    break;
                case Keys.Escape:
                    InputPosition = 0;
                    Text = string.Empty;
                    InputReceived?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        private bool TextFitsBox()
        {
            if (String.IsNullOrEmpty(text))
                return true;

            return Renderer.GetTextDimensions(
                        text.Substring(TextStartPosition, TextEndPosition - TextStartPosition),
                        FontIndex).X < ClientRectangle.Width - TEXT_HORIZONTAL_MARGIN * 2;
        }

        public override void OnLeftClick()
        {
            if (WindowManager.SelectedControl == this)
            {
                int x = GetCursorPoint().X;
                int inputPosition = TextEndPosition;

                StringBuilder text = new StringBuilder();

                for (int i = TextStartPosition; i < TextEndPosition - TextStartPosition; i++)
                {
                    text.Append(Text[i]);
                    if (Renderer.GetTextDimensions(text.ToString(), FontIndex).X + 
                        TEXT_HORIZONTAL_MARGIN > x)
                    {
                        inputPosition = i - 1;
                        break;
                    }
                }

                InputPosition = Math.Max(0, inputPosition);
            }
            else
            {
                InputPosition = TextEndPosition;
            }

            barTimer = TimeSpan.Zero;

            base.OnLeftClick();
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
                    HandleScrollKeyDown(gameTime, ScrollLeft);
                else if (Keyboard.IsKeyHeldDown(Keys.Right))
                    HandleScrollKeyDown(gameTime, ScrollRight);
                else if (Keyboard.IsKeyHeldDown(Keys.Delete))
                    HandleScrollKeyDown(gameTime, DeleteCharacter);
                else if (Keyboard.IsKeyHeldDown(Keys.Back))
                    HandleScrollKeyDown(gameTime, Backspace);
                else
                {
                    isScrollingQuickly = false;
                    timeSinceLastScroll = TimeSpan.Zero;
                    scrollKeyTime = TimeSpan.Zero;
                }
            }
        }

        private void ScrollLeft()
        {
            if (InputPosition == 0)
                return;

            InputPosition--;
            if (InputPosition < TextStartPosition)
            {
                TextStartPosition--;

                while (!TextFitsBox())
                    TextEndPosition--;
            }
        }

        private void ScrollRight()
        {
            if (InputPosition >= text.Length)
                return;

            InputPosition++;

            if (InputPosition > TextEndPosition)
            {
                TextEndPosition++;

                while (!TextFitsBox())
                {
                    TextStartPosition++;
                }
            }
        }

        private void DeleteCharacter()
        {
            if (text.Length > InputPosition)
            {
                text = text.Remove(InputPosition, 1);

                if (TextStartPosition > 0)
                {
                    TextStartPosition--;
                }

                if (TextEndPosition > text.Length || !TextFitsBox())
                    TextEndPosition--;
            }

            InputReceived?.Invoke(this, EventArgs.Empty);
        }

        private void Backspace()
        {
            if (text.Length > 0 && InputPosition > 0)
            {
                text = text.Remove(InputPosition - 1, 1);
                InputPosition--;

                if (TextStartPosition > 0)
                    TextStartPosition--;

                TextEndPosition--;
            }

            InputReceived?.Invoke(this, EventArgs.Empty);
        }

        private void HandleScrollKeyDown(GameTime gameTime, Action action)
        {
            if (scrollKeyTime.Equals(TimeSpan.Zero))
                action();

            scrollKeyTime += gameTime.ElapsedGameTime;

            if (isScrollingQuickly)
            {
                timeSinceLastScroll += gameTime.ElapsedGameTime;

                if (timeSinceLastScroll > TimeSpan.FromSeconds(SCROLL_REPEAT_TIME))
                {
                    timeSinceLastScroll = TimeSpan.Zero;
                    action();
                }
            }

            if (scrollKeyTime > TimeSpan.FromSeconds(FAST_SCROLL_TRIGGER_TIME) && !isScrollingQuickly)
            {
                isScrollingQuickly = true;
                timeSinceLastScroll = TimeSpan.Zero;
            }

            barTimer = TimeSpan.Zero;
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle displayRectangle = WindowRectangle();

            Renderer.FillRectangle(displayRectangle, BackColor);

            if (WindowManager.SelectedControl == this && Enabled && WindowManager.HasFocus)
                Renderer.DrawRectangle(displayRectangle, ActiveBorderColor);
            else
                Renderer.DrawRectangle(displayRectangle, IdleBorderColor);

            Renderer.DrawStringWithShadow(Text.Substring(TextStartPosition, TextEndPosition - TextStartPosition),
                FontIndex, new Vector2(displayRectangle.X + TEXT_HORIZONTAL_MARGIN, displayRectangle.Y + TEXT_VERTICAL_MARGIN),
                TextColor);

            if (WindowManager.SelectedControl == this && Enabled && WindowManager.HasFocus &&
                barTimer.TotalSeconds < BAR_ON_TIME)
            {
                int barLocationX = TEXT_HORIZONTAL_MARGIN;

                string inputText = Text.Substring(TextStartPosition, InputPosition - TextStartPosition);
                barLocationX += (int)Renderer.GetTextDimensions(inputText, FontIndex).X;

                Renderer.DrawRectangle(new Rectangle(displayRectangle.X + barLocationX,
                    displayRectangle.Y + 2, 1, displayRectangle.Height - 4), Color.White);
            }

            base.Draw(gameTime);
        }
    }
}
