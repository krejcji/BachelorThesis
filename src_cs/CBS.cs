using Priority_Queue;
using System;
using System.Collections.Generic;

namespace src_cs {
    class CBS {
        private List<int>[] nodesVisitors0;
        private List<int>[] nodesVisitors1;
        private readonly int[] zero;
        private readonly byte[] zeroNodes;
        private readonly WarehouseInstance instance;
        private readonly GTSPSolver solver;
        private readonly int maxTime;
        int[][] solutions;

        public Tour[][] FindRoutes() {
            var queue = new FastPriorityQueue<CBSNode>(instance.agents.Length * 200);
            var root = new CBSNode(instance.agents);
            root.CalculateInitRoutes(instance.graph, solver);
            queue.Enqueue(root, root.cost);

            while (queue.Count != 0) {
                var currNode = queue.Dequeue();
                var foundConflict = FindConflicts(currNode.solution, out var conflict);
                if (!foundConflict) {
                    Console.WriteLine("Found solution.");
                    return currNode.solution;
                }
                var constraints = conflict.MakeConstraints();
                var left = new CBSNode(currNode, constraints.Item1);
                var right = new CBSNode(currNode, constraints.Item2);
                left.UpdateRoutes(instance.graph, solver);
                right.UpdateRoutes(instance.graph, solver);
                // TODO: Special constraints & better priority heuristic
                queue.Enqueue(left, left.cost);
                queue.Enqueue(right, right.cost);
            }
            return null;
        }

        public CBS(WarehouseInstance instance, int maxTime) {
            this.maxTime = maxTime;
            this.instance = instance;
            this.zeroNodes = new byte[instance.graph.vertices.Count];
            this.zero = new int[maxTime];
            this.solutions = new int[instance.agents.Length][];
            this.nodesVisitors0 = new List<int>[instance.graph.vertices.Count];
            this.nodesVisitors1 = new List<int>[instance.graph.vertices.Count];
            for (int i = 0; i < nodesVisitors0.Length; i++) {
                nodesVisitors0[i] = new List<int>();
                nodesVisitors1[i] = new List<int>();
            }
            for (int i = 0; i < instance.agents.Length; i++) {
                solutions[i] = new int[maxTime];
            }

            int maxClasses = 0;
            int maxOrders = 0;
            int maxItems = 0;
            foreach (var agent in instance.agents) {
                maxOrders = maxOrders < agent.orders.Length ? agent.orders.Length : maxOrders;
                foreach (var order in agent.orders) {
                    maxClasses = maxClasses < order.classes[^1] ? order.classes[^1] : maxClasses;
                    maxItems = maxItems < order.vertices.Length ? order.vertices.Length : maxItems;
                }
            }
            int maxSolverTime = maxTime / maxOrders;
            this.solver = new GTSPSolver(maxClasses, maxItems, maxSolverTime);
        }

