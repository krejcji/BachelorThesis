using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace src_cs {

    public class WarehouseInstanceOld {
        public Graph graph;
        public Agent[] agents;

        public WarehouseInstanceOld(Graph graph, Agent[] agents) {
            this.graph = graph;
            this.agents = agents;
            //graph.Initialize(agents);
        }
    }
    class InstanceParser {
        public static WarehouseInstanceOld Parse(string instancePath) {
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
                    graph.AddVertex(new Vertex(i));
                }
                else if (tokens[2] == "Depot") {
                    graph.AddVertex(new Vertex(i));
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
                    // TODO: Point?
                    graph.AddVertex(new StorageVertex(i, left, right, new Point()));
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
                var ordersList = new List<OrderInstance>();

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
                    ordersList.Add(new OrderInstance(orderId++, orderItems, source, target, graph));
                }
                agents.Add(new Agent(ordersList.ToArray(), i));
            }
            return new WarehouseInstanceOld(graph, agents.ToArray());
        }

        public static WarehouseInstance Parse2(string instancePath) {
            StreamReader file = new StreamReader(instancePath);
            string line;
            string[] tokens;

            line = file.ReadLine();
            if (line.Substring(0, 10) != "Dimension:")
                throw new FormatException("File format not by the specification.");
            tokens = line.Split(':')[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
            int x, y, z = 0;
            x = int.Parse(tokens[0]);
            y = int.Parse(tokens[1]);
            z = int.Parse(tokens[2]) + 1;

            if ((line = file.ReadLine()) != "LOCATIONmaster")
                throw new FormatException("File format not by the specification.");

            var grid = new Location[x, y];
            var locationDict = new Dictionary<string, Tuple<int, int, int>>();
            file.ReadLine();

            while ((line = file.ReadLine()) != "") {
                tokens = line.Split(',');
                var x_coord = int.Parse(tokens[0]) - 1;
                var y_coord = int.Parse(tokens[1]) - 1;
                var z_coord = int.Parse(tokens[2]);
                locationDict.Add(tokens[3], new Tuple<int, int, int>(x_coord, y_coord, z_coord));

                if (z_coord != 0)
                    continue;

                grid[x_coord, y_coord] = Parse_location(tokens, z);
            }

            if ((line = file.ReadLine()) != "ITEMmaster")
                throw new FormatException("File format not by the specification.");
            file.ReadLine();
            int item_index = 0;
            var items = new Dictionary<string, int>();

            while ((line = file.ReadLine()) != "") {
                tokens = line.Split(',');
                items.Add(tokens[1], item_index++);
            }

            if ((line = file.ReadLine()) != "Inventory balance")
                throw new FormatException("File format not by the specification.");
            file.ReadLine();
            string date = null;

            while ((line = file.ReadLine()) != "") {
                if (line[0] == '*') {
                    date = line;
                    continue;
                }
                else {
                    tokens = line.Split(',');
                    var record = locationDict[tokens[0]];
                    var itemId = items[tokens[1]];
                    var loc = grid[record.Item1, record.Item2];
                    var storage = loc as StorageRack;
                    if (storage != null) {
                        storage.items[record.Item3] = itemId;
                    }
                    else {
                        throw new Exception("Items outside of storage rack.");
                    }
                }
            }

            if ((line = file.ReadLine()) != "Orders")
                throw new FormatException("File format not by the specification.");
            file.ReadLine();
            var ordersDict = new Dictionary<string, Dictionary<int, List<Tuple<string, string, int>>>>();

            while ((line = file.ReadLine()) != null) {
                tokens = line.Split(',');
                int orderId = int.Parse(tokens[0]);
                var dir = tokens[2];
                var itemId = tokens[3];
                var qty = int.Parse(tokens[4]);
                var picker = tokens[5];
                var order = new Tuple<string, string, int>(itemId, dir, qty);

                if (!ordersDict.ContainsKey(picker))
                    ordersDict.Add(picker, new Dictionary<int, List<Tuple<string, string, int>>>());

                if (!ordersDict[picker].ContainsKey(orderId))
                    ordersDict[picker].Add(orderId, new List<Tuple<string, string, int>>());

                ordersDict[picker][orderId].Add(order);
            }
            file.Close();


            int pickers = ordersDict.Keys.Count;
            var orders = new Order[pickers][];
            int i = 0;
            foreach (var picker in ordersDict.Keys) {
                orders[i] = new Order[ordersDict[picker].Keys.Count];
                int j = 0;
                foreach (var orderId in ordersDict[picker].Keys) {
                    var orderLines = ordersDict[picker][orderId];
                    int[] itemsArr = new int[orderLines.Count];
                    for (int k = 0; k < orderLines.Count; k++) {
                        itemsArr[k] = items[orderLines[k].Item1];
                    }
                    Point from;
                    Point to;
                    if (orderLines[0].Item2 == "outbound") {
                        // TODO: Random starting location in outbound area?
                        from = new Point(3 + i, 3);
                        to = new Point(3 + i, 3);
                    }
                    else {
                        from = new Point(3 + i, 3);
                        to = new Point(3 + i, 3);
                    }
                    orders[i][j] = new Order(from, to, itemsArr);
                    j++;
                }
                i++;
            }
            return new WarehouseInstance(grid, orders);

            Location Parse_location(string[] tokens, int levels) {
                switch (tokens[4]) {
                    case "Floor":
                        return new Floor();
                    case "Staging area":
                        return new StagingArea();
                    case "Storage Rack":
                        return new StorageRack(levels);
                    default:
                        return null;
                }
            }
        }
    }
}
