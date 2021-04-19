using System;
using System.Collections.Generic;

namespace src_cs {
    class SingleAgentPlanner : ConstraintSolver {

        public SingleAgentPlanner(WarehouseInstance instance) : base(instance) {
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
            return resolveConflicts(solution);
        }

        // Resolves all conflicts in a solution to a Multi-agent picker routing problem
        Tour[][] resolveConflicts(Tour[][] tours) {
            Tour[][] solution;
            constraints = new ConstraintManagerDense();

            // Sort orders by their order in input file
            List<(int priority, int agent, int order)> sortedOrders = new List<(int, int, int)>();
            for (int i = 0; i < instance.orders.Length; i++) {
                for (int j = 0; j < instance.orders[i].Length; j++) {
                    var order = instance.orders[i][j];
                    sortedOrders.Add((order.orderId, i, j));
                }
            }
            sortedOrders.Sort((a, b) => a.priority.CompareTo(b.priority));

            // Init solution array
            solution = new Tour[tours.Length][];
            for (int i = 0; i < tours.Length; i++) {
                solution[i] = new Tour[tours[i].Length];
            }

            // Find alternative non-blocking tours for agents according to order priority
            foreach (var order in sortedOrders) {
                var agent = order.agent;
                var i = order.order;
                int offset = GetOrderOffset(solution, agent, i);
                solution[agent][i] = resolveConflict(tours[agent][i], offset, constraints);

                constraints.AddConstraints(solution[agent][i]);
            }
            return solution;
        }

        // Plans new paths for one tour, so that it doesn't conflict with previously planned and executed tours.
        Tour resolveConflict(Tour tour, int tourStartTime, ConstraintManager constraints) {
            var g = instance.graph;
            int pathsOffset = 0;
            int depot = tour.routes[^1][^1];
            int[][] newRoutes = new int[tour.routes.Length][];

            // Initiate the pick list
            List<(int startV, int pickV, int nextV, int pickTime)> pickList = new List<(int, int, int, int)>();
            if (tour.pickVertices.Length == 1)
                pickList.Add((tour.routes[0][0], tour.pickVertices[0], tour.routes[^1][^1], tour.pickTimes[0]));
            else {
                pickList.Add((tour.routes[0][0], tour.pickVertices[0], tour.pickVertices[1], tour.pickTimes[0]));
                for (int i = 1; i < tour.pickVertices.Length - 1; i++) {
                    pickList.Add((tour.pickVertices[i - 1], tour.pickVertices[i], tour.pickVertices[i + 1], tour.pickTimes[i]));
                }
                pickList.Add((tour.pickVertices[^2], tour.pickVertices[^1], tour.routes[^1][^1], tour.pickTimes[^1]));
            }

            // Plan all non-conflicting paths except the last
            int j = 0;
            foreach (var pick in pickList) {
                int pathStartTime = tourStartTime + pathsOffset;

                // Find the shortest possible tour
                var (pathCost, route) = g.ShortestRoute(pick.startV, pick.pickV, 0, pathStartTime, constraints, false);

                bool firstPickOffset = true;
                int pickStart = pathStartTime + pathCost;
                if (constraints.IsConstrainedPick(pick.pickV, pickStart, pick.pickTime)) {
                    pickStart += nextPickOffset(pick.pickV, pickStart, pick.pickTime, firstPickOffset);
                    firstPickOffset = false;
                }

                while (true) {                    
                    // Does the (delayed) route exist?
                    (pathCost, route) = g.AStar(pick.startV, pick.pickV, constraints, pathStartTime, false, pickStart - pathStartTime);
                    if (route == null) {
                        pickStart += nextPickOffset(pick.pickV, pickStart, pick.pickTime, firstPickOffset);
                        firstPickOffset = false;
                        continue;
                    }

                    // Is it possible to leave the vertex?
                    var (length, r) = g.AStar(pick.pickV, depot, constraints, pickStart + pick.pickTime);
                    if (r == null) {
                        pickStart += nextPickOffset(pick.pickV, pickStart, pick.pickTime, firstPickOffset);
                        firstPickOffset = false;
                        continue;
                    }

                    newRoutes[j++] = route;
                    pathsOffset += pathCost + pick.pickTime;
                    break;
                }  
            }

            // Find the last path
            var (t1, r1) = g.ShortestRoute(tour.pickVertices[^1], depot, 0, pathsOffset+tourStartTime, constraints, false);
            newRoutes[^1] = r1;
            pathsOffset += t1;

            return new Tour(tourStartTime, pathsOffset+1, tour.pickVertices, tour.pickTimes, newRoutes);            
            
            // Called after failed isPickPossible - there will be a pickVertex constraint at constraintTime,
            // or delayed route doesn't exist, or pick vertex cannot be left
            int nextPickOffset(int pickVertex, int lastPickTime, int pickDuration, bool firstCall) {
                if (!firstCall) {
                    if (!constraints.IsConstrained(pickVertex, lastPickTime+pickDuration) && !constraints.IsConstrained(pickVertex, lastPickTime + pickDuration + 1))
                        return 1;
                }

                int delay = 1;
                int clearTimeSpan = 0;

                while (true) {
                    if (!constraints.IsConstrained(pickVertex, lastPickTime + delay + clearTimeSpan)) {                        
                        // Pick is possible at time lastPickTime+delay
                        if (clearTimeSpan == pickDuration)
                            return delay;
                        
                        clearTimeSpan++;
                    }
                    else {
                        delay += clearTimeSpan + 1;
                        clearTimeSpan = 0;
                    }
                }
                    // Constraint at the last checked vertex!
                    // Go through next pickTime free vertices, reset counter if constraint
                
                throw new Exception("Error in process of finding the next offset.");
            }
        }
    }
}
