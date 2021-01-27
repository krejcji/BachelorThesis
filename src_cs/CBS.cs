using Priority_Queue;
using System;
using System.Collections.Generic;

namespace src_cs {
    class CBS : ConstraintSolver {
        public CBS(WarehouseInstance instance) : base(instance) {

        }

        public override Tour[][] FindTours() {
            var queue = new FastPriorityQueue<CBSNode>(agents * 15000);
            var root = new CBSNode(instance.orders);
            root.CalculateInitRoutes(instance.graph, solver);
            queue.Enqueue(root, root.cost);

            int counter = 0;

            while (queue.Count != 0) {
                var currNode = queue.Dequeue();
                var foundConflict = FindConflicts(currNode.solution, out var conflict);
                if (!foundConflict) {
                    Console.WriteLine("Found solution.");
                    solver.PrintStatistic();
                    return currNode.solution;
                }
                var constraints = conflict.MakeConstraints();
                var left = new CBSNode(currNode, constraints.Item1);
                var right = new CBSNode(currNode, constraints.Item2);
                if (left.cost != 0) {
                    left.UpdateRoutes(instance.graph, solver);
                    queue.Enqueue(left, left.cost);
                }
                if (right.cost != 0) {
                    right.UpdateRoutes(instance.graph, solver);
                    queue.Enqueue(right, right.cost);
                }
                // TODO: Special constraints & better priority heuristic
                counter++;
                if (counter % 10 == 0) {
                    solver.PrintStatistic();
                }
            }
            return null;
        }
    }

    class CBSNode : FastPriorityQueueNode {
        OrderInstance[][] orders;
        public int cost;
        int agentConstrained;
        List<Constraint>[] constraints;
        // List<Conflict> conflicts;
        public Tour[][] solution;

        public CBSNode(OrderInstance[][] orders) {
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
            cost = 1;
            orders = pred.orders;
            solution = new Tour[orders.Length][];
            for (int i = 0; i < orders.Length; i++) {
                solution[i] = new Tour[orders[i].Length];
            }

            for (int i = 0; i < orders.Length; i++) {
                for (int j = 0; j < orders[i].Length; j++) {
                    this.solution[i][j] = pred.solution[i][j];
                }
            }

            // Copy old constraints and add new ones.
            constraints = new List<Constraint>[orders.Length];
            agentConstrained = newConstraint[0].agent;
            int tourLength = 0;
            for (int j = 0; j < solution[agentConstrained].Length; j++) {
                tourLength += solution[agentConstrained][j].Length;
            }
            if (newConstraint[0].time >= tourLength) {
                this.cost = 0;
                return;
            }

            for (int i = 0; i < orders.Length; i++) {
                if (i != agentConstrained) {
                    this.constraints[i] = pred.constraints[i];
                }
                else {
#if DEBUG
                    int conflicts = 0;
                    this.constraints[i] = pred.constraints[i];
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
                                if (constraints[i][j].Equals(newConstraint[k])) {
                                    conflicts++;
                                    if (conflicts == newConstraint.Length)
                                        throw new Exception("Adding existing constraint.");
                                }
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

            // Sum constraints count as a tie breaking heristic for CBS
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
}