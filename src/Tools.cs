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
        public static List<PointMass> QueryAreaForPoints(float centerX, float centerY, float radius, Context context)
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

        public static List<MassShape> QueryAreaForShapes(float centerX, float centerY, float radius, Context context)
        {
            Vector2 center = new(centerX, centerY);
            List<MassShape> foundShapes = new();
            foreach (MassShape s in context.MassShapes)
            {
                BoundingBox boundingBox = s.GetAABB();
                Rectangle AABB = new() {
                    X = boundingBox.Min.X,
                    Y = boundingBox.Min.Y,
                    Width = boundingBox.Max.X - boundingBox.Min.X,
                    Height = boundingBox.Max.Y - boundingBox.Min.Y
                };
                if (CheckCollisionCircleRec(center, radius, AABB))
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
    public enum ToolType
    {
        Pull,
        Delete,
    }

    public abstract class Tool
    {
        public const float BaseRadiusChange = 10f;
        public const float RadiusChangeMult = 5f;

        public static int ToolIndex { get; set; }
        public static float Radius { get; set; }

        public string Type { get { return GetType().ToString().Split(".")[1]; } }

        protected Context _context;

        abstract public void Update();

        public static void Draw()
        {
            DrawCircleLines(GetMouseX(), GetMouseY(), Radius, Color.YELLOW);
        }

        public static void ChangeRadius(float change)
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

        public static void ChangeToolType(Context context)
        {
            ToolType[] toolTypes = (ToolType[]) Enum.GetValues(typeof(ToolType));
            int newToolIndex = (context.SelectedTool.GetToolIndex() + 1) % toolTypes.Length;
            ToolType newTool = toolTypes[newToolIndex];
            switch (newTool)
            {
                case ToolType.Pull :
                    context.SelectedTool = new Pull(context);
                    break;
                case ToolType.Delete :
                    context.SelectedTool = new Delete(context);
                    break;
            }
        }

        public int GetToolIndex()
        {
            ToolType[] toolTypes = (ToolType[]) Enum.GetValues(typeof(ToolType));
            for (int i = 0; i < toolTypes.Length; i++)
            {
                if (Type == toolTypes[i].ToString())
                {
                    return i;
                }
            }
            return 0;
        }
    }

    public class Delete : Tool
    {   
        public Delete(Context context)
        {
            _context = context;
        }

        public override void Update()
        {
            Vector2 mousePos = GetMousePosition();
            var shapes = Utils.Entities.QueryAreaForShapes(mousePos.X, mousePos.Y, Radius, _context);
            if (shapes.Any())
            {
                var shape = shapes.First();
                shape._constraints.RemoveAll(c => {
                    return Vector2.DistanceSquared(mousePos, c.PointA.Pos) < Radius * Radius ||
                    Vector2.DistanceSquared(mousePos, c.PointB.Pos) < Radius * Radius;
                });
                shape._points.RemoveAll(p => Vector2.DistanceSquared(mousePos, p.Pos) < Radius * Radius);
            }
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
                Vector2 com = s.CenterOfMass;
                Vector2 force = PullForceCoeff * (mousePos - com);
                s.ApplyForce(force);
                DrawLine((int) com.X, (int) com.Y, (int) mousePos.X, (int) mousePos.Y, Color.RED);
            }
        }
    }
}
