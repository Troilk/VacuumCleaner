using System.Collections.Generic;
using System.Linq;
using System.Text;
using VacuumCleaner.Env;

namespace VacuumCleaner.Agents
{
    public class RandomAgent : IAgent
    {
        #region Declarations

        public string Name { get { return "RandomAgent"; } }

        private bool bump_ = false;
        private bool dirty_ = false;
        private System.Random Rand = new System.Random();

        #endregion

        #region Methods

        public void Perceive(Environment env)
        {
            bump_ = env.isJustBump;
            dirty_ = env.IsCurrentPosDirty();
        }

        public ActionType Think()
        {
            if (dirty_) return ActionType.actSUCK;
                else return (ActionType)Rand.Next(4);
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
