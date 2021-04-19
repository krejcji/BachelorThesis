using System;
using System.Collections.Generic;

namespace src_cs {
    public struct NewConstraint {
        int time;
        int from;
        int vertex;
    }
    public abstract class ConstraintManager {
        protected bool isCached;
        protected Dictionary<(int,int), bool[]> pickCache;        


        void CachePickPossibility(int vertex, int pickTime, int maxTime) {
            if (pickCache.ContainsKey((vertex, pickTime)))
                return;

            pickCache.Add((vertex, pickTime), new bool[maxTime]);

            int freeCount = 0;            
            for (int i = 0; i < maxTime-pickTime; i++) {
                if (IsConstrained(vertex, i))
                    freeCount = 0;
                else
                    freeCount++;

                if (i < pickTime)
                    continue;
                
                if (freeCount > pickTime) {
                    pickCache[(vertex,pickTime)][i - pickTime] = false;
                } else {
                    pickCache[(vertex, pickTime)][i - pickTime] = true;
                }                
            }
        }

        public void CachePickConstraints(int[] pickVertices, int[] pickTimes, int maxTime, int maxVertices) {
            pickCache = new Dictionary<(int, int), bool[]>();

            for (int i = 0; i < pickVertices.Length; i++) {                
                CachePickPossibility(pickVertices[i], pickTimes[i], maxTime);
            }
            isCached = false;
        }
        public abstract bool IsConstrained(int vertex, int time, int predecessor = -1);
        public abstract bool IsConstrainedPick(int vertex, int time, int pickDuration);

        public abstract bool IsConstrainedRoute(int pickVertex, int target, int pickDuration);

        public abstract void AddConstraint(int vertex, int time, int predecessor);
        public abstract void AddConstraint(Constraint constraint);
        public abstract void AddConstraints(Tour tour);
        public abstract void AddConstraintPick(int vertex, int time, int pickDuration);

        public abstract void InitConstraints(List<Constraint> constraints);
        public abstract void Clear();

        public virtual int Count { get; set; }
    }

    public class ConstraintManagerSparse : ConstraintManager {
        SortedList<int, List<(int vertex,int predecessor)>> constraints;

        public ConstraintManagerSparse() {
            constraints = new SortedList<int, List<(int,int)>>();
        }

        public override void AddConstraint(int vertex, int time, int predecessor) {
            if (constraints.ContainsKey(time))
                constraints[time].Add((vertex,predecessor));

            else
                constraints.Add(time, new List<(int,int)>() { (vertex,predecessor) });
            isCached = false;
        }

        public override void AddConstraint(Constraint constraint) {
            AddConstraint(constraint.vertex, constraint.time, -1);
        }

        public override void AddConstraintPick(int vertex, int time, int pickDuration) {
            for (int i = 0; i < pickDuration; i++) {
                int currTime = time + i;
                if (constraints.ContainsKey(currTime))
                    constraints[currTime].Add((vertex,-1));
                
                else 
                    constraints.Add(currTime, new List<(int,int)>() { (vertex,-1) });                
            }            
        }

        public override void AddConstraints(Tour tour) {
            throw new NotImplementedException();
        }

        public override void Clear() {
            constraints.Clear();
        }

        public override void InitConstraints(List<Constraint> newConstraints) {
            constraints.Clear();

            foreach (var constraint in newConstraints) {
                AddConstraint(constraint);
            }
        }      

        public override bool IsConstrained(int vertex, int time, int predecessor = -1) {            
            if (constraints.ContainsKey(time)) {
                if (constraints[time].Exists((a) => a.vertex == vertex))
                    return true;
                else if (predecessor != -1)
                    return constraints[time].Contains((predecessor, vertex));                
            }
            return false;
        }

        public override bool IsConstrainedPick(int vertex, int time, int pickDuration) {
            if (isCached)
                return pickCache[(vertex,pickDuration)][time];

            int minTime = time + 1;
            int maxTime = time + pickDuration + 1;

            foreach (var timeKey in constraints.Keys) {
                if (timeKey >= minTime && timeKey < maxTime) {
                    if (IsConstrained(vertex, timeKey))
                        return true;
                }
            }
            return false;
        }

