using System;
using System.Collections.Generic;
using System.Text;

namespace src_cs {
    class GradualPrioritizedPlanner : PrioritizedPlanner {
        public GradualPrioritizedPlanner(WarehouseInstance instance) : base(instance, Heuristic.Default) {
            
        }

        public GradualPrioritizedPlanner(WarehouseInstance instance, Heuristic h) : base(instance, h) {

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
            foreach (var (tour, agent) in heuristicEnum) {
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
    }
}
