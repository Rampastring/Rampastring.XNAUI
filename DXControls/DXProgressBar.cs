using Microsoft.Xna.Framework;
using System;

namespace Rampastring.XNAUI.DXControls
{
    public class DXProgressBar : DXControl
    {
        public DXProgressBar(WindowManager windowManager) : base(windowManager)
        {
            BorderColor = UISettings.PanelBorderColor;
            FilledColor = UISettings.AltColor;
            UnfilledColor = new Color(FilledColor.R / 3, FilledColor.G / 3, FilledColor.B / 3, FilledColor.A);
        }

        int _borderWidth = 1;
        public int BorderWidth
        {
            get { return _borderWidth; }
            set { _borderWidth = value; }
        }

        public Color BorderColor { get; set; }

        public Color FilledColor { get; set; }

        public Color UnfilledColor { get; set; }

        public int Maximum { get; set; }

        public bool SmoothBackwardsTransition { get; set; }

        public bool SmoothForwardTransition { get; set; }

        int _smoothTransitionRate = 1;
        public int SmoothTransitionRate
        {
            get { return _smoothTransitionRate; }
            set { _smoothTransitionRate = value; }
        }

        int _value = 0;
        public int Value
        {
            get { return _value; }
            set
            {
                if (value > Maximum)
                    _value = Maximum;
                else
                    _value = value;
            }
        }

        int _shownValue = 0;

        public override void Update(GameTime gameTime)
        {
            if (_shownValue < _value)
            {
                if (SmoothForwardTransition)
                    _shownValue = Math.Min(_shownValue + SmoothTransitionRate, _value);
                else
                    _shownValue = _value;
            }
            else if (_shownValue > _value)
            {
                if (SmoothBackwardsTransition)
                    _shownValue = Math.Max(0, _shownValue - SmoothTransitionRate);
                else
                    _shownValue = _value;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle wrect = WindowRectangle();

            for (int i = 0; i < BorderWidth; i++)
            {
                Rectangle rect = new Rectangle(wrect.X + i, wrect.Y + i,
                    wrect.Width - i, wrect.Height - i);

                Renderer.DrawRectangle(rect, GetColorWithAlpha(BorderColor));
            }

            int filledWidth = (int)((_shownValue / (double)Maximum) * (ClientRectangle.Width - BorderWidth * 2));

            Rectangle filledRect = new Rectangle(wrect.X + BorderWidth, wrect.Y + BorderWidth, 
                filledWidth, wrect.Height - BorderWidth * 2);

            Renderer.FillRectangle(filledRect, GetColorWithAlpha(FilledColor));

            Rectangle unfilledRect = new Rectangle(wrect.X + BorderWidth + filledWidth, wrect.Y + BorderWidth,
                wrect.Width - filledWidth - BorderWidth * 2, wrect.Height - BorderWidth * 2);

            Renderer.FillRectangle(unfilledRect, GetColorWithAlpha(UnfilledColor));

            base.Draw(gameTime);
        }
    }
}
