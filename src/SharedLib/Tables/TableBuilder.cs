using System.Collections.Generic;

namespace AutomataLib.Tables
{
    public class TableBuilder
    {
        private string _title;
        private readonly List<Column> _columns = new List<Column>();

        public TableBuilder SetTitle(string title)
        {
            _title = title;
            return this;
        }

        public TableBuilder SetColumns(params Column[] columns)
        {
            _columns.AddRange(columns);
            return this;
        }

        public TableBuilder SetColumns(IEnumerable<Column> columns)
        {
            _columns.AddRange(columns);
            return this;
        }

        public Table Build()
        {
            return new Table(_title, _columns);
        }
    }
}
