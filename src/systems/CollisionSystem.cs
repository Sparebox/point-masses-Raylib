using System.Numerics;
using Collision;
using Entities;
using Sim;
using SimSystems;
using Utils;
using static Raylib_cs.Raylib;

namespace Systems
{
    public class CollisionSystem : ISystem
    {
        private readonly Context _ctx;

        public CollisionSystem(Context ctx) => _ctx = ctx;

        public void Update()
        {
            if (_ctx._collisionsEnabled)
            {
                HandleLineColliders();
                HandleCollisions();
            }
        }

        public void Draw() {}

        private void HandleCollisions()
        {
            foreach (var shapeA in _ctx.MassShapes)
            {
                var nearShapes = _ctx.GetMassShapes(shapeA.Aabb);
                foreach (var shapeB in nearShapes)
                {
                    if (shapeA.Equals(shapeB) || !CheckCollisionBoxes(shapeA.Aabb, shapeB.Aabb))
                    {
                        continue;
                    }
                    HandlePointOnPointCollisions(shapeA, shapeB, _ctx);
                    HandleLineCollisions(shapeA, shapeB, _ctx);
                }
            }
        }

        private static bool CheckLineCollision(MassShape shape, PointMass otherPoint)
        {
            for (int i = 0; i < shape._points.Count; i++)
            {
                Vector2 startPos =shape. _points[i].Pos;
                Vector2 endPos = shape._points[(i + 1) % shape._points.Count].Pos;
                Vector2 towardsClosestPoint = Geometry.ClosestPointOnLine(startPos, endPos, otherPoint.Pos) - otherPoint.Pos;
                float distSq = towardsClosestPoint.LengthSquared();
                if (distSq <= otherPoint.Radius * otherPoint.Radius)
                {
                    return true;
                }
            }
            return false;
        }

        public void HandleLineColliders()
        {
            // TODO: Optimize with quad tree
            foreach (LineCollider c in _ctx.LineColliders)
            {
                foreach (MassShape shape in _ctx.MassShapes)
                {
                    foreach (PointMass point in shape._points)
                    {
                        CollisionData? collisionResult = c.CheckCollision(point);
                        if (collisionResult.HasValue)
                        {
                            LineCollider.SolvePointCollision(collisionResult.Value, _ctx);
                        }
                    }
                }
            }
        }

        private static void HandleLineCollisions(MassShape shapeA, MassShape shapeB, Context ctx)
        {
            if (shapeA._points.Count == 1 && shapeB._points.Count == 1)
            {
                return;
            }
            var thisAABB = shapeA.Aabb;
            foreach (var point in shapeB._points)
            {
                if (!CheckCollisionBoxes(point.Aabb, thisAABB))
                {
                    continue;
                }
                if (CheckLineCollision(shapeA, point))
                {
                    HandleLineCollision(shapeA, point, ctx);
                }
            }
        }

        private static void HandlePointOnPointCollisions(MassShape shapeA, MassShape shapeB, Context ctx)
        {
            foreach (var pointA in shapeA._points)
            {
                foreach (var pointB in shapeB._points)
                {
                    var collisionResult = CheckPointToPointCollision(pointA, pointB);
                    if (collisionResult.HasValue)
                    {
                        HandlePointToPointCollision(collisionResult.Value, ctx);
                    }
                }
            }
        }

        private static CollisionData? CheckPointToPointCollision(PointMass pointA, PointMass pointB)
        {
            Vector2 normal = pointB.Pos - pointA.Pos;
            float dist = normal.LengthSquared();
            if (dist <= MathF.Pow(pointA.Radius + pointB.Radius, 2f))
            {
                dist = MathF.Sqrt(dist);
                if (dist == 0f)
                {
                    return null;
                }
                return new CollisionData()
                {
                    PointMassA = pointA,
                    PointMassB = pointB,
                    Normal = normal / dist,
                    Separation = pointA.Radius + pointB.Radius - dist,
                };
            }
            return null;
        }

