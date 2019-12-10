using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutomataLib.Graphs;

namespace AutomataLib
{
    // Format this file as a Postscript file with ");
    //    dot -Tps [filename.dot] -o out.ps
    //
    // Web pages for drawing with dot
    // http://viz-js.com/                       ( The BEST)
    // https://graphs.grevian.org/graph
    // http://www.webgraphviz.com/              (NOT good)
    // Tools for drawing with dot in the browser
    // https://github.com/magjac/d3-graphviz
    public static class DotLanguagePrinter
    {
        /// <summary>
        /// Print the initial sets and the digraph superset relations graph.
        /// </summary>
        public static string PrintGraph<TTokenKind>(
            string functionName,
            IReadOnlyList<IReadOnlySet<Terminal<TTokenKind>>> initSets,
            IGraph graph,
            Func<int, string> labelFunc = null,
            DotRankDirection direction = DotRankDirection.LeftRight
            ) where TTokenKind : Enum
        {
            var labelOf = labelFunc ?? (v => v.ToString());

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

            // label each vertex (node)
            foreach (var vertex in graph.Vertices)
            {
                sb.AppendLine("n" + vertex + " [label=\"" + labelOf(vertex) + "\"];");
            }

            // directed arcs are defined by edges
            foreach (var edge in graph.Edges)
            {
                sb.AppendLine("n" + edge.Item1 + " -> n" + edge.Item2);
            }

            // Show init sets: INITFIRST(E) = {..}, INITFIRST(T) = {..} etc...
            sb.AppendLine("node [shape=box];");
            sb.Append("node_docs [label=\"");
            for (int i = 0; i < initSets.Count; i++)
            {
                if (i > 0)
                    sb.Append(",\\l");

                sb.Append(functionName + "(" + labelOf(i) + ") = { ");
                int c = 0;
                foreach (var terminal in initSets[i])
                {
                    if (c > 0)
                        sb.Append(", ");
                    sb.Append(terminal.Name);
                    c += 1;
                }

                sb.Append(" }");
            }

            sb.AppendLine("\\l\"];");

            sb.AppendLine("}");

            return sb.ToString();
        }


        public static string ToDotLanguage<TAlphabet, TState>(
            IFiniteAutomaton<TAlphabet, TState> fa,
            DotRankDirection direction = DotRankDirection.LeftRight,
            bool skipStateLabeling = false)
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
            sb.AppendLine("n999999 [style=invis];");                                // Invisible pseudo node required
            sb.AppendLine("n999999 -> n" + fa.GetStateId(fa.StartState) + ";");     // Edge into start state

            // label states (overriding default n0, n1 names)
            if (skipStateLabeling)
            {
                foreach (TState state in fa.GetTrimmedStates())
                {
                    sb.AppendLine("n" + fa.GetStateId(state) + " [label=\"" + fa.GetStateId(state) + "\"];");
                }
            }
            else
            {
                foreach (TState state in fa.GetTrimmedStates())
                {
                    sb.AppendLine("n" + fa.GetStateId(state) + " [label=\"" + fa.GetStateLabel(state, "\\l") + "\"];");
                }
            }

            // accept states are double circles
            foreach (TState state in fa.GetAcceptStates())
            {
                sb.AppendLine("n" + fa.GetStateId(state) + " [peripheries=2];");
            }

            // nodes and edges are defined by transitions
            foreach (var t in fa.GetTrimmedTransitions())
            {
                sb.AppendLine("n" + fa.GetStateId(t.SourceState) + " -> n" + fa.GetStateId(t.TargetState) +
                              " [label=\"" + t.Label + "\"];");
            }

            if (skipStateLabeling)
            {
                sb.AppendLine("node [shape=box];");
                foreach (TState state in fa.GetTrimmedStates().Reverse())
                {
                    sb.AppendLine("b" + fa.GetStateId(state) + " [label=\"" + fa.GetStateId(state) + ": " + fa.GetStateLabel(state, "\\l     ") + "\"];");
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
