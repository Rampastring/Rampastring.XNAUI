using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;

namespace Rampastring.XNAUI.XNAControls
{
    /// <summary>
    /// A text box that displays its characters as a different character 
    /// (a star by default), regardless of what the characters actually are.
    /// In other words, a text box for inputting passwords.
    /// </summary>
    public class XNAPasswordBox : XNATextBox
    {
        /// <summary>
        /// The character that is displayed in place of all other characters.
        /// </summary>
        public char VisibleChar { get; set; }

        /// <summary>
        /// Creates a new XNAPasswordBox.
        /// </summary>
        /// <param name="wm">The WindowManager.</param>
        public XNAPasswordBox(WindowManager wm) : base(wm)
        {
            VisibleChar = '*';
        }

        /// <summary>
        /// Gets or sets the real text of the password box.
        /// </summary>
        public string Password
        {
            get
            {
                return base.Text;
            }
            set
            {
                Text = value;
            }
        }

        /// <summary>
        /// Gets the visible string of the password box.
        /// If set, changes the actual text / password in the box.
        /// </summary>
        public override string Text
        {
            get
            {
                return new string(VisibleChar, base.Text.Length);
            }

            set
            {
                base.Text = value;
            }
        }
    }
}
