using System.Numerics;
using Editing;
using Entities;
using ImGuiNET;
using PointMasses.Systems;
using Sim;
using Tools;
using static Raylib_cs.Raylib;
using static Tools.Spawn;

namespace UI;

public class Gui
{
    
    private static bool ShowSystemEnergy;

    public static void Draw(Context ctx)
    {
        var toolSystem = (ToolSystem) ctx.GetSystem(typeof(ToolSystem));
        toolSystem.ToolEnabled = !ImGui.IsAnyItemHovered();
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Simulation info"))
            {
                ShowSimulationInfo(ctx, toolSystem);
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
            ImGui.EndMainMenuBar();
        }
    }

    private static void ShowSimulationInfo(Context ctx, ToolSystem toolSystem)
    {
        ImGui.Text(string.Format("FPS: {0}", GetFPS()));
        ImGui.PushStyleColor(ImGuiCol.Text, ctx._simPaused ? new Vector4(255f, 0f, 0f, 255f) : new Vector4(0f, 255f, 0f, 255f));
        if (ImGui.Checkbox(ctx._simPaused ? "PAUSE" : "RUNNING", ref ctx._simPaused))
        {
            ((NbodySystem) ctx.GetSystem(typeof(NbodySystem))).PauseEvent.Reset();
        }
        ImGui.PopStyleColor();
        ImGui.Text($"Masses: {ctx.MassCount}");
        ImGui.Text($"Constraints: {ctx.ConstraintCount}");
        ImGui.Text($"Shapes: {ctx.MassShapes.Count}");
        ImGui.Text($"Substeps: {ctx._substeps}");
        ImGui.Text($"Step: { ctx._timestep * 1e3f:0.0000} ms");
        ImGui.Text($"Substep: {ctx.Substep * 1e3f:0.0000} ms");
        if (ShowSystemEnergy)
        {
            ImGui.Text(string.Format("System energy: {0:0.###} kJ", ctx.SystemEnergy / 1e3f));
            if (ImGui.Button("Hide system energy"))
            {
                ShowSystemEnergy = false;
            }
        } else if (ImGui.Button("Show system energy"))
        {
            ShowSystemEnergy = true;
        }
        ImGui.Checkbox("Gravity", ref ctx._gravityEnabled);
        ImGui.Checkbox("Draw forces", ref ctx._drawForces);
        ImGui.Checkbox("Draw AABBs", ref ctx._drawAABBS);
        ImGui.Checkbox("Draw quadtree", ref ctx._drawQuadTree);
        if (ImGui.Checkbox("Collisions enabled", ref ctx._collisionsEnabled))
        {
            if (ctx._collisionsEnabled && !ctx._simPaused)
            {
                ctx.QuadTree.PauseEvent.Set();
            }
            else
            {
                ctx.QuadTree.PauseEvent.Reset();
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
        if (ImGui.Button("Save current state"))
        {
            ctx.SaveCurrentState();
        }
        if (ctx.SavedShapeCount > 0)
        {
            if (ImGui.Button($"Load saved state ({ctx.SavedShapeCount} shapes saved)"))
            {
                ctx.LoadSavedState();
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
                ctx.UpdateSubstep();
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
                ImGui.InputFloat("Force coefficient", ref ((Pull) toolSystem.SelectedTool)._forceCoeff);
                break;
            case PullCom :
                ImGui.InputFloat("Force coefficient", ref ((PullCom) toolSystem.SelectedTool)._forceCoeff);
                break;
            case GravityWell :
                ImGui.InputFloat("Gravitational constant", ref ((GravityWell) toolSystem.SelectedTool)._gravConstant);
                ImGui.InputFloat("Minimum distance", ref ((GravityWell) toolSystem.SelectedTool)._minDist);
                break;
        }
    }
    
    private static void ShowSpawnToolOptions(Context ctx)
    {
        var toolSystem = (ToolSystem) ctx.GetSystem(typeof(ToolSystem));
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
        var toolSystem = (ToolSystem) ctx.GetSystem(typeof(ToolSystem));
        ImGui.Text("EDITOR OPTIONS");
        ImGui.Spacing();
        var editor = (Editor) toolSystem.SelectedTool;
        if (ImGui.InputInt("Points per meter", ref editor._grid._pointsPerMeter))
        {
            editor._grid._pointsPerMeter = Math.Max(editor._grid._pointsPerMeter, 1);
            editor._grid.SetGridScale(editor._grid._pointsPerMeter);
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
        var nBodySystem = (NbodySystem) ctx.GetSystem(typeof(NbodySystem));
        ImGui.Checkbox("Running", ref nBodySystem._running);
        ImGui.Checkbox("Post-Newtonian relativistic corrections", ref nBodySystem._postNewtonianEnabled);
        ImGui.InputFloat("Gravitational constant", ref nBodySystem._gravConstant);
        ImGui.InputFloat("Minimum distance", ref nBodySystem._minDist);
        ImGui.InputFloat("Threshold", ref nBodySystem._threshold);
    }

    private static void ShowCameraSettings(Context ctx)
    {
        ImGui.Text("Camera settings");
        ImGui.Text(string.Format("Current offset: ({0:0}, {1:0})", ctx.Camera.Offset.X, ctx.Camera.Offset.Y));
        ImGui.PushItemWidth(50f);
        ImGui.InputFloat("Move speed", ref ctx.Camera._moveSpeed);
        ImGui.PopItemWidth();
        if (ImGui.Button("Reset camera"))
        {
            ctx.Camera.Reset();
        }
    }
}
