using System.Collections.Generic;
using System.Linq;
using System.Text;
using VacuumCleaner.Env;

namespace VacuumCleaner.Agents
{
    public class ModelAgentNoIdle : IAgent
    {
        #region Helper classes and structs
        private struct Tile
        {
            public TileStates State;
            public int LastVisit;

            public Tile(TileStates tileState, int lastVisit)
            {
                this.State = tileState;
                this.LastVisit = lastVisit;
            }
        }

        public struct Point
        {
            public int Row;
            public int Col;

            public Point(int row, int col)
            {
                this.Row = row;
                this.Col = col;
            }

            public int ManhattanDistance(Point point)
            {
                return System.Math.Abs(Row - point.Row) + System.Math.Abs(Col - point.Col);
            }

            public int ManhattanDistance(int row, int col)
            {
                return System.Math.Abs(Row - row) + System.Math.Abs(Col - col);
            }

            public static bool operator ==(Point p1, Point p2)
            {
                return p1.Row == p2.Row && p1.Col == p2.Col;
            }

            public static bool operator !=(Point p1, Point p2)
            {
                return p1.Row != p2.Row || p1.Col != p2.Col;
            }

            public static Point operator +(Point p1, Point p2)
            {
                return new Point(p1.Row + p2.Row, p1.Col + p2.Col);
            }
        }

        private struct NeighbourAction : System.IComparable
        {
            public TileStates State;
            public ActionType Action;
            public Point Offset;
            public int LastVisit;

            public NeighbourAction(TileStates state, ActionType action, Point offset)
            {
                this.State = state;
                this.Action = action;
                this.Offset = offset;
                this.LastVisit = 0;
            }

            public int CompareTo(object obj)
            {
                return LastVisit - ((NeighbourAction)obj).LastVisit;
            }
        }

        public class Node
        {
            public Point Value;
            public Node Parent = null;
            public int RealCost;
            public int HeuristicCost;

            public Node(Point val, int realCost, int heuristicCost, Node parentNode)
            {
                this.Value = val;
                this.RealCost = realCost;
                this.HeuristicCost = heuristicCost;
                this.Parent = parentNode;
            }
        }

        private struct PoolObject
        {
            public bool InFringe;
            public Node Value;
        }

        /// <summary>
        /// Used for A* to produce less garbage
        /// </summary>
        private class DataPool
        {
            private PoolObject[] Values;
            private int Top = 0;

            public int Count { get { return Top; } }

            public DataPool(int capacity)
            {
                Values = new PoolObject[capacity];
                for (int i = 0; i < capacity; ++i)
                    Values[i].Value = new Node(new Point(), 0, 0, null);
            }

            /// <summary>
            /// Adds node to fringe only if it is not already in fringe or it's cheaper (then old node is rewritten)
            /// </summary>
            /// <param name="point">point of the path</param>
            /// <param name="parent">parent node</param>
            /// <param name="realCost">real cost to this node</param>
            /// <param name="heuristicCost">heuristic cost to target node</param>
            public void Add(Point point, Node parent, int realCost, int heuristicCost)
            {
                int top = 0;
                for (; top < Top; ++top)
                    if (Values[top].Value.Value == point)
                        if (realCost + heuristicCost < Values[top].Value.RealCost + Values[top].Value.HeuristicCost)
                            break;
                        else return;

                Node node = Values[top].Value;
                Values[top].InFringe = true;
                node.Value = point;
                node.RealCost = realCost;
                node.HeuristicCost = heuristicCost;
                node.Parent = parent;
                ++Top;
            }

            public void RemoveFromFringe(Node node)
            {
                for (int i = 0; i < Top; ++i)
                    if (Values[i].Value == node)
                        Values[i].InFringe = false;
            }

            public void Clear()
            {
                for (int i = 0; i < Top; ++i)
                    Values[i].InFringe = false;
                Top = 0;
            }

            public Node GetBestNode()
            {
                if (Top == 0)
                    return null;
                int bestIdx = -1;
                int cost = int.MaxValue;
                Node val;

                for (int i = 0; i < Top; ++i)
                {
                    val = Values[i].Value;
                    if (Values[i].InFringe && ((val.HeuristicCost + val.RealCost) < cost))
                    {
                        bestIdx = i;
                        cost = val.RealCost + val.HeuristicCost;
                    }
                }

                if (bestIdx == -1)
                    return null;

                Values[bestIdx].InFringe = false;
                return Values[bestIdx].Value;
            }
        }

