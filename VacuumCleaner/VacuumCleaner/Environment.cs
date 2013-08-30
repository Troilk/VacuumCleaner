using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace VacuumCleaner.Env
{
    public partial class Environment
    {
        #region Declaration

        private const int OBSTACLE = -1;
        private const string MAP_ROAD = "-", MAP_OBSTACLE = "O";
        private const int CLEAN_PER_TIME = 10;

        private bool bump_;
        private int agentPosX_, agentPosY_;
        //int maze_[MAZE_SIZE][MAZE_SIZE]; // -1: Obstacle, >=0: amount of dirt
        private List<List<int>> maze_;
        private int randomSeed_;
        private double dirtyProb_;
        private long cleanedDirty_;
        private long newDirty_;
        private ActionType preAction_;

        public static int mazeSize_;
        public bool isJustBump { get { return bump_; } }
        public int RandomSeed { get { return randomSeed_; } }
        public long getCleanedDirty { get { return cleanedDirty_; } }
        public long getNewDirty { get { return newDirty_; } }

        #endregion

        #region Methods

        public Environment(string mapFileName)
        {
            string[] lines;
            using (TextReader reader = new StreamReader(mapFileName))
            {
                lines = reader.ReadToEnd().Replace("\r", "").Split('\n');
            }

            string[] vals = lines[1].Split(' ');
            mazeSize_ = int.Parse(vals[0]);
            if (mazeSize_ < 1)
                throw new Exception("Maze size < 1");
            agentPosX_ = int.Parse(vals[1]);
            agentPosY_ = int.Parse(vals[2]);
            dirtyProb_ = double.Parse(vals[3], System.Globalization.CultureInfo.InvariantCulture);
            randomSeed_ = int.Parse(vals[4]);

            maze_ = new List<List<int>>(mazeSize_);
            for (int i = 0; i < mazeSize_; ++i)
            {
                maze_.Add(new List<int>(mazeSize_));
                vals = lines[i + 2].Split(' ');
                for (int j = 0; j < mazeSize_; ++j)
                {
                    if (vals[j] == MAP_ROAD)
                        maze_[i].Add(0);
                    else
                        if (vals[j] == MAP_OBSTACLE)
                            maze_[i].Add(OBSTACLE);
                        else
                            throw new Exception("Wrong map description!");
                }
            }

            bump_ = false;
            preAction_ = ActionType.actIDLE;
            cleanedDirty_ = 0;
            newDirty_ = 0;
        }

        public int DirtAmount(int x, int y)
        {
            if (maze_[x][y] == OBSTACLE) return 0;
                else return maze_[x][y];
        }

        public void Change(RandomNumGen rng)
        {
            newDirty_ = 0;
            for (int row = 0; row < mazeSize_; row++)
            {
                for (int col = 0; col < mazeSize_; col++)
                {
                    if (maze_[row][col] != OBSTACLE &&
                        rng.RandomValue() < dirtyProb_) // probability: 0.01
                    {
                        maze_[row][col] ++;
                        newDirty_++;
                    }
                }
            }
        }

        public void AcceptAction(ActionType action)
        {
            bump_ = false;
            cleanedDirty_ = 0;
            switch (action)
            {
                case ActionType.actSUCK:
                    if (maze_[agentPosX_][agentPosY_] > 0)
                    {
                        cleanedDirty_ = (maze_[agentPosX_][agentPosY_] > CLEAN_PER_TIME ? CLEAN_PER_TIME : maze_[agentPosX_][agentPosY_]);
                        maze_[agentPosX_][agentPosY_] -= (int)cleanedDirty_;
                    }
                    break;
                case ActionType.actUP:
                    if (maze_[agentPosX_ - 1][agentPosY_] != OBSTACLE) agentPosX_ -= 1;
                    else bump_ = true;
                    break;
                case ActionType.actDOWN:
                    if (maze_[agentPosX_ + 1][agentPosY_] != OBSTACLE) agentPosX_ += 1;
                    else bump_ = true;
                    break;
                case ActionType.actLEFT:
                    if (maze_[agentPosX_][agentPosY_ - 1] != OBSTACLE) agentPosY_ -= 1;
                    else bump_ = true;
                    break;
                case ActionType.actRIGHT:
                    if (maze_[agentPosX_][agentPosY_ + 1] != OBSTACLE) agentPosY_ += 1;
                    else bump_ = true;
                    break;
            }
            preAction_ = action;
        }

        public bool IsCurrentPosDirty()
        {
            Debug.Assert(agentPosX_ >= 0 && agentPosX_ < mazeSize_ && agentPosY_ >= 0 && agentPosY_ < mazeSize_);
            return maze_[agentPosX_][agentPosY_] > 0;
        }

        #endregion
    }
}
