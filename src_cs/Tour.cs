using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace src_cs {
    public sealed class Tour : IEnumerable<int> {
        public int Length { get; private set; }
        public int Distance {     // Distance is equal to the number of edges
            get {
                return Length - 1;
            }
        }
        public int startTime;
        public int[] pickVertices;
        public int[] pickTimes;
        public int[][] routes;

        public Tour(int startTime, LinkedList<(int from, int to, int pickTime, int[] route)> solution) {
            this.startTime = startTime;
            pickVertices = new int[solution.Count - 1];
            pickTimes = new int[solution.Count - 1];
            routes = new int[solution.Count][];
            var first = solution.First;
            Length = first.Value.route.Length - 1;
            routes[0] = first.Value.route;

            int i = 0;
            var node = first.Next;
            while (node != null) {
                var value = node.Value;
                pickVertices[i] = value.from;
                pickTimes[i] = value.pickTime;
                routes[i + 1] = value.route;
                Length += value.pickTime + value.route.Length - 1;
                i++;
                node = node.Next;
            }
            /*
            i = 0;
            foreach (var vertex in this) {
                Console.WriteLine($"i={i++} v={vertex}");
            }
            Console.ReadKey();
            */
            Length += 1;
        }

        public Tour(int startTime, int length, int[] pickVertices, int[] pickTimes, int[][] routes) {
            this.startTime = startTime;
            this.Length = length;
            this.pickVertices = pickVertices;
            this.pickTimes = pickTimes;
            this.routes = routes;
        }

        public IEnumerator<int> GetEnumerator() {
            return new TourEnum(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return (IEnumerator)GetEnumerator();
        }

        public static IEnumerator<int> GetArrayEnum(Tour[] tours) {
            bool first = true;
            foreach (var tour in tours) {
                if (tour == null) {
                    yield return 0;
                }
                else {
                    var tourEnum = first ? tour : tour.Skip(1);
                    first = false;

                    foreach (var vertex in tourEnum) {
                        yield return vertex;
                    }
                }
            }

        }

        public static int GetSumOfCosts(Tour[][] solution) {
            int cost = 0;
            foreach (var agent in solution) {
                foreach (var tour in agent) {
                    cost += tour.Distance;
                }
            }
            return cost + 1;
        }

        public static int GetMakespan(Tour[][] solution) {
            int maxCost = 0;
            foreach (var agent in solution) {
                int agentCost = 0;
                foreach (var tour in agent) {
                    agentCost += tour.Distance;
                }
                maxCost = Math.Max(maxCost, agentCost);
            }
            return maxCost + 1;
        }

        public static void Serialize(Tour[][] solution, string path = "./../../serializedSol.txt") {
            if (solution == null)
                return;
            int agents = solution.Length;
            StreamWriter sw = new StreamWriter(path);
            StringBuilder sb = new StringBuilder();

            var enums = new IEnumerator<int>[solution.Length];
            var currVertices = new int[solution.Length];

            for (int agent = 0; agent < agents; agent++) {
                enums[agent] = Tour.GetArrayEnum(solution[agent]);
            }

            bool updated = true;
            int i = 0;
            while (updated) {
                updated = false;
                sb.Append($"{i}: ");
                for (int agent = 0; agent < agents; agent++) {
                    if (enums[agent].MoveNext()) {
                        updated = true;
                        currVertices[agent] = enums[agent].Current;
                    }
                    sb.Append($"{currVertices[agent]} ");
                }
                sb.Append("\n");
                i++;
            }

            sw.Write(sb.ToString());
            sw.Close();
        }
    }

    public class TourEnum : IEnumerator<int> {
        public int Current { get; private set; }

        object IEnumerator.Current => Current;

        private readonly Tour _tour;
        private int pickIdx;
        private int routeIdx;
        private bool isPicking;

        public TourEnum(Tour tour) {
            _tour = tour;

            pickIdx = 0;
            routeIdx = 0;
            isPicking = false;
        }

        public void Dispose() { }

        public bool MoveNext() {
            if (!isPicking && routeIdx < _tour.routes[pickIdx].Length) {
                Current = _tour.routes[pickIdx][routeIdx++];
                return true;
            }
            else if (!isPicking && pickIdx < _tour.pickVertices.Length) {
                routeIdx = 1;
                isPicking = true;
                Current = _tour.pickVertices[pickIdx];
                return true;
            }
            else if (isPicking && routeIdx < _tour.pickTimes[pickIdx]) {
                routeIdx++;
                return true;
            }
            else if (isPicking && pickIdx < _tour.routes.Length - 1) {
                pickIdx++;
                if (_tour.routes[pickIdx].Length > 1) {
                    isPicking = false;
                    Current = _tour.routes[pickIdx][1];
                    routeIdx = 2;
                }
                else {
                    routeIdx = 1;
                }
                return true;
            }

            return false;
        }

        public void Reset() {
            pickIdx = 0;
            routeIdx = 0;
            isPicking = false;
        }
    }
}