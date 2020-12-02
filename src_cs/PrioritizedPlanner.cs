using System.Collections.Generic;

namespace src_cs {
    class PrioritizedPlanner : ConstraintSolver {
        public PrioritizedPlanner(WarehouseInstance instance) : base(instance) {

        }

        public override Tour[][] FindTours() {
            Tour[][] solution;
            List<Constraint> constraints = new List<Constraint>();

            // Init tours array
            solution = new Tour[agents][];
            for (int i = 0; i < agents; i++) {
                solution[i] = new Tour[instance.orders[i].Length];
            }

            // Calculate non-conflicting routes sequentially
            for (int i = 0; i < solution.Length; i++) {
                constraints.Clear();                
                while (true) {
                    int offsetTime = 0;
                    for (int j = 0; j < solution[i].Length; j++) {
                        if (j > 0)
                            offsetTime += solution[i][j - 1].Length;
                        solution[i][j] = solver.SolveGTSP(instance.graph, constraints, instance.orders[i][j], offsetTime);

                    }

                    if (FindConflicts(solution, out Conflict c)) {
                        var conf = c.MakeConstraints();
                        var constraint = conf.Item1[0].agent == i ? conf.Item1 : conf.Item2;
                        for (int k = 0; k < conf.Item1.Length; k++) {
                            constraints.Add(constraint[k]);
                        }
                    }
                    else
                        break;
                }
            }
            System.Console.WriteLine("PP found a solution.");
            return solution;
        }
    }
}
