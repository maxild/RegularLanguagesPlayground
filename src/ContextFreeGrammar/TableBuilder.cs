using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ContextFreeGrammar
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

        public Table Build()
        {
            return new Table(_title, _columns);
        }
    }

    // Columns are indexed 0,1,2,...,n

    // Rows are inserted via IEnumerable<string[]>, i.e. sequence of rows indexed by columns

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

    public class Column
    {
        public Column(string name, int width, Align align = Align.Center)
        {
            Name = name;
            Width = width;
            Align = align;
        }

        public string Name { get; }

        public int Width { get; }

        public Align Align { get; }
    }

    // TODO: make abstract
    //      latex,        https://www.overleaf.com/project/5cd00f077680ad36299002cf
    //      markdown,     https://dillinger.io/
    //      html,         https://codepen.io/
    //      text          Console.Write/Console.WriteLine API

    // NOTE: Console.WriteLine API used directly here (including colors)...forget about color support!!!!
    // TODO: System.IO.TextWriter (StringWriter, StreamWriter, HttpWriter, Console.Out Property)

    public abstract class TableWriter
    {
        public abstract void WriteHead();
        public abstract void WriteRow(params string[] values);
        public abstract void WriteFooter();

        protected TableWriter(TextWriter @out)
        {
            Out = @out;
        }

        protected TextWriter Out { get; }
    }

    public class TextTableWriter : TableWriter
    {
        public TextTableWriter(Table table, TextWriter writer)
            : base(writer)
        {
            Table = table;
        }

        public Table Table { get; }

        private int? _tableWidth;
        private int TableWidth => _tableWidth ?? (_tableWidth = Table.Columns.Sum(x => x.Width) + Table.Columns.Count + 1).GetValueOrDefault();

        public override void WriteHead()
        {
            var str = new List<StringBuilder>(5);
            for (int i = 0; i < str.Capacity; i++)
                str.Add(new StringBuilder(TableWidth));

            //str[0] Øvre grænse                                           "╔═════════════════════════════╗"
            //str[1] Tabeloverskrift                                       "║       Tabeloverskrift       ║"
            //str[2] Border mellem tabeloverskrift og kolonne navne        "╠═════╤═══════════╤═══════════╣"
            //str[3] Kolonne navne                                         "║ Nr. │  Fornavn  │ Efternavn ║"
            //str[4] Border mellem kolonne navne og indhold                "╠═════╪═══════════╪═══════════╣"

            if (!string.IsNullOrEmpty(Table.Title))
            {
                str[0].Append('╔');
                str[0].Append('═', TableWidth - 2);
                str[0].Append('╗');

                str[1].Append('║');
                str[1].Append(Table.Title.Length <= TableWidth - 2
                    ? Table.Title.Center(TableWidth - 2)
                    : Table.Title.Substring(0, TableWidth - 2));
                str[1].Append("║");
            }

            str[2].Append(!string.IsNullOrEmpty(Table.Title) ? '╠' : '╔');
            str[3].Append('║');
            str[4].Append('╠');
            foreach (Column c in Table.Columns)
            {
                str[2].Append('═', c.Width);
                str[2].Append('╤');
                str[3].Append(c.Name.Length <= c.Width
                    ? c.Name.Center(c.Width)
                    : c.Name.Substring(0, c.Width));
                str[3].Append('│');
                str[4].Append('═', c.Width);
                str[4].Append('╪');
            }
            str[2].Replace('╤', !string.IsNullOrEmpty(Table.Title) ? '╣' : '╗', str[2].Length - 1, 1);
            str[3].Replace('│', '║', str[3].Length - 1, 1);
            str[4].Replace('╪', '╣', str[4].Length - 1, 1);

            foreach (StringBuilder s in str)
                if (s.ToString() != "")
                    Out.WriteLine(s);
        }

        public override void WriteRow(params string[] values)
        {
            StringBuilder str = new StringBuilder(TableWidth);

            Out.Write('║');
            for (int i = 0; i < Table.Columns.Count; i++)
            {
                if (values.GetUpperBound(0) >= i)
                    if (values[i].Length >= Table.Columns[i].Width)
                    {
                        Out.Write(values[i].Substring(0, Table.Columns[i].Width));
                    }
                    else
                        switch (Table.Columns[i].Align)
                        {
                            case Align.Left:
                                Out.Write(values[i].PadRight(Table.Columns[i].Width));
                                break;
                            case Align.Center:
                                Out.Write(values[i].Center(Table.Columns[i].Width));
                                break;
                            case Align.Right:
                                Out.Write(values[i].PadLeft(Table.Columns[i].Width));
                                break;
                        }
                else
                    Out.Write(new string(' ', Table.Columns[i].Width));
                Out.Write(i != Table.Columns.Count - 1 ? '│' : '║');
            }

            Out.WriteLine(str);
        }

        public override void WriteFooter()
        {
            StringBuilder str = new StringBuilder(TableWidth + 1);
            str.Append('╚');
            foreach (Column c in Table.Columns)
            {
                str.Append('═', c.Width);
                str.Append('╧');
            }
            str.Replace('╧', '╝', str.Length - 1, 1);
            Out.WriteLine(str);
        }
    }
}
