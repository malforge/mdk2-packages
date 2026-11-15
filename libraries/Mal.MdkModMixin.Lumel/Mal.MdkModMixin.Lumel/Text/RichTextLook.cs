using VRageMath;
using Lumel.Styling;

namespace Lumel.Text
{
    /// <summary>
    ///     The look of a document in one state, adding how each kind of block and run is formatted to what any text
    ///     look sets.
    /// </summary>
    public class RichTextLook : TextLook
    {
        float? _blockSpacing;
        Color? _bulletColor;
        float? _bulletSize;
        Thickness? _calloutPadding;
        Color? _codeBackground;
        string _codeFont;
        Thickness? _codePadding;
        Color? _emphasisColor;
        string _emphasisFont;
        float? _heading1Size;
        float? _heading2Size;
        float? _heading3Size;
        Color? _headingColor;
        string _headingFont;
        float? _headingRuleSpacing;
        float? _headingSpacing;
        float? _indent;
        Color? _linkBackground;
        Color? _linkColor;
        float? _linkUnderlineThickness;
        Color? _quoteBarColor;
        float? _quoteBarWidth;
        float? _quoteIndent;
        Color? _ruleColor;
        float? _ruleThickness;
        Color? _strongColor;
        string _strongEmphasisFont;
        string _strongFont;
        float? _tableColumnSpacing;
        TextAlignment? _textAlignment;
        float? _underlineOffset;
        int? _underlinedHeadingLevel;

        /// <summary>Creates a look that sets nothing.</summary>
        public RichTextLook()
        {
            Callouts = new CalloutLooks(OnChanged);
            Links = new LinkLooks(OnChanged);
            Spans = new SpanLooks(OnChanged);
        }

