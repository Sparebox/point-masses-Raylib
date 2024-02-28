using System.Numerics;
using System.Text;
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

    public static class Graphic
    {
        public const float ArrowBranchLength = 30f;
        public const float ArrowAngle = 20f;

        public static void DrawArrow(float x0, float y0, float x1, float y1, Color color)
        {
            Vector2 start = new(x0, y0);
            Vector2 end = new(x1, y1);
            Vector2 dir = start - end;
            if (dir.LengthSquared() == 0f)
            {
                return;
            }
            dir = Vector2.Normalize(dir);
            float dirAngle = (float) Math.Atan2(dir.Y, dir.X);
            float radians = DEG2RAD * ArrowAngle;
            float angle1 = dirAngle + radians;
            float angle2 = dirAngle - radians;
            Vector2 branchA = new(ArrowBranchLength * (float) Math.Cos(angle1), ArrowBranchLength * (float) Math.Sin(angle1));
            Vector2 branchB = new(ArrowBranchLength * (float) Math.Cos(angle2), ArrowBranchLength * (float) Math.Sin(angle2));
            DrawLine((int) start.X, (int) start.Y, (int) end.X, (int) end.Y, color);
            DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchA.X), (int) (end.Y + branchA.Y), color);
            DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchB.X), (int) (end.Y + branchB.Y), color);
        }

        public static void DrawArrow(in Vector2 start, in Vector2 end, Color color)
        {
            Vector2 dir = start - end;
            if (dir.LengthSquared() == 0f)
            {
                return;
            }
            dir = Vector2.Normalize(dir);
            float dirAngle = (float) Math.Atan2(dir.Y, dir.X);
            float radians = DEG2RAD * ArrowAngle;
            float angle1 = dirAngle + radians;
            float angle2 = dirAngle - radians;
            Vector2 branchA = new(ArrowBranchLength * (float) Math.Cos(angle1), ArrowBranchLength * (float) Math.Sin(angle1));
            Vector2 branchB = new(ArrowBranchLength * (float) Math.Cos(angle2), ArrowBranchLength * (float) Math.Sin(angle2));
            DrawLine((int) start.X, (int) start.Y, (int) end.X, (int) end.Y, color);
            DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchA.X), (int) (end.Y + branchA.Y), color);
            DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchB.X), (int) (end.Y + branchB.Y), color);
        }
    }
}

namespace Tools
{
    public enum ToolType
    {
        PullCom,
        Pull,
        Wind,
        Delete,
    }

    public abstract class Tool
    {
        public const float BaseRadiusChange = 10f;
        public const float RadiusChangeMult = 5f;
        public const float BaseAngleChange = 10f;

        public static float Radius { get; set; }
        public static Vector2 Direction { get; set; } = new(1f, 0f);

        public string Type { get { return GetType().ToString().Split(".")[1]; } }

        protected Context _context;

        abstract public void Update();

        public void Draw()
        {
            int mouseX = GetMouseX();
            int mouseY = GetMouseY();
            if (Type == ToolType.Wind.ToString())
            {   
                Utils.Graphic.DrawArrow(mouseX, mouseY, mouseX + (int) (100f * Direction.X), mouseY + (int) (100f * Direction.Y), Color.Yellow);
                return;
            }
            DrawCircleLines(mouseX, mouseY, Radius, Color.Yellow);
        }

        public void ChangeRadius(float change)
        {
            if (GetType() == typeof(Wind))
            {
                return;
            }
            if (IsKeyDown(KeyboardKey.LeftShift))
            {
                change *= RadiusChangeMult;
            }
            Radius += change;
            if (Radius < 0f)
            {
                Radius = 0f;
            }
        }

        public void ChangeDirection(float angleChange)
        {
            if (GetType() != typeof(Wind))
            {
                return;
            }
            float currentAngle = (float) Math.Atan2(Direction.Y, Direction.X);
            float newAngle = currentAngle + angleChange;
            Vector2 newDirection = new((float) Math.Cos(newAngle), (float) Math.Sin(newAngle));
            Direction = newDirection;
        }

        public static void ChangeToolType(Context context)
        {
            ToolType[] toolTypes = (ToolType[]) Enum.GetValues(typeof(ToolType));
            ToolType newTool = toolTypes[context._selectedToolIndex];
            switch (newTool)
            {
                case ToolType.PullCom :
                    context.SelectedTool = new PullCom(context);
                    break;
                case ToolType.Pull :
                    context.SelectedTool = new Pull(context);
                    break;
                case ToolType.Wind :
                    context.SelectedTool = new Wind(context);
                    break;
                case ToolType.Delete :
                    context.SelectedTool = new Delete(context);
                    break;
            }
        }

        public static string ToolsToString()
        {
            StringBuilder sb = new();
            ToolType[] toolTypes = (ToolType[]) Enum.GetValues(typeof(ToolType));
            foreach (var tool in toolTypes)
            {
                sb.Append(tool.ToString() + "\0");
            }
            return sb.ToString();
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

    public class PullCom : Tool
    {
        private const float PullForceCoeff = 1e2f;

        public PullCom(Context context)
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
                DrawLine((int) com.X, (int) com.Y, (int) mousePos.X, (int) mousePos.Y, Color.Red);
            }
        }
    }

    public class Pull : Tool
    {
        private const float PullForceCoeff = 1e3f;

        public Pull(Context context)
        {
            _context = context;
        }

        public override void Update()
        {
            Vector2 mousePos = GetMousePosition();
            var points = Utils.Entities.QueryAreaForPoints(mousePos.X, mousePos.Y, Radius, _context);
            if (points.Any())
            {
                foreach (var p in points)
                {
                    Vector2 force = PullForceCoeff * (mousePos - p.Pos);
                    p.ApplyForce(force);
                    DrawLine((int) p.Pos.X, (int) p.Pos.Y, (int) mousePos.X, (int) mousePos.Y, Color.Red);
                }
            }
        }
    }

    public class Wind : Tool
    {
        private const int MinForce = 500;
        private const int MaxForce = 5000; 

        public Wind(Context context)
        {
            _context = context;
            Direction = new(1f, 0f);
        }

        public override void Update()
        {
            foreach (var s in _context.MassShapes)
            {
                foreach (var p in s._points)
                {
                    float forceMult = GetRandomValue(MinForce, MaxForce);
                    p.ApplyForce(forceMult * Direction);
                }
            }
        }
    }
}
