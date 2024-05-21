using System.Data;
using System.Numerics;
using Collision;
using Entity;
using Physics;
using Entities;
using Textures;
using Tools;

namespace Sim;

public class Context
{
    public readonly float TimeStep;
    public readonly float SubStep;
    public readonly int Substeps;
    public readonly Vector2 Gravity;
    public readonly TextureManager TextureManager;

    private State _saveState;
    public bool _gravityEnabled;
    public bool _drawForces;
    public bool _drawAABBS;
    public bool _drawQuadTree;
    public bool _drawBodyInfo;
    public bool _simPaused;
    public bool _toolEnabled;
    public float _globalRestitutionCoeff = 0.3f;
    public float _globalKineticFrictionCoeff = 1f;
    public float _globalStaticFrictionCoeff = 1.1f;
    public QuadTree QuadTree { get; set; }
    public HashSet<LineCollider> LineColliders { get; set; }
    public HashSet<MassShape> MassShapes { get; set; }
    public Tool SelectedTool { get; set; }
    public int _selectedToolIndex;
    public int _selectedSpawnTargetIndex;
    public int MassCount 
    {
        get 
        {
            return MassShapes.Aggregate(0, (count, shape) => count + shape._points.Count);
        }
    }
    public int ConstraintCount 
    {
        get
        {
            return MassShapes.Aggregate(0, (count, shape) => count + shape._constraints.Count);
        }
    }
    public float SystemEnergy
    {
        get
        {
            return MassShapes.Aggregate(0f, (energy, shape) => energy + shape.LinEnergy + shape.RotEnergy);
        }
    }

    public Context(float timeStep, int subSteps, Vector2 gravity)
    {
        TimeStep = timeStep;
        Substeps = subSteps;
        SubStep = timeStep / subSteps;
        Gravity = gravity;
        TextureManager = new TextureManager();
        _toolEnabled = true;
        _gravityEnabled = false;
        _drawAABBS = false;
        _drawForces = false;
        _simPaused = true;
        MassShapes = new();
        LineColliders = new();
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
        _simPaused = true;
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

    public void LoadDemoScenarioOne()
    {
        //MassShapes.Add(MassShape.Chain(Program.WinW / 2f - 100f, Program.WinH / 2f, Program.WinW / 2f + 100f, Program.WinH / 2f, 10f, 2, (false, false), this));
        //assShapes.Add(MassShape.Particle(Program.WinW / 2f, Program.WinH / 2f - 100f, 10f, this));
        // MassShapes.Add(MassShape.Cloth(x: 300f, y: 10f, width: 500f, height: 500f, mass: 0.7f, res: 42, stiffness: 1e5f, this));
        MassShapes.Add(MassShape.SoftBall(Program.WinW / 2f - 300f, Program.WinH / 2f - 200f, 50f, 50f, 20, 1000f, 10f, this));
        MassShapes.Add(MassShape.Box(Program.WinW / 2f, Program.WinH / 2f - 300f, 100f, 10f, this));
        MassShapes.Add(MassShape.SoftBox(Program.WinW / 2f + 300f, Program.WinH / 2f - 200f, 150f, 20f, 1e4f, this));
        MassShapes.Add(MassShape.HardBall(Program.WinW / 2f + 600f, Program.WinH / 2f - 300f, 50f, 35f, 13, this));
    }

    public void LoadDemoScenarioTwo()
    {
        MassShapes.Add(MassShape.Cloth(x: 300f, y: 10f, width: 500f, height: 500f, mass: 0.7f, res: 42, stiffness: 1e5f, this));
    }
}