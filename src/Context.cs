using System.Numerics;
using Collision;
using Physics;
using Entities;
using Textures;
using Utils;
using Tools;
using Editing;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Sim;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class Context
{
    public float TimeStep { get; init; }
    public float SubStep { get; init; }
    public int Substeps { get; init; }
    public Vector2 Gravity { get; init; }
    public TextureManager TextureManager { get; init; }

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
    public QuadTree QuadTree { get; init; }
    public HashSet<LineCollider> LineColliders { get; init; }
    public HashSet<MassShape> MassShapes { get; init; }
    public Tool SelectedTool { get; set; }
    public int _selectedToolIndex;
    public int _selectedSpawnTargetIndex;
    public Tool[] Tools { get; init; }
    public NbodySim NbodySim
    {
        get
        {
            return Tools[(int) ToolType.NbodySim] as NbodySim;
        }
    }
    public int SavedShapeCount => _saveState.MassShapes.Count;
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
        Tools = CreateTools();
    }

    public void AddMassShape(MassShape shape)
    {
        MassShapes.Add(shape);
    }

    public void AddMassShapes(IEnumerable<MassShape> shapes)
    {
        foreach (var shape in shapes)
        {
            MassShapes.Add(new(shape));
        }
    }

    public void SaveCurrentState()
    {
        _saveState = new();
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

    public void LoadSavedState()
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

    private Tool[] CreateTools()
    {
        var tools = new Tool[Tool.ToolTypes.Length];
        tools[(int) ToolType.PullCom] = new PullCom(this);
        tools[(int) ToolType.Pull] = new Pull(this);
        tools[(int) ToolType.Wind] = new Wind(this);
        tools[(int) ToolType.Rotate] = new Rotate(this);
        tools[(int) ToolType.Spawn] = new Spawn(this);
        tools[(int) ToolType.Ruler] = new Ruler(this);
        tools[(int) ToolType.Delete] = new Delete(this);
        tools[(int) ToolType.Editor] = new Editor(this);
        tools[(int) ToolType.GravityWell] = new GravityWell(this);
        tools[(int) ToolType.NbodySim] = new NbodySim(this);
        return tools;
    }
    
    private struct SaveState
    {
        public HashSet<LineCollider> LineColliders { get; set; }
        public HashSet<MassShape> MassShapes { get; set; }

        public SaveState()
        {
            LineColliders = new();
            MassShapes = new();
        }
    }

    public void LoadClothScenario()
    {
        AddMassShape(MassShape.Cloth(
            x: UnitConv.PixelsToMeters(500f),
            y: UnitConv.PixelsToMeters(10f),
            width: UnitConv.PixelsToMeters(500f),
            height: UnitConv.PixelsToMeters(500f),
            mass: 0.7f,
            res: 42,
            stiffness: 0.9f,
            this
        ));
    }
}