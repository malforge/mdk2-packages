using Lumel.Styling;

namespace Lumel.Layout
{
    /// <summary>The style for a form: its box and the space between its labels, fields and rows.</summary>
    public class FormStyle : Style<Form, FormLook>
    {
        /// <summary>Creates a style that sets nothing, leaving the form at its neutral values.</summary>
        public FormStyle()
        {
        }

        /// <summary>
        ///     Creates a style that includes everything in <paramref name="basedOn" />, with whatever this style sets
        ///     applied on top.
        /// </summary>
        public FormStyle(IStyle<Element> basedOn) : base(basedOn)
        {
        }
    }
}
