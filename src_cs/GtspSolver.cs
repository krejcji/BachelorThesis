using System;
using System.Collections.Generic;
using System.Linq;

namespace src_cs {
    class SetOperations {
        static readonly int MAXSIZE = 1024;
        static readonly int[] shifts = new int[] { 0, 1, 2, 4, 8, 16, 32, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
        static readonly ulong[] zeros = new ulong[MAXSIZE];
        static readonly ulong[][] masks;
        // static readonly ulong[] cutOffMasks;
        readonly int classesCount;
        public readonly int longsUsed;
        public delegate bool IsCompleteDel(ulong[] sets);
        public readonly IsCompleteDel IsComplete;


        static SetOperations() {
            masks = new ulong[17][];
            for (int i = 0; i < 17; i++) {
                masks[i] = new ulong[MAXSIZE];
            }

            // Init masks periodical under 64 bits
            for (int i = 1; i < 7; i++) {
                ulong mask = 0;
                int step = shifts[i];
                for (int j = 0; j < 64; j += 2 * step) {
                    mask <<= (step + 1);
                    for (int k = 0; k < step; k++) {
                        mask += 1;
                        if (k < step - 1) {
                            mask <<= 1;
                        }
                    }
                }
                for (int j = 0; j < MAXSIZE; j++) {
                    masks[i][j] = mask;
                }
            }

            // Init masks periodical at or over 64 bits
            for (int i = 7; i < shifts.Length; i++) {
                ulong mask = ulong.MaxValue;
                int step = shifts[i];
                for (int j = 0; j < MAXSIZE; j += 2 * step) {
                    for (int k = 0; k < step; k++) {
                        masks[i][j + k] = mask;
                    }
                }
            }
            /*
            // Init cutoff masks
            cutOffMasks = new ulong[6];
            for (int i = 1; i < cutOffMasks.Length; i++) {
                ulong mask = ulong.MaxValue;
                int shift = 64 - (2 << (i - 1));
                mask >>= shift;
                cutOffMasks[i] = mask;
            }
            */
        }

        public SetOperations(int classesCount) {
            this.classesCount = classesCount;
            if (classesCount < 7) {
                this.longsUsed = 1;
                this.IsComplete = IsCompleteShort;
            }
            else {
                this.longsUsed = 1 << (classesCount - 6);
                this.IsComplete = IsCompleteLong;
            }
        }

        public void Unify(ulong[] setSource, ulong[] setTarget) {
            for (int i = 0; i < longsUsed; i++) {
                setTarget[i] |= setSource[i];
            }
        }

        public void AddElement(ulong[] sets, int element) {
            if (element < 7) {
                for (int i = 0; i < longsUsed; i++) {
                    sets[i] <<= shifts[element];
                }
            }
            else {
                for (int i = longsUsed - 1 - shifts[element]; i >= 0; i--) {
                    sets[i + shifts[element]] = sets[i];
                }
                for (int i = 0; i < shifts[element]; i++) {
                    sets[i] = 0;
                }
            }
        }

        bool IsCompleteLong(ulong[] sets) {
            ulong last = sets[longsUsed - 1];
            last >>= 63;
            if (last == 1)
                return true;
            return false;
        }

        bool IsCompleteShort(ulong[] sets) {
            ulong value = sets[0];
            int bitIndex = (1 << classesCount) - 1;
            value <<= 63 - bitIndex;
            value >>= 63;
            if (value == 1)
                return true;
            return false;
        }

        public void FilterElement(ulong[] sets, int element) {
            for (int i = 0; i < longsUsed; i += 4) {
                sets[i] &= masks[element][i];
                sets[i + 1] &= masks[element][i + 1];
                sets[i + 2] &= masks[element][i + 2];
                sets[i + 3] &= masks[element][i + 3];
            }
        }

        public bool SearchSubset(ulong[] sets, int[] elements) {
            int bitIndex = 0;
            int longIndex = 0;
            for (int i = 0; i < elements.Length; i++) {
                if (elements[i] != 0) {
                    if (elements[i] < 7) {
                        bitIndex += 1 << (elements[i] - 1);
                    }
                    else {
                        longIndex += 1 << (elements[i] - 7);
                    }
                }
            }
            ulong theOne = sets[longIndex];
            theOne <<= 63 - bitIndex;
            theOne >>= 63;
            if (theOne == 1) return true;
            return false;
        }

        public void CreateSet(ulong[] emptySet, int initElement) {
            if (initElement < 7) {
                int bitPosition = 1 << (initElement - 1);
                emptySet[0] = (ulong)1 << bitPosition;
            }
            else {
                int firstLongPosition = 1 << (initElement - 7);
                emptySet[firstLongPosition] = 1;
            }
        }
    }

