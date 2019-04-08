using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeGrammar
{
    /// <summary>
    /// A set of LR(0) items constructed via the subset construction algorithm. Each set of LR items,
    /// constructed from the NFA of all possible items, will represent a single state in a DFA. This
    /// DFA is our so called "LR(0) viable prefix recognition machine".
    /// The DFA states (and transitions) can also be constructed in a more efficient single pass algorithm.
    /// </summary>
    public class ProductionItemSet : IEquatable<ProductionItemSet>
    {
        public ProductionItemSet(ProductionItem coreItem, IEnumerable<ProductionItem> closureItems)
        {
            CoreItem = coreItem;
            ClosureItems = new HashSet<ProductionItem>(closureItems);
        }

        // The partially parsed rules for a state are called its core LR(0) items. If we also call S′ −→ .S
        // a core item, we observe that every state in the DFA is completely determined by its subset of core items.
        // The other items in the state are obtained via ϵ-closure. Therefore the DFA can be constructed in
        // one step without the intermediate NFA.
        public ProductionItem CoreItem { get; } // NOTE: After minimization many core items in one DFA state

        // These additional items are called the "closure" of the core items. All closure items
        // have the dot at the beginning of the rule, and these items have therefore not been parsed yet,
        // and therefore the closure items are not partially parsed rules.
        public IReadOnlyCollection<ProductionItem> ClosureItems { get; }

        public bool Contains(ProductionItem item)
        {
            return CoreItem.Equals(item) || ClosureItems.Contains(item);
        }

        public bool Equals(ProductionItemSet other)
        {
            if (other == null) return false;
            return CoreItem.Equals(other.CoreItem);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProductionItemSet)) return false;
            return Equals((ProductionItemSet) obj);
        }

        public override int GetHashCode()
        {
            return CoreItem.GetHashCode();
        }
    }
}
