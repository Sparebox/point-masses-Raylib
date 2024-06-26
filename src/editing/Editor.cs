using System.Numerics;
using System.Text;
using Physics;
using Raylib_cs;
using Sim;
using Tools;
using Utils;
using static Raylib_cs.Raylib;

namespace Editing;

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
    public bool _connectLoop;
    public bool _inflateLoop;
    public bool _isRigidConstraint;
    public float _gasAmount = Spawn.DefaultGasAmt;
    public float _stiffness = Spawn.DefaultStiffness;
    public readonly Grid _grid;
    private (uint, uint) _clickedPointIndices;
    
    public enum EditorAction
    {
        CreateParticles,
        CreateLoop,
        Freeform
    }

    public Editor(Context context)
    {
        _context = context;
        _grid = new Grid(5);
        ActionComboString = GetActionComboString();
        _selectedActionIndex = 0;
        _clickedPointIndices = new();
    }

    public override void Draw()
    {
        _grid.Draw();
        Vector2 mousePos = GetMousePosition();
        if (SelectedAction == EditorAction.Freeform && IsMouseButtonDown(MouseButton.Left) && IsKeyDown(KeyboardKey.LeftAlt))
        {
            var startPos = UnitConv.MetersToPixels(_grid.GridPoints[_clickedPointIndices.Item1]._pos);
            DrawLine((int) startPos.X, (int) startPos.Y, (int) mousePos.X, (int) mousePos.Y, Color.Yellow);
        }
        else
        {
            DrawCircleLines((int) mousePos.X, (int) mousePos.Y, UnitConv.MetersToPixels(Radius), Color.Yellow);
        }
    }

    public override void Update()
    {
        if (!_context._toolEnabled)
        {
            return;
        }
        if (IsMouseButtonDown(MouseButton.Left) && !IsKeyDown(KeyboardKey.LeftAlt))
        {
            try {
                var mousePos = GetMousePosition();
                if (IsKeyDown(KeyboardKey.LeftShift))
                {
                    _grid.ToggleGridPoint((int) mousePos.X, (int) mousePos.Y, false);
                }
                else
                {
                    _grid.ToggleGridPoint((int) mousePos.X, (int) mousePos.Y, true);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLine(e);
            }
        }
        if (IsMouseButtonPressed(MouseButton.Left))
        {
            var mousePos = GetMousePosition();
            try {
                if (SelectedAction == EditorAction.Freeform && IsKeyDown(KeyboardKey.LeftAlt)) // Creating constraint start point
                {
                    _clickedPointIndices.Item1 = _grid.GetIndexFromPixel((int) mousePos.X, (int) mousePos.Y);
                    if (_grid.GridPoints[_clickedPointIndices.Item1].IsSelected)
                    {
                        _grid.GridPoints[_clickedPointIndices.Item1].IsConstrained = true;
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLine(e);
            }
        }
        if (IsMouseButtonReleased(MouseButton.Left))
        {
            try {
                var mousePos = GetMousePosition();
                if (SelectedAction == EditorAction.Freeform && IsKeyDown(KeyboardKey.LeftAlt)) // Creating constraint end point
                {
                    _clickedPointIndices.Item2 = _grid.GetIndexFromPixel((int) mousePos.X, (int) mousePos.Y);
                    if (_clickedPointIndices.Item1 != _clickedPointIndices.Item2 && _grid.GridPoints[_clickedPointIndices.Item2].IsSelected)
                    {
                        _grid.GridPoints[_clickedPointIndices.Item2].IsConstrained = true;
                        _grid.ConstrainedPointIndexPairs.Add(_clickedPointIndices);
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }

    public void BuildShape()
    {
        if (Radius == 0f)
        {
            return;
        }
        switch (SelectedAction)
        {
            case EditorAction.CreateParticles:
                CreateParticlesFromIndices();
                break;
            case EditorAction.CreateLoop:
                CreateLoopFromIndices(PointMass.RadiusToMass(Radius));
                break;
            case EditorAction.Freeform:
                CreateFreeformShape(PointMass.RadiusToMass(Radius));
                break;
        }
        _grid.ClearSelectedPoints();
    }

    private void CreateParticlesFromIndices()
    {
        var particles = new List<MassShape>(_grid.SelectedPointIndices.Count);
        foreach (var index in _grid.SelectedPointIndices)
        {
            var gridPoint = _grid.GridPoints[index];
            particles.Add(MassShape.Particle(
                gridPoint._pos.X,
                gridPoint._pos.Y,
                PointMass.RadiusToMass(Radius),
                _context
            ));
        }
        _context.AddMassShapes(particles);  
    }

    private void CreateLoopFromIndices(float mass)
    {
        MassShape loop = new(_context, _inflateLoop)
        {
            _gasAmount = _gasAmount
        };
        // Points
        foreach (var index in _grid.SelectedPointIndices)
        {
            Vector2 pos = _grid.GridPoints[index]._pos;
            loop._points.Add(new(pos.X, pos.Y, mass, false, _context));
        }
        // Constraints
        for (int i = 0; i < (_connectLoop ? loop._points.Count : loop._points.Count - 1); i++)
        {
            Constraint c;
            if (_isRigidConstraint)
            {
                c = new RigidConstraint(loop._points[i], loop._points[(i + 1) % loop._points.Count]);
            }
            else
            {
                c = new SpringConstraint(loop._points[i], loop._points[(i + 1) % loop._points.Count], _stiffness, SpringConstraint.DefaultDamping);
            }
            loop._constraints.Add(c);
        }
        _context.AddMassShape(loop);
    }

    private void CreateFreeformShape(float mass)
    {
        MassShape shape = new(_context, _inflateLoop);
        // Points
        foreach (var gridIndex in _grid.SelectedPointIndices)
        {
            Vector2 pos = _grid.GridPoints[gridIndex]._pos;
            shape._points.Add(new(pos.X, pos.Y, mass, false, _context));
        }
        // Constraints
        foreach (var pair in _grid.ConstrainedPointIndexPairs)
        {
            Constraint c;
            PointMass a = shape._points.Find(p => p.Pos == _grid.GridPoints[pair.Item1]._pos);
            PointMass b = shape._points.Find(p => p.Pos == _grid.GridPoints[pair.Item2]._pos);
            if (_isRigidConstraint)
            {
                c = new RigidConstraint(a, b);
            }
            else
            {
                c = new SpringConstraint(a, b, _stiffness, SpringConstraint.DefaultDamping);
            }
            shape._constraints.Add(c);
        }
        _context.AddMassShape(shape);
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