        /// <summary>Gets or sets the color of strong text, or null to inherit it from the looks beneath.</summary>
        public Color? StrongColor
        {
            get { return _strongColor; }
            set
            {
                _strongColor = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of emphasized text, or null to inherit it from the looks beneath.</summary>
        public Color? EmphasisColor
        {
            get { return _emphasisColor; }
            set
            {
                _emphasisColor = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of headings, or null to inherit it from the looks beneath.</summary>
        public Color? HeadingColor
        {
            get { return _headingColor; }
            set
            {
                _headingColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of rules and of the line under a table's heading row, or null to inherit it from
        ///     the looks beneath.
        /// </summary>
        public Color? RuleColor
        {
            get { return _ruleColor; }
            set
            {
                _ruleColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the thickness of rules and of the lines under headings and a table's heading row, or null
        ///     to inherit it from the looks beneath.
        /// </summary>
        public float? RuleThickness
        {
            get { return _ruleThickness; }
            set
            {
                _ruleThickness = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of a bullet's mark, or null to inherit it from the looks beneath.</summary>
        public Color? BulletColor
        {
            get { return _bulletColor; }
            set
            {
                _bulletColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the width of a bullet's square mark, or null to inherit it from the looks beneath.
        /// </summary>
        public float? BulletSize
        {
            get { return _bulletSize; }
            set
            {
                _bulletSize = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color of the bar along a quote's edge, or null to inherit it from the looks beneath.
        /// </summary>
        public Color? QuoteBarColor
        {
            get { return _quoteBarColor; }
            set
            {
                _quoteBarColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the width of the bar along the edge of a quote or callout, or null to inherit it from the
        ///     looks beneath.
        /// </summary>
        public float? QuoteBarWidth
        {
            get { return _quoteBarWidth; }
            set
            {
                _quoteBarWidth = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the font for code, or null to inherit it from the looks beneath.</summary>
        public string CodeFont
        {
            get { return _codeFont; }
            set
            {
                _codeFont = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the font for strong text, or null to inherit it from the looks beneath.</summary>
        public string StrongFont
        {
            get { return _strongFont; }
            set
            {
                _strongFont = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the font for emphasized text, or null to inherit it from the looks beneath.</summary>
        public string EmphasisFont
        {
            get { return _emphasisFont; }
            set
            {
                _emphasisFont = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the font for text that is both strong and emphasized, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public string StrongEmphasisFont
        {
            get { return _strongEmphasisFont; }
            set
            {
                _strongEmphasisFont = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the font for headings and a table's heading row, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public string HeadingFont
        {
            get { return _headingFont; }
            set
            {
                _headingFont = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the line height of first-level headings, or null to inherit it from the looks beneath.
        /// </summary>
        public float? Heading1Size
        {
            get { return _heading1Size; }
            set
            {
                _heading1Size = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the line height of second-level headings, or null to inherit it from the looks beneath.
        /// </summary>
        public float? Heading2Size
        {
            get { return _heading2Size; }
            set
            {
                _heading2Size = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the line height of headings at the third level or deeper, or null to inherit it from the
        ///     looks beneath.
        /// </summary>
        public float? Heading3Size
        {
            get { return _heading3Size; }
            set
            {
                _heading3Size = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space between one block and the next, or null to inherit it from the looks beneath.
        /// </summary>
        public float? BlockSpacing
        {
            get { return _blockSpacing; }
            set
            {
                _blockSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space above a heading that is not the first block, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? HeadingSpacing
        {
            get { return _headingSpacing; }
            set
            {
                _headingSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space between a heading and the rule under it, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? HeadingRuleSpacing
        {
            get { return _headingRuleSpacing; }
            set
            {
                _headingRuleSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the deepest heading level drawn with a rule under it, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public int? UnderlinedHeadingLevel
        {
            get { return _underlinedHeadingLevel; }
            set
            {
                _underlinedHeadingLevel = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how far a list item's text is indented from its mark, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? Indent
        {
            get { return _indent; }
            set
            {
                _indent = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how far a quote's blocks are indented from its edge, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? QuoteIndent
        {
            get { return _quoteIndent; }
            set
            {
                _quoteIndent = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space between a table's columns, or null to inherit it from the looks beneath.
        /// </summary>
        public float? TableColumnSpacing
        {
            get { return _tableColumnSpacing; }
            set
            {
                _tableColumnSpacing = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the space between a callout's edge and its blocks, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public Thickness? CalloutPadding
        {
            get { return _calloutPadding; }
            set
            {
                _calloutPadding = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the alignment of lines within the available width, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public TextAlignment? TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                _textAlignment = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color of link text, or null to inherit it from the looks beneath.</summary>
        public Color? LinkColor
        {
            get { return _linkColor; }
            set
            {
                _linkColor = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the color painted behind link text, or null to inherit it from the looks beneath.
        /// </summary>
        public Color? LinkBackground
        {
            get { return _linkBackground; }
            set
            {
                _linkBackground = value;
                OnChanged();
            }
        }

        /// <summary>Gets or sets the color painted behind code, or null to inherit it from the looks beneath.</summary>
        public Color? CodeBackground
        {
            get { return _codeBackground; }
            set
            {
                _codeBackground = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets how far the box behind code extends past its text on each side, or null to inherit it from
        ///     the looks beneath.
        /// </summary>
        public Thickness? CodePadding
        {
            get { return _codePadding; }
            set
            {
                _codePadding = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the thickness of the underline under link text, or null to inherit it from the looks
        ///     beneath.
        /// </summary>
        public float? LinkUnderlineThickness
        {
            get { return _linkUnderlineThickness; }
            set
            {
                _linkUnderlineThickness = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the vertical position of a link's underline as a fraction of the line height, or null to
        ///     inherit it from the looks beneath.
        /// </summary>
        public float? UnderlineOffset
        {
            get { return _underlineOffset; }
            set
            {
                _underlineOffset = value;
                OnChanged();
            }
        }

        /// <summary>
        ///     Gets the look of a callout of each class, with each class's look applied over the looks beneath for that
        ///     class.
        /// </summary>
        public CalloutLooks Callouts { get; }

        /// <summary>
        ///     Gets the look of a link of each class, with each class's look applied over the looks beneath for that class.
        /// </summary>
        public LinkLooks Links { get; }

        /// <summary>
        ///     Gets the look of a span of each class, with each class's look applied over the looks beneath for that class.
        /// </summary>
        public SpanLooks Spans { get; }

        /// <inheritdoc />
        public override void Merge(ElementLook over)
        {
            base.Merge(over);
            var look = over as RichTextLook;
            if (look == null)
                return;

            _strongColor = look._strongColor ?? _strongColor;
            _emphasisColor = look._emphasisColor ?? _emphasisColor;
            _headingColor = look._headingColor ?? _headingColor;
            _ruleColor = look._ruleColor ?? _ruleColor;
            _ruleThickness = look._ruleThickness ?? _ruleThickness;
            _bulletColor = look._bulletColor ?? _bulletColor;
            _bulletSize = look._bulletSize ?? _bulletSize;
            _quoteBarColor = look._quoteBarColor ?? _quoteBarColor;
            _quoteBarWidth = look._quoteBarWidth ?? _quoteBarWidth;
            _codeFont = look._codeFont ?? _codeFont;
            _strongFont = look._strongFont ?? _strongFont;
            _emphasisFont = look._emphasisFont ?? _emphasisFont;
            _strongEmphasisFont = look._strongEmphasisFont ?? _strongEmphasisFont;
            _headingFont = look._headingFont ?? _headingFont;
            _heading1Size = look._heading1Size ?? _heading1Size;
            _heading2Size = look._heading2Size ?? _heading2Size;
            _heading3Size = look._heading3Size ?? _heading3Size;
            _blockSpacing = look._blockSpacing ?? _blockSpacing;
            _headingSpacing = look._headingSpacing ?? _headingSpacing;
            _headingRuleSpacing = look._headingRuleSpacing ?? _headingRuleSpacing;
            _underlinedHeadingLevel = look._underlinedHeadingLevel ?? _underlinedHeadingLevel;
            _indent = look._indent ?? _indent;
            _quoteIndent = look._quoteIndent ?? _quoteIndent;
            _tableColumnSpacing = look._tableColumnSpacing ?? _tableColumnSpacing;
            _calloutPadding = look._calloutPadding ?? _calloutPadding;
            _textAlignment = look._textAlignment ?? _textAlignment;
            _linkColor = look._linkColor ?? _linkColor;
            _linkBackground = look._linkBackground ?? _linkBackground;
            _codeBackground = look._codeBackground ?? _codeBackground;
            _codePadding = look._codePadding ?? _codePadding;
            _linkUnderlineThickness = look._linkUnderlineThickness ?? _linkUnderlineThickness;
            _underlineOffset = look._underlineOffset ?? _underlineOffset;
            Callouts.Merge(look.Callouts);
            Links.Merge(look.Links);
            Spans.Merge(look.Spans);
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            _strongColor = null;
            _emphasisColor = null;
            _headingColor = null;
            _ruleColor = null;
            _ruleThickness = null;
            _bulletColor = null;
            _bulletSize = null;
            _quoteBarColor = null;
            _quoteBarWidth = null;
            _codeFont = null;
            _strongFont = null;
            _emphasisFont = null;
            _strongEmphasisFont = null;
            _headingFont = null;
            _heading1Size = null;
            _heading2Size = null;
            _heading3Size = null;
            _blockSpacing = null;
            _headingSpacing = null;
            _headingRuleSpacing = null;
            _underlinedHeadingLevel = null;
            _indent = null;
            _quoteIndent = null;
            _tableColumnSpacing = null;
            _calloutPadding = null;
            _textAlignment = null;
            _linkColor = null;
            _linkBackground = null;
            _codeBackground = null;
            _codePadding = null;
            _linkUnderlineThickness = null;
            _underlineOffset = null;
            Callouts.Clear();
            Links.Clear();
            Spans.Clear();
        }
    }
}
