using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace src_cs {

    public class SetOperations {
        static readonly int MAXSIZE = 1024;
        static readonly int[] shifts = new int[] { 0, 1, 2, 4, 8, 16, 32, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
        public static readonly ulong[] zeros = new ulong[MAXSIZE];
        static readonly ulong[][] masks;
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

        public virtual void Unify(ulong[] setSource, ulong[] setTarget) {
            if (longsUsed >= 4) {
                for (int i = 0; i < longsUsed; i += 4) {
                    Vector<ulong> src = new Vector<ulong>(setSource, i);
                    Vector<ulong> target = new Vector<ulong>(setTarget, i);
                    Vector.BitwiseOr(src, target).CopyTo(setTarget, i);
                }
            }
            else {
                for (int i = 0; i < longsUsed; i++) {
                    setTarget[i] |= setSource[i];
                }
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
            for (int i = 0; i < longsUsed; i++) {
                sets[i] &= masks[element][i];
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

    public sealed class SetSmall : SetOperations {
        public SetSmall(int classes) : base(classes) {

        }
        public sealed override void Unify(ulong[] setSource, ulong[] setTarget) {
            for (int i = 0; i < longsUsed; i++) {
                setTarget[i] |= setSource[i];
            }

        }
    }

    public sealed class SetLarge : SetOperations {
        public SetLarge(int classes) : base(classes) {

        }
        public sealed override void Unify(ulong[] setSource, ulong[] setTarget) {
            for (int i = 0; i < longsUsed; i += 4) {
                Vector<ulong> src = new Vector<ulong>(setSource, i);
                Vector<ulong> target = new Vector<ulong>(setTarget, i);
                Vector.BitwiseOr(src, target).CopyTo(setTarget, i);
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

    sealed class GTSPSolver {
        SetOperations so;
        (int, int, int, int[]) bestSol;
        readonly int[][] timeDistances;
        readonly ulong[][][] sets;
        readonly int timeLimit;

        public GTSPSolver(int maxClasses, int maxItems, int maxTime) {
            so = new SetOperations(maxClasses);
            this.timeLimit = maxTime;

            // Allocate memory for subsets
            sets = new ulong[maxItems][][];
            for (int i = 0; i < maxItems; i++) {
                sets[i] = new ulong[timeLimit][];
            }
            for (int i = 0; i < maxItems; i++) {
                for (int j = 0; j < timeLimit; j++) {
                    sets[i][j] = new ulong[so.longsUsed];
                }
            }

            // Init shortest distances matrix
            this.timeDistances = new int[maxItems][];
            for (int i = 0; i < timeDistances.Length; i++) {
                timeDistances[i] = new int[timeLimit];
            }
        }

        public void Init() {
            bestSol = (99999999, 9999999, 99999999, new int[0]);
            for (int i = 0; i < timeDistances.Length; i++) {
                for (int j = 0; j < timeDistances[i].Length; j++) {
                    timeDistances[i][j] = 0;
                }
            }
            for (int i = 0; i < sets.Length; i++) {
                for (int j = 0; j < sets[i].Length; j++) {
                    Array.Copy(SetOperations.zeros, sets[i][j], so.longsUsed);
                }
            }
        }

        // TODO: Start and target vertices are no longer vertices[0] and vertices[1] - patch the method
        public Tour SolveGTSP(Graph graph, List<Constraint> constraints, OrderInstance order, int timeOffset) {
            Init();
            var sortedList = new SortedList<int, List<int>>();
            for (int i = 0; i < constraints.Count; i++) {
                if (sortedList.ContainsKey(constraints[i].time)) {
                    sortedList[constraints[i].time].Add(constraints[i].vertex);
                }
                else {
                    sortedList.Add(constraints[i].time, new List<int>() { constraints[i].vertex });
                }
            }
            return FindShortestTour(graph, sortedList, order, timeOffset);
        }

        public Tour FindShortestTour(Graph graph, SortedList<int, List<int>> constraints, OrderInstance order, int timeOffset) {
            var vertices = order.vertices;
            var classes = order.classes;
            var orderId = order.orderId;
            var pickTimes = order.pickTimes;
            var classesCount = classes[^1];
            SetOperations so;
            if (classesCount < 8) {
                so = new SetSmall(classesCount);
            }
            else {
                so = new SetLarge(classesCount);
            }

            ulong copy = 0;
            ulong uni = 0;

            var startLoc = order.startLoc;
            var targetLoc = order.targetLoc;
            // Init paths from depot.
            for (int i = 2; i < vertices.Length; i++) {
                var (time, r) = graph.ShortestRoute(startLoc, vertices[i], pickTimes[i], timeOffset, constraints, false, false);
                if (time == 0) continue;
                timeDistances[i][time] = 1;
                sets[i][time][0] = 1;
            }

            int tMax = timeLimit;
            // Find shortest tour
            for (int time = 0; time < timeLimit && time < tMax; time++) {
                for (int i = 2; i < vertices.Length; i++) {
                    if (timeDistances[i][time] == 0) continue;
                    int vertexClass_0 = classes[i];
                    so.FilterElement(sets[i][time], vertexClass_0);
                    so.AddElement(sets[i][time], vertexClass_0);

                    // Check, whether to add vertex into potential second to last on shortest path
                    if (so.IsComplete(sets[i][time])) {
                        var (t, r) = graph.ShortestRoute(vertices[i], targetLoc, pickTimes[i], time + timeOffset, constraints, false, true);
                        if (t == 0) continue;
                            int finishTime = time + t;
                        if (tMax > finishTime) {
                            tMax = finishTime;
                            bestSol = (finishTime, time, i, r);
                        }                        
                    }

                    for (int j = 2; j < vertices.Length; j++) {
                        int vertexClass_1 = classes[j];
                        if (vertexClass_0 == vertexClass_1) continue;
                        var (pickTime, r) = graph.ShortestRoute(vertices[i], vertices[j], pickTimes[i], time + timeOffset, constraints, false, false);
                        if (pickTime == 0) continue;
                        if (time + pickTime < timeLimit) {
                            timeDistances[j][time + pickTime] = 1;
                            so.Unify(sets[i][time], sets[j][time + pickTime]); // Note : Happens (itemsInOrder/3) times per array entry on average
                        }
                    }
                }
            }

            // Reverse search the shortest tour.
            LinkedList<(int, int, int[])> solution = new LinkedList<(int, int, int[])>();
            solution.AddLast((bestSol.Item1, targetLoc, new int[0]));
            solution.AddFirst((bestSol.Item2, bestSol.Item3, bestSol.Item4));
            int lastClass = classes[bestSol.Item3];
            int[] classesLeft = new int[classes[^1] + 1];
            for (int i = 0; i < classesLeft.Length; i++) {
                if (i != lastClass)
                    classesLeft[i] = i;
            }
            var shortestRoutesBck = new int[vertices.Length];
            for (int i = 0; i < classesLeft.Length - 1; i++) {
                (int, int, int[]) currVertex = solution.First.Value;
                (int, int, int[]) previous = Backtrack(currVertex.Item1, currVertex.Item2, classesLeft);
                classesLeft[classes[previous.Item2]] = 0;
                solution.AddFirst(previous);
            }

            return new Tour(timeOffset, solution);

            (int, int, int[]) Backtrack(int visitTime, int lastVertex, int[] unvisitedClasses) {
                for (int i = 2; i < vertices.Length; i++) {
                    int vClass = classes[i];
                    //int pickTime = order.pickTimes[i];
                    if (unvisitedClasses[vClass] == 0) continue;
                    var (t, r) = graph.ShortestRoute(vertices[i], vertices[lastVertex], pickTimes[i], visitTime + timeOffset, constraints, true, true);
                    if (t == 0) continue;
                    int vTime = visitTime - t;
                    if (vTime < 0) continue;
                    ulong[] originSet = sets[i][vTime];
                    if (so.SearchSubset(originSet, unvisitedClasses))
                        return (vTime, i, r);
                    shortestRoutesBck[i] = t;
                }

                // Find route back to depot.
                bool allZero = true;
                for (int i = 0; i < unvisitedClasses.Length; i++) {
                    if (unvisitedClasses[i] > 0) {
                        allZero = false;
                        break;
                    }
                }
                if (allZero) {                                                  // TODO: pickTimes[0] was pick time of start location
                    var (t1, r1) = graph.ShortestRoute(startLoc, vertices[lastVertex], pickTimes[0], visitTime + timeOffset, constraints, true, true);
                    if (visitTime - t1 == 0 && t1 != 0)
                        return (0, 0, r1);
                    (t1, r1) = graph.ShortestRoute(startLoc, vertices[lastVertex], pickTimes[0], 0+timeOffset, constraints, false, true);
#if DEBUG
                    if (visitTime - t1 != 0)
                        throw new Exception("Route beginning time is not correct.");
#endif
                    return (0, 0, r1);

                }

                // If not found vertex at shortest routes, the route must be longer.
                int routeExtension = 1;
                while (routeExtension <= visitTime) {
                    for (int i = 2; i < vertices.Length; i++) {
                        if (shortestRoutesBck[i] == 0) continue;
                        int vClass = classes[i];
                        if (unvisitedClasses[vClass] == 0) continue;
                        int vTime = visitTime - shortestRoutesBck[i] - routeExtension;
                        if (vTime < 0) continue;
                        ulong[] originSet = sets[i][vTime];
                        if (so.SearchSubset(originSet, unvisitedClasses)) {
                            var (t, r) = graph.ShortestRoute(vertices[i], vertices[lastVertex], pickTimes[i], vTime + timeOffset, constraints, false, true);
                            if (vTime + t == visitTime && t != 0) {
                                return (vTime, i, r);
                            }
                        }
                    }
                    routeExtension += 1;
                }
                throw new Exception();
            }
        }
        public static void FindMaxValues(Agent[] agents, int maxTime, out int maxClasses, out int maxItems,
                                         out int maxSolverTime) {
            int maxOrders = 0;
            maxClasses = 0;
            maxItems = 0;
            foreach (var agent in agents) {
                maxOrders = maxOrders < agent.orders.Length ? agent.orders.Length : maxOrders;
                foreach (var order in agent.orders) {
                    maxClasses = maxClasses < order.classes[^1] ? order.classes[^1] : maxClasses;
                    maxItems = maxItems < order.vertices.Length ? order.vertices.Length : maxItems;
                }
            }
            maxSolverTime = maxTime / maxOrders;
        }
    }

    public sealed class Tour {
        public int cost;
        public int startTime;
        public int[] tourVertices;

        public Tour(int startTime, LinkedList<(int, int, int[])> solution) {
            this.startTime = startTime;
            List<int> sol = new List<int>();
            var currNode = solution.First;
            while (currNode != null) {
                var solutionArr = currNode.Value.Item3;
                for (int j = 0; j < solutionArr.Length - 1; j++) {
                    sol.Add(solutionArr[j]);
                }
                currNode = currNode.Next;
            }
            sol.Add(solution.ElementAt(solution.Count - 1).Item2);
            this.tourVertices = sol.ToArray();
            this.cost = tourVertices.Length;
        }        
    }
}
