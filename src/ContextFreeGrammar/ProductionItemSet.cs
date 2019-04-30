using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutomataLib;

namespace ContextFreeGrammar
{
    /// <summary>
    /// A set of LR(0) items that together form a single state in the DFA of the so-called LR(0) automaton.
    /// This DFA is our so called "LR(0) viable prefix (handle) recognizer" used to construct
    /// the parser table of any shift/reduce LR parser. Note that all states of the DFA except the initial state
    /// satisfies the so-called spelling property that only a single label/symbol will move/transition into that state.
    /// Thus each state except the initial state has a unique grammar symbol associated with it.
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ",nq}")]
    public class ProductionItemSet : IEquatable<ProductionItemSet>
    {
        private string DebuggerDisplay => ClosureItems.Any()
            ? string.Concat(CoreItems.ToVectorString(), ":", ClosureItems.ToVectorString())
            : CoreItems.ToVectorString();

        private readonly HashSet<ProductionItem> _coreItems;    // always non-empty (the core items identifies the LR(0) item set)
        private readonly List<ProductionItem> _closureItems;    // can be empty (can always be generated on the fly, but we calculate anyway)

        public ProductionItemSet(IEnumerable<ProductionItem> items)
        {
            _coreItems = new HashSet<ProductionItem>();
            _closureItems = new List<ProductionItem>();
            foreach (var item in items)
            {
                if (item.IsCoreItem)
                    _coreItems.Add(item);
                else
                    _closureItems.Add(item);
            }
        }

        // TODO: Do we need the unique (spelling property) grammar symbol associated with this state as part of the API?

        public IEnumerable<ProductionItem> Items => _coreItems.Concat(_closureItems);

        /// <summary>
        /// The partially parsed rules for a state are called its core LR(0) items.
        /// If we also call S′ −→ .S a core item, we observe that every state in the
        /// DFA is completely determined by its subset of core items.
        /// </summary>
        public IEnumerable<ProductionItem> CoreItems => _coreItems;

        /// <summary>
        /// The closure items (obtained via ϵ-closure) do not determine the state of the LR(0) automaton,
        /// because they can all be forgotten about, and regenerated on the fly. All closure items have
        /// the dot at the beginning of the rule, and are therefore not parsed yet.
        /// </summary>
        public IEnumerable<ProductionItem> ClosureItems => _closureItems;

        /// <summary>
        /// Reduce items
        /// </summary>
        public IEnumerable<ProductionItem> ReduceItems => Items.Where(item => item.IsReduceItem);

        /// <summary>
        /// Goto or shift items (this is the core items of the GOTO function in dragon book)
        /// </summary>
        public ILookup<Symbol, ProductionItem> GetTargetItems()
        {
            return Items
                .Where(item => !item.IsReduceItem)
                .ToLookup(item => item.GetNextSymbol(), item => item.GetNextItem());
        }

        public bool Equals(ProductionItemSet other)
        {
            return other != null && _coreItems.SetEquals(other.CoreItems);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProductionItemSet)) return false;
            return Equals((ProductionItemSet) obj);
        }

        public override int GetHashCode()
        {
            int hashCode = 17;
            foreach (var item in CoreItems)
                hashCode = 31 * hashCode + item.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return ClosureItems.Any()
                ? string.Concat(CoreItems.ToVectorString(), Environment.NewLine, ClosureItems.ToVectorString())
                : CoreItems.ToVectorString();
        }
    }

    /// <summary>
    /// A specialization of a Deterministic Pushdown Automaton (DPDA)
    /// </summary>
    public class LrParser
    {
        private readonly Grammar _grammar;

        public LrParser(Grammar grammar)
        {
            _grammar = grammar;
        }

        enum LrActionKind
        {
            Error = 0,
            Shift,          // Si, where i is a state
            Reduce,         // Rj, where j is production index
            Accept
        }

        struct LrAction
        {
            private readonly LrActionKind _kind;
            private readonly int _value;

            public LrAction(LrActionKind kind, int value)
            {
                _kind = kind;
                _value = value;
            }

            public bool IsShift => _kind == LrActionKind.Shift;
            public bool IsReduce => _kind == LrActionKind.Reduce;
            public bool IsAccept => _kind == LrActionKind.Accept;
            public bool IsError => _kind == LrActionKind.Error;

            public int ShiftTo => _value;

            public Production ReduceTo(Grammar g) => g.Productions[_value];
        }

        // TODO
        // DFA should be converted to Parsing table (adjacency matrix with hashtable for symbols),
        // and item sets should be converted to integer states

        private LrAction Action(int state, Terminal token)
        {
            // 4 possible outcomes
            //      shift to state i
            //      reduce production j
            //      accept
            //      error

            // TODO
            return new LrAction();
        }

        private int Goto(int state, NonTerminal variable)
        {
            // TODO
            return 0;
        }

        // Driver program here
        public void Parse(string input)
        {
            int s0 = 0;
            // stack of states (each state uniquely identifies a symbol, such that each
            // configuration (s(0)s(1)...s(m), a(i)a(i+1)...a(n)$) of the parser can generate a
            // right sentential form X(1)X(2)...X(m)a(i+1)...a(n)$). That is X(i) is the grammar
            // symbol associated with state s(i), i > 0. Note the s(0) is the only state not associated
            // with a grammar symbol, because this state represents the initial state og the LR(0) automaton
            // and its role is as a bottom-of-stack marker we can use to accept the parsed string.
            Stack<int> stack = new Stack<int>(s0); // initial state

            foreach (var a in Letterizer<Terminal>.Default.GetLetters(input))
            {
                int s = stack.Peek();
                var action = Action(s, a);
                // Action(s, a) = shift t
                if (action.IsShift)
                {
                    // push t onto the stack
                    int t = action.ShiftTo;
                    stack.Push(t);
                    // TODO: GetNextInputSymbol (local function)...do not use foreach above!!!!
                }
                // Action(s, a) = reduce A → β (DFA recognized a handle)
                else if (action.IsReduce)
                {
                    Production p = action.ReduceTo(_grammar);
                    // pop |β| symbols off the stack
                    stack.PopItemsOfLength(p.Length);
                    // let state t now be on top of the stack
                    int t = stack.Peek();
                    // push GOTO(t, A) onto the stack
                    int v = Goto(t, p.Head);
                    stack.Push(v);
                    // output the production A → β
                    Console.WriteLine(p);
                }
                // DFA recognized a the accept handle of the initial item set
                else if (action.IsAccept)
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
