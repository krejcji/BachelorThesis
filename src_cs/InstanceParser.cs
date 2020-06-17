using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace src_cs {    
    class WarehouseInstance {
        Graph graph;
        List<List<int>>[] orders;

        public WarehouseInstance(Graph graph, List<List<int>>[] orders) {
            this.graph = graph;
            this.orders = orders;
        }
    }
    class InstanceParser {
        public static WarehouseInstance Parse(string instancePath) {
            StreamReader file = new StreamReader(instancePath);
            Graph graph = new Graph();
            List<List<int>>[] orders;
            Dictionary<string, int> verticesIndices = new Dictionary<string, int>();
            string line = null;
            string[] tokens;
            file.ReadLine();
            tokens = file.ReadLine().Split();
            int vertices = int.Parse(tokens[1]);
            int storageHeight = int.Parse(file.ReadLine().Split()[3]);

            // Parse vertices and items stored inside.
            for (int i = 0; i < vertices; i++) {
                tokens = file.ReadLine().Split();
                verticesIndices.Add(tokens[1], i);
                if (tokens[2]== "Steiner") {
                    graph.AddNode(new Node(i));
                }
                else if (tokens[2] == "Depot") {
                    graph.AddNode(new Node(i));
                }
                else if (tokens[2]== "Shelf") {
                    int[] left = new int[storageHeight];
                    int[] right = new int[storageHeight];
                    line = file.ReadLine();
                    string[] leftTokens = line[1..^1].Split(" ", StringSplitOptions.RemoveEmptyEntries); ;
                    line = file.ReadLine();
                    string[] rightTokens = line[1..^1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < leftTokens.Length; j++) {
                        left[j] = int.Parse(leftTokens[j]);
                        right[j] = int.Parse(rightTokens[j]);
                    }
                    graph.AddNode(new StorageNode(i, left, right));
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

            // Parse orders.
            tokens = file.ReadLine().Split();
            int ordersCount = int.Parse(tokens[1]);
            orders = new List<List<int>>[ordersCount];
            for (int i = 0; i < ordersCount; i++) {
                orders[i] = new List<List<int>>();
                tokens = file.ReadLine().Split();
                int itemsCount = int.Parse(tokens[4]);
                for (int j = 0; j < itemsCount; j++) {
                    List<int> items = new List<int>();
                    orders[i].Add(items);
                    line = file.ReadLine();
                    line = line.Substring(1, line.Length - 2);
                    tokens = line.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in tokens) {
                        items.Add(int.Parse(item));
                    }                    
                }
            }

            return new WarehouseInstance(graph, orders);
        }
    }
}