        public bool FindConflicts(Tour[][] tours, out Conflict conflict) {
            bool foundConflict = false;
            conflict = new Conflict();
            for (int i = 0; i < tours.Length; i++) {
                for (int j = 0; j < tours[i].Length; j++) {
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

    class CBSNode : FastPriorityQueueNode {
        CBSNode pred;
        Agent[] agents;
        public int cost;
        int agentConstrained;
        List<Constraint>[] constraints;
        // List<Conflict> conflicts;
        public Tour[][] solution;

        public CBSNode(Agent[] agents) {
            pred = null;
            // conflicts = new List<Conflict>();
            this.agents = agents;

            // Init empty constraints lists
            constraints = new List<Constraint>[agents.Length];
            for (int i = 0; i < constraints.Length; i++) {
                constraints[i] = new List<Constraint>();
            }

            // Init tours array
            solution = new Tour[agents.Length][];
            for (int i = 0; i < agents.Length; i++) {
                solution[i] = new Tour[agents[i].orders.Length];
            }
        }

        public CBSNode(CBSNode pred, Constraint[] newConstraint) {
            this.pred = pred;
            this.agents = pred.agents;
            this.solution = new Tour[agents.Length][];
            for (int i = 0; i < agents.Length; i++) {
                solution[i] = new Tour[agents[i].orders.Length];
            }

            for (int i = 0; i < agents.Length; i++) {
                for (int j = 0; j < agents[i].orders.Length; j++) {
                    this.solution[i][j] = pred.solution[i][j];
                }
            }

            // Copy old constraints and add new ones.
            this.constraints = new List<Constraint>[agents.Length];
            this.agentConstrained = newConstraint[0].agent;
            for (int i = 0; i < agents.Length; i++) {
                if (i != agentConstrained) {
                    this.constraints[i] = pred.constraints[i];
                }
                else {
#if DEBUG
                    if (newConstraint.Length > 1) {
                        for (int j = 0; j < newConstraint.Length; j++) {
                            for (int k = 0; k < newConstraint.Length; k++) {
                                if (j == k) continue;
                                if (newConstraint[j].Equals(newConstraint[k])) {
                                    throw new Exception("Duplicate new constraints.");
                                }
                            }
                        }
                    }
                    if (constraints[i] != null) {
                        for (int j = 0; j < constraints[i].Count; j++) {
                            for (int k = 0; k < newConstraint.Length; k++) {
                                if (constraints[i][j].Equals(newConstraint[k]))
                                    throw new Exception("Adding existing constraint.");
                            }
                        }
                    }
#endif
                    this.constraints[i] = new List<Constraint>();
                    for (int j = 0; j < pred.constraints[i].Count; j++) {
                        this.constraints[i].Add(pred.constraints[i][j]);
                    }
                    for (int j = 0; j < newConstraint.Length; j++) {
                        this.constraints[i].Add(newConstraint[j]);
                    }
                }
            }
        }

        public void UpdateRoutes(Graph graph, GTSPSolver solver) {
            var constrainedSol = solution[agentConstrained];
            int offsetTime = 0;
            for (int i = 0; i < constrainedSol.Length; i++) {
                if (i > 0)
                    offsetTime += constrainedSol[i - 1].cost;
                constrainedSol[i].startTime = offsetTime;
                constrainedSol[i] = solver.SolveGTSP(graph, constraints[agentConstrained], agents[agentConstrained].orders[i], offsetTime);
            }

            // Calculate solution cost as a sum of costs of tours
            for (int i = 0; i < solution.Length; i++) {
                for (int j = 0; j < solution[i].Length; j++) {
                    this.cost += solution[i][j].cost;
                }
            }

            // Sum constraints count as a tie braking heristic for CBS
            int constr = 0;
            for (int i = 0; i < constraints.Length; i++) {
                constr += constraints[i].Count;
            }
            this.cost = (cost << 10) + constr;
        }

        public void CalculateInitRoutes(Graph graph, GTSPSolver solver) {
            int offsetTime = 0;
            for (int i = 0; i < solution.Length; i++) {
                for (int j = 0; j < solution[i].Length; j++) {
                    if (j > 0)
                        offsetTime += solution[i][j - 1].cost;
                    solution[i][j] = solver.SolveGTSP(graph, constraints[i], agents[i].orders[j], offsetTime);
                    this.cost += solution[i][j].cost;
                }
                offsetTime = 0;
            }
        }
    }

    public struct Constraint {
        public int agent;
        public int time;
        public int vertex;

        public Constraint(int time, int vertex, int agent) {
            this.time = time;
            this.vertex = vertex;
            this.agent = agent;
        }

        public override bool Equals(object obj) {
            if (obj is Constraint) {
                var x = (Constraint)obj;
                if (x.agent == this.agent &&
                    x.time == this.time &&
                    x.vertex == this.vertex) {
                    return true;
                }
                return false;
            }
            return base.Equals(obj);
        }
    }

    public struct Conflict {
        public int type;   // 0-vertex conflict, 1-edge conflict
        public int time;
        public int agent1;
        public int agent2;
        public int v1;
        public int v2;

        public Conflict(int type, int time, int agent1, int agent2, int v1, int v2) {
            this.type = type;
            this.time = time;
            this.agent1 = agent1;
            this.agent2 = agent2;
            this.v1 = v1;
            this.v2 = v2;
        }

        public (Constraint[], Constraint[]) MakeConstraints() {
            if (type == 0) {
                var c1 = new Constraint(time, v1, agent1);
                var c2 = new Constraint(time, v1, agent2);
                return (new Constraint[] { c1 }, new Constraint[] { c2 });
            }
            else {
                var c1 = new Constraint[2];
                var c2 = new Constraint[2];
                c1[0] = new Constraint(time + 1, v1, agent1);
                c1[1] = new Constraint(time + 1, v2, agent1);
                c2[0] = new Constraint(time + 1, v1, agent2);
                c2[1] = new Constraint(time + 1, v2, agent2);
                return (c1, c2);
            }
        }
    }
}
