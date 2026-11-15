using Lumel.Input;
using Lumel.Windows;

namespace Lumel.Controls
{
    public partial class ColorPicker
    {
        /// <summary>The popup a picker opens, holding its panel and its Done button, where Enter does what Done does.</summary>
        class PanelPopup : Popup
        {
            readonly ICommand _done;

            /// <summary>Creates the popup around <paramref name="content" />, carrying out <paramref name="done" /> on Enter.</summary>
            public PanelPopup(Element content, ICommand done)
            {
                Content = content;
                _done = done;
            }

            /// <inheritdoc />
            protected override bool OnIntent(InputRoot input, Intent intent)
            {
                if (intent != Intent.Accept)
                    return base.OnIntent(input, intent);

                _done.Execute();
                return true;
            }
        }
    }
}
