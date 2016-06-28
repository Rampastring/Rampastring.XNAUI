using Microsoft.Xna.Framework;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A text box that displays a "suggestion" text when it's not active.
    /// </summary>
    public class XNASuggestionTextBox : XNATextBox
    {
        public XNASuggestionTextBox(WindowManager windowManager) : base(windowManager)
        {
            SuggestedTextColor = UISettings.SubtleTextColor;
        }

        public string Suggestion { get; set; }

        public Color SuggestedTextColor { get; set; }

        Color realTextColor;

        public override void Initialize()
        {
            base.Initialize();

            Text = Suggestion;
            base.TextColor = SuggestedTextColor;
        }

        public override Color TextColor
        {
            get
            {
                return base.TextColor;
            }

            set
            {
                realTextColor = value;

                if (IsSelected)
                    base.TextColor = realTextColor;
            }
        }

        public override void OnSelectedChanged()
        {
            base.OnSelectedChanged();

            if (IsSelected)
            {
                base.TextColor = realTextColor;

                if (Text == Suggestion)
                    Text = string.Empty;
            }
            else
            {
                base.TextColor = SuggestedTextColor;
                if (string.IsNullOrEmpty(Text))
                    Text = Suggestion;
            }
        }

    }
}
