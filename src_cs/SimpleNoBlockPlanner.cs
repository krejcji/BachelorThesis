using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace src_cs {
    class SimpleNoBlockPlanner : ConstraintSolver {
        public SimpleNoBlockPlanner(WarehouseInstance instance) : base(instance) {
            constraints = new ConstraintManagerDummy();
        }

        public override Tour[][] FindTours() {
            Tour[][] solution;            

            // Init solution array
            solution = new Tour[agents][];
            for (int i = 0; i < agents; i++) {
                solution[i] = new Tour[instance.orders[i].Length];
            }

            // Calculate tours independently
            for (int agent = 0; agent < instance.AgentCount; agent++) {
                int offset = 0;
                for (int order = 0; order < instance.orders[agent].Length; order++) {                  
                    solution[agent][order] = solver.SolveGTSP(instance.graph, constraints, instance.orders[agent][order], offset);
                    offset += solution[agent][order].Distance;
                }
            }
            return solution;
        }
    }
}
