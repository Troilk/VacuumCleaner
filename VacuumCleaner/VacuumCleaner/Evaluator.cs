using System.Collections.Generic;
using System.Linq;
using System.Text;
using VacuumCleaner.Env;

namespace VacuumCleaner
{
    public class Evaluator
    {
        #region Declarations

        public const int LIFE_TIME = 2000;

        private long dirtyDegree_ = 0, consumedEnergy_ = 0, totalDirtyDegree_ = 0, cleanedDirty_ = 0, newDirty_ = 0;

        public long DirtyDegree { get { return dirtyDegree_; } }
        public long ConsumedEnergy { get { return consumedEnergy_; } }
        public long TotalDirtyDegree { get { return totalDirtyDegree_; } }
        public long CleanedDirty { get { return cleanedDirty_; } }
        public long NewDirty { get { return newDirty_; } }

        #endregion

        #region Methods

        public void Eval(ActionType action, Environment env)
        {
            if (action == ActionType.actSUCK)
                consumedEnergy_ += 2;
            else if (action != ActionType.actIDLE)
                consumedEnergy_ += 1;
            cleanedDirty_ += env.getCleanedDirty;
            newDirty_ = env.getNewDirty;
            totalDirtyDegree_ += newDirty_;

            dirtyDegree_ = 0;
            for (int row = 0; row < Environment.mazeSize_; row++)
            {
                for (int col = 0; col < Environment.mazeSize_; col++)
                {
                    long da = env.DirtAmount(row, col);
                    dirtyDegree_ += da;
                }
            }
        }


        #endregion
    }
}
