using System.Numerics;
using Physics;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Utils
{
    public static class Geometry
    {
        public static Vector2 ClosestPointOnLine(in Vector2 lineStart, in Vector2 lineEnd, in Vector2 point)
        {
            Vector2 startToPoint = point - lineStart;
            Vector2 startToEnd = lineEnd - lineStart;
            Vector2 startToEndNorm = Vector2.Normalize(startToEnd);
            float distOnLine = Vector2.Dot(startToPoint, startToEndNorm);
            if (distOnLine < 0f)
            {
                return new Vector2(lineStart.X, lineStart.Y);
            }
            if (distOnLine * distOnLine > startToEnd.LengthSquared())
            {
                return new Vector2(lineEnd.X, lineEnd.Y);
            }
            return lineStart + distOnLine * startToEndNorm;
        }

        public static Vector2 ReflectVec(in Vector2 vec, in Vector2 normal)
        {
            return vec - 2f * Vector2.Dot(vec, normal) * normal;
        }
    }

    public static class Entities
    {
        public static List<PointMass> QueryAreaForPoints(float centerX, float centerY, float radius, in Context context)
        {
            Vector2 center = new(centerX, centerY);
            List<PointMass> foundPoints = new();
            foreach (MassShape s in context.MassShapes)
            {
                foreach (PointMass p in s._points)
                {
                    float distSq = Vector2.DistanceSquared(center, p.Pos);
                    if (distSq < radius * radius)
                    {
                        foundPoints.Add(p);
                    }
                }
            }
            return foundPoints;
        }

        public static List<MassShape> QueryAreaForShapes(float centerX, float centerY, float radius, in Context context)
        {
            Vector2 center = new(centerX, centerY);
            List<MassShape> foundShapes = new();
            foreach (MassShape s in context.MassShapes)
            {
                Vector2 com = s.CalculateCenterOfMass();
                float distSq = Vector2.DistanceSquared(center, com);
                if (distSq < radius * radius)
                {
                    foundShapes.Add(s);
                }
                
            }
            return foundShapes;
        }
    }
}

namespace Tools
{
    public abstract class Tool
    {
        public const float BaseRadiusChange = 10f;
        public const float RadiusChangeMult = 5f;

        public float Radius { get; set; }
        protected Context _context;

        abstract public void Update();

        public void Draw()
        {
            DrawCircleLines(GetMouseX(), GetMouseY(), Radius, Color.WHITE);
        }

        public void ChangeRadius(float change)
        {
            if (IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT))
            {
                change *= RadiusChangeMult;
            }
            Radius += change;
            if (Radius < 0f)
            {
                Radius = 0f;
            }
        }
    }

    public class Delete : Tool
    {   
        public override void Update()
        {
            
        }
    }

    public class Pull : Tool
    {
        private const float PullForceCoeff = 1e2f;

        public Pull(Context context)
        {
            _context = context;
        }

        public override void Update()
        {
            Vector2 mousePos = GetMousePosition();
            var shapes = Utils.Entities.QueryAreaForShapes(mousePos.X, mousePos.Y, Radius, _context);
            if (shapes.Any())
            {
                MassShape s = shapes.First();
                Vector2 com = s.CalculateCenterOfMass();
                Vector2 force = PullForceCoeff * (mousePos - com);
                s.ApplyForce(force);
                DrawLine((int) com.X, (int) com.Y, (int) mousePos.X, (int) mousePos.Y, Color.RED);
            }
        }
    }
}
