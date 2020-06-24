using Priority_Queue;
using System;
using System.Collections.Generic;

namespace src_cs {
    public class Vertex {
        public int index;
        public List<Edge> edges;

        public Vertex(int index) {
            this.index = index;
            this.edges = new List<Edge>();
        }

        public void AddEdge(Edge edge) {
            edges.Add(edge);
        }
    }

    public class StorageVertex : Vertex {
        public int[,] itemsLeft;
        public int[,] itemsRight;

        public StorageVertex(int index, int[,] left, int[,] right) : base(index) {
            this.itemsLeft = left;
            this.itemsRight = right;
        }
    }

    public class Edge {
        public int x;
        public int y;
        public int cost;

        public Edge(int x, int y, int cost) {
            this.x = x;
            this.y = y;
            this.cost = cost;
        }
    }

    public class Graph {
        private int[][] distancesCache;
        private int[][][] routesCache;
        private AStarNode[] queueCacheA;
        private AStarNode[] emptyArr;
        private AStarNodeFactory nodeFactory;
        private FastPriorityQueue<AStarNode> aStarQueue;
        public List<Vertex> vertices;
        public List<Edge> edges;
        public Order[] orders;

        public Graph() {
            this.vertices = new List<Vertex>();
            this.edges = new List<Edge>();
        }

        public void AddNode(Vertex vertex) {
            vertex.index = vertices.Count;
            vertices.Add(vertex);
        }

        public void AddEdge(Edge edge) {
            edges.Add(edge);
            vertices[edge.x].AddEdge(edge);
            vertices[edge.y].AddEdge(edge);
        }

        public void Initialize(Agent[] agents) {
            aStarQueue = new FastPriorityQueue<AStarNode>(2 * vertices.Count);
            nodeFactory = new AStarNodeFactory(2 * vertices.Count);
            queueCacheA = new AStarNode[vertices.Count];
            emptyArr = new AStarNode[vertices.Count];

            // Init orders
            var tmpOrders = new List<Order>();
            foreach (var agent in agents) {
                foreach (var order in agent.orders) {
                    tmpOrders.Add(order);
                }
            }
            this.orders = tmpOrders.ToArray();

            // Init cache arrays
            distancesCache = new int[vertices.Count][];
            routesCache = new int[vertices.Count][][];
            for (int i = 0; i < this.vertices.Count; i++) {
                distancesCache[i] = new int[this.vertices.Count];
                routesCache[i] = new int[this.vertices.Count][];
            }

            InitTourRoutes();


            void InitTourRoutes() {
                HashSet<int> verticesUsed = new HashSet<int>();
                // Init shortest distances for pick destinations
                Queue<QueueNode> q = new Queue<QueueNode>(vertices.Count);
                foreach (var order in orders) {
                    for (int i = 0; i < order.vertices.Length; i++) {
                        verticesUsed.Add(order.vertices[i]);
                    }
                }
                foreach (var vertex in verticesUsed) {
                    BFS(q, vertex);
                }
                foreach (var vertex in verticesUsed) {
                    for (int i = 0; i < vertices.Count; i++) {
                        distancesCache[i][vertex] = distancesCache[vertex][i];
                    }
                }

                // For each order, calculate one shortest route.
                foreach (var order in orders) {
                    var vertices = order.vertices;

                    // Cache distance between each pair or items.
                    for (int j = 0; j < vertices.Length; j++) {
                        for (int k = 0; k < vertices.Length; k++) {
                            if (j < k) {
                                if (routesCache[vertices[j]][vertices[k]] != null) continue;
                                var (distance, route) = AStar(vertices[j], vertices[k], null, 0, false);
                                routesCache[vertices[j]][vertices[k]] = route;
                                var reverseRoute = new int[route.Length];
                                for (int i = 0; i < route.Length; i++) {
                                    reverseRoute[route.Length - i - 1] = route[i];
                                }
                                routesCache[vertices[k]][vertices[j]] = reverseRoute;
                            }
                        }
                    }
                }
            }
        }

        public int GetPickTime(int vertex, int position, int height) {
            var sVertex = vertices[vertex] as StorageVertex;
            if (position == 0) {
                return sVertex.itemsLeft[height, 1];
            }
            else {
                return sVertex.itemsRight[height, 1];
            }
        }

