using System.Data;
using System.Numerics;
using Collision;
using Entity;
using Physics;
using Textures;
using Tools;

namespace Sim;

public class Context
{
    public readonly float _timeStep;
    public readonly float _subStep;
    public readonly int _substeps;
    public readonly Vector2 _gravity;
    public readonly TextureManager _textureManager;
    private State _saveState;

    public RotatingCollider _ramp;
    public bool _gravityEnabled;
    public bool _drawForces;
    public bool _drawAABBS;
    public bool _drawBodyInfo;
    public bool _simPaused;
    public bool _toolEnabled;
    public HashSet<LineCollider> LineColliders { get; set; }
    public HashSet<MassShape> MassShapes { get; set; }
    public Tool SelectedTool { get; set; }
    public int _selectedToolIndex;
    public int _selectedSpawnTargetIndex;
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

    public Context(float timeStep, int subSteps, Vector2 gravity)
    {
        _timeStep = timeStep;
        _substeps = subSteps;
        _subStep = timeStep / subSteps;
        _gravity = gravity;
        _textureManager = new TextureManager();
        _toolEnabled = true;
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
        //_saveState.Ramp = new RotatingCollider(_ramp);
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
        //_ramp = new RotatingCollider(_saveState.Ramp);
        Console.WriteLine("Loaded state");
    }

    private struct State
    {
        public HashSet<LineCollider> LineColliders { get; set; }
        public HashSet<MassShape> MassShapes { get; set; }
        public RotatingCollider Ramp { get; set; }
    }
}