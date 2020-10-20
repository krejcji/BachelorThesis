using System;
using System.Collections.Generic;
using System.Drawing;

namespace src_cs {
    class InstanceGenerator {
        /// <summary>
        /// Generates an test instance. The instance is parametrized by warehouse size, number of agents
        /// existence of the special area such as freezer. TBD: More advanced parameters such as item distribution.
        /// </summary>
        /// <param name="aisles"></param>
        /// <param name="crossAisles"></param>
        /// <param name="aisleRows"></param>
        /// <param name="storageLevels"></param>
        /// <param name="items"></param>
        /// <param name="agents"></param>
        /// <param name="specialArea"></param>
        /// <returns></returns>
        public static WarehouseInstance GenerateInstance(int aisles, int crossAisles, int aisleRows, int storageLevels,
                                                   int items, int agents, bool specialArea) {
            // Generate the grid
            int width = 3 * aisles - 2;
            int height = 2 * crossAisles + (crossAisles - 1) * aisleRows + 6;
            Location[,] grid = new Location[width, height];
            List<StorageRack> storage = new List<StorageRack>();
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    if (isFloor(i, j)) {
                        grid[i, j] = new Floor();
                    }
                    else if (isStaging(i, j)) {
                        grid[i, j] = new StagingArea();
                    }
                    else {
                        var rack = new StorageRack(storageLevels);
                        grid[i, j] = rack;
                        storage.Add(rack);
                    }
                }
            }

            // Fill the empty shelves randomly, more advanced methods will be implemented
            var rand = new Random();
            var itemsList = GenerateRandomItems(items, storage.Count * storageLevels, rand);
            int itemsIdx = 0;
            foreach (var rack in storage) {
                for (int j = 0; j < storageLevels; j++) {
                    rack.items.Add(itemsList[itemsIdx++]);
                }
            }

            // Generate orders
            var orders = GenerateRandomOrders(agents, 8, 3, items, 3, rand);

            // Make WarehouseInstace2 - graph and full order objects


            return null;

            bool isFloor(int x, int y) {
                return x % 3 == 0 ||
                    y == 0 ||
                    y == height - 1 ||
                    (y - 3) % (aisleRows + 2) == 0 ||
                    (y - 3) % (aisleRows + 2) == 1;
            }

            bool isStaging(int x, int y) {
                return (y == 1 || y == 2 || y == height - 2 || y == height - 3)
                    && (x != 0 && x != width - 1);
            }
        }

        static List<int> GenerateRandomItems(int uniqueItems, int itemsTotal, Random rand) {
            List<int> itemsList = new List<int>(itemsTotal);
            for (int i = 0; i < uniqueItems; i++) {
                itemsList.Add(i);
            }
            for (int i = itemsList.Count; i < itemsTotal; i++) {
                itemsList.Add(rand.Next(uniqueItems));
            }
            Shuffle(itemsList, rand);
            return itemsList;
        }

        static Order[][] GenerateRandomOrders(int agents, int averageItems, int averageOrders, int uniqueItems, int variability,
                                       Random rand) {
            Order[][] orders = new Order[agents][];
            for (int i = 0; i < agents; i++) {
                orders[i] = new Order[rand.Next(averageOrders - 1, averageOrders + 2)];
            }

            for (int i = 0; i < agents; i++) {
                for (int j = 0; j < orders[i].Length; j++) {
                    int orderLength = rand.Next(averageItems - variability, averageItems + variability + 1);
                    int[] items = new int[orderLength];
                    orders[i][j] = new Order(new Point(2*i, 3), new Point(2*i, 2), items);
                    for (int k = 0; k < orderLength; k++) {
                        while (true) {
                            int item = rand.Next(uniqueItems);
                            bool itemUsed = false;
                            for (int l = 0; l < k; l++) {
                                if (orders[i][j].items[l] == item)
                                    itemUsed = true;
                            }
                            if (!itemUsed) {
                                orders[i][j].items[k] = item;
                                break;
                            }
                        }
                    }
                }
            }

            return orders;
        }

        static void Shuffle(List<int> list, Random rng) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }



    public abstract class Location {
    }

    public class Floor : Location {

    }

    public class StagingArea : Location {

    }

    public class StorageRack : Location {
        int levels;
        public List<int> items;

        public StorageRack(int levels) {
            this.levels = levels;
            items = new List<int>(levels);
        }
    }

    public class Order {
        public Point from;
        public Point to;
        public int[] items;

        public Order(Point from, Point to, int[] items) {
            this.from = from;
            this.to = to;
            this.items = items;
        }
    }
}
