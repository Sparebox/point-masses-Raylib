using System.Numerics;
using System.Text;
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
    Ruler,
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
            case ToolType.Ruler :
                context.SelectedTool = new Ruler(context);
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
    private const float DefaultMass = 10f;
    private const int DefaultRes = 15;
    private const float DefaultStiffness = 1e5f;
    private const float DefaultGasAmt = 10f;

    public SpawnTarget _currentTarget;
    public float _mass;
    public float _gasAmount;
    public float _stiffness;
    public int _resolution;
    private MassShape _shapeToSpawn;

    public enum SpawnTarget
    {
        Box,
        SoftBox,
        Ball,
        SoftBall,
        Particle
    }

    public Spawn(Context context)
    {
        _context = context;
        _currentTarget = SpawnTarget.Box;
        _mass = DefaultMass;
        _resolution = DefaultRes;
        _stiffness = DefaultStiffness;
        _gasAmount = DefaultGasAmt;
        Vector2 mousePos = GetMousePosition();
        _shapeToSpawn = MassShape.Box(mousePos.X, mousePos.Y, Radius, _mass, _context);
    }

    public override void Use() 
    {
        if (!IsMouseButtonPressed(MouseButton.Left) || !_context._toolEnabled)
        {
            return;
        }
        if (_shapeToSpawn is null || Radius == 0f || _mass == 0f)
        {
            return;
        }
        _context.MassShapes.Add(_shapeToSpawn);
        _shapeToSpawn = new MassShape(_shapeToSpawn);
    }

    public override void Draw()
    {
        if (_shapeToSpawn is null)
        {
            return;
        }
        Vector2 mousePos = GetMousePosition();
        Vector2 translation = mousePos - _shapeToSpawn.Centroid;
        _shapeToSpawn.Move(translation);
        _shapeToSpawn.Draw();
    }

    public void UpdateSpawnTarget()
    {
        SpawnTarget[] spawnTargets = (SpawnTarget[]) Enum.GetValues(typeof(SpawnTarget));
        _currentTarget = spawnTargets[_context._selectedSpawnTargetIndex];
        if (_shapeToSpawn is null)
        {
            return;
        }
        Vector2 mousePos = GetMousePosition();
        switch (_currentTarget)
        {
            case SpawnTarget.Box:
                _shapeToSpawn = MassShape.Box(mousePos.X, mousePos.Y, Radius, _mass, _context);
                break;
            case SpawnTarget.SoftBox:
                _shapeToSpawn = MassShape.SoftBox(mousePos.X, mousePos.Y, Radius, _mass, 1e3f, _context);
                break;
            case SpawnTarget.Ball:
                _shapeToSpawn = MassShape.HardBall(mousePos.X, mousePos.Y, Radius, _mass, _resolution, _context);
                break;
            case SpawnTarget.SoftBall:
                _shapeToSpawn = MassShape.SoftBall(mousePos.X, mousePos.Y, Radius, _mass, _resolution, _stiffness, _gasAmount, _context);
                break;
            case SpawnTarget.Particle:
                _shapeToSpawn = MassShape.Particle(mousePos.X, mousePos.Y, Radius, _context);
                break;
        }
    }

    public static string TargetsToComboString()
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
        if (!IsMouseButtonDown(MouseButton.Left) || !_context._toolEnabled)
        {
            return;
        }
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
    private bool _shouldVisualize = false;
    private Vector2 _centerOfMass;

    public PullCom(Context context)
    {
        _context = context;
    }

    public override void Use()
    {
        if (!IsMouseButtonDown(MouseButton.Left) || !_context._toolEnabled)
        {
            return;
        }
        Vector2 mousePos = GetMousePosition();
        var shapes = Utils.Entities.QueryAreaForShapes(mousePos.X, mousePos.Y, Radius, _context);
        if (!shapes.Any())
        {
            return;
        }
        MassShape closest = FindClosestShape(mousePos, shapes);
        _centerOfMass = closest.CenterOfMass;
        Vector2 force = PullForceCoeff * (mousePos - _centerOfMass);
        closest.ApplyForceCOM(force);
        _shouldVisualize = true;
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, Radius, Color.Yellow);
        if (_shouldVisualize)
        {
            _shouldVisualize = false;
            DrawLine((int) _centerOfMass.X, (int) _centerOfMass.Y, (int) mousePos.X, (int) mousePos.Y, Color.Red);
        }
    }
}

public class Pull : Tool
{
    private const float PullForceCoeff = 1e3f;
    private readonly HashSet<Vector2> _positions;
    private bool _shouldVisualize;

    public Pull(Context context)
    {
        _context = context;
        _positions = new();
    }

    public override void Use()
    {
        if (!IsMouseButtonDown(MouseButton.Left) || !_context._toolEnabled)
        {
            return;
        }
        Vector2 mousePos = GetMousePosition();
        var points = Utils.Entities.QueryAreaForPoints(mousePos.X, mousePos.Y, Radius, _context);
        if (!points.Any())
        {
            return;
        }
        _positions.Clear();
        foreach (var p in points)
        {
            Vector2 force = PullForceCoeff * (mousePos - p.Pos);
            p.ApplyForce(force);
            _positions.Add(p.Pos);
        }
        _shouldVisualize = true;
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, Radius, Color.Yellow);
        if (_shouldVisualize)
        {
            _shouldVisualize = false;
            foreach (var pos in _positions)
            {
               DrawLine((int) pos.X, (int) pos.Y, (int) mousePos.X, (int) mousePos.Y, Color.Red);
            }
        }
    }
}

public class Wind : Tool
{
    private const int MinForce = (int) 5e2;
    private const int MaxForce = (int) 5e3; 

    public Wind(Context context)
    {
        _context = context;
        Direction = new(1f, 0f);
    }

    public override void Use()
    {
        if (!IsMouseButtonDown(MouseButton.Left) || !_context._toolEnabled)
        {
            return;
        }
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
        Utils.Graphics.DrawArrow(mousePos.X, mousePos.Y, mousePos.X + (int) (100f * Direction.X), mousePos.Y + (int) (100f * Direction.Y), Color.Yellow);
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
        if (!IsMouseButtonDown(MouseButton.Left) || !_context._toolEnabled)
        {
            return;
        }
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

public class Ruler : Tool
{
    private Vector2 _startPos;
    private bool _shouldVisualize;

    public Ruler(Context context)
    {
        _context = context;
    }

    public override void Draw()
    {
        if(!_shouldVisualize)
        {
            return;
        }
        Vector2 mousePos = GetMousePosition();
        float len = Utils.UnitConversion.PixelsToMeters(Vector2.Distance(_startPos, mousePos));
        DrawText(string.Format("{0:0.00} m", len), (int) mousePos.X, (int) mousePos.Y + 20, 30, Color.Yellow);
        DrawLine((int) _startPos.X, (int) _startPos.Y, (int) mousePos.X, (int) mousePos.Y, Color.Yellow);
    }

    public override void Use()
    {
        _shouldVisualize = IsMouseButtonDown(MouseButton.Left) && _context._toolEnabled;
        if (IsMouseButtonReleased(MouseButton.Left) || IsMouseButtonPressed(MouseButton.Left))
        {
            _startPos = GetMousePosition();
            _shouldVisualize = false;
        }
    }
}