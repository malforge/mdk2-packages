using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;

namespace Lumel
{
    public partial class CodexTip
    {
        VStack _column;
        HStack _steps;
        Glyph _back;
        Glyph _forward;
        RichText _pinHint;

        /// <summary>
        ///     Creates the row of back and forward steps at the top right, above the page's text, <paramref name="body" />,
        ///     whose links it follows, and the pin hint.
        /// </summary>
        protected override Element CreateContent(RichText body)
        {
            body.LinkClicked += FollowLink;
            return new VStack
            {
                Children =
                {
                    new HStack
                    {
                        Align = Align.Right,
                        IsVisible = false,
                        Children =
                        {
                            new Glyph { Align = Align.Center, Command = new RelayCommand(GoBack) }.As(out _back),
                            new Glyph { Align = Align.Center, Command = new RelayCommand(GoForward) }.As(out _forward)
                        }
                    }.As(out _steps),
                    body,
                    new RichText { IsVisible = false }.As(out _pinHint)
                }
            }.As(out _column);
        }
    }
}
