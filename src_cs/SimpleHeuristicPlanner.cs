using System;
using System.Collections.Generic;

namespace src_cs {
    class SimpleHeuristicPlanner : Solver {
        readonly protected WarehouseInstance instance;
        readonly protected int agents;

        protected SimpleHeuristicPlanner(WarehouseInstance instance) {
            this.instance = instance;
            this.agents = instance.AgentCount;            
        }

        public override Tour[][] FindTours() {
            Tour[][] solution;

            // Init solution array
            solution = new Tour[agents][];
            for (int i = 0; i < agents; i++) {
                solution[i] = new Tour[instance.orders[i].Length];
            }

            for (int agent = 0; agent < agents; agent++) {
                for (int order = 0; order < instance.orders[agent].Length; order++) {
                    var orderInstance = instance.orders[agent][order];
                    solution[agent][order] = FindTour(instance.graph, orderInstance);
                }
            }
            return solution;
        }

        Tour FindTour(Graph graph, OrderInstance order) {
            var startLoc = order.startLoc;
            var targetLoc = order.targetLoc;
            var vertices = order.vertices;
            var classes = order.classes;
            var orderId = order.orderId;
            var pickTimes = order.pickTimes;
            var classesCount = classes[^1];

            List<(int vClass, int vertex)> selected = new List<(int, int)>();


            // Filter locations so only one is left per item
            List<int>[] sorted = new List<int>[classesCount+1];
            for (int i = 0; i < classesCount; i++) {
                sorted[i] = new List<int>();
            }

            for (int i = 0; i < vertices.Length; i++) {
                sorted[classes[i]].Add(i);
            }

            List<(int classId, int count)> locCount = new List<(int classId, int count)>();
            for (int i = 1; i < classesCount+1; i++) {
                locCount.Add((i, sorted[i].Count));
            }
            locCount.Sort((a,b) => a.count.CompareTo(b.count));

            // Pick classes with the least degrees of freedom first.
            for (int i = 0; i < locCount.Count; i++) {
                var currClass = locCount[i].classId;
                var vertIndices = sorted[currClass];

                // Pick vertex, that is closest to start loc and all other selected vertices
                int minDistance = int.MaxValue;
                int minVertex = 0;

                foreach (var vertexId in vertIndices) {
                    var vertex = graph.vertices[vertices[vertexId]];
                    var curVertex = vertices[vertexId];
                    

                    var (dist,path) = graph.ShortestRoute(startLoc, curVertex, 0, 0, null, false);
                    int currDistance = dist + ((selected.Count+1) * pickTimes[vertexId]);
                    foreach (var selVal in selected) {                        
                        (dist, path) = graph.ShortestRoute(selVal.vertex, curVertex, 0, 0, null, false);
                        currDistance += dist;                        
                    }

                    if (currDistance < minDistance) {
                        minDistance = currDistance;
                        minVertex = curVertex;
                    }
                }
                selected.Add((currClass, minVertex));
            }
            

            // Use some heuristic to plan a route
            // Traversal, mid-point, s-shape, largest gap


            return null;
        }

    }
}