        #endregion

        #region Declarations

        private const float DistanceFactor = 0.0f;

        private enum TileStates : byte
        {
            None = 0,
            Wall = 1,
            Visited = 2,
            TopNV = 4,
            RightNV = 8,
            BottomNV = 16,
            LeftNV = 32
        };
        private static TileStates GreyTileSign = TileStates.TopNV | TileStates.RightNV |
            TileStates.BottomNV | TileStates.LeftNV;

        public string Name { get { return "ModelAgent"; } }

        /// <summary>
        /// Used while discovering the environment (phase 1)
        /// </summary>
        private Tile[,] HugeMap = new Tile[2 * Environment.mazeSize_ - 1, 2 * Environment.mazeSize_ - 1];
        /// <summary>
        /// Used in phase 2
        /// </summary>
        private Tile[,] SmallMap = new Tile[Environment.mazeSize_, Environment.mazeSize_];
        /// <summary>
        /// Used for stroring path to the nearest grey node generated by A* without creating garbage
        /// </summary>
        private DataPool Fringe = new DataPool(Environment.mazeSize_ * Environment.mazeSize_);

        /// <summary>
        /// Actions that can be performed in current tile with correspoinding position offsets
        /// </summary>
        private NeighbourAction[] NeighbourActions = new NeighbourAction[4] 
        { 
            new NeighbourAction(TileStates.TopNV, ActionType.actUP, new Point(-1, 0)),
            new NeighbourAction(TileStates.RightNV, ActionType.actRIGHT, new Point(0, 1)),
            new NeighbourAction(TileStates.BottomNV, ActionType.actDOWN, new Point(1, 0)),
            new NeighbourAction(TileStates.LeftNV, ActionType.actLEFT, new Point(0, -1))
        };

        /// <summary>
        /// Used for tracking current cleaner position. Initially with assume that cleaner is in the semter of HugeMap
        /// </summary>
        private Point CurrentPos = new Point(Environment.mazeSize_, Environment.mazeSize_);
        private Point PossiblePos = new Point(Environment.mazeSize_, Environment.mazeSize_);

        private bool Phase1Running = true;
        private bool Phase1Preparing = true;
        private Node GoingToGrey = null;

        private int CurrentIteration = -1;

        //Perceived values
        private bool IsBump;
        private bool IsDirty;

        #endregion

        #region Methods

        public void Perceive(Environment env)
        {
            IsBump = env.isJustBump;
            IsDirty = env.IsCurrentPosDirty();
        }

        public ActionType Think()
        {
            ++CurrentIteration;
            if (Phase1Running)
            {
                if (IsBump)
                {
                    HugeMap[PossiblePos.Row, PossiblePos.Col].State = (TileStates.Wall | TileStates.Visited);
                    PossiblePos = CurrentPos;
                }
                else
                    CurrentPos = PossiblePos;

                HugeMap[CurrentPos.Row, CurrentPos.Col].LastVisit = CurrentIteration;
            }
            else
                SmallMap[CurrentPos.Row, CurrentPos.Col].LastVisit = CurrentIteration;

            if (IsDirty)
            {
                return ActionType.actSUCK;
            }

            return Phase1Running ? Phase1Iteration() : Phase2Iteration();
        }

