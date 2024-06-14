using System.Numerics;
using System.Text;
using Physics;
using Raylib_cs;
using Sim;
using Tools;
using Utils;
using static Raylib_cs.Raylib;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Editing;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class Editor : Tool
{
    public int _selectedActionIndex;
    public EditorAction SelectedAction { 
        get 
        {
            EditorAction[] actions = (EditorAction[]) Enum.GetValues(typeof(EditorAction));
            return actions[_selectedActionIndex];
        } 
    }
    public string ActionComboString { get; init; }
    private readonly Grid _grid;
    private readonly LinkedList<uint> _selectedPointIndices;

    public enum EditorAction
    {
        CreateParticles,
        CreateLoop
    }

    public Editor(Context context)
    {
        _context = context;
        _grid = new Grid(5);
        _selectedPointIndices = new();
        ActionComboString = GetActionComboString();
        _selectedActionIndex = 0;
    }

    public override void Draw()
    {
        _grid.Draw();
        Vector2 mousePos = GetMousePosition();
        DrawCircleLines((int) mousePos.X, (int) mousePos.Y, UnitConv.MetersToPixels(Radius), Color.Yellow);
    }

    public override void Update()
    {
        if (IsMouseButtonDown(MouseButton.Left))
        {
            try {
                var mousePos = GetMousePosition();
                ref var closestGridPoint = ref _grid.GetClosestGridPoint((uint) mousePos.X, (uint) mousePos.Y);
                closestGridPoint.IsSelected = true;
                uint gridIndex = _grid.GetIndexFromPixel((uint) mousePos.X, (uint) mousePos.Y);
                if (!_selectedPointIndices.Contains(gridIndex))
                {
                    _selectedPointIndices.AddLast(gridIndex);
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.Error.WriteLine("Grid index was out of range");
            }
        }
        else
        {
            _grid.ClearSelectedPoints();
        }
        if (IsMouseButtonReleased(MouseButton.Left)) // Execute editor action
        {
            switch (SelectedAction)
            {
                case EditorAction.CreateParticles:
                    CreateParticlesFromIndices();
                    break;
                case EditorAction.CreateLoop:
                    CreateLoopFromIndices(PointMass.RadiusToMass(Radius));
                    break;
            }
            _selectedPointIndices.Clear();
        }
    }

    private void CreateParticlesFromIndices()
    {
        var particles = new List<MassShape>(_selectedPointIndices.Count);
        foreach (var index in _selectedPointIndices)
        {
            var gridPoint = _grid.GridPoints[index];
            particles.Add(MassShape.Particle(
                gridPoint.Pos.X,
                gridPoint.Pos.Y,
                PointMass.RadiusToMass(Radius),
                _context
            ));
        }
        _context.AddMassShapes(particles);  
    }

    private void CreateLoopFromIndices(float mass)
    {
        MassShape loop = new(_context);
        // Points
        foreach (var index in _selectedPointIndices)
        {
            Vector2 pos = _grid.GridPoints[index].Pos;
            loop._points.Add(new(pos.X, pos.Y, mass, false, _context));
        }
        // Constraints
        for (int i = 0; i < loop._points.Count; i++)
        {
            loop._constraints.Add(new RigidConstraint(loop._points[i], loop._points[(i + 1) % loop._points.Count]));
        }
        _context.AddMassShape(loop);
    }

    private static string GetActionComboString()
    {
        StringBuilder sb = new();
        EditorAction[] actions = (EditorAction[]) Enum.GetValues(typeof(EditorAction));
        foreach (var action in actions)
        {
            sb.Append(action.ToString() + "\0");
        }
        return sb.ToString();
    }
}