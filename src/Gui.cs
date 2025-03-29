using System.Numerics;
using PointMasses.Editing;
using PointMasses.Entities;
using ImGuiNET;
using PointMasses.Systems;
using PointMasses.Sim;
using PointMasses.Tools;
using static Raylib_cs.Raylib;
using static PointMasses.Tools.Spawn;
using Raylib_cs;
using System.Text;
using PointMasses.Input;
using PointMasses.Utils;

namespace PointMasses.UI;

public class Gui
{
    private const float MainMenuWidth = 300f;
    private const float MainMenuHeight = 350f;

    private struct State
    {
        public bool _showSystemEnergy;
        public bool _showConfirmDialog;
        public bool _showSavePopup;
        public bool _showScenesPopup;
        public bool _showMenuSettingsPopup;
        public bool _useWinPercentage;
        public bool _preserveAspectRatio;
        public int _winWidth;
        public int _winHeight;
        public float _winSizePercentage;
        public string _sceneNameInputstr;
        public List<string> _savedScenes;

        public State()
        {    
            _sceneNameInputstr = string.Empty;
            _savedScenes = new(GetSceneFileNames());
        }
    }

    private static State _state = new();

    public static void Draw(ref Scene activeScene, ref bool inMenu)
    {
        var ctx = activeScene.Ctx;
        var toolSystem = ctx.GetSystem<ToolSystem>();
        toolSystem.ToolEnabled = !ImGui.IsAnyItemHovered();
        if (ImGui.BeginMainMenuBar())
        {
            int fps = GetFPS();
            if (fps < 60)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255f, 0f, 0f, 255f));
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 255f, 0f, 255f));
            }
            ImGui.Text($"FPS: {fps}");
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255f, 255f, 0f, 255f));
            ImGui.Text($"Selected tool: {toolSystem.SelectedTool.GetType().ToString().Split('.').LastOrDefault()}");
            ImGui.PopStyleColor();
            ImGui.Spacing();
            if (ImGui.BeginMenu("File"))
            {
                ShowFileMenu(ref inMenu, ref activeScene);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Simulation info"))
            {
                ShowSimulationInfo(activeScene);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Tools"))
            {
                ShowTools(ctx, toolSystem);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Camera"))
            {
                ShowCameraSettings(ctx);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("N-Body Sim"))
            {
                ShowNbodySimOptions(ctx);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Shortcuts"))
            {
                ShowShortcuts();
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();
        }
    }

    private static void ShowFileMenu(ref bool inMenu, ref Scene activeScene)
    {
        if (ImGui.Button("Save scene"))
        {
            _state._showSavePopup = true;
            _state._sceneNameInputstr = activeScene.Name;
            InputManager.InputEnabled = false;
            ImGui.OpenPopup("Save scene");
        }
        if (ImGui.BeginPopupModal("Save scene", ref _state._showSavePopup, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.InputText("Scene name", ref _state._sceneNameInputstr, 100);
            ImGui.Spacing();
            if (ImGui.Button("Save"))
            {
                activeScene.Name = _state._sceneNameInputstr;
                activeScene.SaveToFile();
                _state._savedScenes.Clear();
                _state._savedScenes.AddRange(GetSceneFileNames());
                InputManager.InputEnabled = true;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                InputManager.InputEnabled = true;
                ImGui.CloseCurrentPopup();
            }   
            ImGui.EndPopup();
        }
        if (ImGui.Button("Load scene"))
        {
            ImGui.OpenPopup("Load scene");
        }
        if (ImGui.BeginPopup("Load scene"))
        {
            ShowScenesMenu(ref inMenu, ref activeScene);
            ImGui.EndPopup();
        }
        if (ImGui.Button("Main menu"))
        {
            _state._showConfirmDialog = true;
            activeScene.Ctx._simPaused = true;
            ImGui.SetNextWindowPos(new(GetScreenWidth() * 0.5f, GetScreenHeight() * 0.5f));
            ImGui.OpenPopup("Return to main menu");
        }
        if (ImGui.BeginPopupModal("Return to main menu", ref _state._showConfirmDialog, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
        {
            ImGui.Text("Are you sure you want to return to main menu?");
            ImGui.Spacing();

            if (ImGui.Button("Yes"))
            {
                inMenu = true;
                activeScene.Destroy();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                activeScene.Ctx._simPaused = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private static void ShowSimulationInfo(Scene activeScene)
    {
        var ctx = activeScene.Ctx;
        ImGui.PushStyleColor(ImGuiCol.Text, ctx._simPaused ? new Vector4(0f, 255f, 0f, 255f) : new Vector4(255f, 0f, 0f, 255f));
        if (ImGui.Button(ctx._simPaused ? "START" : "STOP")) {
            ctx._simPaused = !ctx._simPaused;
            if (ctx._simPaused)
            {
                ctx.GetSystem<NbodySystem>()._running = false;
            }
        }
        ImGui.PopStyleColor();
        ImGui.PushStyleColor(ImGuiCol.Text, ctx._simPaused ? new Vector4(255f, 0f, 0f, 255f) : new Vector4(0f, 255f, 0f, 255f));
        ImGui.SameLine();
        ImGui.Text($"Sim {(ctx._simPaused ? "paused" : "running")}");
        ImGui.PopStyleColor();
        ImGui.Text($"Active scene: {activeScene.Name}");
        ImGui.Text($"Masses: {ctx.MassCount}");
        ImGui.Text($"Constraints: {ctx.ConstraintCount}");
        ImGui.Text($"Shapes: {ctx.MassShapes.Count}");
        ImGui.Text($"Substeps: {ctx._substeps}");
        ImGui.Text($"Step: { ctx._timestep * 1e3f:0.0000} ms");
        ImGui.Text($"Substep: {ctx.Substep * 1e3f:0.0000} ms");
        if (_state._showSystemEnergy)
        {
            ImGui.Text($"System energy: {ctx.SystemEnergy / 1e3f:0.###} kJ");
            if (ImGui.Button("Hide system energy"))
            {
                _state._showSystemEnergy = false;
            }
        } else if (ImGui.Button("Show system energy"))
        {
            _state._showSystemEnergy = true;
        }
        ImGui.Checkbox("Gravity", ref ctx._gravityEnabled);
        ImGui.Checkbox("Draw forces", ref ctx._drawForces);
        ImGui.Checkbox("Draw AABBs", ref ctx._drawAABBS);
        ImGui.Checkbox("Draw quadtree", ref ctx._drawQuadTree);
        if (ImGui.Checkbox("Collisions enabled", ref ctx._collisionsEnabled))
        {
            if (ctx._collisionsEnabled && !ctx._simPaused)
            {
                QuadTree.ResumeEvent.Set();
            }
            else
            {
                QuadTree.ResumeEvent.Reset();
            }
        }
        ImGui.PushItemWidth(50f);
        ImGui.InputFloat("Global restitution coeff", ref ctx._globalRestitutionCoeff);
        ImGui.InputFloat("Global kinetic friction coeff", ref ctx._globalKineticFrictionCoeff);
        ImGui.InputFloat("Global static friction coeff", ref ctx._globalStaticFrictionCoeff);
        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.Button("Delete all shapes"))
        {
            ctx.Lock.EnterWriteLock();
            Entity.ResetIdCounter();
            foreach (var shape in ctx.MassShapes)
            {
                shape._toBeDeleted = true;
            }
            ctx.Lock.ExitWriteLock();
        }
        if (ImGui.Button("Save snapshot"))
        {
            ctx.SaveCurrentState();
        }
        if (ctx.SavedShapeCount > 0)
        {
            if (ImGui.Button($"Load snapshot ({ctx.SavedShapeCount} shapes saved)"))
            {
                ctx.LoadSnapshot();
            }
        }
        if (ImGui.Button("Timestep settings"))
        {
            ImGui.OpenPopup("TimestepSettings");
        }
        if (ImGui.BeginPopup("TimestepSettings"))
        {
            ctx._simPaused = true;
            ImGui.Text("Substep settings");
            ImGui.InputFloat("Timestep [s]", ref ctx._timestep);
            ImGui.InputInt("Substep count", ref ctx._substeps);
            if (ImGui.Button("Close"))
            {
                ctx.SetTimestep(null, null);
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private static void ShowTools(Context ctx, ToolSystem toolSystem)
    {
        ImGui.PushItemWidth(100f);
        if (ImGui.Combo("Tool", ref toolSystem._selectedToolIndex, ToolSystem.ToolComboString))
        {
            toolSystem.ChangeToolType();
        }
        ImGui.Separator();
        ImGui.Spacing();
        switch (toolSystem.SelectedTool)
        {
            case Spawn :
                ShowSpawnToolOptions(ctx);
                break;
            case Editor :
                ShowEditorOptions(ctx);
                break;
            case Pull :
                ImGui.Text("Pulls multiple point masses towards the cursor");
                ImGui.Spacing();
                ImGui.InputFloat("Force", ref ((Pull) toolSystem.SelectedTool)._force);
                break;
            case PullCom :
                ImGui.Text("Pulls the center of mass of one shape towards the cursor");
                ImGui.Spacing();
                ImGui.InputFloat("Force", ref ((PullCom) toolSystem.SelectedTool)._force);
                break;
            case GravityWell :
                ImGui.InputFloat("Gravitational constant", ref ((GravityWell) toolSystem.SelectedTool)._gravConstant);
                ImGui.InputFloat("Minimum distance", ref ((GravityWell) toolSystem.SelectedTool)._minDist);
                break;
            default :
                throw new Exception("Unknown tool type selected");
        }
    }
    
    private static void ShowSpawnToolOptions(Context ctx)
    {
        var toolSystem = ctx.GetSystem<ToolSystem>();
        var spawnTool = (Spawn) toolSystem.SelectedTool;
        if (ImGui.Combo("Spawn target", ref spawnTool._selectedSpawnTargetIndex, TargetsToComboString()))
        {
            spawnTool.UpdateSpawnPreview();
        }
        if (ImGui.InputFloat("Mass", ref spawnTool._mass))
        {
            spawnTool._mass = MathF.Abs(spawnTool._mass);
            spawnTool.UpdateSpawnPreview();
        }
        if (spawnTool._currentTarget == SpawnTarget.Ball || spawnTool._currentTarget == SpawnTarget.SoftBall)
        {
            if (ImGui.InputInt("Resolution", ref spawnTool._resolution))
            {
                spawnTool._resolution = Math.Abs(spawnTool._resolution);
                spawnTool.UpdateSpawnPreview();
            }
        }
        if (spawnTool._currentTarget == SpawnTarget.SoftBox || spawnTool._currentTarget == SpawnTarget.SoftBall)
        {
            if (ImGui.InputFloat("Stiffness", ref spawnTool._stiffness))
            {
                spawnTool._stiffness = MathF.Abs(spawnTool._stiffness);
                spawnTool.UpdateSpawnPreview();
            }
            if (spawnTool._currentTarget == SpawnTarget.SoftBall)
            {
                if (ImGui.InputFloat("Gas amount", ref spawnTool._gasAmount))
                {
                    spawnTool._gasAmount = MathF.Abs(spawnTool._gasAmount);
                    spawnTool.UpdateSpawnPreview();
                }
            }
        }
    }

    private static void ShowEditorOptions(Context ctx)
    {
        var toolSystem = ctx.GetSystem<ToolSystem>();
        ImGui.Text("EDITOR OPTIONS");
        ImGui.Spacing();
        var editor = (Editor) toolSystem.SelectedTool;
        if (ImGui.InputInt("Points per meter", ref editor._grid._pointsPerMeter))
        {
            editor._grid._pointsPerMeter = Math.Max(editor._grid._pointsPerMeter, 1);
            editor._grid.SetGridScale(editor._grid._pointsPerMeter, ctx.WinSize);
        }
        ImGui.Combo("Editor action", ref editor._selectedActionIndex, editor.ActionComboString);
        if (editor.SelectedAction == Editor.EditorAction.CreateLoop)
        {
            ImGui.Checkbox("Connect ends", ref editor._connectLoop);
            if (editor._connectLoop)
            {
                ImGui.Checkbox("Inflate", ref editor._inflateLoop);
                if (editor._inflateLoop)
                {
                    ImGui.InputFloat("Gas amount", ref editor._gasAmount);
                }
            }
        }
        if (editor.SelectedAction == Editor.EditorAction.CreateLoop || 
            editor.SelectedAction == Editor.EditorAction.Freeform
        )
        {
            ImGui.InputFloat("Stiffness", ref editor._stiffness);
        }
        if (ImGui.Button("Clear selected points"))
        {
            editor._grid.ClearSelectedPoints();
        }
        if (ImGui.Button("Build shape"))
        {
            editor.BuildShape();
        }
    }

    private static void ShowNbodySimOptions(Context ctx)
    {
        var nBodySystem = ctx.GetSystem<NbodySystem>();
        ImGui.Checkbox("Running", ref nBodySystem._running);
        ImGui.Checkbox("Post-Newtonian relativistic corrections", ref nBodySystem._postNewtonianEnabled);
        ImGui.PushItemWidth(50f);
        ImGui.InputFloat("Gravitational constant", ref nBodySystem._gravConstant);
        ImGui.InputFloat("Minimum distance", ref nBodySystem._minDist);
        ImGui.InputFloat("Threshold", ref nBodySystem._threshold);
        ImGui.InputInt("Update interval ms", ref nBodySystem._updateIntervalMs, 0, 10);
        ImGui.PopItemWidth();
    }

    private static void ShowCameraSettings(Context ctx)
    {
        ImGui.Text("Camera settings");
        ImGui.Text($"Current offset: ({ctx._camera.Offset.X:0}, {ctx._camera.Offset.Y:0})");
        ImGui.PushItemWidth(50f);
        ImGui.InputFloat("Move speed", ref ctx._cameraMoveSpeed);
        ImGui.PopItemWidth();
        if (ImGui.Button("Reset camera"))
        {
            ctx._camera.Offset = Vector2.Zero;
        }
    }

    public static void DrawMainMenu(ref bool _inMenu, ref Scene activeScene)
    {
        const string title = "Point Masses";
        const int titleSize = 40;
        int width = MeasureText(title, titleSize);
        DrawText(title, (int) (GetScreenWidth() * 0.5f - width * 0.5), (int) (0.10f * GetScreenHeight()), titleSize, Color.White);
        var winFlags = 
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove;
        ImGui.Begin("Main menu", winFlags);
        Vector2 winSize = new(MainMenuWidth, MainMenuHeight);
        ImGui.SetWindowSize(winSize);
        ImGui.SetWindowPos(new(GetScreenWidth() * 0.5f - winSize.X * 0.5f, GetScreenHeight() * 0.5f - winSize.Y * 0.5f));
        Vector2 buttonSize = new(100, 30);
        Vector2 buttonPos = new((winSize.X - buttonSize.X) * 0.5f, buttonSize.Y);
        ImGui.SetCursorPos(buttonPos);
        if (ImGui.Button("New scene", buttonSize))
        {
            activeScene = Scene.LoadEmptyScene(new(GetScreenWidth(), GetScreenHeight()));
            activeScene.Init();
            _inMenu = false;
        }
        buttonPos.Y += 35;
        ImGui.SetCursorPos(buttonPos);
        if (ImGui.Button("Scenes", buttonSize))
        {
            _state._showScenesPopup = true;
            ImGui.OpenPopup("Scenes");
        }
        if (ImGui.BeginPopupModal("Scenes", ref _state._showScenesPopup, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ShowScenesMenu(ref _inMenu, ref activeScene);
            ImGui.EndPopup();
        }
        buttonPos.Y += 35;
        ImGui.SetCursorPos(buttonPos);
        if (ImGui.Button("Settings", buttonSize))
        {
            _state._showMenuSettingsPopup = true;
            _state._winWidth = GetScreenWidth();
            _state._winHeight = GetScreenHeight();
            _state._winSizePercentage = _state._winWidth / (float) GetMonitorWidth(GetCurrentMonitor()) * 100f;
            ImGui.OpenPopup("Settings");
        }
        if (ImGui.BeginPopupModal("Settings", ref _state._showMenuSettingsPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            var popUpSize = ImGui.GetWindowSize();
            ImGui.SetWindowPos(new(GetScreenWidth() * 0.5f - popUpSize.X * 0.5f, GetScreenHeight() * 0.5f));
            ShowMainMenuSettings();
            ImGui.EndPopup();
        }
        buttonPos.Y += 35;
        ImGui.SetCursorPos(buttonPos);
        if (ImGui.Button("Exit", buttonSize))
        {
            Program.Shutdown();
        }
        ImGui.Spacing();
        ImGui.BeginChild("Shortcuts");
        ShowShortcuts();
        ImGui.EndChild();
        ImGui.End();
    }

    private static void ShowShortcuts()
    {
        ImGui.SeparatorText("SHORTCUTS");
        ImGui.Spacing();
        ImGui.BulletText("Esc - exit application");
        ImGui.BulletText("Space - start/pause simulation");
        ImGui.BulletText("B - load n-body benchmark");
        ImGui.BulletText("C - load cloth scenario");
        ImGui.BulletText("G - toggle gravity");
        ImGui.BulletText("F - show forces");
        ImGui.BulletText("R - load snapshot");
        ImGui.SeparatorText("CAMERA CONTROLS");
        ImGui.Spacing();
        ImGui.BulletText("W - up");
        ImGui.BulletText("A - left");
        ImGui.BulletText("S - down");
        ImGui.BulletText("D - right");
    }

    private static void ShowMainMenuSettings()
    {
        var currentAspectRatio = GetMonitorWidth(GetCurrentMonitor()) / (float) GetMonitorHeight(GetCurrentMonitor());
        ImGui.SeparatorText("Window settings");
        ImGui.SetNextItemWidth(50f);

        ImGui.Checkbox("Use screen size percentage", ref _state._useWinPercentage);
        
        if (!_state._useWinPercentage)
        {
            ImGui.BeginDisabled();
        }
        ImGui.SetNextItemWidth(100f);
        if (ImGui.InputFloat("Window size % of screen size", ref _state._winSizePercentage, 10, 30, "%.1f"))    
        {
            _state._winSizePercentage = MathF.Min(100.0f, MathF.Max(Constants.MinWindowSizePercentage, _state._winSizePercentage));
        }
        if (!_state._useWinPercentage)
        {
            ImGui.EndDisabled();
        }
        else
        {
            ImGui.BeginDisabled();
        }
        ImGui.Spacing();
        ImGui.SetNextItemWidth(100f);
        if (ImGui.InputInt("Window width [px]", ref _state._winWidth, 100, 500))
        {
            _state._winWidth = Math.Max(Constants.MinWindowWidth, _state._winWidth);
            if (_state._preserveAspectRatio)
            {
                _state._winHeight = Math.Max(Constants.MinWindowHeight, (int) (_state._winWidth / currentAspectRatio));
            }
        }
        ImGui.SetNextItemWidth(100f);
        if (ImGui.InputInt("Window height [px]", ref _state._winHeight, 100, 500))
        {
            _state._winHeight = Math.Max(Constants.MinWindowHeight, _state._winHeight);
            if (_state._preserveAspectRatio)
            {
                _state._winWidth = Math.Max(Constants.MinWindowWidth, (int) (_state._winHeight * currentAspectRatio));
            }
        }
        if (ImGui.Checkbox("Preserve aspect ratio", ref _state._preserveAspectRatio))
        {
            if (_state._preserveAspectRatio)
            {
                _state._winHeight = Math.Max(Constants.MinWindowHeight, (int) (_state._winWidth / currentAspectRatio));
            }
        }
        
        if (_state._useWinPercentage)
        {
            ImGui.EndDisabled();
        }

        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Button("Apply"))
        {   
            if (_state._useWinPercentage && _state._winSizePercentage >= Constants.MinWindowSizePercentage)
            {
                Program.SetWinSize(null, _state._winSizePercentage / 100f, null, null);
            }
            if (!_state._useWinPercentage)
            {
                Program.SetWinSize(null, null, _state._winWidth, _state._winHeight);
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Close"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    private static void ShowScenesMenu(ref bool _inMenu, ref Scene activeScene)
    {
        ImGui.BeginChild("SceneList", new (Constants.MinWindowWidth * 0.6f, Constants.MinWindowHeight * 0.6f));
        for (int i = 0; i < _state._savedScenes.Count; i++)
        {
            var fullSceneName = _state._savedScenes[i].AsSpan();
            var sceneName = fullSceneName[..fullSceneName.IndexOf('.')];
            ImGui.SeparatorText($"Scene: {sceneName}");
            if (ImGui.Button($"Load ##{i}"))
            {
                activeScene?.Destroy();
                activeScene = Scene.LoadFromFile(Path.Combine("scenes", fullSceneName.ToString()));
                activeScene.Init();
                _inMenu = false;
            }
            ImGui.SameLine();
            if (ImGui.Button($"Delete ##{i}"))
            {
                ImGui.OpenPopup($"Delete scene: {sceneName}");
            }
            if (ImGui.BeginPopupModal($"Delete scene: {sceneName}"))
            {
                ImGui.Text("Are you sure you want to delete this scene?");
                if (ImGui.Button($"Yes ##{i}"))
                {
                    try 
                    {
                        File.Delete(Path.Combine("scenes", fullSceneName.ToString()));
                        AsyncLogger.Info($"Deleted scene: {fullSceneName}");
                        _state._savedScenes.Remove(fullSceneName.ToString());
                    }
                    catch (Exception e)
                    {
                        AsyncLogger.Error($"Could not delete scene: {e.Message}");
                    }
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button($"No ##{i}"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        if (_state._savedScenes.Count == 0)
        {
            ImGui.Text("No saved scenes");
        }
        ImGui.EndChild();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();
        if (ImGui.Button("Close"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    private static string[] GetSceneFileNames()
    {
        var scenePaths = Directory.GetFiles("scenes", "*.json", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < scenePaths.Length; i++)
        {
            scenePaths[i] = Path.GetFileName(scenePaths[i]);
        }
        return scenePaths;
    }
}
