using System.Text;
using AutomataLib;

namespace AutomataRepresentations
{
    public static class DotLanguagePrinter
    {
        public static string ToDotLanguage(IFiniteAutomaton fa, DotRankDirection direction = DotRankDirection.LeftRight)
        {
            var sb = new StringBuilder();

            sb.AppendLine("digraph dfa {");
            switch (direction)
            {
                case DotRankDirection.TopBottom:
                    sb.AppendLine("size=\"8.25,11\"; /* A4 paper portrait: 8.27 × 11.69 inches */");
                    sb.AppendLine("rankdir=TB;");
                    break;
                case DotRankDirection.LeftRight:
                    sb.AppendLine("size=\"11,8.25\"; /* A4 paper landscape: 11.69 x 8.27 inches */");
                    sb.AppendLine("rankdir=LR;");
                    break;
            }

            // start state arrow indicator
            sb.AppendLine("n999999 [style=invis];");        // Invisible pseudo node required
            sb.AppendLine("n999999 -> n" + fa.StartState);  // Edge into start state

            // label states (overriding default n0, n1 names)
            foreach (int state in fa.GetTrimmedStates())
            {
                sb.AppendLine("n" + state + " [label=\"" + fa.DescribeState(state) + "\"]");
            }

            // accepting states are double circles
            foreach (int state in fa.GetAcceptStates())
            {
                sb.AppendLine("n" + state + " [peripheries=2];");
            }

            // nodes and edges are defined by transitions
            foreach (var t in fa.GetTrimmedTransitions())
            {
                sb.AppendLine("n" + t.SourceState + " -> n" + t.TargetState + " [label=\"" + t.Label + "\"];");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
