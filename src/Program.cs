using System.Numerics;
using PointMasses.Entities;
using Raylib_cs;
using rlImGui_cs;
using PointMasses.UI;
using PointMasses.Utils;
using static Raylib_cs.Raylib;
using ImGuiNET;
using PointMasses.Textures;
using point_masses;

namespace PointMasses.Sim;

public class Program 
{   
    public static readonly int TargetFPS = GetMonitorRefreshRate(GetCurrentMonitor());

    public static TextureManager TextureManager { get; set; }

    private static Scene ActiveScene { get; set; }
    private static RenderTexture2D RenderTexture { get; set;}

    public static void Main() 
    {
        TextureManager = new();
        ActiveScene = Init(0.8f, 1f);
        rlImGui.Setup(true);
        unsafe { ImGui.GetIO().NativePtr->IniFilename = null; } // Disable imgui.ini file
        while (!WindowShouldClose())
        {
            if (!ActiveScene.Ctx._simPaused)
            {
                Update();
            }
            ActiveScene.Ctx.HandleInput();
            Draw();
        }
        rlImGui.Shutdown();
        CloseWindow();
    }

    private static Scene Init(float winSizePercentage, float renderPercentage)
    {
        InitWindow(0, 0, "Point-masses");
        SetTargetFPS(TargetFPS);

        int winWidth = (int) (winSizePercentage * GetMonitorWidth(GetCurrentMonitor()));
        int winHeight = (int) (winSizePercentage * GetMonitorHeight(GetCurrentMonitor()));
        int renderWidth = (int) (renderPercentage * winWidth);
        int renderHeight = (int) (renderPercentage * winHeight);
        SetWindowSize(winWidth, winHeight);
        
        int winPosX = GetMonitorWidth(GetCurrentMonitor()) / 2 - winWidth / 2;
        int winPosY = GetMonitorHeight(GetCurrentMonitor()) / 2 - winHeight / 2;
        SetWindowPosition(winPosX, winPosY);

        RenderTexture = LoadRenderTexture(renderWidth, renderHeight);
        
        float winWidthMeters = UnitConv.PtoM(winWidth);
        float winHeightMeters = UnitConv.PtoM(winHeight);
        Context ctx = new(timeStep: 1f / 60f, 3, gravity: new(0f, 9.81f), winSize: new(winWidth, winHeight))
        {
            QuadTree = new(
                new Vector2(winWidthMeters * 0.5f, winHeightMeters * 0.5f),
                new Vector2(winWidthMeters, winHeightMeters),
                1,
                6
            )
        };
        ctx.LineColliders = new() {
            new(0f, 0f, winWidthMeters, 0f, ctx),
            new(0f, 0f, 0f, winHeightMeters, ctx),
            new(winWidthMeters, 0f, winWidthMeters, winHeightMeters, ctx),
            new(0f, winHeightMeters, winWidthMeters, winHeightMeters, ctx)
        };
        ctx.SaveCurrentState();
        // Load textures
        TextureManager.LoadTexture("center_of_mass.png");

        // Start quad tree update thread
        var quadTreeUpdateThread = new Thread(new ParameterizedThreadStart(QuadTree.ThreadUpdate), 0)
        {
            IsBackground = true
        };
        quadTreeUpdateThread.Start(ctx);
        return new Scene() { Ctx = ctx };
    }

    private static void Update()
    {
        ActiveScene.Update();
    }

    private static void Draw()
    {
        BeginDrawing(); // raylib
        rlImGui.Begin(); // GUI

        BeginTextureMode(RenderTexture);
        ClearBackground(Color.Black);
        ActiveScene.Draw();
        EndTextureMode();

        DrawTexturePro(
            RenderTexture.Texture,
            new (0f, 0f, RenderTexture.Texture.Width, -RenderTexture.Texture.Height),
            new (0f, 0f, ActiveScene.Ctx.WinSize.X, ActiveScene.Ctx.WinSize.Y),
            Vector2.Zero,
            0f,
            Color.White
        );
        Gui.Draw(ActiveScene.Ctx); // GUI

        rlImGui.End();
        EndDrawing(); // raylib
    }
}