        /// <summary>
        /// Map discovery phase
        /// </summary>
        /// <returns>action for robot to perform</returns>
        private ActionType Phase1Iteration()
        {
            Tile[,] hugeMap = HugeMap;
            Point currentPos = CurrentPos;
            bool followPath = false;

            //If it's first iteration in the phase
            if (Phase1Preparing)
            {
                //set all tiles as black (unexplored)
                TileStates black = TileStates.TopNV | TileStates.RightNV | TileStates.BottomNV | TileStates.LeftNV;
                for (int i = 0; i < hugeMap.GetLength(0); ++i)
                    for (int j = 0; j < hugeMap.GetLength(1); ++j)
                    {
                        hugeMap[i, j] = new Tile(black, -1);
                    }
                //set initial tile as grey tile
                hugeMap[currentPos.Row - 1, currentPos.Col].State &= ~TileStates.BottomNV;
                hugeMap[currentPos.Row, currentPos.Col + 1].State &= ~TileStates.LeftNV;
                hugeMap[currentPos.Row + 1, currentPos.Col].State &= ~TileStates.TopNV;
                hugeMap[currentPos.Row, currentPos.Col - 1].State &= ~TileStates.RightNV;
                hugeMap[currentPos.Row, currentPos.Col].State |= TileStates.Visited;
                Phase1Preparing = false;
            }

            Tile currentTile = hugeMap[currentPos.Row, currentPos.Col];
            ActionType action = ActionType.actIDLE;

            //if we are not following path to nearest grey tile generated by A*
            if (GoingToGrey == null)
            {
                //if current tile is grey (has unexplored neighbours)
                if ((currentTile.State & GreyTileSign) != 0)
                {
                    if ((currentTile.State & TileStates.TopNV) != 0)
                    {
                        --currentPos.Row;
                        action = ActionType.actUP;
                    }
                    else
                        if ((currentTile.State & TileStates.RightNV) != 0)
                        {
                            ++currentPos.Col;
                            action = ActionType.actRIGHT;
                        }
                        else
                            if ((currentTile.State & TileStates.BottomNV) != 0)
                            {
                                ++currentPos.Row;
                                action = ActionType.actDOWN;
                            }
                            else
                                if ((currentTile.State & TileStates.LeftNV) != 0)
                                {
                                    --currentPos.Col;
                                    action = ActionType.actLEFT;
                                }

                    hugeMap[currentPos.Row - 1, currentPos.Col].State &= ~TileStates.BottomNV;
                    hugeMap[currentPos.Row, currentPos.Col + 1].State &= ~TileStates.LeftNV;
                    hugeMap[currentPos.Row + 1, currentPos.Col].State &= ~TileStates.TopNV;
                    hugeMap[currentPos.Row, currentPos.Col - 1].State &= ~TileStates.RightNV;
                    hugeMap[currentPos.Row, currentPos.Col].State |= TileStates.Visited;

                    PossiblePos = currentPos;

                    return action;
                }
                else
                {
                    //if no current tile has no unexplored neighbours and there are still grey tiles on map
                    if (GreyExists())
                    {
                        //A* to search for nearest grey
                        GetPathToGrey();
                        followPath = true;
                    }
                    else
                    {
                        //Finishing phase 1
                        //Rewrting HugeMap contents to SmallMap
                        int minRow = int.MaxValue, minCol = int.MaxValue;
                        for (int row = 0; row < hugeMap.GetLength(0); ++row)
                        {
                            for (int col = 0; col < hugeMap.GetLength(1); ++col)
                            {
                                if ((hugeMap[row, col].State & TileStates.Visited) != 0)
                                {
                                    minRow = System.Math.Min(minRow, row);
                                    minCol = System.Math.Min(minCol, col);
                                }
                                else
                                {
                                    hugeMap[row, col].State |= TileStates.Wall;
                                }
                            }
                        }

                        Tile[,] smallMap = SmallMap;
                        for (int row = 0; row < Environment.mazeSize_; ++row)
                            for (int col = 0; col < Environment.mazeSize_; ++col)
                                smallMap[row, col] = hugeMap[row + minRow, col + minCol];

                        CurrentPos.Row -= minRow;
                        CurrentPos.Col -= minCol;
                        HugeMap = null;
                        Fringe = null;
                        Phase1Running = false;
                    }
                }
            }
            else
                followPath = true;

            if (followPath)
            {
                //Following path generated by A* on some previous step
                Point dest = GoingToGrey.Value;
                GoingToGrey = GoingToGrey.Parent;
                PossiblePos = dest;

                if (dest.Row < currentPos.Row)
                    return ActionType.actUP;
                if (dest.Col > currentPos.Col)
                    return ActionType.actRIGHT;
                if (dest.Row > currentPos.Row)
                    return ActionType.actDOWN;
                if (dest.Col < currentPos.Col)
                    return ActionType.actLEFT;
            }
            return ActionType.actIDLE;
        }

        /// <summary>
        /// Check if map contains grey tiles
        /// </summary>
        /// <returns>true if map contais grey tiles</returns>
        private bool GreyExists()
        {
            Tile[,] hugeMap = HugeMap;

            for (int row = 0; row < hugeMap.GetLength(0); ++row)
                for (int col = 0; col < hugeMap.GetLength(1); ++col)
                    if ((hugeMap[row, col].State & GreyTileSign) != 0 && (hugeMap[row, col].State & TileStates.Visited) != 0)
                        return true;

            return false;
        }

