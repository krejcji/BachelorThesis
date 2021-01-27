using System;
using System.Collections.Generic;


namespace src_cs {
    class PrioritizedPlanner : ConstraintSolver {
        public delegate int HeuristicMetric(OrderInstance i);
        readonly IEnumerable<(int, int)> heuristicEnum;

        public PrioritizedPlanner(WarehouseInstance instance) : base(instance) {
            heuristicEnum = DefaultEnum(instance.orders);
        }

        public PrioritizedPlanner(WarehouseInstance instance, Heuristic h) : base(instance) {
            heuristicEnum = h switch {
                Heuristic.ClassesHigh => LessClassesLast(instance.orders),
                Heuristic.ClassesLow => LessClassesFirst(instance.orders),
                _ => throw new NotImplementedException(),
            };
        }

        public PrioritizedPlanner(WarehouseInstance instance, HeuristicMetric h) :base(instance) {
            this.heuristicEnum = HeuristicEnum(instance.orders, h, false);
        }

        public override Tour[][] FindTours() {
            Tour[][] solution;
            List<Constraint> constraints = new List<Constraint>();

            // Init tours array
            solution = new Tour[agents][];
            for (int i = 0; i < agents; i++) {
                solution[i] = new Tour[instance.orders[i].Length];
            }
            
            var orders = instance.orders;

            // Calculate the non-conflicting routes based on a heuristic
            foreach (var (tour,agent) in heuristicEnum)  {
                constraints.Clear();
                int offsetTime = GetOrderOffset(solution, agent, tour);

                while (true) {
                    solution[agent][tour] = solver.SolveGTSP(instance.graph, constraints, instance.orders[agent][tour], offsetTime);

                    if (FindConflicts(solution, out Conflict c)) {
                        var conf = c.MakeConstraints();
                        var constraint = conf.Item1[0].agent == agent ? conf.Item1 : conf.Item2;
                        for (int k = 0; k < conf.Item1.Length; k++) {
                            constraints.Add(constraint[k]);
                        }
                    }
                    else
                        break;
                }

            }

            System.Console.WriteLine("PP found a solution.");
            solver.PrintStatistic();
            return solution;
        }

        private int GetOrderOffset(Tour[][] solution, int agent, int orderIdx) {
            int offset = 0;
            for (int i = 0; i < orderIdx; i++) {
                offset += solution[agent][i].Length;
            }
            return offset;
        }

        private static int GetMaxTours(OrderInstance[][] orders) {
            var agents = orders;
            int max = int.MinValue;
            foreach (var agent in agents) {
                max = Math.Max(max, agent.Length);
            }
            return max;
        }

        private static int GetItemsCount(OrderInstance o) {
            return o.positions.Length;
        }

        private static int GetClassesCount(OrderInstance o) {
            return o.classes[^1];
        }

        private static IEnumerable<(int ,int)> HeuristicEnum(OrderInstance[][] orders, HeuristicMetric h, bool descending ) {
            int maxTours = GetMaxTours(orders);
            (int agent,int hValue)[] permutation = new (int,int)[orders.Length];

            for (int t = 0; t < maxTours; t++) {
                for (int a = 0; a < orders.Length; a++) {
                    if (t >= orders[a].Length)
                        permutation[a] = (a, -1);
                    else {
                        permutation[a] = (a, h(orders[a][t]));
                    }
                }

                if (!descending) 
                    Array.Sort(permutation, (v0, v1) => v0.hValue.CompareTo(v1.hValue));
                else 
                    Array.Sort(permutation, (v0, v1) => v1.hValue.CompareTo(v0.hValue));
                
                for (int i = 0; i < permutation.Length; i++) {
                    if (permutation[i].hValue != -1) {
                        yield return (t, permutation[i].agent);
                    }
                }
            }
        }

        private static IEnumerable<(int, int)> DefaultEnum(OrderInstance[][] orders) {
            for (int agent = 0; agent < orders.Length; agent++) {
                for (int order = 0; order < orders[agent].Length; order++) {
                    yield return (order, agent);
                }
            }            
        }

        private static IEnumerable<(int, int)> LessItemsFirst(OrderInstance[][] orders) {            
            return HeuristicEnum(orders, GetItemsCount, false);
        }

        private static IEnumerable<(int, int)> LessItemsLast(OrderInstance[][] orders) {
            return HeuristicEnum(orders, GetItemsCount, true);
        }

        private static IEnumerable<(int, int)> LessClassesFirst(OrderInstance[][] orders) {
            return HeuristicEnum(orders, GetClassesCount, false);
        }

        private static IEnumerable<(int, int)> LessClassesLast(OrderInstance[][] orders) {
            return HeuristicEnum(orders, GetClassesCount, true);
        }    
        
        public enum Heuristic {
            ClassesLow,
            ClassesHigh
        }
    }
}