        private static void HandlePointToPointCollision(in CollisionData colData, Context ctx)
        {   
            // Save pre-collision velocities
            Vector2 preVelA = colData.PointMassA.Vel;
            Vector2 preVelB = colData.PointMassB.Vel;
            Vector2 relVel = preVelB - preVelA;
            // Correct penetration
            Vector2 offsetVector = 0.5f * colData.Separation * colData.Normal;
            colData.PointMassA.Pos += -offsetVector;
            colData.PointMassB.Pos += offsetVector;
            // Apply impulse
            float impulseMag = -(1f + ctx._globalRestitutionCoeff) * Vector2.Dot(relVel, colData.Normal) / (colData.PointMassA.InvMass + colData.PointMassB.InvMass);
            Vector2 impulse = impulseMag * colData.Normal;
            colData.PointMassA.Vel = preVelA - impulse * colData.PointMassA.InvMass;
            colData.PointMassB.Vel = preVelB + impulse * colData.PointMassB.InvMass;
            // Apply friction
            colData.PointMassA.ApplyFriction(-colData.Normal);
            colData.PointMassB.ApplyFriction(colData.Normal);
        }

        private static void HandleLineCollision(MassShape shape, PointMass pointMass, Context ctx)
        {
            (PointMass closestA, PointMass closestB, Vector2 closestPointOnLine) = FindClosestPoints(shape, pointMass.Pos);
            Vector2 pointToClosest = closestPointOnLine - pointMass.Pos;
            float totalOffset = pointMass.Radius - pointToClosest.Length();
            if (totalOffset == 0f)
            {
                return;
            }
            float lineLen = Vector2.Distance(closestA.Pos, closestB.Pos);
            if (lineLen == 0f)
            {
                return;
            }
            float distToB = Vector2.Distance(closestPointOnLine, closestB.Pos);
            float aOffset = distToB / lineLen * totalOffset;
            float bOffset = totalOffset - aOffset;
            var normal = Vector2.Normalize(pointToClosest);
            Vector2 avgVel = (closestA.Vel + closestB.Vel) / 2f;
            Vector2 preVel = pointMass.Vel;
            Vector2 closestApreVel = closestA.Vel;
            Vector2 closestBpreVel = closestB.Vel;
            Vector2 relVel = preVel - avgVel;
            // Penetration correction
            pointMass.Pos += totalOffset * -normal;
            closestA.Pos += aOffset * normal;
            closestB.Pos += bOffset * normal;
            // Apply impulse
            float combinedMass = closestA.Mass + closestB.Mass;
            float impulseMag = -(1f + ctx._globalRestitutionCoeff) * Vector2.Dot(relVel, normal) / (1f / combinedMass + pointMass.InvMass);
            Vector2 impulse = impulseMag * normal;
            pointMass.Vel = preVel + impulse * pointMass.InvMass;
            closestA.Vel = closestApreVel - impulse * 0.5f / (combinedMass - closestB.Mass);
            closestB.Vel = closestBpreVel - impulse * 0.5f / (combinedMass - closestA.Mass);
            // Apply friction
            pointMass.ApplyFriction(-normal);
            closestA.ApplyFriction(normal);
            closestB.ApplyFriction(normal);
        }

        private static (PointMass closestA, PointMass closestB, Vector2 closestPoint) 
        FindClosestPoints(MassShape shape, Vector2 pos)
        {
            float closestDistSq = float.MaxValue;
            PointMass closestA = null;
            PointMass closestB = null;
            Vector2 closestPoint = new();
            for (int i = 0; i < shape._points.Count; i++)
            {
                PointMass lineStart = shape._points[i];
                PointMass lineEnd = shape._points[(i + 1) % shape._points.Count];
                Vector2 pointOnLine = Geometry.ClosestPointOnLine(lineStart.Pos, lineEnd.Pos, pos);
                float distSq = Vector2.DistanceSquared(pointOnLine, pos);
                if (distSq < closestDistSq)
                {
                    closestA = lineStart;
                    closestB = lineEnd;
                    closestDistSq = distSq;
                    closestPoint = pointOnLine;
                }
            }
            return (closestA, closestB, closestPoint);
        }
    }
}