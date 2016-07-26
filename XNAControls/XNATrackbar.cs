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

        public override void Initialize()
        {
            base.Initialize();

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

        public override void OnLeftClick()
        {

        }

        public override void OnMouseOnControl(MouseEventArgs e)
        {
            if (!Cursor.LeftPressed)
                return;

            int xOffset = e.RelativeLocation.X;

            int tabCount = MaxValue - MinValue;

            int pixelsPerTab = ClientRectangle.Width / tabCount;

            int currentTab = 0;

            for (int i = 0; i <= tabCount; i++)
            {
                if (i * pixelsPerTab - (pixelsPerTab / 2) < xOffset)
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

            double pixelsPerTab = windowRectangle.Width / (double)tabCount;

            double tabLocationX = tabIndex * pixelsPerTab - (ButtonTexture.Width / 2);

            if (tabIndex == 0)
                tabLocationX += ButtonTexture.Width / 2;
            else if (tabIndex == tabCount)
                tabLocationX -= ButtonTexture.Width / 2;

            Renderer.DrawTexture(ButtonTexture,
                new Rectangle((int)(windowRectangle.X + tabLocationX), windowRectangle.Y, ButtonTexture.Width, windowRectangle.Height),
                GetColorWithAlpha(Color.White));
        }
    }
}
