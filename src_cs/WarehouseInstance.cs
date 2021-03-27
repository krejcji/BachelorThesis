﻿using System.Collections.Generic;

namespace src_cs {
    public class WarehouseInstance {
        public Graph graph;
        public OrderInstance[][] orders;
        public int AgentCount { get { return orders.Length; } }


        public WarehouseInstance(Graph graph, OrderInstance[][] orders) {
            this.graph = graph;
            this.orders = orders;
            // graph.Initialize(agents);
        }

        public WarehouseInstance(Location[,] grid, Order[][] orders) {            
            // Make graph from grid and storage
            Vertex[,] tmpGrid = new Vertex[grid.GetLength(0), grid.GetLength(1)];

            int ctr = 0;
            for (int i = 0; i < grid.GetLength(0); i++) {
                for (int j = 0; j < grid.GetLength(1); j++) {
                    int[] storageLeft = null;
                    int[] storageRight = null;
                    if (grid[i,j] is StorageRack) {
                        continue;
                    }
                    else if (grid[i, j] is Floor) {
                        if (i > 0) {
                            var tmp = grid[i - 1, j] as StorageRack;
                            if (tmp != null) {
                                storageLeft = tmp.items;
                            }
                        }
                        if (i < grid.GetLength(0)-1) {
                            var tmp = grid[i + 1, j] as StorageRack;
                            if (tmp != null) {
                                storageRight = tmp.items;
                            }
                        }
                        if (storageLeft != null || storageRight != null) {
                            var left = ConvertStorage(storageLeft);
                            var right = ConvertStorage(storageRight);
                            tmpGrid[i, j] = new StorageVertex(ctr++,left, right, new System.Drawing.Point(i,j));
                        }
                        else {
                            tmpGrid[i, j] = new Vertex(ctr++, new System.Drawing.Point(i, j));
                        }
                    }
                    else if (grid[i, j] is StagingArea) {
                        tmpGrid[i, j] = new StagingVertex(ctr++, new System.Drawing.Point(i, j));
                    }
                }
            }
            this.graph = new Graph(tmpGrid);

            // Make OrderInstances
            var itemMaster = new Dictionary<int, List<(int,int,int)>>();
            foreach (var vertex in graph.vertices) {
                var storageV = vertex as StorageVertex;
                if (storageV == null)
                    continue;
                var storageLeft = storageV.itemsLeft;
                var storageRight = storageV.itemsRight;
                int height = storageLeft == null ? storageRight.GetLength(0) : storageLeft.GetLength(0);

                for (int i = 0; i < height; i++) {
                    if (storageLeft != null) {
                        if (!itemMaster.ContainsKey(storageLeft[i, 0])) {
                            itemMaster.Add(storageLeft[i, 0], new List<(int, int, int)> { (storageV.index, 0, i) });
                        }
                        else {
                            itemMaster[storageLeft[i, 0]].Add((storageV.index, 0, i));
                        }
                    }
                    if (storageRight != null) {
                        if (!itemMaster.ContainsKey(storageRight[i, 0])) {
                            itemMaster.Add(storageRight[i, 0], new List<(int, int, int)> { (storageV.index, 1, i)});
                        }
                        else {
                            itemMaster[storageRight[i, 0]].Add((storageV.index, 0, i));
                        }
                    }
                }
            }

            var oInstances = new OrderInstance[orders.Length][];
            for (int i = 0; i < orders.Length; i++) {
                oInstances[i] = new OrderInstance[orders[i].Length];                
                for (int j = 0; j < orders[i].Length; j++) {
                    var orderItems = new List<List<(int, int, int)>>();
                    var order = orders[i][j];
                    for (int k = 0; k < order.items.Length; k++) {
                        orderItems.Add(itemMaster[order.items[k]]);
                    }
                    var from = graph.FindLocation(order.from);
                    var to = graph.FindLocation(order.to);
                    oInstances[i][j] = new OrderInstance(order.orderId, orderItems, from.index, to.index, graph);                    
                }
            }
            this.orders = oInstances;

            // Init the graph data structures
            graph.Initialize(oInstances);           


            // Make Orders from Graph and the array
            int[,] ConvertStorage(int[] storage) {
                if (storage == null)
                    return null;
                var newStorage = new int[storage.Length, 2];
                for (int i = 0; i < storage.Length; i++) {
                    newStorage[i, 0] = storage[i];
                }
                return newStorage;
            }
        }

        public void ModifyItems() {
            // Generate new distribution of items in a given warehouse

        }

        public void ModifyOrders() {
            // Generate new set of orders

        }        
    }

    public struct InstanceDescription {
        public WarehouseLayout layout;
        public StorageDescription storageDescription;
        public OrdersDescription ordersDescription;

        public static InstanceDescription Test0() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(3, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test1() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(5, 3, 0, 6, 0);
            return id;
        }
        public static InstanceDescription Test2() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(7, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test3() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(9, 3, 0, 6, 0);
            return id;
        }
        public static InstanceDescription Test4() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(11, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test5() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(13, 3, 0, 6, 0);
            return id;
        }

        
        public static InstanceDescription Test6() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(7, 3, 0, 7, 0);
            return id;
        }
        public static InstanceDescription Test7() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(7, 3, 0, 8, 0);
            return id;
        }
        public static InstanceDescription Test8() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(7, 3, 0, 9, 0);
            return id;
        }
        public static InstanceDescription Test9() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(7, 3, 0, 10, 0);
            return id;
        }
        public static InstanceDescription Test10() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(7, 3, 0, 11, 0);
            return id;
        }

        public static InstanceDescription Test11() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(13, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription GetTiny() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(5, 3, 5, false);
            id.storageDescription = new StorageDescription(3, true, 30);
            id.ordersDescription = new OrdersDescription(7, 3, 1, 7, 3);
            return id;
        }

        public static InstanceDescription GetSmall() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 500);
            id.ordersDescription = new OrdersDescription(7,3,1,7,3);
            return id;
        }
        public static InstanceDescription GetMedium() {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(30, 5, 10, false);
            id.storageDescription = new StorageDescription(5, true, 2000);
            id.ordersDescription = new OrdersDescription(4, 5, 2, 10, 2);
            return id;
        }
    }

    public struct WarehouseLayout {
        public int aisles;
        public int crossAisles;
        public int aisleRows;
        // int aisleWidth = 1;
        // int crossAisleWidth = 2;
        public bool specialArea;  // 'fridge area'

        public WarehouseLayout(int aisles, int crossAisles, int aisleRows, bool specialArea) {
            this.aisles = aisles;
            this.crossAisles = crossAisles;
            this.aisleRows = aisleRows;
            this.specialArea = specialArea;
        }
    }

    public struct StorageDescription {
        public int storageLevels;
        public bool randomizedPlacement;
        public int uniqueItems;

        public StorageDescription(int storageLevels, bool randomizedPlacement, int uniqueItems) {
            this.storageLevels = storageLevels;
            this.randomizedPlacement = randomizedPlacement;
            this.uniqueItems = uniqueItems;
        }
    }

    public struct OrdersDescription {
        public int agents;
        public int ordersPerAgent;
        public int ordersVariance;
        public int itemsPerOrder;
        public int itemsVariance;

        public OrdersDescription(int agents, int ordersPerAgent, int ordersVariance, 
                                 int itemsPerOrder, int itemsVariance) {
            this.agents = agents;
            this.ordersPerAgent = ordersPerAgent;
            this.ordersVariance = ordersVariance;
            this.itemsPerOrder = itemsPerOrder;
            this.itemsVariance = itemsVariance;
        }
    }
}
