using src_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace src_cs {
class WarehousePathFinder {
        static void Main(string[] args) {
            var wi1 = InstanceGenerator.GenerateInstance(5, 3, 10, 5, 60, 5, false);
            var wi2 = InstanceParser.Parse2("../../../../../data/test_warehouse.txt");
            WarehouseInstanceOld wi = InstanceParser.Parse("../../../../../data/whole_instance.txt");
            // sw.Start();          
            PrioritizedPlanner pp = new PrioritizedPlanner(wi, 1000);
            var tours = pp.FindTours();
            CBS cbs = new CBS(wi, 1000);
            var sol = cbs.FindTours();
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