using System.Numerics;
using Collision;
using Entities;
using Textures;
using Utils;
using Tools;
using Editing;
using Raylib_cs;

namespace Sim;

public class Context
{
    public ReaderWriterLockSlim Lock { get; set; }
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
    public QuadTree QuadTree { get; set; }
    public HashSet<LineCollider> LineColliders { get; set; }
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
            Lock.EnterReadLock();
            int count = MassShapes.Aggregate(0, (count, shape) => count + shape._points.Count);
            Lock.ExitReadLock();
            return count;
        }
    }
    public int ConstraintCount 
    {
        get
        {
            Lock.EnterReadLock();
            int count = MassShapes.Aggregate(0, (count, shape) => count + shape._constraints.Count);
            Lock.ExitReadLock();
            return count;
        }
    }
    public float SystemEnergy
    {
        get
        {
            Lock.EnterReadLock();
            float energy = MassShapes.Aggregate(0f, (energy, shape) => energy + shape.LinEnergy + shape.RotEnergy);
            Lock.ExitReadLock();
            return energy;
        }
    }

    public Context(float timeStep, int subSteps, Vector2 gravity)
    {
        TimeStep = timeStep;
        Substeps = subSteps;
        SubStep = timeStep / subSteps;
        Gravity = gravity;
        Lock = new ReaderWriterLockSlim();
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
        Lock.EnterWriteLock();
        MassShapes.Add(shape);
        Lock.ExitWriteLock();
    }

    public void AddMassShapes(IEnumerable<MassShape> shapes)
    {
        Lock.EnterWriteLock();
        foreach (var shape in shapes)
        {
            MassShapes.Add(new(shape));
        }
        Lock.ExitWriteLock();
    }

    public void SaveCurrentState()
    {
        Lock.EnterWriteLock();
        _saveState = new();
        foreach (var c in LineColliders)
        {
            _saveState.LineColliders.Add(new LineCollider(c));
        }
        foreach (var s in MassShapes)
        {
            _saveState.MassShapes.Add(new MassShape(s));
        }
        Lock.ExitWriteLock();
        Console.WriteLine("Saved state");
    }

    public void LoadSavedState()
    {
        Lock.EnterWriteLock();
        _simPaused = true;
        LineColliders.Clear();
        MassShapes.Clear();
        foreach (var c in _saveState.LineColliders)
        {
            LineColliders.Add(new LineCollider(c));
        }
        Lock.ExitWriteLock();
        AddMassShapes(_saveState.MassShapes);
        Console.WriteLine("Loaded state");
    }

    public MassShape GetMassShape(uint id)
    {
        Lock.EnterReadLock();
        var activeShapes = MassShapes.Where(s => !s._toBeDeleted);
        if (!activeShapes.Any())
        {
            return null;
        }
        if (!activeShapes.Select(s => s.Id).Contains(id))
        {
            return null;
        }
        MassShape shape = activeShapes.Single(s => s.Id == id);
        Lock.ExitReadLock();
        return shape;
    }

    public IEnumerable<MassShape> GetMassShapes(in BoundingBox area)
    {
        HashSet<MassShape> found = new();
        QuadTree.Lock.EnterReadLock();
        QuadTree.QueryShapes(area, found);
        QuadTree.Lock.ExitReadLock();
        return found;
    }

    public IEnumerable<MassShape> GetMassShapes(HashSet<uint> shapeIds)
    {
        Lock.EnterReadLock();
        var filteredShapes = MassShapes.Where(s => shapeIds.Contains(s.Id));
        Lock.ExitReadLock();
        return filteredShapes;
    }

    public IEnumerable<PointMass> GetPointMasses(uint shapeId)
    {
        return GetMassShape(shapeId)._points;
    }

    public IEnumerable<PointMass> GetPointMasses(HashSet<uint> pointIds)
    {
        return MassShapes.SelectMany(s => s._points).Where(p => pointIds.Contains(p.Id));
    }

    public IEnumerable<PointMass> GetPointMasses(in BoundingBox area)
    {
        HashSet<PointMass> found = new();
        QuadTree.Lock.EnterReadLock();
        QuadTree.QueryPoints(in area, found);
        QuadTree.Lock.ExitReadLock();
        return found;
    }

    private Tool[] CreateTools()
    {
        var tools = new Tool[Tool.ToolTypes.Length];
        tools[(int) ToolType.PullCom]       = new PullCom(this);
        tools[(int) ToolType.Pull]          = new Pull(this);
        tools[(int) ToolType.Wind]          = new Wind(this);
        tools[(int) ToolType.Rotate]        = new Rotate(this);
        tools[(int) ToolType.Spawn]         = new Spawn(this);
        tools[(int) ToolType.Ruler]         = new Ruler(this);
        tools[(int) ToolType.Delete]        = new Delete(this);
        tools[(int) ToolType.Editor]        = new Editor(this);
        tools[(int) ToolType.GravityWell]   = new GravityWell(this);
        tools[(int) ToolType.NbodySim]      = new NbodySim(this);
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
            stiffness: 0.8f,
            true,
            this
        ));
    }

    public void LoadBenchmark(int maxParticleCount, float particleMass, float spacing, Vector2 offset)
    {
        List<MassShape> particles = new(maxParticleCount);
        int sideCount = (int) Math.Sqrt(maxParticleCount);
        spacing = UnitConv.PixelsToMeters(spacing);
        offset = UnitConv.PixelsToMeters(offset);
        for (int y = 0; y < sideCount; y++)
        {
            for (int x = 0; x < sideCount; x++)
            {
                particles.Add(MassShape.Particle(x * spacing + offset.X, y * spacing + offset.Y, particleMass, this));
            }
        }
        AddMassShapes(particles);
    }
}