using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A vertical scroll bar, mainly for list boxes but it could also be utilized
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

        /// <summary>
        /// The number of items in the scrollable parent control.
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// The number of items that the scrollable parent control
        /// is able to display at once.
        /// </summary>
        public int DisplayedItemCount { get; set; }

        /// <summary>
        /// The index of the first displayed item of the scrollable parent control's items.
        /// The parent of the scroll-bar has to keep the scrollbar up-to-date when the 
        /// top index of the parent changes.
        /// </summary>
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
                Refresh();
            }
        }

        /// <summary>
        /// Returns the width of the scroll bar.
        /// </summary>
        public int ScrollWidth
        {
            get { return btnScrollUp.IdleTexture.Width; }
        }

        private int thumbHeight { get; set; }

        private int scrollablePixels { get; set; }

        private int buttonMinY = 0;

        private int buttonMaxY = 0;

        private int buttonY = 0;

        private XNAButton btnScrollUp;

        private XNAButton btnScrollDown;

        private Texture2D background;
        private Texture2D thumbMiddle;
        private Texture2D thumbTop;
        private Texture2D thumbBottom;

        private bool isHeldDown = false;

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

        /// <summary>
        /// Scrolls up when the user presses on the "scroll up" arrow.
        /// </summary>
        private void BtnScrollUp_LeftClick(object sender, EventArgs e)
        {
            if (TopIndex > 0)
                TopIndex--;

            RefreshButtonY();

            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Scrolls down when the user presses on the "scroll down" arrow.
        /// </summary>
        private void BtnScrollDown_LeftClick(object sender, EventArgs e)
        {
            int nonDisplayedLines = ItemCount - DisplayedItemCount;

            if (TopIndex < nonDisplayedLines)
                TopIndex++;

            RefreshButtonY();

            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Refreshes the scroll bar's thumb size.
        /// </summary>
        public void Refresh()
        {
            int height = ClientRectangle.Height - 
                btnScrollUp.ClientRectangle.Height - btnScrollDown.ClientRectangle.Height;

            int nonDisplayedLines = ItemCount - DisplayedItemCount;

            if (nonDisplayedLines <= 0)
            {
                thumbHeight = height;
                scrollablePixels = 0;
                btnScrollDown.Disable();
                btnScrollUp.Disable();
            }
            else
            {
                thumbHeight = Math.Max(height - (int)(height * nonDisplayedLines / (double)ItemCount),
                    MIN_BUTTON_HEIGHT);

                scrollablePixels = height - thumbHeight;

                btnScrollDown.Enable();
                btnScrollUp.Enable();
            }

            buttonMinY = btnScrollUp.ClientRectangle.Bottom + thumbHeight / 2;
            buttonMaxY = ClientRectangle.Height - btnScrollDown.ClientRectangle.Height - (thumbHeight / 2);

            RefreshButtonY();
        }

        /// <summary>
        /// Scrolls the scrollbar when it's clicked on.
        /// </summary>
        public override void OnLeftClick()
        {
            base.OnLeftClick();

            Scroll();
        }

        /// <summary>
        /// Scrolls the scrollbar if the user presses the mouse left button
        /// while moving the cursor over the scrollbar.
        /// </summary>
        public override void OnMouseMove()
        {
            base.OnMouseMove();

            if (Cursor.LeftPressed)
            {
                Scroll();
                isHeldDown = true;
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
                return;
            }

            double difference = buttonMaxY - buttonMinY;

            double location = point.Y - buttonMinY;

            int nonDisplayedLines = ItemCount - DisplayedItemCount;

            TopIndex = (int)(location / difference * nonDisplayedLines);
            RefreshButtonY();

            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates the top item index of the scroll bar,
        /// and the vertical position of the scroll bar's thumb.
        /// </summary>
        public void RefreshButtonY(int topIndex)
        {
            TopIndex = topIndex;
            RefreshButtonY();
        }

        /// <summary>
        /// Updates the vertical position of the scroll bar's thumb.
        /// </summary>
        public void RefreshButtonY()
        {
            int nonDisplayedLines = ItemCount - DisplayedItemCount;

            if (nonDisplayedLines <= 0)
            {
                buttonY = btnScrollUp.WindowRectangle().Bottom;
                return;
            }

            buttonY = WindowRectangle().Y + Math.Min(
                buttonMinY + (int)(((TopIndex / (double)nonDisplayedLines) * scrollablePixels) - thumbHeight / 2),
                ClientRectangle.Height - btnScrollDown.ClientRectangle.Height - thumbHeight);
        }

        /// <summary>
        /// Updates the scroll bar's logic each frame.
        /// Makes it possible to drag the scrollbar thumb even if the cursor
        /// leaves the scroll bar's surface.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (isHeldDown)
            {
                if (!Cursor.LeftPressed)
                    isHeldDown = false;
                else
                    Scroll();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the scroll bar.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            var drawArea = WindowRectangle();

            if (scrollablePixels > 0)
            {
                Renderer.DrawTexture(background, drawArea, Color.White);

                Renderer.DrawTexture(thumbTop, new Rectangle(drawArea.X, buttonY, ScrollWidth, thumbTop.Height), RemapColor);
                Renderer.DrawTexture(thumbBottom, new Rectangle(drawArea.X,
                    buttonY + thumbHeight - thumbBottom.Height, ScrollWidth, thumbBottom.Height), Color.White);
                Renderer.DrawTexture(thumbMiddle, new Rectangle(drawArea.X,
                    buttonY + thumbTop.Height, ScrollWidth, thumbHeight - thumbTop.Height - thumbBottom.Height), Color.White);
            }

            base.Draw(gameTime);
        }
    }
}
