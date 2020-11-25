using Priority_Queue;
using System;
using System.Collections.Generic;

namespace src_cs {
    class CBS : Solver {
        private List<int>[] nodesVisitors0;
        private List<int>[] nodesVisitors1;
        private readonly WarehouseInstance instance;
        private readonly GTSPSolver solver;
        private readonly int maxTime;
        private readonly int agents;

        public CBS(WarehouseInstance instance, int maxTime) {
            this.maxTime = maxTime;
            this.instance = instance;
            this.agents = instance.AgentCount;
            this.nodesVisitors0 = new List<int>[instance.graph.vertices.Count];
            this.nodesVisitors1 = new List<int>[instance.graph.vertices.Count];
            for (int i = 0; i < nodesVisitors0.Length; i++) {
                nodesVisitors0[i] = new List<int>();
                nodesVisitors1[i] = new List<int>();
            }

            GTSPSolver.FindMaxValues(instance.orders, maxTime, out int maxClasses,
                out int maxItems, out int maxSolverTime);
            this.solver = new GTSPSolver(maxClasses, maxItems, maxSolverTime);
        }

        public override Tour[][] FindTours() {
            var queue = new FastPriorityQueue<CBSNode>(agents * 15000);
            var root = new CBSNode(instance.orders);
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

        public bool FindConflicts(Tour[][] tours, out Conflict conflict) {
            conflict = new Conflict();
            var currVertices = new int[tours.Length];
            var nextVertices = new int[tours.Length];
            var enums = new IEnumerator<int>[tours.Length];
            for (int i = 0; i < enums.Length; i++) {
                enums[i] = Tour.GetArrayEnum(tours[i]);
                if (enums[i].MoveNext()) {
                    currVertices[i] = enums[i].Current;
                }
                if (enums[i].MoveNext()) {
                    nextVertices[i] = enums[i].Current;
                }
            }

            // Add agent indicies into visited vertices at 0 timestep
            for (int j = 0; j < agents; j++) {
                if (currVertices[j] == 0) continue;  // Depot/default value can be occupied by more agents
                nodesVisitors0[currVertices[j]].Add(j);
            }

            for (int time = 0; time < maxTime - 1; time++) {
                // Look for vertex conflicts.
                for (int i = 0; i < agents; i++) {
                    if (nodesVisitors0[currVertices[i]].Count > 1) {
                        if (currVertices[i] == 0) continue;

                        // Report conflict and clean up the lists.
                        var visitList = nodesVisitors0[currVertices[i]];
                        conflict = new Conflict(0, time, visitList[0], visitList[1], currVertices[i], 0);
                        ClearTimeStep();
                        return true;
                    }
                }

                // Look for edge conflicts.
                // Fill in t+1 used vertices.
                if (time == maxTime - 1) continue;
                for (int j = 0; j < agents; j++) {
                    if (nextVertices[j] == 0) continue;
                    nodesVisitors1[nextVertices[j]].Add(j);
                }

                // Look for conflicts. If edge x->y, then not y->x.
                for (int j = 0; j < agents; j++) {
                    int from = currVertices[j];
                    int to = nextVertices[j];
                    if (from == to) continue;
                    if (nodesVisitors0[to].Count == 1 && nodesVisitors1[from].Count > 0) {
                        var visitor0 = nodesVisitors0[to][0];
                        for (int k = 0; k < nodesVisitors1[from].Count; k++) {
                            if (visitor0 == nodesVisitors1[from][k]) {
                                conflict = new Conflict(1, time, j,
                                    visitor0, from, to);
                                ClearTimeStep();
                                return true;
                            }
                        }
                    }
                }
                NextTimeStep();
            }

            ClearTimeStep();
            return false;

            void ClearTimeStep() {
                for (int i = 0; i < agents; i++) {
                    nodesVisitors0[currVertices[i]].Clear();
                    nodesVisitors1[nextVertices[i]].Clear();
                }
            }

            void NextTimeStep() {
                for (int i = 0; i < agents; i++) {
                    nodesVisitors0[currVertices[i]].Clear();
                }
                {
                    var tmp = nodesVisitors0;
                    nodesVisitors0 = nodesVisitors1;
                    nodesVisitors1 = tmp;
                }
                {
                    var tmp = currVertices;
                    currVertices = nextVertices;
                    nextVertices = tmp;
                }
                for (int i = 0; i < agents; i++) {
                    if (enums[i].MoveNext()) {
                        nextVertices[i] = enums[i].Current;
                    }
                    else {
                        nextVertices[i] = currVertices[i];
                    }
                }
            }
        }
    }

    class CBSNode : FastPriorityQueueNode {
        CBSNode pred;
        OrderInstance[][] orders;
        public int cost;
        int agentConstrained;
        List<Constraint>[] constraints;
        // List<Conflict> conflicts;
        public Tour[][] solution;

        public CBSNode(OrderInstance[][] orders) {
            pred = null;
            // conflicts = new List<Conflict>();
            this.orders = orders;

            // Init empty constraints lists
            constraints = new List<Constraint>[orders.Length];
            for (int i = 0; i < constraints.Length; i++) {
                constraints[i] = new List<Constraint>();
            }

            // Init tours array
            solution = new Tour[orders.Length][];
            for (int i = 0; i < orders.Length; i++) {
                solution[i] = new Tour[orders[i].Length];
            }
        }

        public CBSNode(CBSNode pred, Constraint[] newConstraint) {
            //this.pred = pred;
            this.orders = pred.orders;
            this.solution = new Tour[orders.Length][];
            for (int i = 0; i < orders.Length; i++) {
                solution[i] = new Tour[orders[i].Length];
            }

            for (int i = 0; i < orders.Length; i++) {
                for (int j = 0; j < orders[i].Length; j++) {
                    this.solution[i][j] = pred.solution[i][j];
                }
            }

            // Copy old constraints and add new ones.
            this.constraints = new List<Constraint>[orders.Length];
            this.agentConstrained = newConstraint[0].agent;
            for (int i = 0; i < orders.Length; i++) {
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
                    offsetTime += constrainedSol[i - 1].Length;
                constrainedSol[i].startTime = offsetTime;
                constrainedSol[i] = solver.SolveGTSP(graph, constraints[agentConstrained], orders[agentConstrained][i], offsetTime);
            }

            // Calculate solution cost as a sum of costs of tours
            for (int i = 0; i < solution.Length; i++) {
                for (int j = 0; j < solution[i].Length; j++) {
                    this.cost += solution[i][j].Length;
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
                        offsetTime += solution[i][j - 1].Length;
                    solution[i][j] = solver.SolveGTSP(graph, constraints[i], orders[i][j], offsetTime);
                    this.cost += solution[i][j].Length;
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