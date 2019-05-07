using System.Collections.Generic;

namespace AutomataLib.Tables
{
    /// <summary>
    /// Immutable table containing metadata (columns, aligning etc) but no data-rows
    /// </summary>
    public class Table
    {
        public Table(string title, List<Column> columns)
        {
            Title = title;
            Columns = columns;
        }

        public string Title { get; }

        public IReadOnlyList<Column> Columns { get; }
    }
}
