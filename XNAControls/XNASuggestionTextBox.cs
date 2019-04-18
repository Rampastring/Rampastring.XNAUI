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
        }

        public string Suggestion { get; set; }

        private Color? _suggestedTextColor;

        public Color SuggestedTextColor
        {
            get => _suggestedTextColor ?? UISettings.ActiveSettings.SubtleTextColor;
            set => _suggestedTextColor = value;
        }


        private Color realTextColor;

        public override void Initialize()
        {
            base.Initialize();

            Text = Suggestion ?? string.Empty;
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

                if (WindowManager.SelectedControl == this)
                    base.TextColor = realTextColor;
            }
        }

        public override void OnSelectedChanged()
        {
            base.OnSelectedChanged();

            if (WindowManager.SelectedControl == this)
            {
                base.TextColor = realTextColor;

                if (Text == Suggestion)
                    Text = string.Empty;
            }
            else
            {
                base.TextColor = SuggestedTextColor;
                if (string.IsNullOrEmpty(Text))
                    Text = Suggestion ?? string.Empty;
            }
        }

    }
}
