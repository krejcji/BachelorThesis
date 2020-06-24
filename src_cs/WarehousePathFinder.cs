using src_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace src_cs {
    class WarehousePathFinder {
        static void Main(string[] args) {
            WarehouseInstance wi = InstanceParser.Parse("../../../../../data/whole_instance.txt");
            Stopwatch sw = new Stopwatch();
            // sw.Start();
            CBS cbs = new CBS(wi, 1000);
            var sol = cbs.FindRoutes();
            for (int i = 0; i < 1000; i++) {
                for (int j = 0; j < sol.Length; j++) {
                    for (int k = 0; k < sol.Length; k++) {
                        if (sol[j][0].tourVertices.Length > i && sol[k][0].tourVertices.Length > i) {
                            if (j!=k && sol[j][0].tourVertices[i] == sol[k][0].tourVertices[i]) {
                                throw new Exception();
                            }
                        }
                    }
                }
            }
            Console.ReadKey();
            Console.WriteLine();
            // sw.Stop();
            // Console.WriteLine("Elapsed={0}", sw.ElapsedMilliseconds);
        }
    }
}

public class Agent {
    public Order[] orders;
    public int index;

    public Agent(Order[] orders, int idx) {
        this.index = idx;
        this.orders = orders;
    }
}

public class Order {
    public int orderId;
    public int[] vertices;
    public int[][] positions; //[left/right, height]
    public int[] classes;
    public int[] pickTimes;


    public Order(int orderId, List<List<(int, int, int)>> orderItems, int source, int target, Graph graph) {
        this.orderId = orderId;
        List<int> vertices = new List<int>();
        List<int[]> positions = new List<int[]>();
        List<int> classes = new List<int>();
        List<int> pickTimes = new List<int>();
        vertices.Add(source);
        positions.Add(new int[] { 0, 0 });
        pickTimes.Add(0);
        classes.Add(0);
        vertices.Add(target);
        positions.Add(new int[] { 0, 0 });
        pickTimes.Add(0);
        classes.Add(0);

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