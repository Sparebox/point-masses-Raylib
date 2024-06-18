using System.Numerics;
using Editing;
using ImGuiNET;
using Sim;
using Tools;
using static Raylib_cs.Raylib;
using static Tools.Spawn;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace UI;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class Gui
{
    public static void DrawInfo(Context context)
    {
        ImGui.Begin("Simulation info", ImGuiWindowFlags.NoMove);
        ImGui.SetWindowPos(Vector2.Zero);
        ImGui.Text(string.Format("FPS: {0}", GetFPS()));
        ImGui.PushStyleColor(ImGuiCol.Text, context._simPaused ? new Vector4(255f, 0f, 0f, 255f) : new Vector4(0f, 255f, 0f, 255f));
        ImGui.Checkbox(context._simPaused ? "PAUSE" : "RUNNING", ref context._simPaused);
        ImGui.PopStyleColor();
        ImGui.Text(string.Format("Masses: {0}", context.MassCount));
        ImGui.Text(string.Format("Constraints: {0}", context.ConstraintCount));
        ImGui.Text(string.Format("Shapes: {0}", context.MassShapes.Count));
        ImGui.Text(string.Format("Substeps: {0}", context.Substeps));
        ImGui.Text(string.Format("Step: {0:0.0000} ms", context.TimeStep * 1e3f));
        ImGui.Text(string.Format("Substep: {0:0.0000} ms", context.SubStep * 1e3f));
        if (context._drawBodyInfo)
        {
            ImGui.Text(string.Format("System energy: {0} kJ", context.SystemEnergy / 1e3f));
        }
        ImGui.Checkbox("Gravity", ref context._gravityEnabled);
        ImGui.Checkbox("Draw forces", ref context._drawForces);
        ImGui.Checkbox("Draw AABBs", ref context._drawAABBS);
        ImGui.Checkbox("Draw quadtree", ref context._drawQuadTree);
        ImGui.Checkbox("Draw body info", ref context._drawBodyInfo);
        ImGui.PushItemWidth(50f);
        ImGui.InputFloat("Global restitution coeff", ref context._globalRestitutionCoeff);
        ImGui.InputFloat("Global kinetic friction coeff", ref context._globalKineticFrictionCoeff);
        ImGui.InputFloat("Global static friction coeff", ref context._globalStaticFrictionCoeff);
        ImGui.PushItemWidth(100f);
        if (ImGui.Combo("Tool", ref context._selectedToolIndex, Tool.ToolComboString))
        {
            Tool.ChangeToolType(context);
        }
        ImGui.Separator();
        ImGui.Spacing();
        switch (context.SelectedTool)
        {
            case Spawn :
                ShowSpawnToolOptions(context);
                break;
            case Editor :
                ShowEditorOptions(context);
                break;
            case Pull :
                ImGui.InputFloat("Force coefficient", ref ((Pull) context.SelectedTool)._forceCoeff);
                break;
            case PullCom :
                ImGui.InputFloat("Force coefficient", ref ((PullCom) context.SelectedTool)._forceCoeff);
                break;
            case GravityWell :
                ImGui.InputFloat("Gravitational constant", ref ((GravityWell) context.SelectedTool)._gravConstant);
                ImGui.InputFloat("Minimum distance", ref ((GravityWell) context.SelectedTool)._minDist);
                break;
        }
        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.Button("Delete all shapes"))
        {
            foreach (var shape in context.MassShapes)
            {
                shape._toBeDeleted = true;
            }
        }
        if (ImGui.Button("Save current state"))
        {
            context.SaveCurrentState();
        }
        if (context.SavedShapeCount > 0)
        {
            if (ImGui.Button($"Load saved state ({context.SavedShapeCount} shapes saved)"))
            {
                context.LoadSavedState();
            }
        }
        // Disable tool if mouse is over info window
        Vector2 margin = new(500f, 500f);
        if (ImGui.IsMouseHoveringRect(ImGui.GetWindowContentRegionMin() - margin, ImGui.GetWindowContentRegionMax() + margin))
        {
            context._toolEnabled = false;
        }
        else
        {
            context._toolEnabled = true;
        }
        ImGui.End();
    }

    private static void ShowSpawnToolOptions(Context context)
    {
        var spawnTool = (Spawn) context.SelectedTool;
        if (ImGui.Combo("Spawn target", ref context._selectedSpawnTargetIndex, TargetsToComboString()))
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

    private static void ShowEditorOptions(Context context)
    {
        ImGui.Text("EDITOR OPTIONS");
        ImGui.Spacing();
        var editor = (Editor) context.SelectedTool;
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