    class GTSPSolverFactory {
        GTSPSolver[] solvers;
        int tMax;

        public GTSPSolverFactory(int tMax) {
            this.solvers = new GTSPSolver[16];
            this.tMax = tMax;
        }

        public GTSPSolver GetSolver(int classes) {
            if (solvers[classes] == null) {
                return new GTSPSolver(classes, 50, tMax);
                // TODO : Solve variable item counts
            }
            else {
                solvers[classes].Init();
                return solvers[classes];
            }
        }
    }

    class GTSPSolver {
        SetOperations so;
        (int, int, int, int[]) bestSol;
        int[][] timeDistances;
        ulong[][][] sets;
        ulong[] temp = new ulong[1024];
        int timeLimit;

        public GTSPSolver(int classes, int items, int tMax) {
            so = new SetOperations(classes);
            this.timeLimit = tMax;

            // Allocate memory for subsets
            sets = new ulong[items][][];
            for (int i = 0; i < items; i++) {
                sets[i] = new ulong[timeLimit][];
            }
            for (int i = 0; i < items; i++) {
                for (int j = 0; j < timeLimit; j++) {
                    sets[i][j] = new ulong[1024];
                }
            }

            // Init shortest distances matrix
            this.timeDistances = new int[items][];
            for (int i = 0; i < timeDistances.Length; i++) {
                timeDistances[i] = new int[tMax];
            }
        }

        public void Init() {
            // TODO:
        }

        public static Tour SolveGTSP(Graph graph, List<Constraint> constraints, Agent agent) {
            var instance = new GTSPSolver(agent.classes[agent.classes.Length - 1], agent.vertices.Length, 1500);
            var sortedList = new SortedList<int, List<int>>();            
            for (int i = 0; i < constraints.Count; i++) {
                if (sortedList.ContainsKey(constraints[i].time)) {
                    sortedList[constraints[i].time].Add(constraints[i].vertex);
                }
                else {
                    sortedList.Add(constraints[i].time, new List<int>() { constraints[i].vertex });
                }
            }
            return instance.FindShortestTour(graph, sortedList, agent);
        }

