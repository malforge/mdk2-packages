using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    public static class HStackX
    {
        public static HStack HStack(this IIon ion) => ion.View<HStack>();

        // public static IReadOnlyList<TR> Rows<T, TR>(this IIon ion, IEnumerable<T> items, Func<T, TR> rowFn) where TR: IContainer
        // {
        //     var rows = new List<TR>();
        //     var columnWidths = ion.Lease<List<float>>();
        //     try
        //     {
        //         foreach (var item in items)
        //         {
        //             var row = rowFn(item);
        //             float rowHeight = 0;
        //             for (var i = 0; i < row.Children.Count; i++)
        //             {
        //                 var child = row.Children[i];
        //                 if (child == null)
        //                     continue;
        //                 if (columnWidths.Count <= i) columnWidths.Add(0);
        //                 var childBounds = child.Get<RectangleF>("Bounds");
        //                 var childMargin = child.Get<Thickness>("Margin");
        //                 columnWidths[i] = Math.Max(columnWidths[i], childBounds.Width);
        //                 rowHeight = Math.Max(rowHeight, childBounds.Height + childMargin.VerticalThickness);
        //             }
        //             row.Set("Bounds", new RectangleF(0, 0, 0, rowHeight));
        //             rows.Add(row);
        //         }
        //
        //         foreach (var row in rows)
        //         {
        //             var width = 0f;
        //             for (var j = 0; j < row.Children.Count; j++)
        //             {
        //                 var child = row.Children[j];
        //                 if (child == null)
        //                     continue;
        //                 var childBounds = child.Get<RectangleF>("Bounds");
        //                 var childMargin = child.Get<Thickness>("Margin");
        //                 width += columnWidths[j] + childMargin.HorizontalThickness;
        //                 child.Set("Bounds", new RectangleF(
        //                     childBounds.X,
        //                     childBounds.Y,
        //                     columnWidths[j],
        //                     childBounds.Height));
        //             }
        //
        //             var rowBounds = row.Get<RectangleF>("Bounds");
        //             row.Set("Bounds", new RectangleF(0, 0, width, rowBounds.Height));
        //         }
        //
        //         return rows;
        //     }
        //     finally
        //     {
        //         rows.Clear();
        //         columnWidths.Clear();
        //     }
        // }
        
        public static T Rows<T, TData, TRow>(this T view, IEnumerable<TData> items, Func<TData, TRow> rowFn) where T : IContainer where TRow: IContainer
        {
            var columnWidths = view.Context.Lease<List<float>>();
            try
            {
                var firstRow = view.Children.Count;
                foreach (var item in items)
                {
                    var row = rowFn(item);
                    float rowHeight = 0;
                    for (var i = 0; i < row.Children.Count; i++)
                    {
                        var child = row.Children[i];
                        if (child == null)
                            continue;
                        if (columnWidths.Count <= i) columnWidths.Add(0);
                        var childBounds = child.Get<RectangleF>("Bounds");
                        var childMargin = child.Get<Thickness>("Margin");
                        columnWidths[i] = Math.Max(columnWidths[i], childBounds.Width);
                        rowHeight = Math.Max(rowHeight, childBounds.Height + childMargin.VerticalThickness);
                    }

                    row.Set("Bounds", new RectangleF(0, 0, 0, rowHeight));
                    view.Add(row);
                }

                for (var i = firstRow; i < view.Children.Count; i++)
                {
                    var row = (IContainer)view.Children[i];
                    var width = 0f;
                    for (var j = 0; j < row.Children.Count; j++)
                    {
                        var child = row.Children[j];
                        if (child == null)
                            continue;
                        var childMargin = child.Get<Thickness>("Margin");
                        width += columnWidths[j] + childMargin.HorizontalThickness;
                        var childBounds = child.Get<RectangleF>("Bounds");
                        child.Set("Bounds", new RectangleF(
                            childBounds.X,
                            childBounds.Y,
                            columnWidths[j],
                            childBounds.Height));
                    }

                    var rowBounds = row.Get<RectangleF>("Bounds");
                    row.Set("Bounds", new RectangleF(0, 0, width, rowBounds.Height));
                }

                return view;
            }
            finally
            {
                columnWidths.Clear();
            }
        }
    }
}