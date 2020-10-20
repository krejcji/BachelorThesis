using System.Collections.Generic;
using System.Threading;

namespace src_cs {
    public class WarehouseInstance {
        public Graph graph;
        public OrderInstance[][] orders;

        public WarehouseInstance(Graph graph, OrderInstance[][] orders) {
            this.graph = graph;
            this.orders = orders;
            // graph.Initialize(agents);
        }

        public WarehouseInstance(Location[,] grid, List<StorageRack> storage, Order[][] orders) {
            // Make graph from grid and storage
            Vertex[,] tmpGrid = new Vertex[grid.GetLength(0), grid.GetLength(1)];

            int ctr = 0;
            for (int i = 0; i < grid.GetLength(0); i++) {
                for (int j = 0; j < grid.GetLength(1); j++) {
                    List<int> storageLeft = null;
                    List<int> storageRight = null;
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
                        if (i < grid.GetLength(0)) {
                            var tmp = grid[i + 1, j] as StorageRack;
                            if (tmp != null) {
                                storageRight = tmp.items;
                            }
                        }
                        if (storageLeft != null || storageRight != null) {
                            var left = ConvertStorage(storageLeft);
                            var right = ConvertStorage(storageRight);
                            tmpGrid[i, j] = new StorageVertex(ctr++,left, right);
                        }
                        else {
                            tmpGrid[i, j] = new Vertex(ctr++);
                        }
                    }
                    else if (grid[i, j] is StagingArea) {
                        tmpGrid[i, j] = new Vertex(ctr++);
                    }
                }

            }

            // Make Orders from Graph and the array


            int[,] ConvertStorage(List<int> storage) {
                if (storage == null)
                    return null;
                var newStorage = new int[storage.Count, 2];
                for (int i = 0; i < storage.Count; i++) {
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
}