        /// <summary>
        /// Get nearest grey tile evaluated by Manhattan distance
        /// </summary>
        /// <param name="currentPos">current position</param>
        /// <returns>path to neares grey tile</returns>
        private int NearestGrey(Point currentPos)
        {
            int result = int.MaxValue;
            Tile[,] map = HugeMap;
            for (int row = 0; row < map.GetLength(0); ++row)
                for (int col = 0; col < map.GetLength(1); ++col)
                    if ((map[row, col].State & GreyTileSign) != 0 && (map[row, col].State & TileStates.Visited) != 0)
                        result = System.Math.Min(result, currentPos.ManhattanDistance(row, col));
            return result;
        }

        /// <summary>
        /// A* to generate path to nearest grey tile
        /// </summary>
        private void GetPathToGrey()
        {
            Point currentPos = CurrentPos;
            Fringe.Clear();
            Fringe.Add(CurrentPos, null, 0, NearestGrey(currentPos));

            Node bestNode = Fringe.GetBestNode();
            Point pos = bestNode.Value;
            Point childPos;
            Tile[,] map = HugeMap;
            TileStates state;
            int width = map.GetLength(0);

            //while not goal state
            while ((map[pos.Row, pos.Col].State & GreyTileSign) == 0)
            {
                //adding neighbour tiles to Fringe if they are not walls
                if (pos.Row > 0)
                {
                    state = map[pos.Row - 1, pos.Col].State;
                    if ((state & TileStates.Wall) == 0 && (state & TileStates.Visited) != 0)
                    {
                        childPos = new Point(pos.Row - 1, pos.Col);
                        Fringe.Add(childPos, bestNode, bestNode.RealCost + 1, NearestGrey(childPos));
                    }
                }

                if (pos.Col < width - 1)
                {
                    state = map[pos.Row, pos.Col + 1].State;
                    if ((state & TileStates.Wall) == 0 && (state & TileStates.Visited) != 0)
                    {
                        childPos = new Point(pos.Row, pos.Col + 1);
                        Fringe.Add(childPos, bestNode, bestNode.RealCost + 1, NearestGrey(childPos));
                    }
                }

                if (pos.Row < width - 1)
                {
                    state = map[pos.Row + 1, pos.Col].State;
                    if ((state & TileStates.Wall) == 0 && (state & TileStates.Visited) != 0)
                    {
                        childPos = new Point(pos.Row + 1, pos.Col);
                        Fringe.Add(childPos, bestNode, bestNode.RealCost + 1, NearestGrey(childPos));
                    }
                }

                if (pos.Col > 0)
                {
                    state = map[pos.Row, pos.Col - 1].State;
                    if ((state & TileStates.Wall) == 0 && (state & TileStates.Visited) != 0)
                    {
                        childPos = new Point(pos.Row, pos.Col - 1);
                        Fringe.Add(childPos, bestNode, bestNode.RealCost + 1, NearestGrey(childPos));
                    }
                }

                bestNode = Fringe.GetBestNode();
                pos = bestNode.Value;
            }

            //Reverse path
            Node temp1 = bestNode, temp2 = bestNode.Parent, temp3;

            while (temp2.Parent != null)
            {
                temp3 = temp2.Parent;
                temp2.Parent = temp1;
                temp1 = temp2;
                temp2 = temp3;
            }
            temp2.Parent = temp1;
            bestNode.Parent = null;

            GoingToGrey = temp1;
        }

        /// <summary>
        /// Iteration on explored map
        /// </summary>
        /// <returns>action for robot to perform</returns>
        private ActionType Phase2Iteration()
        {
            Tile[,] map = SmallMap;
            Point currentPos = CurrentPos, temp;

            //Greedy algorithm to choose next tile
            NeighbourAction[] actions = new NeighbourAction[4];
            NeighbourActions.CopyTo(actions, 0);

            actions[0].LastVisit = map[currentPos.Row - 1, currentPos.Col].LastVisit;
            actions[1].LastVisit = map[currentPos.Row, currentPos.Col + 1].LastVisit;
            actions[2].LastVisit = map[currentPos.Row + 1, currentPos.Col].LastVisit;
            actions[3].LastVisit = map[currentPos.Row, currentPos.Col - 1].LastVisit;
            System.Array.Sort(actions);

            for (int i = 0; i < 4; ++i)
            {
                temp = currentPos + actions[i].Offset;
                if ((map[temp.Row, temp.Col].State & TileStates.Wall) == 0)
                {
                    CurrentPos = temp;
                    return actions[i].Action;
                }
            }

            return ActionType.actIDLE;
        }

        #endregion
    }
}
