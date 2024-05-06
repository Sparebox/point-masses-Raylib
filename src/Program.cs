using System.Numerics;
using ImGuiNET;
using Physics;
using Raylib_cs;
using rlImGui_cs;
using Tools;
using static Raylib_cs.Raylib;
using static Tools.Spawn;

namespace Sim;

public class Program 
{   
    public const int WinW = 1600;
    public const int WinH = 900;
    public const int TargetFPS = 165;
    public const float QuadTreeUpdateSeconds = 0.1f;

    private static float _accumulator;
    private static float _quadTreeAccumulator;
    private static Context _context;

    public static void Main() 
    {
        _context = Init();
        rlImGui.Setup(true);
        while (!WindowShouldClose())
        {
            if (!_context._simPaused)
            {
                Update();
            }
            HandleInput();
            Draw();
        }
        rlImGui.Shutdown();
        CloseWindow();
    }

    private static Context Init()
    {
        InitWindow(WinW, WinH, "Point-masses");
        SetTargetFPS(GetMonitorRefreshRate(GetCurrentMonitor()));
        Context context = new(timeStep: 1f / 60f, 13, gravity: new(0f, Utils.UnitConversion.MetersToPixels(9.81f)))
        {
            LineColliders = {
            new(0f, 0f, WinW, 0f),
            new(0f, 0f, 0f, WinH),
            new(WinW, 0f, WinW, WinH),
            new(0f, WinH, WinW, WinH),
            //new(0f, 900f, 1600f, 200f)
            }
        };
        //context.LoadDemoScenario();
        context.SelectedTool = new PullCom(context);
        context.QuadTree = new Entities.QuadTree(new Vector2(WinW / 2f, WinH / 2f), new Vector2(WinW, WinH));

        context.SaveState();
        return context;
    }

    private static void Update()
    {
        _accumulator += GetFrameTime();
        _quadTreeAccumulator += GetFrameTime();
        while (_quadTreeAccumulator >= QuadTreeUpdateSeconds)
        {
            _context.QuadTree.Update(_context);
            _quadTreeAccumulator -= QuadTreeUpdateSeconds;
        }
        while (_accumulator >= _context.TimeStep)
        {
            //_context.QuadTree.Update(_context);
            for (int i = 0; i < _context.Substeps; i++)
            {
                foreach (MassShape s in _context.MassShapes)
                {
                    s.Update();
                }
                MassShape.HandleCollisions(_context);
            }
            _context.MassShapes.RemoveWhere(s => s._toBeDeleted);
            _accumulator -= _context.TimeStep;
        }
    }

    private static void Draw()
    {
        BeginDrawing(); // raylib
        rlImGui.Begin();
        ClearBackground(Color.Black);

        foreach (MassShape s in _context.MassShapes)
        {
            s.Draw();
        }
        foreach (var l in _context.LineColliders)
        {
            l.Draw();
        }
        if (_context._drawQuadTree)
        {
            _context.QuadTree.Draw();
        }
        _context.SelectedTool.Draw();
        DrawInfo(); // GUI
        rlImGui.End();
        EndDrawing(); // raylib
    }

    private static void HandleInput()
    {
        // Keys
        if (IsKeyPressed(KeyboardKey.G))
        {
            _context._gravityEnabled = !_context._gravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.F))
        {
            _context._drawForces = !_context._drawForces;
        }
        if (IsKeyPressed(KeyboardKey.B))
        {
            _context._drawAABBS = !_context._drawAABBS;
        }
        if (IsKeyPressed(KeyboardKey.Q))
        {
            _context._drawQuadTree = !_context._drawQuadTree;
        }
        if (IsKeyPressed(KeyboardKey.R))
        {
            _context.LoadState();
        }
        if (IsKeyPressed(KeyboardKey.Space))
        {
            _context._simPaused = !_context._simPaused;
        }
        if (IsKeyPressed(KeyboardKey.C))
        {
            _context.LineColliders.Add(new(0f, 900f, 1600f, 200f));
        }
        if (_context._toolEnabled)
        {
            _context.SelectedTool.Use();
        }
        // Mouse
        if (GetMouseWheelMoveV().Y > 0f)
        {
            _context.SelectedTool.ChangeRadius(Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(DEG2RAD * Tool.BaseAngleChange);
            if (_context.SelectedTool.GetType() == typeof(Spawn))
            {
                var spawnTool = (Spawn) _context.SelectedTool;
                spawnTool.UpdateSpawnTarget();
            }
        } 
        else if (GetMouseWheelMoveV().Y < 0f)
        {
            _context.SelectedTool.ChangeRadius(-Tool.BaseRadiusChange);
            _context.SelectedTool.ChangeDirection(DEG2RAD * -Tool.BaseAngleChange);
            if (_context.SelectedTool.GetType() == typeof(Spawn))
            {
                var spawnTool = (Spawn) _context.SelectedTool;
                spawnTool.UpdateSpawnTarget();
            }
        }
    }

