namespace AutomataLib.Tables
{
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
}
