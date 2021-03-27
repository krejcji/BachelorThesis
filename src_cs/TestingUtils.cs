using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace src_cs {
    class TestingUtils {
        public delegate Tour[][] SearchAlgorithm(WarehouseInstance instance);

        public static void RunTests(List<TestScenario> tests, int iterations) {
            StringBuilder sb = new StringBuilder();
            StringBuilder header = new StringBuilder();
            TextWriter writer = Console.Out;
            Stopwatch stopwatch = new Stopwatch();
            List<TestResult> results = new List<TestResult>();
            header.Append("TestScenarioID,Iteration,ElapsedTime,SoC,Makespan");
            string statsHeader = null;

            for (int testIdx = 0; testIdx < tests.Count; testIdx++) {
                var test = tests[testIdx];
                TestResult result = new TestResult();
                result.AddScenario(test);

                for (int i = 0; i < iterations; i++) {
                    WarehouseInstance instance = InstanceGenerator.GenerateInstance(test.description, 42 + i);
                    // TODO: Generate new items and orders without the need for whole new instance
                    Solver solver = SolverFactory.GetSolver(test.solver, instance);
                    stopwatch.Restart();

                    stopwatch.Start();
                    var tours = solver.FindTours();
                    stopwatch.Stop();

                    int makespan = Tour.GetMakespan(tours);
                    int sumOfCosts = Tour.GetSumOfCosts(tours);
                    result.AddMeasurement((stopwatch.ElapsedMilliseconds, sumOfCosts, makespan, tours));                    
                    var stats = solver.GetStats();
                    statsHeader = stats[0];
                    sb.Append($"{testIdx},{i},{stopwatch.ElapsedMilliseconds},{sumOfCosts},{makespan}\n");
                    //sb.Append($",{stats[1]}\n");
                }

                result.Evaluate();
                results.Add(result);
            }

            DateTime date = DateTime.Now;
            StreamWriter sw = new StreamWriter($"./test_output_{date.ToString("yyMMdd_HH_mm")}.csv");
            //header.Append($",{statsHeader}");
            sw.Write(header + "\n");
            sw.Write(sb.ToString());
            sw.Close();

            Console.WriteLine();

            Console.ReadKey();

            /*
            void LogResults() {
                writer.WriteLine($"Time elapsed in {i}-th iteration: {stopwatch.ElapsedMilliseconds}");

                
                 * Console.WriteLine("Agent {0} route has been found in {1}", i, sw.Elapsed);
                Console.WriteLine("   Items {0}\n    classes {1}", instance.agents[i].orders[0].vertices.Length, instance.agents[i].orders[0].classes[^1]);
                Console.WriteLine("   Constraint: {0}", constraints.Count);
            */
        }
    }

    public struct TestScenario {
        public SolverType solver;
        public InstanceDescription description;

        public TestScenario(SolverType solver, InstanceDescription description) {
            this.solver = solver;
            this.description = description;
        }
    }

    public class TestResult {
        public TestScenario scenario;        
        public long avgTime;
        public int avgSOC;
        public int avgMakespan;
        public List<(long time, int SumOfCosts, int Makespan, Tour[][] sol)> results;

        public TestResult() {
            this.results = new List<(long, int, int, Tour[][])>();
        }

        public void AddMeasurement((long, int, int, Tour[][]) measurement) {
            results.Add(measurement);
        }

        public void AddScenario(TestScenario scenario) {
            this.scenario = scenario;
        }       

        public void Evaluate() {
            long timeSum = 0;
            int SOCSum = 0;
            int makespanSum = 0;
            foreach (var record in results) {
                timeSum += record.time;
                SOCSum += record.SumOfCosts;
                makespanSum += record.Makespan;
            }
            avgTime = timeSum / results.Count;
            avgSOC = SOCSum / results.Count;
            avgMakespan = makespanSum / results.Count;
        }
    }

    public class SolverFactory {
        public static Solver GetSolver(SolverType type, WarehouseInstance instance) {
            return type switch
            {
                SolverType.CBS => new CBS(instance),
                SolverType.PrioritizedPlanner => new PrioritizedPlanner(instance),
                SolverType.PrioritizedPlannerClassesL => new PrioritizedPlanner(instance, PrioritizedPlanner.Heuristic.ClassesLow),
                SolverType.PrioritizedPlannerClassesH => new PrioritizedPlanner(instance, PrioritizedPlanner.Heuristic.ClassesHigh),
                SolverType.PrioritizedPlannerGrad => new GradualPrioritizedPlanner(instance),
                SolverType.PrioritizedPlannerGradClassesL => new GradualPrioritizedPlanner(instance, PrioritizedPlanner.Heuristic.ClassesLow),
                SolverType.PrioritizedPlannerGradClassesH => new GradualPrioritizedPlanner(instance, PrioritizedPlanner.Heuristic.ClassesHigh),
                SolverType.SingleAgent => new SingleAgentPlanner(instance),
                SolverType.SimpleNoBlock => new SimpleNoBlockPlanner(instance),
                _ => throw new NotImplementedException("Solver not implemented."),
            };
        }
    }

    public enum SolverType {
        CBS,
        PrioritizedPlanner,
        PrioritizedPlannerClassesL,
        PrioritizedPlannerClassesH,
        PrioritizedPlannerGrad,
        PrioritizedPlannerGradClassesL,
        PrioritizedPlannerGradClassesH,
        SingleAgent,
        SimpleNoBlock
    }
}
