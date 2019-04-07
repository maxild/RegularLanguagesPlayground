using System.Collections.Generic;
using AutomataLib;

namespace AutomataRepresentations
{
    public interface IFiniteAutomaton
    {
        int StartState { get; }

        bool IsAcceptState(int state);

        IEnumerable<int> GetStates();
        IEnumerable<int> GetTrimmedStates();

        string DescribeState(int state);

        //IEnumerable<TAlphabet> GetAlphabet();
        //string DescribeLabel(char label);

        IEnumerable<int> GetAcceptStates();

        IEnumerable<Transition<char, int>> GetTransitions();
        IEnumerable<Transition<char, int>> GetTrimmedTransitions();
    }
}