        public Tour FindShortestTour(Graph graph, SortedList<int, List<int>> constraints, Agent agent) {
            var vertices = agent.vertices;// TODO : Make instance with factory and solve it.. bitch
            var classes = agent.classes;
            int timeOffset = agent.tourBeginTime;

            // Init sets
            ulong[] emptyLong = new ulong[1024];
            for (int i = 0; i < vertices.Length; i++) {
                for (int j = 0; j < timeLimit; j++) {
                    Array.Copy(emptyLong, sets[i][j], 1024);
                }
            }
            for (int i = 0; i < so.longsUsed; i++) {
                sets[0][0][i] = ulong.MaxValue;
            }

            // Init paths from depot.
            for (int i = 1; i < vertices.Length; i++) {
                var (time, r) = graph.ShortestRoute(0, vertices[i], timeOffset, constraints, false);
                timeDistances[i][time] = 1;
                sets[i][time][0] = 1;
            }

            int tMax = timeLimit;
            // Find shortest tour
            for (int time = 0; time < timeLimit && time < tMax; time++) {
                for (int i = 1; i < vertices.Length; i++) {
                    if (timeDistances[i][time] == 0) continue;
                    int vertexClass_0 = classes[i];
                    so.AddElement(sets[i][time], vertexClass_0);

                    // Check, whether to add vertex into potential second to last on shortest path
                    if (so.IsComplete(sets[i][time])) {
                        var (t, r) = graph.ShortestRoute(vertices[i], 0, time + timeOffset, constraints, false);
                        int[] reversed = new int[r.Length];
                        for (int j = 0; j < r.Length; j++) {
                            reversed[r.Length - 1 - j] = r[j];
                        }
                        int finishTime = time + t;
                        if (tMax > finishTime) {
                            tMax = finishTime;
                            bestSol = (finishTime, time, i, reversed);
                        }
                    }

                    for (int j = 1; j < vertices.Length; j++) {
                        int vertexClass_1 = classes[j];
                        if (vertexClass_0 == vertexClass_1) continue;
                        var (pickTime, r) = graph.ShortestRoute(vertices[i], vertices[j], time+timeOffset, constraints, false);
                        if (time + pickTime < timeLimit) {
                            timeDistances[j][time + pickTime] = 1;
                            Array.Copy(sets[i][time], temp, so.longsUsed);
                            so.FilterElement(temp, vertexClass_1);
                            so.Unify(temp, sets[j][time + pickTime]);
                        }
                    }
                }
            }

            // Reverse search the shortest tour.
            LinkedList<(int, int, int[])> solution = new LinkedList<(int, int, int[])>();
            solution.AddLast((bestSol.Item1, 0, new int[0]));
            solution.AddFirst((bestSol.Item2, bestSol.Item3, bestSol.Item4));
            int lastClass = classes[bestSol.Item3];
            int[] classesLeft = new int[classes[classes.Length-1]+1]; // TODO: CLasses length+1
            for (int i = 0; i < classesLeft.Length; i++) {
                if (i != lastClass)
                    classesLeft[i] = i;
            }
            var shortestRoutesBck = new int[vertices.Length];
            for (int i = 0; i < classesLeft.Length-1; i++) {
                (int, int, int[]) currVertex = solution.First.Value;
                (int, int, int[]) previous = Backtrack(currVertex.Item1, currVertex.Item2, classesLeft);
                classesLeft[classes[previous.Item2]] = 0;
                solution.AddFirst(previous);
            }

            return new Tour(timeOffset, solution);

            (int, int, int[]) Backtrack(int visitTime, int lastVertex, int[] unvisitedClasses) {
                for (int i = 1; i < vertices.Length; i++) {
                    int vClass = classes[i];
                    int lClass = classes[lastVertex];
                    if (unvisitedClasses[vClass] == 0) continue;
                    var (t, r) = graph.ShortestRoute(vertices[lastVertex], vertices[i], visitTime +timeOffset, constraints, true);
                    int vTime = visitTime - t;
                    if (vTime < 0) continue;
                    ulong[] originSet = sets[i][vTime];
                    if (so.SearchSubset(originSet, unvisitedClasses))
                        return (vTime, i, r);
                    shortestRoutesBck[i] = t;
                }
                for (int i = 0; i < unvisitedClasses.Length; i++) {
                    if (unvisitedClasses[i] > 0)
                        break;
                    var (t1, r1) = graph.ShortestRoute(vertices[lastVertex], 0, visitTime + timeOffset, constraints, true);
                    return (0, 0, r1);
                }                
                int[] r2 = null;
                return (0, 0, r2);
            }
        }
    }

    public class Tour {
        public int cost;
        public int startTime;
        public int[] tourVertices;

        public Tour(int startTime, LinkedList<(int, int, int[])> solution) {
            this.startTime = startTime;
            List<int> sol = new List<int>();            
            for (int i = 0; i < solution.Count; i++) {
                var solutionArr = solution.ElementAt(i).Item3;
                for (int j = solutionArr.Length - 1; j > 0; j--) {
                    sol.Add(solutionArr[j]);
                }
            }
            sol.Add(0);
            this.tourVertices = sol.ToArray();
            this.cost = tourVertices.Length;
        }
    }
}
