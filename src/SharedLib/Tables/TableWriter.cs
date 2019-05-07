using System.IO;

namespace AutomataLib.Tables
{
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
}
