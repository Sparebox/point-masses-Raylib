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

    private static float _accumulator;
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
        SetTargetFPS(TargetFPS);
        Context context = new(timeStep: 1f / 60f, 13, gravity: new(0f, Utils.UnitConversion.MetersToPixels(6f)))
        {
            LineColliders = {
            new(0f, 0f, WinW, 0f),
            new(0f, 0f, 0f, WinH),
            new(WinW, 0f, WinW, WinH),
            new(0f, WinH, WinW, WinH),
            //new(0f, 900f, 1600f, 200f)
            }
        };
        context.SelectedTool = new PullCom(context);
        //context.MassShapes.Add(MassShape.Cloth(x: 300f, y: 50f, width: 700f, height: 700f, mass: 0.7f, res: 42, stiffness: 1e5f, context));
        //context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 300f, WinH / 2f - 200f, 50f, 20f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.SoftBall(WinW / 2f + 300f, WinH / 2f - 200f, 50f, 20f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 300f, WinH / 2f - 100f, 50f, 20f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 100f, WinH / 2f - 100f, 50f, 50f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.SoftBall(WinW / 2f - 300f, WinH / 2f + 200f, 200f, 10f, 20, 1000f, context));
        //context.MassShapes.Add(MassShape.Pendulum(WinW / 2f, 30f, 700f, 10f, 10, context));
        //context.MassShapes.Add(MassShape.Particle(200f, 50f, 10f, context));
        //context.MassShapes.Add(MassShape.Box(WinW / 2f, WinH / 2f - 300f, 100f, 10f, context));
        //context.MassShapes.Add(MassShape.Box(WinW / 2f, WinH / 2f - 100f, 200f, 50f, context));
        //context.MassShapes.Add(MassShape.SoftBox(WinW / 2f, WinH / 2f - 200f, 60f, 20f, 5e4f, context));
        //context.MassShapes.Add(MassShape.SoftBox(WinW / 2f, WinH / 2f, 100f, 20f, 5e4f, context));
        //context.MassShapes.Add(MassShape.HardBall(500f, 200f, 50f, 20f, 6, context));
        //context.MassShapes.Add(MassShape.HardBall(700f, 200f, 50f, 20f, 6, context));
        //context.MassShapes.Add(MassShape.HardBall(300f, 200f, 50f, 20f, 6, context));
        //context.MassShapes.Add(MassShape.Particle(WinW / 2f, WinH / 2f, 10f, context));
        //context.MassShapes.Add(MassShape.Particle(WinW / 2f + 100f, WinH / 2f, 10f, context));
        //context._ramp = new Entity.RotatingCollider(0f, 200f, WinW, WinH);
        context.SaveState();
        return context;
    }

    private static void Update()
    {
        _accumulator += GetFrameTime();
        while (_accumulator >= _context.TimeStep)
        {
            for (int i = 0; i < _context.Substeps; i++)
            {
                foreach (MassShape s in _context.MassShapes)
                {
                    s.Update();
                }
                MassShape.SolveCollisions(_context);
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
        if (IsKeyPressed(KeyboardKey.R))
        {
            _context.LoadState();
        }
        if (IsKeyPressed(KeyboardKey.Space))
        {
            _context._simPaused = !_context._simPaused;
        }
        // if (IsKeyDown(KeyboardKey.Up))
        // {
        //     _context._ramp.Raise(10f * GetFrameTime());
        // }
        // if (IsKeyDown(KeyboardKey.Down))
        // {
        //     _context._ramp.Lower(10f * GetFrameTime());
        // }
        _context.SelectedTool.Use();
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
                spawnTool._mass = Math.Abs(spawnTool._mass);
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
                    spawnTool._stiffness = Math.Abs(spawnTool._stiffness);
                    spawnTool.UpdateSpawnTarget();
                }
                if (spawnTool._currentTarget == SpawnTarget.SoftBall)
                {
                    if (ImGui.InputFloat("Gas amount", ref spawnTool._gasAmount))
                    {
                        spawnTool._gasAmount = Math.Abs(spawnTool._gasAmount);
                        spawnTool.UpdateSpawnTarget();
                    }
                }
            }
        }
        if (ImGui.Button("Remove all"))
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