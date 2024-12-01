using System.Numerics;
using System.Text;
using PointMasses.Entities;
using PointMasses.Physics;
using Raylib_cs;
using PointMasses.Sim;
using PointMasses.Tools;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Editing;

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

    public Editor(Context ctx)
    {
        _ctx = ctx;
        _grid = new Grid(5, ctx.WinSize);
        ActionComboString = GetActionComboString();
        _selectedActionIndex = 0;
        _clickedPointIndices = new();
    }

    public override void Draw()
    {
        _grid.Draw();
        Vector2 mousePos = GetScreenToWorld2D(GetMousePosition(), _ctx._camera);
        if (SelectedAction == EditorAction.Freeform && IsMouseButtonDown(MouseButton.Left) && IsKeyDown(KeyboardKey.LeftAlt))
        {
            var startPos = UnitConv.MetersToPixels(_grid.GridPoints[_clickedPointIndices.Item1]._pos);
            DrawLineV(startPos, mousePos, Color.Purple);
        }
        else
        {
            DrawCircleLinesV(mousePos, UnitConv.MetersToPixels(Radius), Color.Yellow);
        }
    }

    public override void Update()
    {
        HandleSelect();
        HandleConstraint();
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
                _ctx
            ));
        }
        _ctx.AddMassShapes(particles);  
    }

    private void CreateLoopFromIndices(float mass)
    {
        MassShape loop = new(_ctx, _inflateLoop)
        {
            _gasAmount = _gasAmount
        };
        // Points
        foreach (var index in _grid.SelectedPointIndices)
        {
            Vector2 pos = _grid.GridPoints[index]._pos;
            loop._points.Add(new(pos.X, pos.Y, mass, false, _ctx));
        }
        // Constraints
        for (int i = 0; i < (_connectLoop ? loop._points.Count : loop._points.Count - 1); i++)
        {
            Constraint c = new DistanceConstraint(loop._points[i], loop._points[(i + 1) % loop._points.Count], _stiffness, _ctx);
            loop._constraints.Add(c);
        }
        _ctx.AddMassShape(loop);
    }

    private void CreateFreeformShape(float mass)
    {
        MassShape shape = new(_ctx, _inflateLoop);
        // Points
        foreach (var gridIndex in _grid.SelectedPointIndices)
        {
            Vector2 pos = _grid.GridPoints[gridIndex]._pos;
            bool isPinned = _grid.GridPoints[gridIndex].IsPinned;
            shape._points.Add(new(pos.X, pos.Y, mass, isPinned, _ctx));
        }
        // Constraints
        foreach (var pair in _grid.ConstrainedPointIndexPairs)
        {
            PointMass a = shape._points.Find(p => p.Pos == _grid.GridPoints[pair.Item1]._pos);
            PointMass b = shape._points.Find(p => p.Pos == _grid.GridPoints[pair.Item2]._pos);
            Constraint c = new DistanceConstraint(a, b, _stiffness, _ctx);
            shape._constraints.Add(c);
        }
        _ctx.AddMassShape(shape);
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

    private void SetClickedPoint(Vector2 mousePos, bool isFirstPoint)
    {
        if (isFirstPoint)
        {
            _clickedPointIndices.Item1 = _grid.GetIndexFromPixel((int) mousePos.X, (int) mousePos.Y);
        }
        else
        {
            _clickedPointIndices.Item2 = _grid.GetIndexFromPixel((int) mousePos.X, (int) mousePos.Y);
            bool bothPointsAreSelected = _grid.GridPoints[_clickedPointIndices.Item1].IsSelected && _grid.GridPoints[_clickedPointIndices.Item2].IsSelected;
            if (_clickedPointIndices.Item1 != _clickedPointIndices.Item2 && bothPointsAreSelected)
            {
                _grid.GridPoints[_clickedPointIndices.Item1].IsConstrained = true;
                _grid.GridPoints[_clickedPointIndices.Item2].IsConstrained = true;
                _grid.ConstrainedPointIndexPairs.Add(_clickedPointIndices);
            }   
        }
    }

    private void HandleConstraint()
    {
        if (IsMouseButtonPressed(MouseButton.Left))
        {
            try {
                if (SelectedAction == EditorAction.Freeform && IsKeyDown(KeyboardKey.LeftAlt)) // Creating constraint start point
                {
                    SetClickedPoint(GetScreenToWorld2D(GetMousePosition(), _ctx._camera), true);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLineAsync(e.Message);
            }
        }
        
        if (IsMouseButtonReleased(MouseButton.Left))
        {
            try {
                if (SelectedAction == EditorAction.Freeform && IsKeyDown(KeyboardKey.LeftAlt)) // Creating constraint end point
                {
                    SetClickedPoint(GetScreenToWorld2D(GetMousePosition(), _ctx._camera), false);
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLineAsync(e.Message);
            }
        }
    }

    private void HandleSelect()
    {
        if (IsMouseButtonDown(MouseButton.Left) && !IsKeyDown(KeyboardKey.LeftAlt))
        {
            try 
            {
                var mousePos = GetScreenToWorld2D(GetMousePosition(), _ctx._camera);
                if (IsKeyDown(KeyboardKey.LeftShift))
                {
                    _grid.SetGridPoint((int) mousePos.X, (int) mousePos.Y, false, IsKeyDown(KeyboardKey.LeftControl));
                }
                else
                {
                    _grid.SetGridPoint((int) mousePos.X, (int) mousePos.Y, true, IsKeyDown(KeyboardKey.LeftControl));
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.Error.WriteLineAsync(e.Message);
            }
        }
    }
}