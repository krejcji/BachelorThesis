using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Text;

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
        private DijkstraNode[] queueCacheD;
        private FastPriorityQueue<AStarNode> aStarQueue;
        private FastPriorityQueue<DijkstraNode> dijkstraQueue;
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
            dijkstraQueue = new FastPriorityQueue<DijkstraNode>(vertices.Count);
            nodeFactory = new AStarNodeFactory(vertices.Count * 3);
            queueCacheA = new AStarNode[vertices.Count];
            queueCacheD = new DijkstraNode[vertices.Count];
            for (int i = 0; i < queueCacheD.Length; i++) {
                queueCacheD[i] = new DijkstraNode(i);
            }

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
                // For each order, calculate shortest routes.
                foreach (var order in orders) {
                    var vertices = order.vertices;

                    // Cache distance between each pair or items.
                    for (int j = 0; j < vertices.Length; j++) {
                        for (int k = 0; k < vertices.Length; k++) {
                            if (j < k) {
                                if (distancesCache[vertices[j]][vertices[k]] == 0) {
                                    Dijkstra(vertices[j], vertices[k]);
                                    distancesCache[vertices[k]][vertices[j]] = 0;
                                    Dijkstra(vertices[k], vertices[j]);
                                }
                                if (routesCache[vertices[j]][vertices[k]] != null)
                                    continue;
                                var (distance, route) = AStar(vertices[j], vertices[k], null, 0, false);
                                routesCache[vertices[j]][vertices[k]] = route;
                                var (distance1, route1) = AStar(vertices[k], vertices[j], null, 0, false);
                                routesCache[vertices[k]][vertices[j]] = route1;
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
                if (Dijkstra(x,currNode.index) != currNode.routeCost) {
                    throw new Exception();
                }

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
                    heuristicCost = currNode.routeCost + 1 + Dijkstra(currNode.index, y);
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
                    int dijkstr = Dijkstra(neighborIdx, y);
                    heuristicCost = neighborRouteCost + dijkstr;

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

            public AStarNode() {

            }

            public void InitNode(int index, int cost, AStarNode predecessor) {
                this.routeCost = cost;
                this.predecessor = predecessor;
                this.index = index;
            }
        }

        public class AStarNodeFactory {
            readonly AStarNode[] cache;
            int index = 0;
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

        public int Dijkstra(int x, int y) {
            if (distancesCache[x][y] != 0) {
                return distancesCache[x][y];
            }
            else if (x == y) {
                return 0;
            }
            var queue = dijkstraQueue;
            var firstNode = GetNode(x, 0, -1);
            queue.Enqueue(firstNode, 0);

            while (queue.Count != 0) {
                var currNode = queue.Dequeue();
                currNode.Deque();
                var currRouteCost = currNode.routeCost;               
                distancesCache[currNode.index][x] = currRouteCost;
                distancesCache[x][currNode.index] = currRouteCost;
                                  

                // If goal node.
                if (currNode.index == y) {
                    distancesCache[x][y] = currRouteCost;
                    CleanUpQueueCache();
                    return currRouteCost;
                }

                // Add all neighbors into queue, if edge is not constrained.
                foreach (var neighbor in vertices[currNode.index].edges) {
                    int neighborIdx = neighbor.x == currNode.index ? neighbor.y : neighbor.x;
                    if (neighborIdx == currNode.predecessor) continue;                    
                    int transitionCost = neighbor.cost;
                    var neighborRouteCost = currNode.routeCost + transitionCost;
                    if (distancesCache[x][neighborIdx] != 0 && distancesCache[x][neighborIdx] < neighborRouteCost) continue;
                    EnqueueNode(neighborIdx, neighborRouteCost, currNode.index);
                }
            }
            throw new ArgumentException("Shortest path not found, check the arguments.");

            void EnqueueNode(int nodeIndex, int routeCost, int predecessor) {
                DijkstraNode nextNode = queueCacheD[nodeIndex];
                if (queueCacheD[nodeIndex].isEnqued) {               // update value if better cost                
                    if (nextNode.routeCost > routeCost) {
                        nextNode.routeCost = routeCost;
                        nextNode.predecessor = predecessor;
                        queue.UpdatePriority(nextNode, routeCost);
                    }
                }
                else {                                                // first visiting the vertex
                    nextNode.InitNode(routeCost, predecessor);
                    queue.Enqueue(nextNode, routeCost);
                }
            }

            void CleanUpQueueCache() {
                while (queue.Count > 0) {
                    var node = queue.Dequeue();
                    node.Deque();
                }
            }
        }

        public class DijkstraNode : FastPriorityQueueNode {
            public readonly int index;
            public int predecessor;
            public int routeCost;
            public bool isEnqued;

            public DijkstraNode(int index) {
                this.index = index;
            }

            public void InitNode(int cost, int predecessor) {
                this.isEnqued = true;
                this.routeCost = cost;
                this.predecessor = predecessor;
            }

            public void Deque() {
                isEnqued = false;
            }
        }

        DijkstraNode GetNode(int index, int cost, int predecessor) {
            queueCacheD[index].InitNode(cost, predecessor);
            return queueCacheD[index];
        }        
    }
}