using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A vertical scroll bar, mainly for list boxes but could also be utilized
    /// by other controls.
    /// </summary>
    public class XNAScrollBar : XNAControl
    {
        private const int MIN_BUTTON_HEIGHT = 10;

        public XNAScrollBar(WindowManager windowManager) : base(windowManager)
        {
            var scrollUpTexture = AssetLoader.LoadTexture("sbUpArrow.png");

            btnScrollUp = new XNAButton(WindowManager);
            btnScrollUp.ClientRectangle = new Rectangle(0, 0, scrollUpTexture.Width, scrollUpTexture.Height);
            btnScrollUp.IdleTexture = scrollUpTexture;

            var scrollDownTexture = AssetLoader.LoadTexture("sbDownArrow.png");

            btnScrollDown = new XNAButton(WindowManager);
            btnScrollDown.ClientRectangle = new Rectangle(0, ClientRectangle.Height - scrollDownTexture.Height,
                scrollDownTexture.Width, scrollDownTexture.Height);
            btnScrollDown.IdleTexture = scrollDownTexture;
        }

        public event EventHandler Scrolled;
        public event EventHandler ScrolledToBottom;

        public int ItemCount { get; set; }

        public int DisplayedItemCount { get; set; }

        public int TopIndex { get; set; }

        public override Rectangle ClientRectangle
        {
            get
            {
                return base.ClientRectangle;
            }

            set
            {
                base.ClientRectangle = value;
                btnScrollDown.ClientRectangle = new Rectangle(0, 
                    ClientRectangle.Height - btnScrollDown.ClientRectangle.Height,
                    btnScrollDown.ClientRectangle.Width, btnScrollDown.ClientRectangle.Height);
            }
        }

        /// <summary>
        /// Returns the width of the scroll bar.
        /// </summary>
        public int ScrollWidth
        {
            get { return btnScrollUp.IdleTexture.Width; }
        }

        private int buttonHeight { get; set; }

        private int scrollablePixels { get; set; }

        private double linesPerScrolledPixel { get; set; }

        private int buttonMinY = 0;

        private int buttonMaxY = 0;

        private int buttonY = 0;

        private XNAButton btnScrollUp;

        private XNAButton btnScrollDown;

        Texture2D background;
        Texture2D thumbMiddle;
        Texture2D thumbTop;
        Texture2D thumbBottom;

        public override void Initialize()
        {
            base.Initialize();

            AddChild(btnScrollUp);
            AddChild(btnScrollDown);

            btnScrollUp.LeftClick += BtnScrollUp_LeftClick;
            btnScrollDown.LeftClick += BtnScrollDown_LeftClick;

            background = AssetLoader.LoadTexture("sbBackground.png");
            thumbMiddle = AssetLoader.LoadTexture("sbMiddle.png");
            thumbTop = AssetLoader.LoadTexture("sbThumbTop.png");
            thumbBottom = AssetLoader.LoadTexture("sbThumbBottom.png");
        }

        private void BtnScrollUp_LeftClick(object sender, EventArgs e)
        {
            if (TopIndex > 0)
                TopIndex--;

            RefreshButtonY();

            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        private void BtnScrollDown_LeftClick(object sender, EventArgs e)
        {
            int nonDisplayedLines = ItemCount - DisplayedItemCount;

            if (TopIndex < nonDisplayedLines)
                TopIndex++;

            RefreshButtonY();

            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        public void Refresh()
        {
            int height = ClientRectangle.Height - 
                btnScrollUp.ClientRectangle.Height - btnScrollDown.ClientRectangle.Height;

            int nonDisplayedLines = ItemCount - DisplayedItemCount;

            if (nonDisplayedLines <= 0)
            {
                buttonHeight = height;
                scrollablePixels = 0;
                return;
            }
            else
            {
                //buttonHeight = Math.Max(height - (int)(height * nonDisplayedLines / (double)ItemCount),
                //    MIN_BUTTON_HEIGHT);
                buttonHeight = 100;

                scrollablePixels = height - buttonHeight;

                linesPerScrolledPixel = scrollablePixels / (double)ItemCount;
            }

            buttonMinY = btnScrollUp.ClientRectangle.Bottom + buttonHeight / 2;
            buttonMaxY = ClientRectangle.Height - btnScrollDown.ClientRectangle.Height - (buttonHeight / 2);

            RefreshButtonY();
        }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            Scroll();
        }


        public override void OnMouseMove()
        {
            base.OnMouseMove();

            if (Cursor.LeftPressed)
            {
                Scroll();
            }
        }

        private void Scroll()
        {
            var point = GetCursorPoint();

            if (point.Y < btnScrollUp.ClientRectangle.Height
                || point.Y > btnScrollDown.ClientRectangle.Y)
                return;

            if (point.Y <= buttonMinY)
            {
                TopIndex = 0;
                RefreshButtonY();
                Scrolled?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (point.Y >= buttonMaxY)
            {
                RefreshButtonY();
                ScrolledToBottom?.Invoke(this, EventArgs.Empty);
            }

            double difference = buttonMaxY - buttonMinY;

            double location = point.Y - buttonMinY;

            TopIndex = (int)(location / difference * DisplayedItemCount);
            RefreshButtonY();

            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        public void RefreshButtonY(int topIndex)
        {
            TopIndex = topIndex;
            RefreshButtonY();
        }

        public void RefreshButtonY()
        {
            int nonDisplayedLines = ItemCount - DisplayedItemCount;

            if (nonDisplayedLines <= 0)
            {
                buttonY = btnScrollUp.WindowRectangle().Bottom;
                return;
            }

            buttonY = WindowRectangle().Y + buttonMinY + (int)(((TopIndex / (double)nonDisplayedLines) * scrollablePixels) - buttonHeight / 2);
        }

        public override void Draw(GameTime gameTime)
        {
            var drawArea = WindowRectangle();

            Renderer.DrawTexture(background, drawArea, Color.White);

            Renderer.DrawTexture(thumbTop, new Rectangle(drawArea.X, buttonY, ScrollWidth, thumbTop.Height), RemapColor);
            Renderer.DrawTexture(thumbBottom, new Rectangle(drawArea.X,
                buttonY + buttonHeight - thumbBottom.Height, ScrollWidth, thumbBottom.Height), Color.White);
            Renderer.DrawTexture(thumbMiddle, new Rectangle(drawArea.X,
                buttonY + thumbTop.Height, ScrollWidth, buttonHeight - thumbTop.Height - thumbBottom.Height), Color.White);

            base.Draw(gameTime);
        }
    }
}
