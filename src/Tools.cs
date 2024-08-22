using System.Numerics;
using System.Text;
using Entities;
using GravitySim;
using Raylib_cs;
using Sim;
using SimSystems;
using Utils;
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
    Editor,
    GravityWell,
    NbodySim,
}

public abstract class Tool
{
    public const float BaseRadiusChange = 0.01f;
    public const float RadiusChangeMultShift = 5f;
    public const float RadiusChangeMultCtrl = 0.1f;
    public const float BaseAngleChange = 10f;
    public const float DefaultRadius = 0.05f;

    public static ToolType[] ToolTypes => (ToolType[]) Enum.GetValues(typeof(ToolType));
    public static float Radius { get; set; } = DefaultRadius;
    public static Vector2 Direction { get; set; } = new(1f, 0f);
    public static string ToolComboString { get; } = ToolsToComboString();

    protected Context _ctx;

    abstract public void Update();
    abstract public void Draw();

    public void ChangeRadius(float change)
    {
        if (GetType() == typeof(Wind))
        {
            return;
        }
        if (IsKeyDown(KeyboardKey.LeftShift))
        {
            change *= RadiusChangeMultShift;
        }
        else if (IsKeyDown(KeyboardKey.LeftControl))
        {
            change *= RadiusChangeMultCtrl;
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
        float currentAngle = MathF.Atan2(Direction.Y, Direction.X);
        float newAngle = currentAngle + angleChange;
        Vector2 newDirection = new(MathF.Cos(newAngle), MathF.Sin(newAngle));
        Direction = newDirection;
    }

    public static void ChangeToolType(Context ctx)
    {
        switch (ToolTypes[ctx._selectedToolIndex])
        {
            case ToolType.PullCom :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.PullCom];
                break;
            case ToolType.Pull :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.Pull];
                break;
            case ToolType.Wind :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.Wind];
                break;
            case ToolType.Rotate :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.Rotate];
                break;
            case ToolType.Spawn :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.Spawn];
                break;
            case ToolType.Ruler :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.Ruler];
                break;
            case ToolType.Delete :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.Delete];
                break;
            case ToolType.Editor :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.Editor];
                break;
            case ToolType.GravityWell :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.GravityWell];
                break;
            case ToolType.NbodySim :
                ctx.SelectedTool = ctx.Tools[(int) ToolType.NbodySim];
                break;
        }
    }

    private static string ToolsToComboString()
    {
        StringBuilder sb = new();
        ToolType[] toolTypes = (ToolType[]) Enum.GetValues(typeof(ToolType));
        foreach (var tool in toolTypes)
        {
            sb.Append(tool.ToString() + "\0");
        }
        return sb.ToString();
    }

    protected static MassShape FindClosestShape(in Vector2 pos, IEnumerable<MassShape> shapes)
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
    public const float DefaultStiffness = 1f;
    public const float DefaultGasAmt = 300f;
    private const float DefaultMass = 30f;
    private const int DefaultRes = 15;

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

    public Spawn(Context ctx)
    {
        _ctx = ctx;
        _currentTarget = SpawnTarget.Box;
        _mass = DefaultMass;
        _resolution = DefaultRes;
        _stiffness = DefaultStiffness;
        _gasAmount = DefaultGasAmt;
        Vector2 mousePos = UnitConv.PixelsToMeters(GetMousePosition());
        _shapeToSpawn = MassShape.Box(mousePos.X, mousePos.Y, Radius, _mass, _ctx);
    }

    public override void Update() 
    {
        if (!IsMouseButtonPressed(MouseButton.Left))
        {
            return;
        }
        if (_shapeToSpawn is null || Radius == 0f || _mass == 0f)
        {
            return;
        }
        _ctx.AddMassShape(_shapeToSpawn);
        _shapeToSpawn = new MassShape(_shapeToSpawn);
    }

    public override void Draw()
    {
        if (_shapeToSpawn is null)
        {
            return;
        }
        Vector2 mousePos = UnitConv.PixelsToMeters(GetMousePosition());
        Vector2 translation = mousePos - _shapeToSpawn.Centroid;
        _shapeToSpawn.Move(translation);
        _shapeToSpawn.Draw();
    }

    public void UpdateSpawnTarget()
    {
        SpawnTarget[] spawnTargets = (SpawnTarget[]) Enum.GetValues(typeof(SpawnTarget));
        _currentTarget = spawnTargets[_ctx._selectedSpawnTargetIndex];
        if (_shapeToSpawn is null)
        {
            return;
        }
        Vector2 mousePos = UnitConv.PixelsToMeters(GetMousePosition());
        switch (_currentTarget)
        {
            case SpawnTarget.Box:
                _shapeToSpawn = MassShape.Box(mousePos.X, mousePos.Y, Radius, _mass, _ctx);
                break;
            case SpawnTarget.SoftBox:
                _shapeToSpawn = MassShape.SoftBox(mousePos.X, mousePos.Y, Radius, _mass, _stiffness, _ctx);
                break;
            case SpawnTarget.Ball:
                _shapeToSpawn = MassShape.HardBall(mousePos.X, mousePos.Y, Radius, _mass, _resolution, _ctx);
                break;
            case SpawnTarget.SoftBall:
                _shapeToSpawn = MassShape.SoftBall(mousePos.X, mousePos.Y, Radius, _mass, _resolution, _stiffness, _gasAmount, _ctx);
                break;
            case SpawnTarget.Particle:
                _shapeToSpawn = MassShape.Particle(mousePos.X, mousePos.Y, PointMass.RadiusToMass(Radius), _ctx);
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
    public Delete(Context ctx) => _ctx = ctx;
    

    public override void Update()
    {
        if (!IsMouseButtonDown(MouseButton.Left))
        {
            return;
        }
        Vector2 mousePos = UnitConv.PixelsToMeters(GetMousePosition());
        BoundingBox area = new(new(mousePos.X - Radius, mousePos.Y - Radius, 0f), new(mousePos.X + Radius, mousePos.Y + Radius, 0f));
        var shapes = _ctx.GetMassShapes(area).ToHashSet();
        if (!shapes.Any())
        {
            return;
        }
        shapes.RemoveWhere(s => !CheckCollisionBoxes(area, s.Aabb));
        List<uint> pointsToDelete = new();
        foreach (var shape in shapes)
        {
            foreach (var p in shape._points)
            {
                if (CheckCollisionCircles(mousePos, Radius, p.Pos, p.Radius))
                {
                    pointsToDelete.Add(p.Id);
                }
            }
            shape.DeletePoints(pointsToDelete);
            shape._inflated = false;
            pointsToDelete.Clear();
        }
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLinesV(mousePos, UnitConv.MetersToPixels(Radius), Color.Yellow);
    }
}

public class PullCom : Tool
{
    public float _forceCoeff = DefaultForceCoeff;
    private const float DefaultForceCoeff = 1e2f;
    private bool _shouldVisualize = false;
    private Vector2 _centerOfMass;

    public PullCom(Context ctx) =>_ctx = ctx;

    public override void Update()
    {
        if (!IsMouseButtonDown(MouseButton.Left))
        {
            return;
        }
        Vector2 mousePos = UnitConv.PixelsToMeters(GetMousePosition());
        BoundingBox area = new(new(mousePos.X - Radius, mousePos.Y - Radius, 0f), new(mousePos.X + Radius, mousePos.Y + Radius, 0f));
        var shapes = _ctx.GetMassShapes(area);
        if (!shapes.Any())
        {
            return;
        }
        MassShape closest = FindClosestShape(mousePos, shapes);
        if (!CheckCollisionBoxes(area, closest.Aabb))
        {
            return;
        }
        _centerOfMass = closest.CenterOfMass;
        Vector2 force = _forceCoeff * (mousePos - _centerOfMass);
        closest.ApplyForceCOM(force);
        _shouldVisualize = true;
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLinesV(mousePos, UnitConv.MetersToPixels(Radius), Color.Yellow);
        if (_shouldVisualize)
        {
            _shouldVisualize = false;
            DrawLineV(UnitConv.MetersToPixels(_centerOfMass), mousePos, Color.Red);
        }
    }
}

public class Pull : Tool
{
    public float _forceCoeff = DefaultForceCoeff;
    private const float DefaultForceCoeff = 1e3f;
    private readonly List<Vector2> _positions;
    private bool _shouldVisualize;

    public Pull(Context ctx)
    {
        _ctx = ctx;
        _positions = new();
    }

    public override void Update()
    {
        if (!IsMouseButtonDown(MouseButton.Left))
        {
            return;
        }
        Vector2 mousePos = UnitConv.PixelsToMeters(GetMousePosition());
        BoundingBox area = new(new(mousePos.X - Radius, mousePos.Y - Radius, 0f), new(mousePos.X + Radius, mousePos.Y + Radius, 0f));
        var points = _ctx.GetPointMasses(area);
        if (!points.Any())
        {
            return;
        }
        _positions.Clear();
        foreach (var p in points)
        {
            if (!CheckCollisionPointCircle(p.Pos, mousePos, Radius))
            {
                continue;
            }
            Vector2 force = _forceCoeff * (mousePos - p.Pos);
            p.ApplyForce(force);
            _positions.Add(p.Pos);
        }
        _shouldVisualize = true;
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        DrawCircleLinesV(mousePos, UnitConv.MetersToPixels(Radius), Color.Yellow);
        if (_shouldVisualize)
        {
            _shouldVisualize = false;
            foreach (var pos in _positions)
            {
               DrawLineV(UnitConv.MetersToPixels(pos), mousePos, Color.Red);
            }
        }
    }
}

public class Wind : Tool
{
    private const int MinForce = (int) 1e1;
    private const int MaxForce = (int) 1e2; 

    public Wind(Context ctx)
    {
        _ctx = ctx;
        Direction = new(1f, 0f);
    }

    public override void Update()
    {
        if (!IsMouseButtonDown(MouseButton.Left))
        {
            return;
        }
        foreach (var s in _ctx.MassShapes)
        {
            foreach (var p in s._points)
            {
                float force = GetRandomValue(MinForce, MaxForce);
                p.ApplyForce(force * Direction);
            }
        }
    }

    public override void Draw()
    {
        Vector2 mousePos = GetMousePosition();
        Graphics.DrawArrow(mousePos.X, mousePos.Y, mousePos.X + (int) (100f * Direction.X), mousePos.Y + (int) (100f * Direction.Y), Color.Yellow);
    }
}

public class Rotate : Tool
{
    private const float ForceAmount = 1e2f;

    public Rotate(Context ctx) => _ctx = ctx;
    

    public override void Update()
    {
        if (!IsMouseButtonDown(MouseButton.Left))
        {
            return;
        }
        Vector2 mousePos = UnitConv.PixelsToMeters(GetMousePosition());
        var shapes = _ctx.GetMassShapes(new BoundingBox(new(mousePos.X - Radius, mousePos.Y - Radius, 0f), new(mousePos.X + Radius, mousePos.Y + Radius, 0f)));
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
        DrawCircleLinesV(mousePos, UnitConv.MetersToPixels(Radius), Color.Yellow);
    }
}

public class Ruler : Tool
{
    private Vector2 _startPos;
    private bool _shouldVisualize;

    public Ruler(Context ctx) => _ctx = ctx;

    public override void Draw()
    {
        if(!_shouldVisualize)
        {
            return;
        }
        Vector2 mousePos = GetMousePosition();
        float len = UnitConv.PixelsToMeters(Vector2.Distance(_startPos, mousePos));
        DrawText(string.Format("{0:0.00} m", len), (int) mousePos.X, (int) mousePos.Y + 20, 30, Color.Yellow);
        DrawLineV(_startPos, mousePos, Color.Yellow);
    }

    public override void Update()
    {
        _shouldVisualize = IsMouseButtonDown(MouseButton.Left) && _ctx._toolEnabled;
        if (IsMouseButtonReleased(MouseButton.Left) || IsMouseButtonPressed(MouseButton.Left))
        {
            _startPos = GetMousePosition();
            _shouldVisualize = false;
        }
    }
}

public class GravityWell : Tool
{
    public float _gravConstant = 0.1f;
    public float _minDist = 0.01f;
    private readonly List<Vector2> _positions;

    public GravityWell(Context ctx)
    {
        _ctx = ctx;
        _positions = new();
    }

    public override void Draw()
    {
        var mousePos = GetMousePosition();
        foreach (var pos in _positions)
        {
            Vector2 pixelPos = UnitConv.MetersToPixels(pos);
            DrawText("G", (int) pixelPos.X, (int) pixelPos.Y, 20, Color.Yellow);
        }
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, UnitConv.MetersToPixels(Radius), Color.Yellow);
    }

    public override void Update()
    {
        var mousePosMeters = UnitConv.PixelsToMeters(GetMousePosition());
        if (IsMouseButtonPressed(MouseButton.Left))
        {
            _positions.Add(mousePosMeters);
        }
        if (IsMouseButtonPressed(MouseButton.Right))
        {
            _positions.RemoveAll(pos => Vector2.DistanceSquared(pos, mousePosMeters) < Radius * Radius);
        }
        ApplyGravityForces();
    }

    private void ApplyGravityForces()
    {
        foreach (var shape in _ctx.MassShapes)
        {
            foreach (var pos in _positions)
            {
                Vector2 dir = pos - shape.CenterOfMass;
                float dist = dir.Length();
                if (dist == 0f || dist < _minDist)
                {
                    continue;
                }
                dir /= dist;
                Vector2 gravForce = dir * _gravConstant * shape.Mass / (dist * dist);
                shape.ApplyForceCOM(gravForce);
            }
        }
    }
}

public class NbodySim : Tool
{
    public float _gravConstant = 0.01f;
    public float _minDist = 0f;
    public float _threshold = 0.01f;
    public bool _running;
    public bool _collisionsEnabled;
    private const int UpdateIntervalMs = 50;
    private readonly BarnesHutTree _barnesHutTree;
    private Thread _updateThread;

    public NbodySim(Context ctx)
    {
        _ctx = ctx;
        _barnesHutTree = new(
            UnitConv.PixelsToMeters(new Vector2(Program.WinW / 2f, Program.WinH / 2f)),
            UnitConv.PixelsToMeters(new Vector2(Program.WinW, Program.WinH))
        );
        _updateThread = new Thread(new ThreadStart(ThreadUpdate), 0)
        {
            IsBackground = true
        };
        _updateThread.Start();
    }

    public override void Draw() {}
    public override void Update() {}

    private void ThreadUpdate()
    {
        for (;;)
        {
            Thread.Sleep(UpdateIntervalMs);
            if (!_running || _ctx._simPaused)
            {
                continue;
            }
            _barnesHutTree.Update(_ctx);
        }
    }
}