using System.Numerics;
using Editing;
using ImGuiNET;
using Sim;
using Tools;
using static Raylib_cs.Raylib;
using static Tools.Spawn;

namespace UI;

public class Gui
{
    public static void DrawInfo(Context ctx)
    {
        ImGui.Begin("Simulation info", ImGuiWindowFlags.NoMove);
        ImGui.SetWindowPos(Vector2.Zero);
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
        ImGui.PushItemWidth(100f);
        if (ImGui.Combo("Tool", ref ctx._selectedToolIndex, Tool.ToolComboString))
        {
            Tool.ChangeToolType(ctx);
        }
        ImGui.Separator();
        ImGui.Spacing();
        switch (ctx.SelectedTool)
        {
            case Spawn :
                ShowSpawnToolOptions(ctx);
                break;
            case Editor :
                ShowEditorOptions(ctx);
                break;
            case Pull :
                ImGui.InputFloat("Force coefficient", ref ((Pull) ctx.SelectedTool)._forceCoeff);
                break;
            case PullCom :
                ImGui.InputFloat("Force coefficient", ref ((PullCom) ctx.SelectedTool)._forceCoeff);
                break;
            case GravityWell :
                ImGui.InputFloat("Gravitational constant", ref ((GravityWell) ctx.SelectedTool)._gravConstant);
                ImGui.InputFloat("Minimum distance", ref ((GravityWell) ctx.SelectedTool)._minDist);
                break;
            case NbodySim :
                ImGui.Checkbox("Running", ref ((NbodySim) ctx.SelectedTool)._running);
                ImGui.Checkbox("Collisions enabled", ref ((NbodySim) ctx.SelectedTool)._collisionsEnabled);
                ImGui.InputFloat("Gravitational constant", ref ((NbodySim) ctx.SelectedTool)._gravConstant);
                ImGui.InputFloat("Minimum distance", ref ((NbodySim) ctx.SelectedTool)._minDist);
                ImGui.InputFloat("Threshold", ref ((NbodySim) ctx.SelectedTool)._threshold);
                break;
        }
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
        // Disable tool if mouse is over info window
        Vector2 margin = new(500f, 500f);
        if (ImGui.IsMouseHoveringRect(ImGui.GetWindowContentRegionMin() - margin, ImGui.GetWindowContentRegionMax() + margin))
        {
            ctx._toolEnabled = false;
        }
        else
        {
            ctx._toolEnabled = true;
        }
        ImGui.End();
    }

    private static void ShowSpawnToolOptions(Context ctx)
    {
        var spawnTool = (Spawn) ctx.SelectedTool;
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
        ImGui.Text("EDITOR OPTIONS");
        ImGui.Spacing();
        var editor = (Editor) ctx.SelectedTool;
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
}