        /// <summary>
        /// Finds shortest route between two picking locations.
        /// </summary>
        /// <param name="pickVertex">Pick location x.</param>
        /// <param name="target">Pick location y.</param>
        /// <param name="realTime">Time at the source vertex.</param>
        /// <param name="constraints"></param>
        /// <param name="reverseSearch">Find backwards route if true.</param>
        /// <returns></returns>
        public (int, int[]) ShortestRoute(int pickVertex, int target, int itemId, int orderId, int realTime, SortedList<int, List<int>> constraints,
            bool reverseSearch, bool returnPath) {
            int pickTime = orders[orderId].pickTimes[itemId];            
            int maxTime = reverseSearch ? realTime : realTime + pickTime + distancesCache[pickVertex][target];
            int minTime = reverseSearch ? realTime - distancesCache[pickVertex][target] - pickTime : realTime;
            (int, int[]) route = (distancesCache[pickVertex][target] + pickTime, routesCache[pickVertex][target]);


            if (constraints != null && minTime >= 0) {
                foreach (var time in constraints.Keys) {
                    if (time >= minTime && time <= maxTime) {
                        int relativeTime = time - minTime;
                        for (int i = 0; i < constraints[time].Count; i++) {
                            if (relativeTime < pickTime && pickVertex == constraints[time][i] ||
                                (relativeTime >= pickTime && route.Item2[relativeTime-pickTime] == constraints[time][i])) {
                                route = AStar(pickVertex, target, constraints, realTime + pickTime, reverseSearch);
                                route.Item1 += pickTime;
                                if (returnPath)
                                    return (route.Item1 + pickTime, RouteWithPickVertices());
                                else
                                    return (route.Item1 + pickTime, null);
                            }
                        }
                    }
                }
            }
            if (!returnPath) {
                return (distancesCache[pickVertex][target] + pickTime, null);
            }
            else {
                return (distancesCache[pickVertex][target] + pickTime, RouteWithPickVertices());
            }

            int[] RouteWithPickVertices() {
                var result = new int[route.Item2.Length + pickTime];
                for (int i = 0; i < pickTime; i++) {
                    result[i] = pickVertex;
                }
                for (int i = pickTime; i < route.Item2.Length+pickTime; i++) {
                    result[i] = route.Item2[i - pickTime];
                }
                return result;
            }
        }

