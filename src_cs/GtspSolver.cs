using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
    class GTSPInstance {
        public int[,] distancesFull;
        public int[,] distancesPick;
        public int[] tourVertices;
        public int[] vertexClass;
        public int[] itemsPerClass;
        public int itemsCount { get { return tourVertices.Length; } }
        public int classesWithDepotCount { get { return itemsPerClass.Length; } }
        public int classesCount { get { return itemsPerClass.Length - 1; } }

        public GTSPInstance(int[,] distancesFull, int[] tourVertices, int[] vertexClass, int classesWithDepotCount) {
            this.distancesFull = distancesFull;
            this.tourVertices = tourVertices;
            this.vertexClass = vertexClass;
            this.distancesPick = new int[itemsCount, itemsCount];
            for (int i = 0; i < itemsCount; i++) {
                for (int j = 0; j < itemsCount; j++) {
                    if (i != j) {
                        distancesPick[i, j] = distancesFull[tourVertices[i], tourVertices[j]] + 20;
                    }
                }
            }

            this.itemsPerClass = new int[classesWithDepotCount];
            for (int i = 0; i < itemsCount; i++) {
                itemsPerClass[vertexClass[i]] += 1;
            }
        }

        public static GTSPInstance LoadFromFile(string path) {
            StreamReader file = new StreamReader(path);
            int verticesCount = int.Parse(file.ReadLine());
            int tourVerticesCount = int.Parse(file.ReadLine());
            int classesCount = int.Parse(file.ReadLine());
            int[,] distancesFull = new int[verticesCount, verticesCount];
            int[] tourVertices = new int[tourVerticesCount];
            int[] vertexClass = new int[tourVerticesCount];

            for (int i = 0; i < verticesCount; i++) {
                string[] tokens = file.ReadLine().Split();
                for (int j = 0; j < verticesCount; j++) {
                    distancesFull[i, j] = int.Parse(tokens[j]);
                }
            }

            for (int i = 0; i < tourVerticesCount; i++) {
                string[] tokens = file.ReadLine().Split();
                tourVertices[i] = int.Parse(tokens[0]);
                vertexClass[i] = int.Parse(tokens[1]);
            }
            return new GTSPInstance(distancesFull, tourVertices, vertexClass, classesCount);
        }
    }

    class GTSPSolver {
        GTSPInstance instance;
        SetOperations so;
        (int, int, int) bestSol;
        int[][] shortest_distances;
        ulong[][][] sets;
        ulong[] temp = new ulong[1024];
        int timeLimit;

        public GTSPSolver(string instancePath, int tMax) {
            instance = GTSPInstance.LoadFromFile(instancePath);
            so = new SetOperations(instance.classesCount);
            this.timeLimit = tMax;

            // Allocate memory for subsets
            sets = new ulong[instance.itemsCount][][];
            for (int i = 0; i < instance.itemsCount; i++) {
                sets[i] = new ulong[timeLimit][];
            }
            for (int i = 0; i < instance.itemsCount; i++) {
                for (int j = 0; j < timeLimit; j++) {
                    sets[i][j] = new ulong[1024];
                }
            }

            // Init shortest distances matrix
            this.shortest_distances = new int[instance.itemsCount][];
            for (int i = 0; i < shortest_distances.Length; i++) {
                shortest_distances[i] = new int[tMax];
            }
        }

        public void FindShortestRoute() {            
            // Init sets
            ulong[] emptyLong = new ulong[1024];
            for (int i = 0; i < instance.tourVertices.Length; i++) {
                for (int j = 0; j < timeLimit; j++) {
                    Array.Copy(emptyLong, sets[i][j], 1024);
                }
            }
            for (int i = 0; i < so.longsUsed; i++) {
                sets[0][0][i] = ulong.MaxValue;
            }

            // Init paths from depot.
            for (int i = 1; i < instance.itemsCount; i++) {
                shortest_distances[i][instance.distancesPick[0, i]] = 1;
                sets[i][instance.distancesPick[0, i]][0] = 1;
            }

            int tMax = timeLimit;
            // Find shortest tour
            for (int time = 0; time < timeLimit && time < tMax; time++) {
                for (int i = 1; i < instance.itemsCount; i++) {
                    if (shortest_distances[i][time] == 0) continue;
                    int vertexClass_0 = instance.vertexClass[i];
                    so.AddElement(sets[i][time], vertexClass_0);

                    // Check, whether to add vertex into potential second to last on shortest path
                    if (so.IsComplete(sets[i][time])) {
                        int finishTime = time + instance.distancesPick[i, 0];
                        if (tMax > finishTime) {
                            tMax = finishTime;
                            bestSol = (finishTime, time, i);
                        }
                    }

                    for (int j = 1; j < instance.itemsCount; j++) {
                        int vertexClass_1 = instance.vertexClass[j];
                        if (vertexClass_0 == vertexClass_1) continue;
                        int pickTime = instance.distancesPick[i, j];
                        if (time + pickTime < timeLimit) {
                            shortest_distances[j][time + pickTime] = 1;
                            Array.Copy(sets[i][time], temp, so.longsUsed);
                            so.FilterElement(temp, vertexClass_1);
                            so.Unify(temp, sets[j][time + pickTime]);
                        }
                    }
                }
            }
            LinkedList<(int, int)> solution = new LinkedList<(int, int)>();
            solution.AddLast((bestSol.Item1, 0));
            solution.AddFirst((bestSol.Item2, bestSol.Item3));
            int lastClass = instance.vertexClass[bestSol.Item3];
            int[] classesLeft = new int[instance.classesWithDepotCount];
            for (int i = 0; i < classesLeft.Length; i++) {
                if (i != lastClass)
                    classesLeft[i] = i;
            }
            for (int i = 0; i < classesLeft.Length - 1; i++) {
                (int, int) currVertex = solution.First.Value;
                (int, int) previous = Backtrack(currVertex.Item1, currVertex.Item2, classesLeft);
                classesLeft[instance.vertexClass[previous.Item2]] = 0;
                solution.AddFirst(previous);
            }

            (int, int) Backtrack(int visitTime, int lastVertex, int[] unvisitedClasses) {
                for (int i = 1; i < instance.tourVertices.Length; i++) {
                    int vClass = instance.vertexClass[i];
                    int lClass = instance.vertexClass[lastVertex];
                    if (unvisitedClasses[vClass] == 0) continue;
                    int vTime = visitTime - instance.distancesPick[lastVertex, i];
                    if (vTime < 0) continue;
                    ulong[] originSet = sets[i][vTime];
                    if (so.SearchSubset(originSet, unvisitedClasses))
                        return (vTime, i);
                }
                return (0, 0);
            }
        }
    }


    class GtspSolver {
        static void Main(string[] args) {
            WarehouseInstance wi = InstanceParser.Parse("../../../../../data/whole_instance.txt");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10; i++) {
                // solver.FindShortestRoute();
            }
            sw.Stop();
            Console.WriteLine("Elapsed={0}", sw.ElapsedMilliseconds);        
        }
    
    }
}
