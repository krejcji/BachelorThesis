using System;
using System.Diagnostics;
using System.IO;

namespace src_cs {
    class TestingUtils {
        public delegate Tour[][] SearchAlgorithm(WarehouseInstance instance);

        public static void RunTests(SearchAlgorithm algorithm, int iterations) {
            TextWriter writer = Console.Out;
            Stopwatch stopwatch = new Stopwatch();

            var wi = InstanceGenerator.GenerateInstance(5, 3, 10, 4, 200, 4, false);

            int i;
            for (i = 0; i < iterations; i++) {
                stopwatch.Restart();
                stopwatch.Start();
                var tours = algorithm(wi);
                stopwatch.Stop();

                LogResults();
            }

            /*
            for (int i = 0; i < 1000; i++) {
                for (int j = 0; j < sol.Length; j++) {
                    for (int k = 0; k < sol.Length; k++) {
                        if (sol[j][0].tourVertices.Length > i && sol[k][0].tourVertices.Length > i) {
                            if (j != k && sol[j][0].tourVertices[i] == sol[k][0].tourVertices[i]) {
                                throw new Exception();
                            }
                        }
                    }
                }
            }
            */
            Console.ReadKey();


            void LogResults() {
                writer.WriteLine($"Time elapsed in {i}-th iteration: {stopwatch.ElapsedMilliseconds}");

                /*
                 * Console.WriteLine("Agent {0} route has been found in {1}", i, sw.Elapsed);
                Console.WriteLine("   Items {0}\n    classes {1}", instance.agents[i].orders[0].vertices.Length, instance.agents[i].orders[0].classes[^1]);
                Console.WriteLine("   Constraint: {0}", constraints.Count);
                */
            }
        }
    }
}
