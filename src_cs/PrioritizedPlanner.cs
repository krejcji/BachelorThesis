using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace src_cs {
    class PrioritizedPlanner {        

        public static Tour[][] FindTours(WarehouseInstance instance) {
            int agents = instance.AgentCount;            
            int maxTime = 1024;
            GTSPSolver.FindMaxValues(instance.orders, maxTime, out int maxClasses,
                out int maxItems, out int maxSolverTime);
            GTSPSolver solver = new GTSPSolver(maxClasses, maxItems, maxSolverTime);

            int[][] solutions =  new int[agents][];
            for (int i = 0; i < agents; i++) {
                solutions[i] = new int[maxTime];
            }

            List<int>[] nodesVisitors0 = new List<int>[instance.graph.vertices.Count];
            List<int>[] nodesVisitors1 = new List<int>[instance.graph.vertices.Count];
            int[] zero = new int[maxTime];            
            
            
            for (int i = 0; i < nodesVisitors0.Length; i++) {
                nodesVisitors0[i] = new List<int>();
                nodesVisitors1[i] = new List<int>();
            }
            
            Tour[][] solution;
            List<Constraint> constraints = new List<Constraint>();
            int totalCost = 0;
            int totalCostOpt = 0;

            // Init tours array
            solution = new Tour[agents][];
            for (int i = 0; i < agents; i++) {
                solution[i] = new Tour[instance.orders[i].Length];
            }

            // Init optimal tours array
            int offsetTime = 0;
            var solutionOpt = new Tour[agents][];
            for (int i = 0; i < agents; i++) {
                solutionOpt[i] = new Tour[instance.orders[i].Length];
            }
            for (int i = 0; i < solution.Length; i++) {
                for (int j = 0; j < solution[i].Length; j++) {
                    solutionOpt[i][j] = solver.SolveGTSP(instance.graph, constraints, instance.orders[i][j], offsetTime);
                    totalCostOpt += solutionOpt[i][j].cost;
                    offsetTime += solutionOpt[i][j].cost;
                }
                offsetTime = 0;

            }
            offsetTime = 0;
            
            // Calculate non-conflicting routes sequentially
            for (int i = 0; i < solution.Length; i++) {
                constraints.Clear();
                
                while (true) {
                    for (int j = 0; j < solution[i].Length; j++) {
                        if (j > 0)
                            offsetTime += solution[i][j - 1].cost;
                        solution[i][j] = solver.SolveGTSP(instance.graph, constraints, instance.orders[i][j], offsetTime);

                    }

                    if (FindFirstConflict(solution, out Conflict c, nodesVisitors0, nodesVisitors1, solutions,
                        agents, maxTime, zero)) {
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
                
                
            }
            return solution;
        }
        
        public static bool FindFirstConflict(Tour[][] tours, out Conflict conflict, List<int>[] nodesVisitors0,
            List<int>[] nodesVisitors1, int[][] solutions, int agents, int maxTime, int[] zero) {
            bool foundConflict = false;
            conflict = new Conflict();
            for (int i = 0; i < tours.Length; i++) {
                for (int j = 0; j < tours[i].Length; j++) {
                    if (tours[i][j] != null)
                        Array.Copy(tours[i][j].tourVertices, 0, solutions[i], tours[i][j].startTime, tours[i][j].tourVertices.Length);
                }
            }

            // Add agent indicies into visited vertices at 0 timestep
            for (int j = 0; j < agents; j++) {
                if (solutions[j][0] == 0) continue;  // Depot/default value can be occupied by more agents
                nodesVisitors0[solutions[j][0]].Add(j);
            }

            for (int i = 0; i < maxTime - 1; i++) {
                // Look for vertex conflicts.
                for (int j = 0; j < agents; j++) {
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
                for (int j = 0; j < agents; j++) {
                    if (solutions[j][i + 1] == 0) continue;
                    nodesVisitors1[solutions[j][i + 1]].Add(j);
                }

                // Look for conflicts. If edge x->y, then not y->x.
                for (int j = 0; j < agents; j++) {
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
                for (int i = 0; i < agents; i++) {
                    nodesVisitors0[solutions[i][time]].Clear();
                    nodesVisitors1[solutions[i][time + 1]].Clear();
                }
            }

            void NextTimeStep(int time) {
                for (int i = 0; i < agents; i++) {
                    nodesVisitors0[solutions[i][time]].Clear();
                }
                var tmpVisitors = nodesVisitors0;
                nodesVisitors0 = nodesVisitors1;
                nodesVisitors1 = tmpVisitors;
            }
        }
    }
}
