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
            aStarQueue = new FastPriorityQueue<AStarNode>(vertices.Count);
            nodeFactory = new AStarNodeFactory(vertices.Count * 3);
            queueCacheA = new AStarNode[vertices.Count];

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
        /// <param name="x">Pick location x.</param>
        /// <param name="y">Pick location y.</param>
        /// <param name="offsetTime">Time at the source vertex.</param>
        /// <param name="constraints"></param>
        /// <param name="reverse">Find backwards route if true.</param>
        /// <returns></returns>
        public (int, int[]) ShortestRoute(int x, int y, int offsetTime, SortedList<int, List<int>> constraints, bool reverse) {
            // TODO : Binary search
            if (!reverse) {
                int maxTime = distancesCache[x][y] + offsetTime;
                var route = routesCache[x][y];
                if (distancesCache[x][y] == 0 || route == null) {
                    return AStar(x, y, constraints, offsetTime, reverse);
                }
                for (int i = offsetTime; i <= maxTime; i++) {
                    if (constraints.ContainsKey(i)) {
                        for (int j = 0; j < constraints[i].Count; j++) {
                            if (route[i - offsetTime] == constraints[i][j]) {
                                return AStar(x, y, constraints, offsetTime, false);
                            }
                        }
                    }
                }
            }
            else {
                int minTime = offsetTime - distancesCache[x][y];
                if (distancesCache[x][y] > 0 && minTime < 0) {
                    return (minTime, new int[0]);
                }
                var route = routesCache[x][y];
                for (int i = offsetTime; i >= minTime; i--) {
                    if (constraints.ContainsKey(i)) {
                        for (int j = 0; j < constraints[i].Count; j++) {
                            if (route[offsetTime - i] == constraints[i][j]) {
                                return AStar(x, y, constraints, offsetTime, true);
                            }
                        }
                    }
                }
            }
            return (distancesCache[x][y], routesCache[x][y]);
        }

        public (int, int[]) AStar(int x, int y, SortedList<int, List<int>> constraints, int beginTime, bool reverse) {
            // TODO: Add time computation for vertex visit 
            // TODO: Call dijkstra for heuristic costs

            var queue = aStarQueue;
            var firstNode = nodeFactory.GetNode(x, 0, null);
            queue.Enqueue(firstNode, 0);

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
                    CleanUpQueueCache();
                    return (currRouteCost, result);
                }

                int heuristicCost = 0;
                int targetOffsetTime = reverse == false ? beginTime + currNode.routeCost + 1 : beginTime - currNode.routeCost - 1;

                // Stay at node for another time step, if not constrained
                if (constraints != null) {
                    if (constraints.ContainsKey(targetOffsetTime)) {
                        foreach (var constraint in constraints[targetOffsetTime]) {
                            if (constraint == currNode.index)
                                goto Neighbors;
                        }
                    }
                    heuristicCost = currNode.routeCost + 1 + distancesCache[currNode.index][y];
                    EnqueueNode(currNode.index, currRouteCost + 1, heuristicCost, currNode);
                }

            Neighbors:
                // Add all neighbors into queue, if edge is not constrained.
                foreach (var neighbor in vertices[currNode.index].edges) {
                    int neighborIdx = neighbor.x == currNode.index ? neighbor.y : neighbor.x;
                    if (currNode.predecessor != null && neighborIdx == currNode.predecessor.index)
                        continue;
                    int transitionCost = neighbor.cost;
                    var neighborRouteCost = currNode.routeCost + transitionCost;
                    heuristicCost = neighborRouteCost + distancesCache[neighborIdx][y];

                    // Check if constraints are not violated.
                    targetOffsetTime = reverse == false ? beginTime + neighborRouteCost : beginTime - neighborRouteCost;
                    if (constraints != null && constraints.ContainsKey(targetOffsetTime)) {
                        foreach (var value in constraints[targetOffsetTime]) {
                            if (value == neighborIdx)
                                goto Skip;
                        }
                    }
                    EnqueueNode(neighborIdx, neighborRouteCost, heuristicCost, currNode);
                Skip:
                    ;
                }
            }
            throw new ArgumentException("Shortest path not found, check the arguments.");

            void EnqueueNode(int nodeIndex, int routeCost, int heuristicCost, AStarNode prevNode) {
                AStarNode nextNode = queueCacheA[nodeIndex];
                if (nextNode != null) {               // update value if better cost                
                    if (nextNode.routeCost > routeCost) {
                        nextNode.routeCost = routeCost;
                        nextNode.predecessor = prevNode;
                        queue.UpdatePriority(nextNode, heuristicCost);
                    }
                }
                else {                                                // first visiting the vertex
                    nextNode = nodeFactory.GetNode(nodeIndex, routeCost, prevNode);
                    queueCacheA[nodeIndex] = nextNode;
                    queue.Enqueue(nextNode, heuristicCost);
                }
            }

            void CleanUpQueueCache() {
                while (queue.Count > 0) {
                    var node = queue.Dequeue();
                    node.predecessor = null;
                    queueCacheA[node.index] = null;
                }
                nodeFactory.ResetIndex();
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