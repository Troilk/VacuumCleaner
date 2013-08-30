using System.Collections.Generic;
using System.Linq;
using System.Text;
using VacuumCleaner.Env;

namespace VacuumCleaner
{
    public class RandomNumGen
    {
        #region Declarations

        private int index_;
        private List<double> rand_num_;
        private System.Random Rand;

        #endregion

        #region Methods

        public RandomNumGen(int seed)
        {
            Rand = new System.Random(seed);

            rand_num_ = new List<double>(Environment.mazeSize_ * Environment.mazeSize_ * Evaluator.LIFE_TIME);
            for (int i=0; i<rand_num_.Capacity; i++)
            {
                rand_num_.Add(Rand.NextDouble());
            }
            index_ = 0;
        }

        public double RandomValue()
        {
            return Rand.NextDouble();
        }

        public double RandomValueA()
        {
            if (index_ >= rand_num_.Capacity)
            {
                throw new System.Exception("Insufficient random values");
            }
            return rand_num_[index_++];
        }

        #endregion
    }
}