        public override bool IsConstrainedRoute(int pickVertex, int target, int pickDuration) {
            throw new NotImplementedException();
        }

        public override int Count {
            get { return constraints.Count; }
        }
    }

    // TODO: Pick cache - IsConstrainedPick is too inefficient
    public class ConstraintManagerDense : ConstraintManager {
        Dictionary<(int time, int vertex), int> constraints;

        public ConstraintManagerDense() {
            constraints = new Dictionary<(int time, int vertex), int>();
        }

        public override void AddConstraint(int vertex, int time, int predecessor) {
            constraints.Add((time, vertex), predecessor);
            isCached = false;
        }

        public override void AddConstraintPick(int vertex, int time, int pickDuration) {
            for (int i = 0; i < pickDuration; i++) {
                constraints.Add((time+i, vertex), -1);
            }
            isCached = false;
        }
        public override void AddConstraints(Tour solution) {
            int time = solution.startTime;
            int predecessor = -1;
            foreach (var vertex in solution) {
                if (predecessor == -1) {
                    if (constraints.ContainsKey((time, vertex))) {
                        time++;
                        continue;
                    }
                }
                constraints.Add((time++, vertex), predecessor);
                predecessor = vertex;
            }
            isCached = false;
        }

        public override bool IsConstrainedPick(int vertex, int time, int pickDuration) {
            if (isCached)
                return pickCache[(vertex,pickDuration)][time];

            for (int i = 1; i <= pickDuration; i++) {
                if (constraints.ContainsKey((time+i, vertex)))
                    return true;            
            }
            return false;
        }        

        public override bool IsConstrained(int vertex, int time, int predecessor = -1) {
            // Vertex conflict
            if (constraints.ContainsKey((time,vertex))) {
                return true;
            }
            // Swapping conflict
            else if (predecessor != -1) {
                if (constraints.ContainsKey((time, predecessor))) {
                    if (constraints[(time, predecessor)] == vertex)
                        return true;
                }
            }
            return false;
        }

        public override void InitConstraints(List<Constraint> newConstraints) {            
            foreach (var constraint in newConstraints) {
                AddConstraint(constraint);
            }
        }

        public override void AddConstraint(Constraint constraint) {
            if (!constraints.ContainsKey((constraint.time, constraint.vertex))) {
                constraints.Add((constraint.time, constraint.vertex), -1);
            }
        }

        public override void Clear() {
            constraints.Clear();
        }

        public override bool IsConstrainedRoute(int pickVertex, int target, int pickDuration) {
            throw new NotImplementedException();
        }

        public override int Count {
            get { return constraints.Count; }
        }

        /*
         bool isPickPossible(int pickVertex, int pickDuration) {
                int maxTime = constraintTime + pickDuration;
                while (constraintTime < maxTime) {
                    if (!constraints.ContainsKey(constraintTime)) {
                        constraintTime++;
                        continue;
                    }
                    else if (constraints[constraintTime].Contains(pickVertex))
                        return false;
                    constraintTime++;
                }            
                return true;
            }
        */
    }

    public class ConstraintManagerDummy : ConstraintManager {
        public override void AddConstraint(int vertex, int time, int predecessor) {
            throw new NotImplementedException();
        }

        public override void AddConstraint(Constraint constraint) {
            throw new NotImplementedException();
        }

        public override void AddConstraintPick(int vertex, int time, int pickDuration) {
            throw new NotImplementedException();
        }

        public override void AddConstraints(Tour tour) {
            throw new NotImplementedException();
        }

        public override void Clear() {
            throw new NotImplementedException();
        }

        public override void InitConstraints(List<Constraint> constraints) {
            throw new NotImplementedException();
        }

        public override bool IsConstrained(int vertex, int time, int predecessor = -1) {
            return false;
        }

        public override bool IsConstrainedPick(int vertex, int time, int pickDuration) {
            return false;
        }

        public override bool IsConstrainedRoute(int pickVertex, int target, int pickDuration) {
            throw new NotImplementedException();
        }
    }

}
