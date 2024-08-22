using System.Numerics;
using Editing;
using ImGuiNET;
using Sim;
using Systems;
using Tools;
using static Raylib_cs.Raylib;
using static Tools.Spawn;

namespace UI;

public class Gui
{
    
    public static void Draw(Context ctx)
    {
        ToolSystem toolSystem = ctx.GetSystem<ToolSystem>(Context.SystemsEnum.ToolSystem);
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
        ImGui.Checkbox(ctx._simPaused ? "PAUSE" : "RUNNING", ref ctx._simPaused);
        ImGui.PopStyleColor();
        ImGui.Text(string.Format("Masses: {0}", ctx.MassCount));
        ImGui.Text(string.Format("Constraints: {0}", ctx.ConstraintCount));
        ImGui.Text(string.Format("Shapes: {0}", ctx.MassShapes.Count));
        ImGui.Text(string.Format("Substeps: {0}", ctx.Substeps));
        ImGui.Text(string.Format("Step: {0:0.0000} ms", ctx.TimeStep * 1e3f));
        ImGui.Text(string.Format("Substep: {0:0.0000} ms", ctx.SubStep * 1e3f));
        if (ctx._drawBodyInfo)
        {
            ImGui.Text(string.Format("System energy: {0} kJ", ctx.SystemEnergy / 1e3f));
        }
        ImGui.Checkbox("Gravity", ref ctx._gravityEnabled);
        ImGui.Checkbox("Draw forces", ref ctx._drawForces);
        ImGui.Checkbox("Draw AABBs", ref ctx._drawAABBS);
        ImGui.Checkbox("Draw quadtree", ref ctx._drawQuadTree);
        ImGui.Checkbox("Draw body info", ref ctx._drawBodyInfo);
        ImGui.PushItemWidth(50f);
        ImGui.InputFloat("Global restitution coeff", ref ctx._globalRestitutionCoeff);
        ImGui.InputFloat("Global kinetic friction coeff", ref ctx._globalKineticFrictionCoeff);
        ImGui.InputFloat("Global static friction coeff", ref ctx._globalStaticFrictionCoeff);
        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.Button("Delete all shapes"))
        {
            ctx.Lock.EnterWriteLock();
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
    }

    private static void ShowTools(Context ctx, ToolSystem toolSystem)
    {
        ImGui.PushItemWidth(100f);
        if (ImGui.Combo("Tool", ref ctx._selectedToolIndex, ToolSystem.ToolComboString))
        {
            toolSystem.ChangeToolType(ctx);
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
        var toolSystem = ctx.GetSystem<ToolSystem>(Context.SystemsEnum.ToolSystem);
        var spawnTool = (Spawn) toolSystem.SelectedTool;
        if (ImGui.Combo("Spawn target", ref ctx._selectedSpawnTargetIndex, TargetsToComboString()))
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

    private static void ShowEditorOptions(Context ctx)
    {
        ToolSystem toolSystem = ctx.GetSystem<ToolSystem>(Context.SystemsEnum.ToolSystem);
        ImGui.Text("EDITOR OPTIONS");
        ImGui.Spacing();
        var editor = (Editor) toolSystem.SelectedTool;
        if (ImGui.InputInt("Points per meter", ref editor._grid._pointsPerMeter))
        {
            editor._grid._pointsPerMeter = Math.Max(editor._grid._pointsPerMeter, 1);
            editor._grid.SetGridScale(editor._grid._pointsPerMeter);
        }
        ImGui.Combo("Editor action", ref editor._selectedActionIndex, editor.ActionComboString);
        if (editor.SelectedAction != Editor.EditorAction.CreateParticles)
        {
            ImGui.Checkbox("Rigid constraint", ref editor._isRigidConstraint);
        }
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
        if (editor.SelectedAction == Editor.EditorAction.Freeform)
        {
            ImGui.Checkbox("Pin point", ref editor._pinPoint);
        }
        if (!editor._isRigidConstraint &&
            (editor.SelectedAction == Editor.EditorAction.CreateLoop || 
            editor.SelectedAction == Editor.EditorAction.Freeform
            )
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
        var nBodySystem = ctx.GetSystem<NbodySystem>(Context.SystemsEnum.NbodySystem);
        ImGui.Checkbox("Running", ref nBodySystem._running);
        ImGui.Checkbox("Collisions enabled", ref nBodySystem._collisionsEnabled);
        ImGui.InputFloat("Gravitational constant", ref nBodySystem._gravConstant);
        ImGui.InputFloat("Minimum distance", ref nBodySystem._minDist);
        ImGui.InputFloat("Threshold", ref nBodySystem._threshold);
    }
}
