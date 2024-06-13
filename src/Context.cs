using System.Numerics;
using Collision;
using Physics;
using Entities;
using Textures;
using Tools;
using Utils;
using Editing;

namespace Sim;

public class Context
{
    public float TimeStep { get; init; }
    public float SubStep { get; init; }
    public int Substeps { get; init; }
    public Vector2 Gravity { get; init; }
    public TextureManager TextureManager { get; init; }
    public Editor Editor { get; init; }

    private SaveState _saveState;
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
        Editor = new(this);
    }

    public void AddMassShape(MassShape shape)
    {
        MassShapes.Add(shape);
    }

    public void AddMassShapes(IEnumerable<MassShape> shapes)
    {
        foreach (var shape in shapes)
        {
            MassShapes.Add(shape);
        }
    }

    public void SaveCurrentState()
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
        AddMassShapes(_saveState.MassShapes);
        Console.WriteLine("Loaded state");
    }

    private struct SaveState
    {
        public HashSet<LineCollider> LineColliders { get; set; }
        public HashSet<MassShape> MassShapes { get; set; }
    }

    public void LoadDemoScenarioOne()
    {
        //MassShapes.Add(MassShape.Chain(Program.WinW / 2f - 100f, Program.WinH / 2f, Program.WinW / 2f + 100f, Program.WinH / 2f, 10f, 2, (false, false), this));
        //assShapes.Add(MassShape.Particle(Program.WinW / 2f, Program.WinH / 2f - 100f, 10f, this));
        // MassShapes.Add(MassShape.Cloth(x: 300f, y: 10f, width: 500f, height: 500f, mass: 0.7f, res: 42, stiffness: 1e5f, this));
        AddMassShape(MassShape.SoftBall(Program.WinW / 2f - 300f, Program.WinH / 2f - 200f, 50f, 50f, 20, 1000f, 10f, this));
        AddMassShape(MassShape.Box(Program.WinW / 2f, Program.WinH / 2f - 300f, 100f, 10f, this));
        AddMassShape(MassShape.SoftBox(Program.WinW / 2f + 300f, Program.WinH / 2f - 200f, 150f, 20f, 1e4f, this));
        AddMassShape(MassShape.HardBall(Program.WinW / 2f + 600f, Program.WinH / 2f - 300f, 50f, 35f, 13, this));
    }

    public void LoadDemoScenarioTwo()
    {
        AddMassShape(MassShape.Cloth(
            x: UnitConv.PixelsToMeters(500f),
            y: UnitConv.PixelsToMeters(10f),
            width: UnitConv.PixelsToMeters(500f),
            height: UnitConv.PixelsToMeters(500f),
            mass: 0.7f,
            res: 42,
            stiffness: 1e5f,
            this
        ));
    }
}