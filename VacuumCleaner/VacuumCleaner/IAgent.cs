using System.Collections.Generic;
using System.Linq;
using System.Text;
using VacuumCleaner.Env;

namespace VacuumCleaner
{
    public enum ActionType { actUP = 0, actDOWN, actLEFT, actRIGHT, actSUCK, actIDLE };

    interface IAgent
    {
        string Name { get; }

        void Perceive(Environment env);
        ActionType Think();
    }
}
