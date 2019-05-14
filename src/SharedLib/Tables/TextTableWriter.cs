using System.IO;
using System.Linq;

namespace AutomataLib.Tables
{
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
                    ? Center(Table.Title, TableWidth - 2)
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
                    ? Center(Table.Columns[i].Name, Table.Columns[i].Width)
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
                                s = Center(values[i], Table.Columns[i].Width);
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
            Out.WriteLine();
        }

        private static string Center(string str, int totalWidth)
        {
            return str.PadLeft((totalWidth - str.Length) / 2 + str.Length).PadRight(totalWidth);
        }

    }
}
