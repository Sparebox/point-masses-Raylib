using System.Numerics;
using PointMasses.Collision;
using PointMasses.Entities;
using PointMasses.Utils;
using PointMasses.Systems;
using Raylib_cs;
using PointMasses.Input;
using Newtonsoft.Json;

namespace PointMasses.Sim;

[JsonObject(MemberSerialization.OptIn)]
public class Context
{
    // Properties
    public ReaderWriterLockSlim Lock { get; set; }
    public ReaderWriterLockSlim QuadTreeLock { get; init; }
    [JsonProperty]
    public float Substep { get; set; }
    [JsonProperty]
    public Vector2 Gravity { get; init; }
    public QuadTree QuadTree { get; set; }
    [JsonProperty]
    public List<LineCollider> LineColliders { get; set; }
    [JsonProperty]
    public List<MassShape> MassShapes { get; init; }
    public List<ISystem> Systems { get; init; }
    public List<ISystem> SubStepSystems { get; init; }
    [JsonProperty]
    public Vector2 WinSize { get; set; }

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

    public int SavedShapeCount => _saveState.MassShapes.Count;

    // Fields
    private Snapshot _saveState;
    [JsonProperty]
    public int _substeps;
    [JsonProperty]
    public float _timestep;
    [JsonProperty]
    public bool _gravityEnabled;
    public bool _collisionsEnabled;
    public bool _drawForces;
    public bool _drawAABBS;
    public bool _drawQuadTree;
    public bool _drawBodyInfo;
    public bool _simPaused;
    [JsonProperty]
    public float _globalRestitutionCoeff = Constants.GlobalRestitutionCoeffDefault;
    [JsonProperty]
    public float _globalKineticFrictionCoeff = Constants.GlobalKineticFrictionCoeffDefault;
    [JsonProperty]
    public float _globalStaticFrictionCoeff = Constants.GlobalStaticFrictionCoeffDefault;
    public Camera2D _camera;
    [JsonProperty]
    public float _cameraMoveSpeed = 1f;
    public float _accumulator;
    
    public Context(float timestep, int substeps, Vector2 gravity, Vector2 winSize)
    {
        SetTimestep(timestep, substeps);
        Gravity = gravity;
        WinSize = winSize;
        _camera = new Camera2D(Vector2.Zero, Vector2.Zero, 0f, 1f);
        Lock = new ReaderWriterLockSlim();
        QuadTreeLock = new ReaderWriterLockSlim();
        InputManager.PauseChanged += QuadTree.OnPauseChanged;
        _gravityEnabled = false;
        _drawAABBS = false;
        _drawForces = false;
        _simPaused = true;
        _collisionsEnabled = true;
        MassShapes = new(100);
        LineColliders = new(100);
        Systems = new();
        SubStepSystems = new();
        LoadSystems();
    }

    public void SetTimestep(float? timestep, int? substeps)
    {
        if (timestep.HasValue) {
            _timestep = timestep.Value;
        }
        if (substeps.HasValue)
        {
            _substeps = substeps.Value;
        }
        Substep = _timestep / _substeps;
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
        AsyncLogger.Info("Saved state");
    }

    public void LoadSnapshot()
    {
        Lock.EnterWriteLock();
        _simPaused = true;
        _camera.Offset = Vector2.Zero;
        LineColliders.Clear();
        MassShapes.Clear();
        foreach (var c in _saveState.LineColliders)
        {
            LineColliders.Add(new LineCollider(c));
        }
        Lock.ExitWriteLock();
        AddMassShapes(_saveState.MassShapes);
        AsyncLogger.Info("Loaded state");
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

    public IEnumerable<MassShape> GetMassShapes(ref BoundingBox area)
    {
        HashSet<MassShape> found = new();
        QuadTreeLock.EnterReadLock();
        QuadTree.QueryShapes(ref area, found);
        QuadTreeLock.ExitReadLock();
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

    public IEnumerable<PointMass> GetPointMasses(ref BoundingBox area)
    {
        HashSet<PointMass> found = new();
        QuadTreeLock.EnterReadLock();
        QuadTree.QueryPoints(ref area, found);
        QuadTreeLock.ExitReadLock();
        return found;
    }
    
    private struct Snapshot
    {
        public List<LineCollider> LineColliders { get; set; }
        public List<MassShape> MassShapes { get; set; }

        public Snapshot()
        {
            LineColliders = new();
            MassShapes = new();
        }
    }

    public void LoadClothScenario()
    {
        AddMassShape(MassShape.Cloth(
            x: UnitConv.PtoM(500f),
            y: UnitConv.PtoM(10f),
            width: UnitConv.PtoM(500f),
            height: UnitConv.PtoM(500f),
            mass: 0.7f,
            res: 42,
            stiffness: 0.5f,
            true,
            this
        ));
    }

    public void LoadBenchmark(int maxParticleCount, float particleMass, float spacing, Vector2 offset)
    {
        List<MassShape> particles = new(maxParticleCount);
        int sideCount = (int) Math.Sqrt(maxParticleCount);
        spacing = UnitConv.PtoM(spacing);
        offset = UnitConv.PtoM(offset);
        for (int y = 0; y < sideCount; y++)
        {
            for (int x = 0; x < sideCount; x++)
            {
                particles.Add(MassShape.Particle(x * spacing + offset.X, y * spacing + offset.Y, particleMass, this));
            }
        }
        AddMassShapes(particles);
    }

    public Type GetSystem<Type>()
    {
        foreach (var system in Systems)
        {
            if (system.GetType().Equals(typeof(Type)))
            {
                return (Type) system;
            }
        }
        throw new Exception($"System {typeof(Type)} could not be found");
    }

    private void LoadSystems()
    {
        Systems.Add(new ToolSystem(this));
        Systems.Add(new NbodySystem(this));
        Systems.Add(new CollisionSystem(this));
        // WaveSystem waveSystem = new();
        // var waveBuilder = new WaveSystem.WaveBuilder(this);
        // waveBuilder.SetStart(UnitConv.PtoM(new Vector2(WinSize.X * 0.01f, WinSize.Y * 0.5f)));
        // waveBuilder.SetEnd(UnitConv.PtoM(new Vector2(WinSize.X * 0.99f, WinSize.Y * 0.5f)));
        // waveBuilder.SetResolution(100);
        // waveBuilder.SetFrequency(1f);
        // waveBuilder.SetAmplitude(0.5f);
        // waveBuilder.SetPhase(0f);
        // waveBuilder.ShowInfo(true);
        // waveSystem.AddWaveInstance(waveBuilder.Build());
        // Systems.Add(waveSystem);
    }

    

}