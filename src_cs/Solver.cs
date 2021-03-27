using System;
using System.Collections.Generic;

namespace src_cs {
    public abstract class Solver {
        public abstract Tour[][] FindTours();
        public virtual string[] GetStats() {            
            return new string[]{"empty", "0"};
        }

        public static int GetOrderOffset(Tour[][] solution, int agent, int orderIdx) {
            int offset = 0;
            for (int i = 0; i < orderIdx; i++) {
                offset += solution[agent][i].Distance;
            }
            return offset;
        }

        public static int GetMaxTours(OrderInstance[][] orders) {
            var agents = orders;
            int max = int.MinValue;
            foreach (var agent in agents) {
                max = Math.Max(max, agent.Length);
            }
            return max;
        }
    }

    abstract class ConstraintSolver : Solver {
        readonly protected WarehouseInstance instance;
        readonly protected GTSPSolver solver;
        readonly protected int maxTime;
        readonly protected int agents;
        protected List<int>[] nodesVisitors0;
        protected List<int>[] nodesVisitors1;
        

        protected ConstraintSolver(WarehouseInstance instance) {
            this.instance = instance;
            this.agents = instance.AgentCount;
            this.nodesVisitors0 = new List<int>[instance.graph.vertices.Count];
            this.nodesVisitors1 = new List<int>[instance.graph.vertices.Count];
            for (int i = 0; i < nodesVisitors0.Length; i++) {
                nodesVisitors0[i] = new List<int>();
                nodesVisitors1[i] = new List<int>();
            }

            GTSPSolver.FindMaxValues(instance.orders, out int maxClasses,
                out int maxItems, out int maxOrders, out int maxSolverTime);
            solver = new GTSPSolver(maxClasses, maxItems, maxSolverTime);
            maxTime = maxSolverTime * maxOrders;
        }

        public override Tour[][] FindTours() {
            throw new NotImplementedException();
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
                        if (currVertices[i] == 0 || instance.graph.vertices[currVertices[i]] is StagingVertex) continue;

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
                        nextVertices[i] = 0;
                    }
                }
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

        public override int GetHashCode() {
            return HashCode.Combine(agent, time, vertex);
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