    private static void DrawInfo()
    {
        ImGui.Begin("Simulation info", ImGuiWindowFlags.NoMove);
        ImGui.SetWindowPos(Vector2.Zero);
        ImGui.Text(string.Format("FPS: {0}", GetFPS()));
        ImGui.PushStyleColor(ImGuiCol.Text, _context._simPaused ? new Vector4(255f, 0f, 0f, 255f) : new Vector4(0f, 255f, 0f, 255f));
        ImGui.Checkbox(_context._simPaused ? "PAUSE" : "RUNNING", ref _context._simPaused);
        ImGui.PopStyleColor();
        ImGui.Text(string.Format("Masses: {0}", _context.MassCount));
        ImGui.Text(string.Format("Constraints: {0}", _context.ConstraintCount));
        ImGui.Text(string.Format("Shapes: {0}", _context.MassShapes.Count));
        ImGui.Text(string.Format("Substeps: {0}", _context.Substeps));
        ImGui.Text(string.Format("Step: {0:0.0000} ms", _context.TimeStep * 1e3f));
        ImGui.Text(string.Format("Substep: {0:0.0000} ms", _context.SubStep * 1e3f));
        if (_context._drawBodyInfo)
        {
            ImGui.Text(string.Format("System energy: {0} kJ", _context.SystemEnergy / 1e3f));
        }
        ImGui.Checkbox("Gravity", ref _context._gravityEnabled);
        ImGui.Checkbox("Draw forces", ref _context._drawForces);
        ImGui.Checkbox("Draw AABBs", ref _context._drawAABBS);
        ImGui.Checkbox("Draw quadtree", ref _context._drawQuadTree);
        ImGui.Checkbox("Draw body info", ref _context._drawBodyInfo);
        ImGui.PushItemWidth(50f);
        ImGui.InputFloat("Global restitution coeff", ref _context._globalRestitutionCoeff);
        ImGui.InputFloat("Global kinetic friction coeff", ref _context._globalKineticFrictionCoeff);
        ImGui.InputFloat("Global static friction coeff", ref _context._globalStaticFrictionCoeff);
        ImGui.PushItemWidth(100f);
        if (ImGui.Combo("Tool", ref _context._selectedToolIndex, Tool.ToolsToComboString()))
        {
            Tool.ChangeToolType(_context);
        }
        if (_context.SelectedTool.GetType().Equals(typeof(Spawn)))
        {
            var spawnTool = (Spawn) _context.SelectedTool;
            if (ImGui.Combo("Spawn target", ref _context._selectedSpawnTargetIndex, TargetsToComboString()))
            {
                spawnTool.UpdateSpawnTarget();
            }
            if (ImGui.InputFloat("Mass", ref spawnTool._mass))
            {
                spawnTool._mass = MathF.Abs(spawnTool._mass);
                spawnTool.UpdateSpawnTarget();
            }
            if (spawnTool._currentTarget == SpawnTarget.Ball || spawnTool._currentTarget == SpawnTarget.SoftBall)
            {
                if (ImGui.InputInt("Resolution", ref spawnTool._resolution))
                {
                    spawnTool._resolution = Math.Abs(spawnTool._resolution);
                    spawnTool.UpdateSpawnTarget();
                }
            }
            if (spawnTool._currentTarget == SpawnTarget.SoftBox || spawnTool._currentTarget == SpawnTarget.SoftBall)
            {
                if (ImGui.InputFloat("Stiffness", ref spawnTool._stiffness))
                {
                    spawnTool._stiffness = MathF.Abs(spawnTool._stiffness);
                    spawnTool.UpdateSpawnTarget();
                }
                if (spawnTool._currentTarget == SpawnTarget.SoftBall)
                {
                    if (ImGui.InputFloat("Gas amount", ref spawnTool._gasAmount))
                    {
                        spawnTool._gasAmount = MathF.Abs(spawnTool._gasAmount);
                        spawnTool.UpdateSpawnTarget();
                    }
                }
            }
        }
        if (ImGui.Button("Delete all"))
        {
            foreach (var shape in _context.MassShapes)
            {
                shape._toBeDeleted = true;
            }
        }
        if (ImGui.IsMouseHoveringRect(ImGui.GetWindowContentRegionMin(), ImGui.GetWindowContentRegionMax()))
        {
            _context._toolEnabled = false;
        }
        else
        {
            _context._toolEnabled = true;
        }
        ImGui.End();
    }
}