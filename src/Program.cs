using System.Numerics;
using PointMasses.Entities;
using Raylib_cs;
using rlImGui_cs;
using PointMasses.UI;
using PointMasses.Utils;
using static Raylib_cs.Raylib;
using ImGuiNET;
using PointMasses.Textures;
using PointMasses;
using PointMasses.Input;

namespace PointMasses.Sim;

public class Program 
{   
    public static readonly int TargetFPS = GetMonitorRefreshRate(GetCurrentMonitor());

    public static TextureManager TextureManager { get; set; }
    public static Vector2 WinSize { get; private set; }

    private static Scene ActiveScene { get; set; }
    private static RenderTexture2D RenderTexture { get; set;}

    public static void Main() 
    {
        Initialize(0.8f, 1.0f);
        TextureManager = new();
        ActiveScene = Scene.LoadDefaultScene(WinSize);
        //ActiveScene = Scene.LoadFromFile("scenes/Default_scene.json");
        rlImGui.Setup(true);
        unsafe { ImGui.GetIO().NativePtr->IniFilename = null; } // Disable imgui.ini file
        while (!WindowShouldClose())
        {
            ActiveScene.Update();
            InputManager.HandleInput(ActiveScene.Ctx);
            Draw();
        }
        TextureManager.Dispose();
        rlImGui.Shutdown();
        CloseWindow();
    }

    private static void Initialize(float winSizePercentage, float renderPercentage)
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
        WinSize = new(winWidth, winHeight);
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