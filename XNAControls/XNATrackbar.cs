using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls
{
    public class XNATrackbar : XNAPanel
    {
        public XNATrackbar(WindowManager windowManager) : base(windowManager)
        {
            
        }

        public event EventHandler ValueChanged;

        public int MinValue { get; set; }

        public int MaxValue { get; set; }

        int value = 0;
        public int Value
        {
            get { return value; }
            set
            {
                int oldValue = this.value;

                if (value > MaxValue)
                    this.value = MaxValue;
                else if (value < MinValue)
                    this.value = MinValue;
                else
                    this.value = value;

                if (oldValue != this.value)
                    ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public SoundEffect SoundEffectOnClick { get; set; }
        SoundEffectInstance clickEffectInstance;

        public Texture2D ButtonTexture {get; set; }

        private bool isHeldDown = false;

        public override void Initialize()
        {
            base.Initialize();

            if (ButtonTexture == null)
                ButtonTexture = AssetLoader.LoadTexture("trackbarButton.png");

            if (SoundEffectOnClick != null)
                clickEffectInstance = SoundEffectOnClick.CreateInstance();
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "MinValue":
                    MinValue = iniFile.GetIntValue(Name, "MinValue", 0);
                    return;
                case "MaxValue":
                    MaxValue = iniFile.GetIntValue(Name, "MaxValue", 10);
                    return;
                case "Value":
                    Value = iniFile.GetIntValue(Name, "Value", 0);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        /// <summary>
        /// Scrolls the scrollbar if the user presses the mouse left button
        /// while moving the cursor over the scrollbar.
        /// </summary>
        public override void OnMouseOnControl(MouseEventArgs e)
        {
            base.OnMouseOnControl(e);

            if (Cursor.LeftPressedDown)
            {
                isHeldDown = true;
            }
        }

        public override void OnLeftClick()
        {
            isHeldDown = true;
            Scroll();

            base.OnLeftClick();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (isHeldDown)
            {
                if (!Cursor.LeftDown)
                {
                    isHeldDown = false;
                    return;
                }

                Scroll();
            }
        }

        private void Scroll()
        {
            int xOffset = GetCursorPoint().X;

            int tabCount = MaxValue - MinValue + 1;

            double pixelsPerTab = Width / (double)tabCount;

            int currentTab = 0;

            for (int i = 0; i <= tabCount; i++)
            {
                if (i * pixelsPerTab < xOffset)
                {
                    currentTab = i;
                }
                else
                {
                    int newValue = currentTab + MinValue;

                    if (Value != newValue)
                        clickEffectInstance?.Play();

                    Value = newValue;

                    return;
                }
            }

            if (Value != MaxValue)
                clickEffectInstance?.Play();

            Value = MaxValue;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            Rectangle windowRectangle = WindowRectangle();

            int tabIndex = Value - MinValue;

            int tabCount = MaxValue - MinValue;

            double pixelsPerTab = (windowRectangle.Width - ButtonTexture.Width) / (double)tabCount;

            double tabLocationX = tabIndex * pixelsPerTab;

            //if (tabIndex == 0)
            //    tabLocationX += ButtonTexture.Width / 2;
            //else if (tabIndex == tabCount)
            //    tabLocationX -= ButtonTexture.Width / 2;

            Renderer.DrawTexture(ButtonTexture,
                new Rectangle((int)(windowRectangle.X + tabLocationX), windowRectangle.Y, ButtonTexture.Width, windowRectangle.Height),
                GetColorWithAlpha(Color.White));
        }
    }
}
