using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using PointMasses.Physics;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;
using Newtonsoft.Json;

namespace PointMasses.Entities;

[JsonObject(MemberSerialization.OptIn)]
public partial class MassShape : Entity
{
    public Vector2 TotalVisForce
    {
        get
        {
            return _points.Aggregate(
                new Vector2(),
                (totalVisForce, p) => totalVisForce += p._visForce
            );
        }
    }

    public override float Mass
    {
        get 
        { 
            if (_mass.HasValue)
            {
                return _mass.Value;
            }
            base.Mass = _points.Select(p => p.Mass).Sum();
            return base.Mass;
        } 
    }

    public override Vector2 CenterOfMass
    {
        get
        {
            return _points.Aggregate(
                new Vector2(), 
                (centerOfMass, p) => centerOfMass += p.Mass * p._pos, 
                centerOfMass => centerOfMass / Mass
            );
        }
    }

    public override Vector2 Centroid
    {
        get
        {
            return _points.Aggregate(
                new Vector2(),
                (centroid, p) => centroid += p._pos,
                centroid => centroid / _points.Count
            );
        }
    }

    public Vector2 Vel => (CenterOfMass - _lastCenterOfMass) / Ctx.Substep;

    public float Inertia
    {
        get
        {
            Vector2 COM = CenterOfMass;
            float inertia = 0f;
            foreach (var p in _points)
            {
                float distSq = Vector2.DistanceSquared(COM, p._pos);
                inertia += p.Mass * distSq;
            }
            return inertia;
        }
    }

    public Vector2 Momentum => Mass * Vel;

    public float AngularMomentum => Inertia * AngVel;

    public float RotEnergy
    {
        get
        {
            return 0.5f * Inertia * AngVel * AngVel;
        }
    }

    public float LinEnergy
    {
        get
        {
            return 0.5f * Mass * Vel.LengthSquared();
        }
    }
    
    // pseudo 2D volume a.k.a. area
    public float Volume
    {
        get
        {
            float area = 0f;
            for (int i = 0; i < _points.Count; i++)
            {
                PointMass p1 = _points[i];
                PointMass p2 = _points[(i + 1) % _points.Count];
                area += (p1._pos.Y + p2._pos.Y) * (p1._pos.X - p2._pos.X);
            }
            return area * 0.5f;
        }
    }

    private float Angle
    {
        get
        {
            if (!_points.Any() || _points.Count == 1)
            {
                return 0f;
            }
            Vector2 pos = _points.First()._pos;
            Vector2 dir = pos - CenterOfMass;
            return MathF.Atan2(dir.Y, dir.X);
        }
    }

    public float AngVel
    {
        get
        {
            return (Angle - _lastAngle) / Ctx.Substep;
        }
    }