        public (int, int[]) AStar(int x, int y, SortedList<int, List<int>> constraints, int beginTime, bool reverseSearch) {
            if (reverseSearch) {
                var tmp = x;
                x = y;
                y = tmp;
            }
            var queue = aStarQueue;
            EnqueueNode(x, 0, 0, null);

            while (queue.Count != 0) {
                var currNode = queue.Dequeue();
                queueCacheA[currNode.index] = null;
                var currRouteCost = currNode.routeCost;

                // If goal node.
                if (currNode.index == y) {
                    var result = new int[currNode.routeCost + 1];
                    result[currNode.routeCost] = currNode.index;
                    for (int i = currNode.routeCost; i > 0; i--) {
                        currNode = currNode.predecessor;
                        result[i - 1] = currNode.index;
                    }
                    if (reverseSearch)
                        ReverseRoute(result);
                   CleanUpQueueCache();
                    return (currRouteCost, result);
                }

                int heuristicCost;
                int neighborRouteCost = currNode.routeCost + 1;
                int targetOffsetTime = 0;

                // Stay at node for another time step, if not constrained
                //TODO: Only before a blocked vertex?
                if (constraints != null) {
                    targetOffsetTime = reverseSearch == false ? beginTime + neighborRouteCost : beginTime - neighborRouteCost;
                    if (constraints.ContainsKey(targetOffsetTime)) {
                        foreach (var constraint in constraints[targetOffsetTime]) {
                            if (constraint == currNode.index)
                                goto Neighbors;
                        }
                    }
                    heuristicCost = neighborRouteCost + distancesCache[currNode.index][y];
                    EnqueueNode(currNode.index, neighborRouteCost, heuristicCost, currNode);
                }

            Neighbors:
                // Add all neighbors into queue, if edge is not constrained.
                foreach (var neighbor in vertices[currNode.index].edges) {
                    int neighborIdx = neighbor.x == currNode.index ? neighbor.y : neighbor.x;

                    // No return edges.
                    if (currNode.predecessor != null && neighborIdx == currNode.predecessor.index)
                        continue;

                    heuristicCost = neighborRouteCost + distancesCache[neighborIdx][y];

                    // Check if constraints are not violated.                    
                    if (constraints != null && constraints.ContainsKey(targetOffsetTime)) {
                        foreach (var constrainedVertexIdx in constraints[targetOffsetTime]) {
                            if (constrainedVertexIdx == neighborIdx)
                                goto SkipEnqueue;
                        }
                    }
                    EnqueueNode(neighborIdx, neighborRouteCost, heuristicCost, currNode);
                SkipEnqueue:
                    ;
                }
            }
            throw new ArgumentException("Shortest path not found, check the arguments.");

            void EnqueueNode(int nodeIndex, int routeCost, int heuristicCost, AStarNode prevNode) {
                AStarNode nextNode = queueCacheA[nodeIndex];

                // newCost invariants - route with longer optimal length will always have higher newCost
                //                    - of routes with the same optimal cost the one furthest from beginnig is preferred
                float newCost = (heuristicCost << 10) - routeCost;

                if (nextNode != null) {               // update value if better cost                
                    if (nextNode.routeCost > routeCost) {
                        nextNode.routeCost = routeCost;
                        nextNode.predecessor = prevNode;
                        queue.UpdatePriority(nextNode, newCost);
                    }
                }
                else {                                // first visiting the vertex or requeue the current vertex
                    nextNode = nodeFactory.GetNode(nodeIndex, routeCost, prevNode);
                    queueCacheA[nodeIndex] = nextNode;
                    queue.Enqueue(nextNode, newCost);
                }
            }

            void CleanUpQueueCache() {
                queue.Clear();
                Array.Copy(emptyArr, queueCacheA, queueCacheA.Length);
                nodeFactory.ResetIndex();
            }

            static void ReverseRoute(int[] result) {
                int tmp;
                for (int i = 0; i < result.Length / 2; i++) {
                    tmp = result[i];
                    result[i] = result[result.Length - 1 - i];
                    result[result.Length - 1 - i] = tmp;
                }
            }
        }

        public class AStarNode : FastPriorityQueueNode {
            public int index;
            public int routeCost;
            public AStarNode predecessor;

            public void InitNode(int index, int cost, AStarNode predecessor) {
                this.routeCost = cost;
                this.predecessor = predecessor;
                this.index = index;
            }
        }

        public class AStarNodeFactory {
            private readonly AStarNode[] cache;
            private int index = 0;
            public AStarNodeFactory(int capacity) {
                cache = new AStarNode[capacity];
                for (int i = 0; i < capacity; i++) {
                    cache[i] = new AStarNode();
                }
            }

            public AStarNode GetNode(int vertexIndex, int cost, AStarNode predecessor) {
                AStarNode node;
                if (index == cache.Length) {
                    node = new AStarNode();
                    node.InitNode(vertexIndex, cost, predecessor);
                }
                else {
                    node = cache[index++];
                    node.InitNode(vertexIndex, cost, predecessor);
                }
                return node;
            }

            public void ResetIndex() {
                index = 0;
            }
        }

        void BFS(Queue<QueueNode> queue, int source) {
            queue.Enqueue(new QueueNode(source, 0));

            while (queue.Count > 0) {
                var currNode = queue.Dequeue();
                int neighborDistance = currNode.distance + 1;

                // Add all neighbors into queue, if edge is not constrained.
                foreach (var neighbor in vertices[currNode.vertexId].edges) {
                    int neighborIdx = neighbor.x == currNode.vertexId ? neighbor.y : neighbor.x;
                    if (distancesCache[source][neighborIdx] == 0) {
                        distancesCache[source][neighborIdx] = neighborDistance;
                        queue.Enqueue(new QueueNode(neighborIdx, neighborDistance));
                    }
                }
            }
            distancesCache[source][source] = 0;
        }

        struct QueueNode {
            public int vertexId;
            public int distance;

            public QueueNode(int id, int distance) {
                this.vertexId = id;
                this.distance = distance;
            }
        }
    }
}