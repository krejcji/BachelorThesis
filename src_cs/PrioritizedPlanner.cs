using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace src_cs {
    class PrioritizedPlanner {
        WarehouseInstance instance;
        GTSPSolver solver;
        int maxTime;
        int[][] solutions;
        private List<int>[] nodesVisitors0;
        private List<int>[] nodesVisitors1;
        private readonly int[] zero;

        public PrioritizedPlanner(WarehouseInstance instance, int maxTime) {
            this.maxTime = maxTime;
            this.instance = instance;
            this.solutions = new int[instance.agents.Length][];
            for (int i = 0; i < instance.agents.Length; i++) {
                solutions[i] = new int[maxTime];
            }
            GTSPSolver.FindMaxValues(instance.agents, maxTime, out int maxClasses,
                out int maxItems, out int maxSolverTime);
            this.solver = new GTSPSolver(maxClasses, maxItems, maxSolverTime);

            this.nodesVisitors0 = new List<int>[instance.graph.vertices.Count];
            this.nodesVisitors1 = new List<int>[instance.graph.vertices.Count];
            for (int i = 0; i < nodesVisitors0.Length; i++) {
                nodesVisitors0[i] = new List<int>();
                nodesVisitors1[i] = new List<int>();
            }
            this.zero = new int[maxTime];
        }

        public Tour[][] FindTours() {
            Tour[][] solution;
            List<Constraint> constraints = new List<Constraint>();
            int totalCost = 0;
            int totalCostOpt = 0;

            // Init tours array
            solution = new Tour[instance.agents.Length][];
            for (int i = 0; i < instance.agents.Length; i++) {
                solution[i] = new Tour[instance.agents[i].orders.Length];
            }

            // Init optimal tours array
            int offsetTime = 0;
            var solutionOpt = new Tour[instance.agents.Length][];
            for (int i = 0; i < instance.agents.Length; i++) {
                solutionOpt[i] = new Tour[instance.agents[i].orders.Length];
            }
            for (int i = 0; i < solution.Length; i++) {                
                for (int j = 0; j < solution[i].Length; j++) {
                    solutionOpt[i][j] = solver.SolveGTSP(instance.graph, constraints, instance.agents[i].orders[j], offsetTime);
                    totalCostOpt += solutionOpt[i][j].cost;
                    offsetTime += solutionOpt[i][j].cost;
                }
                offsetTime = 0;

            }
            offsetTime = 0;
            Stopwatch sw = new Stopwatch();
            // Calculate non-conflicting routes sequentially
            for (int i = 0; i < solution.Length; i++) {
                constraints.Clear();
                sw.Restart();
                sw.Start();
                while (true) {
                    for (int j = 0; j < solution[i].Length; j++) {
                        if (j > 0)
                            offsetTime += solution[i][j - 1].cost;
                        solution[i][j] = solver.SolveGTSP(instance.graph, constraints, instance.agents[i].orders[j], offsetTime);

                    }

                    if (FindFirstConflict(solution, out Conflict c)) {
                        var conf = c.MakeConstraints();
                        if (conf.Item1[0].agent == i) {
                            for (int k = 0; k < conf.Item1.Length; k++) {
                                constraints.Add(conf.Item1[k]);
                            }
                        }
                        else {
                            for (int k = 0; k < conf.Item2.Length; k++) {
                                constraints.Add(conf.Item2[k]);
                            }
                        }
                    }
                    else {
                        totalCost += solution[i][0].cost;
                        break;
                    }
                }
                sw.Stop();
                Console.WriteLine("Agent {0} route has been found in {1}",i, sw.Elapsed);
                Console.WriteLine("   Items {0}\n    classes {1}", instance.agents[i].orders[0].vertices.Length, instance.agents[i].orders[0].classes[^1]);
                Console.WriteLine("   Constraint: {0}", constraints.Count);
            }
            Console.WriteLine(totalCost);
            Console.WriteLine(totalCostOpt);
            return solution;
        }
        public bool FindFirstConflict(Tour[][] tours, out Conflict conflict) {
            bool foundConflict = false;
            conflict = new Conflict();
            for (int i = 0; i < tours.Length; i++) {
                for (int j = 0; j < tours[i].Length; j++) {
                    if (tours[i][j] != null)
                        Array.Copy(tours[i][j].tourVertices, 0, solutions[i], tours[i][j].startTime, tours[i][j].tourVertices.Length);
                }
            }

            // Add agent indicies into visited vertices at 0 timestep
            for (int j = 0; j < instance.agents.Length; j++) {
                if (solutions[j][0] == 0) continue;  // Depot/default value can be occupied by more agents
                nodesVisitors0[solutions[j][0]].Add(j);
            }

            for (int i = 0; i < maxTime - 1; i++) {
                // Look for vertex conflicts.
                for (int j = 0; j < instance.agents.Length; j++) {
                    if (nodesVisitors0[solutions[j][i]].Count > 1) {
                        if (solutions[j][i] == 0) continue;

                        // Report conflict and clean up the lists.
                        var visitList = nodesVisitors0[solutions[j][i]];
                        conflict = new Conflict(0, i, visitList[0], visitList[1], solutions[j][i], 0);
                        foundConflict = true;
                        ClearTimeStep(i);
                        goto Cleanup;
                    }
                }

                // Look for edge conflicts.
                // Fill in t+1 used vertices.
                if (i == maxTime - 1) continue;
                for (int j = 0; j < instance.agents.Length; j++) {
                    if (solutions[j][i + 1] == 0) continue;
                    nodesVisitors1[solutions[j][i + 1]].Add(j);
                }

                // Look for conflicts. If edge x->y, then not y->x.
                for (int j = 0; j < instance.agents.Length; j++) {
                    int from = solutions[j][i];
                    int to = solutions[j][i + 1];
                    if (from == to) continue;
                    if (nodesVisitors0[to].Count == 1 && nodesVisitors1[from].Count > 0) {
                        var visitor0 = nodesVisitors0[to][0];
                        for (int k = 0; k < nodesVisitors1[from].Count; k++) {
                            if (visitor0 == nodesVisitors1[from][k]) {
                                conflict = new Conflict(1, i, j,
                                    visitor0, from, to);
                                foundConflict = true;
                                ClearTimeStep(i);
                                goto Cleanup;
                            }
                        }
                    }
                }
                NextTimeStep(i);
            }

        Cleanup:
            // Clean up the temp arrays.
            for (int i = 0; i < tours.Length; i++) {
                for (int j = 0; j < tours[i].Length; j++) {
                    if (tours[i][j] != null)
                        Array.Copy(zero, 0, solutions[i], tours[i][j].startTime, tours[i][j].tourVertices.Length);
                }
            }
            return foundConflict;

            void ClearTimeStep(int time) {
                for (int i = 0; i < instance.agents.Length; i++) {
                    nodesVisitors0[solutions[i][time]].Clear();
                    nodesVisitors1[solutions[i][time + 1]].Clear();
                }
            }

            void NextTimeStep(int time) {
                for (int i = 0; i < instance.agents.Length; i++) {
                    nodesVisitors0[solutions[i][time]].Clear();
                }
                var tmpVisitors = nodesVisitors0;
                nodesVisitors0 = nodesVisitors1;
                nodesVisitors1 = tmpVisitors;
            }
        }
    }
}