    public override BoundingBox Aabb
    {
        get
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = 0f;
            float maxY = 0f;
            foreach (var p in _points)
            {
                if (p._pos.X - p.Radius <= minX)
                {
                    minX = p._pos.X - p.Radius;
                }
                if (p._pos.Y - p.Radius <= minY)
                {
                    minY = p._pos.Y - p.Radius;
                }
                if (p._pos.X + p.Radius >= maxX)
                {
                    maxX = p._pos.X + p.Radius;
                }
                if (p._pos.Y + p.Radius >= maxY)
                {
                    maxY = p._pos.Y + p.Radius;
                }
            }
            return new BoundingBox()
            {
                Max = new(maxX, maxY, 0f),
                Min = new(minX, minY, 0f)
            };
        }
    }

    public BoundingBox AabbMargin
    {
        get
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = 0f;
            float maxY = 0f;
            foreach (var p in _points)
            {
                if (p._pos.X - p.Radius <= minX)
                {
                    minX = p._pos.X - p.Radius;
                }
                if (p._pos.Y - p.Radius <= minY)
                {
                    minY = p._pos.Y - p.Radius;
                }
                if (p._pos.X + p.Radius >= maxX)
                {
                    maxX = p._pos.X + p.Radius;
                }
                if (p._pos.Y + p.Radius >= maxY)
                {
                    maxY = p._pos.Y + p.Radius;
                }
            }
            float margin = UnitConv.PtoM(1f);
            return new BoundingBox()
            {
                Max = new(maxX + margin, maxY + margin, 0f),
                Min = new(minX - margin, minY - margin, 0f)
            };
        }
    }
    
    [JsonProperty]
    public List<DistanceConstraint> _constraints;
    [JsonProperty]
    public List<PointMass> _points;
    public bool _toBeDeleted;
    [JsonProperty]
    public float _gasAmount;
    [JsonProperty]
    public bool _inflated;
    public bool _showInfo;
    [JsonProperty]
    private Vector2 _lastCenterOfMass;
    private PressureVis _pressureVis;
    [JsonProperty]
    private float _lastAngle;

    public MassShape(Context ctx, bool inflated = false) : base(ctx)
    {
        _inflated = inflated;
        _toBeDeleted = false;
        _points = new();
        _constraints = new();
    }

    [JsonConstructor]
    public MassShape(Context ctx, List<PointMass> points, List<DistanceConstraint> constraints, bool inflated = false) :base(ctx)
    {
        _inflated = inflated;
        _toBeDeleted = false;
        _points = points;
        _constraints = constraints;
    }

    // Copy constructor
    public MassShape(in MassShape shape) : base(shape.Ctx, shape.Mass)
    {
        _inflated = shape._inflated;
        _gasAmount = shape._gasAmount;
        _toBeDeleted = false;
        _points = new();
        _constraints = new();
        foreach (var p in shape._points)
        {
            _points.Add(new PointMass(p));
        }
        foreach (var c in shape._constraints)
        {
            var copyConstraint = new DistanceConstraint(c);
            copyConstraint.PointA = _points.Where(p => p.Equals(copyConstraint.PointA)).First();
            copyConstraint.PointB = _points.Where(p => p.Equals(copyConstraint.PointB)).First();
            _constraints.Add(copyConstraint);
        }
    }

    public override void Update()
    {
        if (!_points.Any())
        {
            _toBeDeleted = true;
            return;
        }
        _lastCenterOfMass = CenterOfMass;
        _lastAngle = Angle;
        
        _constraints = _constraints.OrderBy(_ => Rng.Gen.Next(_constraints.Count)).ToList(); // Iterate in random order to prevent ghost torque
        for (int i = 0; i < Constants.ConstraintIterations; i++)
        {
            foreach (Constraint c in _constraints) 
            {
                c.Update();
            }
        }
        foreach (PointMass p in _points)
        {
            p.Update();
        }
        if (_inflated)
        {
            Inflate();
        }
    }

    public override void Draw()
    {
        foreach (Constraint c in _constraints)
        {
            c.Draw();
        }
        foreach (PointMass p in _points)
        {
            p.Draw();
        }
        if (Ctx._drawAABBS)
        {
            BoundingBox aabb = Aabb;
            aabb.Min = UnitConv.MtoP(aabb.Min);
            aabb.Max = UnitConv.MtoP(aabb.Max);
            DrawRectangleLines(
                (int) aabb.Min.X,
                (int) aabb.Min.Y,
                (int) (aabb.Max.X - aabb.Min.X),
                (int) (aabb.Max.Y - aabb.Min.Y),
                Color.Red
            );
        }
        if (Ctx._drawForces)
        {
            if (_inflated && _pressureVis._lines != null)
            {
                // Draw pressure forces acting on normals
                foreach (VisLine line in _pressureVis._lines)
                {
                    Vector2 clampedLine = Raymath.Vector2ClampValue(line._end - line._start, 0f, 150f);
                    Graphics.DrawArrow(line._start, line._start + clampedLine, Color.Magenta);
                }
            }
            Vector2 COM = UnitConv.MtoP(CenterOfMass);
            Graphics.DrawArrow(COM, COM + Raymath.Vector2ClampValue(TotalVisForce, 0f, 150f), Color.Magenta);
        }
        if (_showInfo)
        {
            DrawInfo();
        }
    }

    public void ApplyForce(Vector2 force)
    {
        foreach (PointMass p in _points)
        {
            p.ApplyForce(force);
        }
    }

    public void ApplyForceCOM(Vector2 force)
    {
        foreach (PointMass p in _points)
        {
            p.ApplyForce(p.Mass * force);
        }
    }

    public void Move(Vector2 translation)
    {
        foreach (PointMass p in _points)
        {
            p._pos += translation;
            p._prevPos = p._pos;
        }
    }

    private void Inflate()
    {
        for (int i = 0; i < _points.Count; i++)
        {
            PointMass p1 = _points[i];
            PointMass p2 = _points[(i + 1) % _points.Count];
            Vector2 P1ToP2 = p2._pos - p1._pos;
            float faceLength = P1ToP2.Length();
            Vector2 normal = new(P1ToP2.Y, -P1ToP2.X);
            normal /= faceLength;
            Vector2 force = faceLength * Constants.GasAmountMult * _gasAmount / Volume * 0.5f * normal;
            p1.ApplyForce(force);
            p2.ApplyForce(force);
            if (Ctx._drawForces)
            {   
                _pressureVis._lines ??= new VisLine[_points.Count];
                if (_pressureVis._lines.Length != _points.Count)
                {
                    // Update line count since points changed
                    _pressureVis._lines = new VisLine[_points.Count];
                }
                VisLine line = new();
                line._start.X = p1._pos.X + 0.5f * P1ToP2.X;
                line._start.Y = p1._pos.Y + 0.5f * P1ToP2.Y;
                line._end.X = p1._pos.X + 0.5f * P1ToP2.X + force.X * PressureVis.VisForceMult;
                line._end.Y = p1._pos.Y + 0.5f * P1ToP2.Y + force.Y * PressureVis.VisForceMult;
                line._start = UnitConv.MtoP(line._start);
                line._end = UnitConv.MtoP(line._end);
                _pressureVis._lines[i] = line;
            }
        }
    }

    public void DeletePoints(List<uint> ids)
    {
        foreach (var id in ids)
        {
            DeletePoint(id);
        }
    }

    public void DeletePoint(uint id)
    {
        HashSet<uint> constraintsToDelete = new();
        uint? pointToDelete = null;
        foreach (var p in _points)
        {
            if (p.Id != id)
            {
                continue;
            }
            // Delete constraint that has this point
            foreach (var c in _constraints)
            {
                if (c.PointA.Id == id || c.PointB.Id == id)
                {
                    constraintsToDelete.Add(c.Id);
                }
            }
            pointToDelete = p.Id;
            break;
        }
        if (!pointToDelete.HasValue)
        {
            return;
        }
        _constraints.RemoveAll(c => constraintsToDelete.Contains(c.Id));
        _points.RemoveAll(p => p.Id == pointToDelete.Value);
    }

    private void DrawInfo()
    {
        Vector2 centroidViewPos = UnitConv.MtoP(Centroid);
        ImGui.Begin($"Body entity id {Id} info", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        ImGui.SetWindowPos(GetWorldToScreen2D(centroidViewPos + new Vector2(25f, 0f), Ctx._camera));
        ImGui.SetWindowSize(new (250f, 130f));
        ImGui.Text($"Mass: {Mass} kg");
        ImGui.Text($"Velocity: {Vel:0.0} m/s");
        ImGui.Text($"Momentum: {Momentum:0.0} kgm/s");
        ImGui.Text($"Moment of inertia: {Inertia:0} kgm^2");
        ImGui.Text($"Angular vel: {AngVel * RAD2DEG / 6f:0} RPM");
        ImGui.Text($"Angular momentum: {AngularMomentum:0} kgm^2/s");
        ImGui.Text($"Linear energy: {LinEnergy:0.##} J");
        ImGui.Text($"Rot energy: {RotEnergy:0.##} J");
        ImGui.End();
        var centerOfMassIcon = Program.TextureManager.GetTexture("center_of_mass.png");
        Vector2 offset = new(-centerOfMassIcon.Width * 0.5f, -centerOfMassIcon.Height * 0.5f);
        DrawTextureEx(centerOfMassIcon, centroidViewPos + offset * 0.5f, 0f, 0.5f, Color.White);
    }

    public static bool operator == (MassShape a, MassShape b)
    {
        return a.Id == b.Id;
    }

    public static bool operator != (MassShape a, MassShape b)
    {
        return a.Id != b.Id;
    }

    public override bool Equals(object obj)
    {
        if (obj is null || !obj.GetType().Equals(typeof(MassShape)))
        {
            return false;
        }
        return Id == ((MassShape) obj).Id;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    private struct PressureVis
    {
        public const float VisForceMult = 0.5e-1f;
        public VisLine[] _lines;
    }

    private struct VisLine
    {
        public Vector2 _start;
        public Vector2 _end;
    }
}