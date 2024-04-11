using System.Numerics;
using System.Text;
using ImGuiNET;
using Physics;
using Raylib_cs;
using Sim;
using static Raylib_cs.Raylib;

namespace Tools;

public enum ToolType
{
    PullCom,
    Pull,
    Wind,
    Rotate,
    Spawn,
    Delete,
}

public abstract class Tool
{
    public const float BaseRadiusChange = 10f;
    public const float RadiusChangeMult = 5f;
    public const float BaseAngleChange = 10f;

    public static float Radius { get; set; }
    public static Vector2 Direction { get; set; } = new(1f, 0f);

    protected Context _context;

    abstract public void Use();
    abstract public void Draw();

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
            case ToolType.Rotate :
                context.SelectedTool = new Rotate(context);
                break;
            case ToolType.Spawn :
                context.SelectedTool = new Spawn(context);
                break;
            case ToolType.Delete :
                context.SelectedTool = new Delete(context);
                break;
        }
    }

    public static string ToolsToComboString()
    {
        StringBuilder sb = new();
        ToolType[] toolTypes = (ToolType[]) Enum.GetValues(typeof(ToolType));
        foreach (var tool in toolTypes)
        {
            sb.Append(tool.ToString() + "\0");
        }
        return sb.ToString();
    }

    protected static MassShape FindClosestShape(in Vector2 pos, List<MassShape> shapes)
    {
        MassShape closest = null;
        float closestDistSq = float.MaxValue;
        foreach (var shape in shapes)
        {
            float distSq = Vector2.DistanceSquared(pos, shape.CenterOfMass);
            if (distSq < closestDistSq)
            {
                closest = shape;
                closestDistSq = distSq;
            }
        }
        return closest;
    }
}

public class Spawn : Tool
{
    private enum SpawnTarget
    {
        Box,
        SoftBox,
        Ball,
        SoftBall,
    }

    private SpawnTarget _currentTarget;
    private MassShape _shapeToSpawn;
    private float _mass;

    public Spawn(Context context)
    {
        _context = context;
        _currentTarget = SpawnTarget.Box;
    }

    public override void Use() 
    {
        switch (_currentTarget)
        {
            case SpawnTarget.Box:
                _context.MassShapes.Add(_shapeToSpawn);
                break;
            case SpawnTarget.SoftBox:
                break;
            case SpawnTarget.Ball:
                break;
            case SpawnTarget.SoftBall:
                break;
        }
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        var winFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse;
        ImGui.Begin("Spawn target", winFlags);
        ImGui.SetWindowPos(mousePos + new Vector2(0f, 25f));
        ImGui.SetWindowSize(new Vector2(150f, 50f));
        if (ImGui.Combo("Spawn target", ref _context._selectedSpawnTargetIndex, TargetsToComboString()))
        ImGui.End();
        _shapeToSpawn?.DrawPreview(mousePos);
    }

    public void ChangeSpawnTarget(bool nextTarget)
    {
        SpawnTarget[] spawnTargets = (SpawnTarget[]) Enum.GetValues(typeof(SpawnTarget));
        _context._selectedSpawnTargetIndex = Math.Max(0, (_context._selectedSpawnTargetIndex + (nextTarget ? 1 : -1)) % spawnTargets.Length);
        _currentTarget = spawnTargets[_context._selectedSpawnTargetIndex];
        Vector2 mousePos = GetMousePosition();
        switch (_currentTarget)
        {
            case SpawnTarget.Box:
                _shapeToSpawn = MassShape.Box(mousePos.X, mousePos.Y, Radius, _mass, _context);
                break;
        }
    }

    private static string TargetsToComboString()
    {
        StringBuilder sb = new();
        SpawnTarget[] spawnTargets = (SpawnTarget[]) Enum.GetValues(typeof(SpawnTarget));
        foreach (var tool in spawnTargets)
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

    public override void Use()
    {
        Vector2 mousePos = GetMousePosition();
        var shapes = Utils.Entities.QueryAreaForShapes(mousePos.X, mousePos.Y, Radius, _context);
        if (!shapes.Any())
        {
            return;
        }
        var shape = shapes.First();
        shape._constraints.RemoveAll(c => {
            return Vector2.DistanceSquared(mousePos, c.PointA.Pos) < Radius * Radius ||
            Vector2.DistanceSquared(mousePos, c.PointB.Pos) < Radius * Radius;
        });
        shape._points.RemoveAll(p => Vector2.DistanceSquared(mousePos, p.Pos) < Radius * Radius);
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, Radius, Color.Yellow);
    }
}

public class PullCom : Tool
{
    private const float PullForceCoeff = 1e2f;

    public PullCom(Context context)
    {
        _context = context;
    }

    public override void Use()
    {
        Vector2 mousePos = GetMousePosition();
        var shapes = Utils.Entities.QueryAreaForShapes(mousePos.X, mousePos.Y, Radius, _context);
        if (!shapes.Any())
        {
            return;
        }
        MassShape closest = FindClosestShape(mousePos, shapes);
        Vector2 com = closest.CenterOfMass;
        Vector2 force = PullForceCoeff * (mousePos - com);
        closest.ApplyForceCOM(force);
        DrawLine((int) com.X, (int) com.Y, (int) mousePos.X, (int) mousePos.Y, Color.Red);
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, Radius, Color.Yellow);
    }
}

public class Pull : Tool
{
    private const float PullForceCoeff = 1e3f;

    public Pull(Context context)
    {
        _context = context;
    }

    public override void Use()
    {
        Vector2 mousePos = GetMousePosition();
        var points = Utils.Entities.QueryAreaForPoints(mousePos.X, mousePos.Y, Radius, _context);
        if (!points.Any())
        {
            return;
        }
        foreach (var p in points)
        {
            Vector2 force = PullForceCoeff * (mousePos - p.Pos);
            p.ApplyForce(force);
            DrawLine((int) p.Pos.X, (int) p.Pos.Y, (int) mousePos.X, (int) mousePos.Y, Color.Red);
        }
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, Radius, Color.Yellow);
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

    public override void Use()
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

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        Utils.Graphic.DrawArrow(mousePos.X, mousePos.Y, mousePos.X + (int) (100f * Direction.X), mousePos.Y + (int) (100f * Direction.Y), Color.Yellow);
    }
}

public class Rotate : Tool
{
    private const float ForceAmount = 1e4f;

    public Rotate(Context context)
    {
        _context = context;
    }

    public override void Use()
    {
        Vector2 mousePos = GetMousePosition();
        var shapes = Utils.Entities.QueryAreaForShapes(mousePos.X, mousePos.Y, Radius, _context);
        if (!shapes.Any())
        {
            return;
        }
        MassShape closest = FindClosestShape(mousePos, shapes);
        Vector2 COM = closest.CenterOfMass;
        foreach (var p in closest._points)
        {
            Vector2 comToPoint = p.Pos - COM;
            float radius = comToPoint.Length();
            if (radius == 0f)
            {
                continue;
            }
            Vector2 normal = new(comToPoint.Y / radius, -comToPoint.X / radius);
            float sign = IsMouseButtonDown(MouseButton.Right) ? -1f : 1f;
            p.ApplyForce(sign * ForceAmount * normal);
        }
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, Radius, Color.Yellow);
    }
}