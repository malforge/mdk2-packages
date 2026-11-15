using System;
using Lumel.Layout;

namespace Lumel
{
    /// <summary>
    ///     An element that holds other elements, which may draw outside its bounds, with padding between its edge and
    ///     its contents.
    /// </summary>
    public abstract class Container : Element, IPadded
    {
        const Align Low = Align.Left | Align.Top;
        const Align High = Align.Right | Align.Bottom;

        Thickness _padding;

        /// <summary>Gets the space inside the bounds within which the children are measured and placed.</summary>
        /// <remarks>Set by this element's looks.</remarks>
        public Thickness Padding => _padding;

        /// <inheritdoc />
        void IPadded.Pad(Thickness padding)
        {
            if (_padding == padding)
                return;

            _padding = padding;
            InvalidateMeasure();
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>
        ///     Gets the alignment this container applies to a child whose <see cref="Align" /> leaves an axis to the
        ///     container, on every axis it does not stack along.
        /// </summary>
        protected abstract Align Aligns { get; }

        /// <summary>
        ///     Computes the position (<paramref name="at" />) and length (<paramref name="length" />) of
        ///     <paramref name="child" /> on one axis of the space of size <paramref name="room" /> starting at
        ///     <paramref name="start" />.
        /// </summary>
        /// <remarks>
        /// <paramref name="given" /> is a length the child's placement fixes, or NaN; <paramref name="desired" /> is the length it
        /// asked for.
        /// </remarks>
        protected void Span(Element child, bool across, float start, float room, float given, float desired, out float at,
            out float length)
        {
            var edges = EdgesOf(child, across);
            var stretched = (edges & Low) != 0 == ((edges & High) != 0) && edges != Align.Center;

            length = !float.IsNaN(given) ? given : stretched ? room : desired;
            if (length <= 0f || length > room)
                length = room;

            at = start + (room - length) * ShareBefore(child, across);
        }

        /// <summary>
        ///     Returns the share of the room left over on one axis that goes before <paramref name="child" /> by its
        ///     alignment: 0 where it is pinned to the start or stretched, 1 where it is pinned to the end, and a half where it
        ///     is centered.
        /// </summary>
        protected float ShareBefore(Element child, bool across)
        {
            var edges = EdgesOf(child, across);
            if ((edges & Low) != 0)
                return 0f;

            if ((edges & High) != 0)
                return 1f;

            return edges == Align.Center ? 0.5f : 0f;
        }

        /// <summary>
        ///     Returns the edges <paramref name="child" /> is aligned to on one axis, or <see cref="Align.Center" />, taking
        ///     this container's <see cref="Aligns" /> where the child's own alignment leaves the axis to it.
        /// </summary>
        Align EdgesOf(Element child, bool across)
        {
            var edges = Edges(child.Align, across);
            return edges != Align.Default ? edges : Edges(Aligns, across);
        }

        /// <summary>
        ///     Returns the length that <paramref name="sized" /> fixes on one axis, or <paramref name="otherwise" /> if
        ///     it fixes none.
        /// </summary>
        protected static float Given(ISized sized, bool across, float otherwise)
        {
            if (sized == null)
                return otherwise;

            var given = across ? sized.Width : sized.Height;
            return float.IsNaN(given) ? otherwise : given;
        }

        /// <summary>
        ///     Rejects <paramref name="placement" /> if it is not <see cref="ISized" />, for a container that places
        ///     its children by size alone.
        /// </summary>
        /// <remarks>Throws where it is neither null nor <see cref="ISized" />.</remarks>
        protected void AdmitSized(IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <summary>
        ///     Returns what <paramref name="align" /> specifies for one axis: its edges there, otherwise
        ///     <see cref="Align.Center" /> if set, otherwise nothing.
        /// </summary>
        static Align Edges(Align align, bool across)
        {
            var edges = align & (across ? Align.FillWidth : Align.FillHeight);
            return edges != Align.Default ? edges : align & Align.Center;
        }

        /// <summary>
        ///     Creates the collection that holds the children, which checks that the container can place each child and
        ///     notifies it of every change.
        /// </summary>
        protected new Collection CreateChildren()
        {
            return CreateChildren(Admit, OnChildAdded, OnChildRemoved, OnChildPlacementChanged);
        }

        /// <summary>
        ///     Checks that this container can place <paramref name="child" /> using <paramref name="placement" />
        ///     before it is added or changed.
        /// </summary>
        /// <remarks>A container that cannot place it throws.</remarks>
        protected virtual void Admit(Element child, IPlacement placement)
        {
        }

        /// <summary>
        ///     Called when <paramref name="child" /> has been added to the children at <paramref name="index" />.
        /// </summary>
        protected virtual void OnChildAdded(Element child, int index)
        {
        }

        /// <summary>
        ///     Called when <paramref name="child" /> has been removed from the children at <paramref name="index" />.
        /// </summary>
        protected virtual void OnChildRemoved(Element child, int index)
        {
        }

        /// <summary>Called when the placement of <paramref name="child" /> has changed.</summary>
        protected virtual void OnChildPlacementChanged(Element child)
        {
        }
    }
}
