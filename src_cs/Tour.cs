using System;
using System.Collections;
using System.Collections.Generic;

public sealed class Tour : IEnumerable<int> {
    public int Length { get; private set; }
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

    public IEnumerator<int> GetEnumerator() {
        return new TourEnum(this);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return (IEnumerator)GetEnumerator();
    }

    public static IEnumerator<int> GetArrayEnum(Tour[] tours) {
        if (tours[0] == null)
            yield return 0;
        else {
            foreach (var tour in tours) {
                foreach (var vertex in tour) {
                    yield return vertex;
                }
            }
        }
    }

    public static int GetSumOfCosts(Tour[][] solution) {
        int cost = 0;
        foreach (var agent in solution) {
            foreach (var tour in agent) {
                cost += tour.Length;
            }
        }
        return cost;
    }

    public static int GetMakespan(Tour[][] solution) {
        int maxCost = 0;
        foreach (var agent in solution) {
            int agentCost = 0;
            foreach (var tour in agent) {
                agentCost += tour.Length;
            }
            maxCost = Math.Max(maxCost, agentCost);
        }
        return maxCost;
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
