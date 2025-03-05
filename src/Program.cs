using System.Numerics;
using Raylib_cs;
using rlImGui_cs;
using PointMasses.UI;
using static Raylib_cs.Raylib;
using ImGuiNET;
using PointMasses.Textures;
using PointMasses.Input;

namespace PointMasses.Sim;

public class Program 
{   
    public static readonly int TargetFPS = GetMonitorRefreshRate(GetCurrentMonitor());
    
    public static TextureManager TextureManager { get; set; }

    private static bool _inMenu = true;
    private static bool _shouldExit;
    private static Scene _activeScene;
    private static RenderTexture2D RenderTexture { get; set;}

    public static void Main() 
    {
        Initialize(0.8f);
        TextureManager = new();
        rlImGui.Setup(true);
        unsafe { ImGui.GetIO().NativePtr->IniFilename = null; } // Disable imgui.ini file
        ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg] = new (0.1f, 0.1f, 0.1f, 1f); // Set popup bg color to opaque black

        while (!_shouldExit && !WindowShouldClose())
        {
            if (!_inMenu)
            {
                _activeScene.Update();
                InputManager.HandleInput(_activeScene.Ctx);
            }
            Draw();
        }
        _activeScene?.Destroy();
        TextureManager.Dispose();
        rlImGui.Shutdown();
        UnloadRenderTexture(RenderTexture);
        CloseWindow();
    }

    private static void Initialize(float winSizePercentage)
    {
        InitWindow(0, 0, "Point-masses");
        SetTargetFPS(TargetFPS);
        SetWinSizePercentage(winSizePercentage);
    }

    private static void Draw()
    {
        BeginDrawing(); // raylib
        rlImGui.Begin(); // GUI
        ClearBackground(Color.Black);

        if (_inMenu) // Selecting a scene
        {
            Gui.DrawMainMenu(ref _inMenu, ref _shouldExit, ref _activeScene);
            rlImGui.End();
            EndDrawing();
            return;
        }
        BeginTextureMode(RenderTexture);
        ClearBackground(Color.Black);
        _activeScene.Draw();
        EndTextureMode();

        DrawTexturePro(
            RenderTexture.Texture,
            new (0f, 0f, RenderTexture.Texture.Width, -RenderTexture.Texture.Height),
            new (0f, 0f, _activeScene.Ctx.WinSize.X, _activeScene.Ctx.WinSize.Y),
            Vector2.Zero,
            0f,
            Color.White
        );
        Gui.Draw(_activeScene, ref _inMenu); // GUI
       
        rlImGui.End();
        EndDrawing(); // raylib
    }

    public static void SetWinSizePercentage(float winSizePercentage)
    {
        UnloadRenderTexture(RenderTexture);
        int winWidth = (int) (winSizePercentage * GetMonitorWidth(GetCurrentMonitor()));
        int winHeight = (int) (winSizePercentage * GetMonitorHeight(GetCurrentMonitor()));
        RenderTexture = LoadRenderTexture(winWidth, winHeight);
        SetWindowSize(winWidth, winHeight);
        int winPosX = GetMonitorWidth(GetCurrentMonitor()) / 2 - winWidth / 2;
        int winPosY = GetMonitorHeight(GetCurrentMonitor()) / 2 - winHeight / 2;
        SetWindowPosition(winPosX, winPosY);
    }
}