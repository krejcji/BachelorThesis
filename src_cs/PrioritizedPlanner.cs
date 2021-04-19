using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace src_cs {
    class PrioritizedPlanner : ConstraintSolver {
        public delegate int HeuristicMetric(OrderInstance i);
        readonly protected IEnumerable<(int, int)> heuristicEnum;
        Tour[][] solution;        

        public PrioritizedPlanner(WarehouseInstance instance) : base(instance) {
            heuristicEnum = DefaultEnum(instance.orders);
            constraints = new ConstraintManagerDense();
        }

        public PrioritizedPlanner(WarehouseInstance instance, Heuristic h) : base(instance) {
            heuristicEnum = h switch {
                Heuristic.ClassesHigh => LessClassesLast(instance.orders),
                Heuristic.ClassesLow => LessClassesFirst(instance.orders),
                Heuristic.Default => DefaultEnum(instance.orders),
                _ => throw new NotImplementedException(),
            };
            constraints = new ConstraintManagerDense();
        }

        public PrioritizedPlanner(WarehouseInstance instance, HeuristicMetric h) :base(instance) {
            this.heuristicEnum = HeuristicEnum(instance.orders, h, false);
            constraints = new ConstraintManagerDense();
        }

        public override Tour[][] FindTours() {            
            // Init solution array
            solution = new Tour[agents][];
            for (int i = 0; i < agents; i++) {
                solution[i] = new Tour[instance.orders[i].Length];
            }

            // Calculate the non-conflicting routes based on a heuristic
            foreach (var (tour,agent) in heuristicEnum)  {
                constraints.Clear();
                int offsetTime = GetOrderOffset(solution, agent, tour);
                
                // Add all constraints
                for (int i = 0; i < solution.Length; i++) {
                    for (int j = 0; j < solution[i].Length; j++) {
                        if (solution[i][j] == null || i == agent) continue;                      

                        constraints.AddConstraints(solution[i][j]);
                    }
                }                              

                Console.WriteLine($"Agent: {agent}, tour: {tour}, constraints: {constraints.Count}");
                //solution[agent][tour] = solver.SolveGTSP(instance.graph, constraints, instance.orders[agent][tour], offsetTime);                
                var sw = new Stopwatch();
                sw.Start();
                solution[agent][tour] = solver.SolveGTSP(instance.graph, constraints, instance.orders[agent][tour], offsetTime);
                sw.Stop();
                var t1 = sw.ElapsedMilliseconds;
                sw.Reset();
                sw.Start();
                var tMax = solution[agent][tour].Length;
                var altSol = solver.DepthLimitedGTSP(instance.graph, constraints, instance.orders[agent][tour], offsetTime, tMax);
                sw.Stop();
                var t2 = sw.ElapsedMilliseconds;
                Console.WriteLine($"Standard: {t1}, New: {t2}");

            }

            System.Console.WriteLine("PP found a solution.");
            solver.PrintStatistic();
            return solution;
        }

        public override string[] GetStats() {
            return solver?.GetStats();
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

        protected static IEnumerable<(int, int)> DefaultEnum(OrderInstance[][] orders) {
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
            Default,
            ClassesLow,
            ClassesHigh
        }
    }
}
