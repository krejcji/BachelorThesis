using System;
using System.Collections.Generic;

namespace src_cs {
    class WarehousePathFinder {
        static void Main(string[] args) {

            var wi = InstanceParser.Parse2("../../../../../src_py/test_warehouse.txt");
            var cbs = new CBS(wi, 80000);
            cbs.FindTours();

            //TestingUtils.RunTests(CBS, 10);
            //TestingUtils.RunTests(PrioritizedPlanner, 10);

            Console.ReadKey();
        }
    }

    public class Agent {
        public OrderInstance[] orders;
        public int index;

        public Agent(OrderInstance[] orders, int idx) {
            this.index = idx;
            this.orders = orders;
        }
    }

    public class OrderInstance {
        public int startLoc;
        public int targetLoc;
        public int orderId;
        public int[] vertices;
        public int[][] positions; //[left/right, height]
        public int[] classes;
        public int[] pickTimes;


        public OrderInstance(int orderId, List<List<(int, int, int)>> orderItems, int startLoc, int targetLoc, Graph graph) {
            this.orderId = orderId;
            this.startLoc = startLoc;
            this.targetLoc = targetLoc;
            List<int> vertices = new List<int>();
            List<int[]> positions = new List<int[]>();
            List<int> classes = new List<int>();
            List<int> pickTimes = new List<int>();

            for (int i = 0; i < orderItems.Count; i++) {
                for (int j = 0; j < orderItems[i].Count; j++) {
                    var item = orderItems[i][j];
                    vertices.Add(item.Item1);
                    classes.Add(i + 1);
                    positions.Add(new int[] { item.Item2, item.Item3 });
                    pickTimes.Add(graph.GetPickTime(item.Item1, item.Item2, item.Item3));
                }
            }
            this.vertices = vertices.ToArray();
            this.positions = positions.ToArray();
            this.classes = classes.ToArray();
            this.pickTimes = pickTimes.ToArray();
        }
    }
}