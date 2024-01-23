using System.Numerics;
using Collision;
using Physics;
using Raylib_cs;
using Tools;
using static Raylib_cs.Raylib;

namespace Sim;

public class Loop 
{   
    public const float TimeStep = 1f / 60f;
    public const int WinW = 1600;
    public const int WinH = 900;
    public const float PixelsPerMeter = 1f / 0.01f;

    private const int Substeps = 15;
    private const float SubStep = TimeStep / Substeps;

    private static float _accumulator;
    private static Context _context;

    public static void Main() 
    {
        Init();
        while (!WindowShouldClose())
        {
            if (!_context.SimPaused)
            {
                _accumulator += GetFrameTime();
                while (_accumulator >= TimeStep)
                {
                    for (int i = 0; i < Substeps; i++)
                    {
                        foreach (MassShape s in _context.MassShapes)
                        {
                            s.Update(SubStep);
                        }
                    }
                    _accumulator -= TimeStep;
                }
            }
            HandleInput();

            BeginDrawing();
            ClearBackground(Color.BLACK);

            foreach (MassShape s in _context.MassShapes)
            {
                s.Draw();
            }
            foreach (var l in _context.LineColliders)
            {
                l.Draw();
            }
            Tool.Draw();
            DrawInfo();

            EndDrawing();
        }
        CloseWindow();
    }

    private static void Init()
    {
        InitWindow(WinW, WinH, "Point-masses");
        SetTargetFPS(165);
        _context = new()
        {
            LineColliders = new() {
            new(0f, 0f, WinW, 0f),
            new(0f, 0f, 0f, WinH),
            new(WinW, 0f, WinW, WinH),
            new(0f, WinH, WinW, WinH),
            //new(0f, 900f, 1600f, 200f)
            },
            GravityEnabled = false,
            MassShapes = new(),
        };
        _context.SelectedTool = new Pull(_context);
        _context.MassShapes.Add(MassShape.Cloth(x: 300f, y: 50f, width: 700f, height: 700f, mass: 0.7f, res: 37, stiffness: 5e4f, _context));
    }

    private static void HandleInput()
    {
        // Keys
        if (IsKeyPressed(KeyboardKey.KEY_G))
        {
            _context.GravityEnabled = !_context.GravityEnabled;
        }
        if (IsKeyPressed(KeyboardKey.KEY_F))
        {
            _context.DrawForces = !_context.DrawForces;
        }
        if (IsKeyPressed(KeyboardKey.KEY_T))
        {
            ToolType[] toolTypes = (ToolType[]) Enum.GetValues(typeof(ToolType));
            int newToolIndex = (_context.SelectedTool.GetToolIndex() + 1) % toolTypes.Length;
            ToolType newTool = toolTypes[newToolIndex];
            switch (newTool)
            {
                case ToolType.Pull :
                    _context.SelectedTool = new Pull(_context);
                    break;
                case ToolType.Delete :
                    _context.SelectedTool = new Delete(_context);
                    break;
            }
        }
        if (IsKeyPressed(KeyboardKey.KEY_SPACE))
        {
            _context.SimPaused = !_context.SimPaused;
        }
        // Mouse
        if (IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
        {
            _context.SelectedTool.Update();
        }
        if (GetMouseWheelMoveV().Y > 0f)
        {
            Tool.ChangeRadius(Tool.BaseRadiusChange);
        } 
        else if (GetMouseWheelMoveV().Y < 0f)
        {
            Tool.ChangeRadius(-Tool.BaseRadiusChange);
        }
    }

    private static void DrawInfo()
    {
        DrawText(string.Format("FPS: {0}", GetFPS()), 10, 10, 20, Color.YELLOW);
        DrawText(string.Format("{0}", _context.SimPaused ? "PAUSED" : "RUNNING"), 10, 30, 20, Color.YELLOW);
        DrawText(string.Format("Masses: {0}", _context.CountMasses()), 10, 50, 20, Color.YELLOW);
        DrawText(string.Format("Constraints: {0}", _context.CountConstraints()), 10, 70, 20, Color.YELLOW);
        DrawText(string.Format("Substeps: {0}", Substeps), 10, 90, 20, Color.YELLOW);
        DrawText(string.Format("Step: {0}", TimeStep), 10, 110, 20, Color.YELLOW);
        DrawText(string.Format("Substep: {0}", SubStep), 10, 130, 20, Color.YELLOW);
        DrawText(string.Format("Gravity: {0}", _context.GravityEnabled ? "Enabled" : "Disabled"), 10, 150, 20, Color.YELLOW);
        DrawText(string.Format("Tool: {0}", _context.SelectedTool.Type), 10, 170, 20, Color.YELLOW);
    }
}

public class Context
{
    public readonly Vector2 Gravity = new(0f, 9.81f * Loop.PixelsPerMeter);
    public List<LineCollider> LineColliders { get; init; }
    public List<MassShape> MassShapes { get; init; }
    public bool GravityEnabled { get; set; }
    public bool DrawForces { get; set; }
    public bool SimPaused { get; set; }
    public Tool SelectedTool { get; set; }

    public int CountMasses()
    {
        int count = 0;
        foreach (var shape in MassShapes)
        {
            count += shape._points.Count;
        }
        return count;
    }

    public int CountConstraints()
    {
        int count = 0;
        foreach (var shape in MassShapes)
        {
            count += shape._constraints.Count;
        }
        return count;
    }
}