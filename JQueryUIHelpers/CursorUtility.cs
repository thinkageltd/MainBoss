namespace JQueryUIHelpers
{
    internal static class CursorUtility
    {
        internal static string GetCursorText(Cursor cursor)
        {
            switch (cursor)
            {
                case JQueryUIHelpers.Cursor.NResize:
                    return "n-resize";
                case JQueryUIHelpers.Cursor.NEResize:
                    return "ne-resize";
                case JQueryUIHelpers.Cursor.EResize:
                    return "e-resize";
                case JQueryUIHelpers.Cursor.SEResize:
                    return "se-resize";
                case JQueryUIHelpers.Cursor.SResize:
                    return "s-resize";
                case JQueryUIHelpers.Cursor.SWResize:
                    return "sw-resize";
                case JQueryUIHelpers.Cursor.WResize:
                    return "w-resize";
                case JQueryUIHelpers.Cursor.NWResize:
                    return "nw-resize";
                case JQueryUIHelpers.Cursor.AllScroll:
                    return "all-scroll";
                case JQueryUIHelpers.Cursor.ColResize:
                    return "col-resize";
                case JQueryUIHelpers.Cursor.RowResize:
                    return "row-resize";
                case JQueryUIHelpers.Cursor.NoDrop:
                    return "no-drop";
                case JQueryUIHelpers.Cursor.NotAllowed:
                    return "not-allowed";
                case JQueryUIHelpers.Cursor.VerticalText:
                    return "vertical-text";
                default:
                    return cursor.ToString().ToLowerInvariant();
            }
        }
    }
}
