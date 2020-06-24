using System;
using System.Collections.Generic;
using System.IO;

namespace src_cs {
    public class WarehouseInstance {
        public Graph graph;
        public Agent[] agents;

        public WarehouseInstance(Graph graph, Agent[] agents) {
            this.graph = graph;
            this.agents = agents;
            graph.Initialize(agents);
        }
    }

    class InstanceParser {
        public static WarehouseInstance Parse(string instancePath) {
            StreamReader file = new StreamReader(instancePath);
            Graph graph = new Graph();
            Dictionary<string, int> verticesIndices = new Dictionary<string, int>();
            string line;
            string[] tokens;
            file.ReadLine();
            tokens = file.ReadLine().Split();
            int vertices = int.Parse(tokens[1]);
            int storageHeight = int.Parse(file.ReadLine().Split()[3]);

            // Parse vertices and items stored inside.
            for (int i = 0; i < vertices; i++) {
                tokens = file.ReadLine().Split();
                verticesIndices.Add(tokens[1], i);
                if (tokens[2] == "Steiner") {
                    graph.AddNode(new Vertex(i));
                }
                else if (tokens[2] == "Depot") {
                    graph.AddNode(new Vertex(i));
                }
                else if (tokens[2] == "Shelf") {
                    int[,] left = new int[storageHeight, 2];
                    int[,] right = new int[storageHeight, 2];
                    line = file.ReadLine();
                    string[] leftTokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    line = file.ReadLine();
                    string[] rightTokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < leftTokens.Length; j++) {
                        string[] itemLeft = leftTokens[j].Split(',');
                        string[] itemRight = rightTokens[j].Split(',');
                        left[j, 0] = int.Parse(itemLeft[0]);
                        left[j, 1] = int.Parse(itemLeft[1]);
                        right[j, 0] = int.Parse(itemRight[0]);
                        right[j, 1] = int.Parse(itemRight[1]);
                    }
                    graph.AddNode(new StorageVertex(i, left, right));
                }
            }

            // Parse edges.
            file.ReadLine();
            tokens = file.ReadLine().Trim().Split();
            foreach (var edge in tokens) {
                string[] edgeVertices = edge.Split(",", StringSplitOptions.RemoveEmptyEntries);
                Edge newEdge = new Edge(verticesIndices[edgeVertices[0]], verticesIndices[edgeVertices[1]], 1);
                graph.AddEdge(newEdge);
            }

            // Parse agents and orders.
            List<Agent> agents = new List<Agent>();
            tokens = file.ReadLine().Split();
            int agentsCount = int.Parse(tokens[1]);
            int orderId = 0;

            for (int i = 0; i < agentsCount; i++) {
                tokens = file.ReadLine().Split();
                int ordersCount = int.Parse(tokens[3]);
                var ordersList = new List<Order>();

                for (int j = 0; j < ordersCount; j++) {
                    var orderItems = new List<List<(int, int, int)>>();
                    tokens = file.ReadLine().Split();
                    int classes = int.Parse(tokens[3]);
                    int source = int.Parse(tokens[5]);
                    int target = int.Parse(tokens[7]);

                    for (int k = 0; k < classes; k++) {
                        orderItems.Add(new List<(int, int, int)>());
                        tokens = file.ReadLine().Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        for (int l = 0; l < tokens.Length; l++) {
                            var tuple = tokens[l].Split(',');
                            var vertexId = int.Parse(tuple[0]);
                            var loc = int.Parse(tuple[1]);
                            var height = int.Parse(tuple[2]);
                            orderItems[k].Add((vertexId, loc, height));
                        }
                    }                    
                    ordersList.Add(new Order(orderId++, orderItems, source, target, graph));
                }
                agents.Add(new Agent(ordersList.ToArray(), i));
            }
            return new WarehouseInstance(graph, agents.ToArray());
        }
    }
}
