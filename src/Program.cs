using System.Numerics;
using Raylib_cs;
using rlImGui_cs;
using PointMasses.UI;
using static Raylib_cs.Raylib;
using ImGuiNET;
using PointMasses.Textures;
using PointMasses.Input;
using PointMasses.Utils;

namespace PointMasses.Sim;

public class Program 
{   
    public static readonly int TargetFPS = GetMonitorRefreshRate(GetCurrentMonitor());
    
    public static TextureManager TextureManager { get; set; }
    public static float RenderSizePercentage { get; set; }

    private static bool _inMenu = true;
    private static bool _shouldExit;
    private static Scene _activeScene;
    private static RenderTexture2D _renderTexture;

    public static void Main() 
    {
        Initialize(0.8f);
        while (!_shouldExit && !WindowShouldClose())
        {
            if (!_inMenu)
            {
                _activeScene.Update();
                InputManager.HandleInput(_activeScene.Ctx);
            }
            Draw();
        }
        CleanUp();
        CloseWindow();
    }

    private static void Initialize(float winSizePercentage)
    {
        InitWindow(0, 0, "Point-masses");
        SetTargetFPS(TargetFPS);
        SetWinSize(null, winSizePercentage, null, null);
        #if DEBUG
            AsyncLogger.Info("Point-masses is running in DEBUG mode");
            SetTraceLogLevel(TraceLogLevel.Debug);
        #else
            AsyncLogger.Info("Point-masses is running in RELEASE mode");
            SetTraceLogLevel(TraceLogLevel.Info);
        #endif
        if (!Directory.Exists("scenes"))
        {
            AsyncLogger.Warn("No scenes directory found, creating one");
            Directory.CreateDirectory("scenes");
        }
        TextureManager = new();
        rlImGui.Setup(true);
        unsafe { ImGui.GetIO().NativePtr->IniFilename = null; } // Disable imgui.ini file
        ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg] = new (0.1f, 0.1f, 0.1f, 1f); // Set popup bg color to opaque black
    }

    private static void CleanUp()
    {
        _activeScene?.Destroy();
        TextureManager.Dispose();
        rlImGui.Shutdown();
        UnloadRenderTexture(_renderTexture);
    }

    private static void Draw()
    {
        BeginDrawing(); // raylib
        rlImGui.Begin(); // GUI
        ClearBackground(Color.Black);

        if (_inMenu) // Selecting a scene
        {
            Gui.DrawMainMenu(ref _inMenu, ref _activeScene);
            rlImGui.End();
            EndDrawing();
            return;
        }
        BeginTextureMode(_renderTexture);
        ClearBackground(Color.Black);
        _activeScene.Draw();
        EndTextureMode();

        DrawTexturePro(
            _renderTexture.Texture,
            new (0f, 0f, _renderTexture.Texture.Width, -_renderTexture.Texture.Height),
            new (0f, 0f, GetScreenWidth(), GetScreenHeight()),
            Vector2.Zero,
            0f,
            Color.White
        );
        Gui.Draw(ref _activeScene, ref _inMenu); // GUI
       
        rlImGui.End();
        EndDrawing(); // raylib
    }

    public static void SetWinSize(float? renderSizePercentage, float? winSizePercentage, int? width, int? height)
    {
        UnloadRenderTexture(_renderTexture);
        int winWidth;
        int winHeight;
        if (winSizePercentage.HasValue)
        {
            winWidth = (int) (winSizePercentage.Value * GetMonitorWidth(GetCurrentMonitor()));
            winHeight = (int) (winSizePercentage.Value * GetMonitorHeight(GetCurrentMonitor()));
        }
        else
        {
            winWidth = width.Value;
            winHeight = height.Value;
        }
        if (renderSizePercentage.HasValue)
        {
            RenderSizePercentage = renderSizePercentage.Value;
            _renderTexture = LoadRenderTexture((int) (renderSizePercentage.Value * winWidth), (int) (renderSizePercentage.Value * winHeight));
        }
        else
        {
            RenderSizePercentage = 1f;
            _renderTexture = LoadRenderTexture(winWidth, winHeight);
        }
        SetWindowSize(winWidth, winHeight);
        int winPosX = GetMonitorWidth(GetCurrentMonitor()) / 2 - winWidth / 2;
        int winPosY = GetMonitorHeight(GetCurrentMonitor()) / 2 - winHeight / 2;
        SetWindowPosition(winPosX, winPosY);
    }

    public static void Shutdown() => _shouldExit = true;
}