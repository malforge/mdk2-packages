using System;

namespace Lumel.Layout
{
    public partial class Form
    {
        /// <summary>The rows of one form, which only that form creates.</summary>
        sealed class OwnedRows : FormRows
        {
            public OwnedRows(Action<Element> joined, Action<Element> released) : base(joined, released)
            {
            }
        }
    }
}
