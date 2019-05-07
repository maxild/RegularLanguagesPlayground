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
            // ╔═══════════════════════════════════════════════════╗
            // ║                      MyTitle                      ║
            // ╠════════════╤════════════╤════════════╤════════════╣
            // ║  Variable  │  Nullable  │   First    │   Follow   ║
            // ╠════════════╪════════════╪════════════╪════════════╣

            char topLeftCorner, topRightCorner;

            if (!string.IsNullOrEmpty(Table.Title))
            {
                // ╔═══════════════════════════════════════════════════╗
                Out.Write('╔');
                Out.Write(new string('═', TableWidth - 2));
                Out.WriteLine('╗');

                string title = Table.Title.Length <= TableWidth - 2
                    ? Table.Title.Center(TableWidth - 2)
                    : Table.Title.Substring(0, TableWidth - 2);

                // ║                      MyTitle                      ║
                Out.Write('║');
                Out.Write(title);
                Out.WriteLine("║");

                topLeftCorner = '╠';
                topRightCorner = '╣';
            }
            else
            {
                topLeftCorner = '╔';
                topRightCorner = '╗';
            }

            // ╠════════════╤════════════╤════════════╤════════════╣
            Out.Write(topLeftCorner);
            for (int i = 0; i < Table.Columns.Count; i++)
            {
                Out.Write(new string('═', Table.Columns[i].Width));
                Out.Write(i < Table.Columns.Count - 1 ? '╤' : topRightCorner);
            }
            Out.WriteLine();

            // ║  Variable  │  Nullable  │   First    │   Follow   ║
            Out.Write('║');
            for (int i = 0; i < Table.Columns.Count; i++)
            {
                string name = Table.Columns[i].Name.Length <= Table.Columns[i].Width
                    ? Table.Columns[i].Name.Center(Table.Columns[i].Width)
                    : Table.Columns[i].Name.Substring(0, Table.Columns[i].Width);
                Out.Write(name);
                Out.Write(i < Table.Columns.Count - 1 ? '│' : '║');
            }
            Out.WriteLine();

            // ╠════════════╪════════════╪════════════╪════════════╣
            Out.Write('╠');
            for (int i = 0; i < Table.Columns.Count; i++)
            {
                Out.Write(new string('═', Table.Columns[i].Width));
                Out.Write(i < Table.Columns.Count - 1 ? '╪' : '╣');
            }
            Out.WriteLine();
        }

        public override void WriteRow(params string[] values)
        {
            // ║E           │   false    │ {(, -, a}  │ {$, +, )}  ║
            Out.Write('║');

            for (int i = 0; i < Table.Columns.Count; i++)
            {
                if (i < values.Length)
                {
                    string s;
                    if (values[i].Length >= Table.Columns[i].Width)
                    {
                        s = values[i].Substring(0, Table.Columns[i].Width);
                    }
                    else
                    {
                        switch (Table.Columns[i].Align)
                        {
                            case Align.Left:
                                s = values[i].PadRight(Table.Columns[i].Width);
                                break;
                            case Align.Right:
                                s = values[i].PadLeft(Table.Columns[i].Width);
                                break;
                            case Align.Center:
                            default:
                                s = values[i].Center(Table.Columns[i].Width);
                                break;
                        }
                    }

                    Out.Write(s);
                }
                else
                {
                    Out.Write(new string(' ', Table.Columns[i].Width));
                }

                if (i < Table.Columns.Count - 1)
                    Out.Write('│');
            }

            Out.WriteLine('║');
        }

        public override void WriteFooter()
        {
            // ╚════════════╧════════════╧════════════╧════════════╝
            Out.Write('╚');
            for (int i = 0; i < Table.Columns.Count; i++)
            {
                Out.Write(new string('═', Table.Columns[i].Width));
                Out.Write(i < Table.Columns.Count - 1 ? '╧' : '╝');
            }
        }
    }
}
