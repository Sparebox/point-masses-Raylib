using System.Data;
using System.Numerics;
using Collision;
using Entity;
using Physics;
using Tools;

namespace Sim;

public class Context
{
    public readonly float _timeStep;
    public readonly float _subStep;
    public readonly int _substeps;
    public readonly float _pixelsPerMeter = 1f / 0.01f;
    public readonly Vector2 _gravity;
    private State _saveState;

    public Ramp _ramp;
    public HashSet<LineCollider> LineColliders { get; set; }
    public HashSet<MassShape> MassShapes { get; set; }
    public bool GravityEnabled { get; set; }
    public bool DrawForces { get; set; }
    public bool DrawAABBs { get; set; }
    public bool SimPaused { get; set; }
    public Tool SelectedTool { get; set; }
    public int MassCount 
    {
        get 
        {
            return MassShapes.Aggregate(0, (count, shape) => count += shape._points.Count);
        }
    }
    public int ConstraintCount 
    {
        get
        {
            return MassShapes.Aggregate(0, (count, shape) => count += shape._constraints.Count);
        }
    }

    public Context(float timeStep, int subSteps, float pixelsPerMeter, Vector2 gravity)
    {
        _timeStep = timeStep;
        _substeps = subSteps;
        _subStep = timeStep / subSteps;
        _pixelsPerMeter = pixelsPerMeter;
        _gravity = gravity * pixelsPerMeter;
    }

    public void SaveState()
    {
        _saveState.LineColliders = new();
        _saveState.MassShapes = new();
        foreach (var c in LineColliders)
        {
            _saveState.LineColliders.Add(new LineCollider(c));
        }
        foreach (var s in MassShapes)
        {
            _saveState.MassShapes.Add(new MassShape(s));
        }
        Console.WriteLine("Saved state");
    }

    public void LoadState()
    {
        LineColliders.Clear();
        MassShapes.Clear();
        foreach (var c in _saveState.LineColliders)
        {
            LineColliders.Add(new LineCollider(c));
        }
        foreach (var s in _saveState.MassShapes)
        {
            MassShapes.Add(new MassShape(s));
        }
        Console.WriteLine("Loaded state");
    }

    private struct State
    {
        public HashSet<LineCollider> LineColliders { get; set; }
        public HashSet<MassShape> MassShapes { get; set; }
    }
}