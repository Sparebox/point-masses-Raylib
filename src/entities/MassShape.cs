using System.Numerics;
using ImGuiNET;
using Physics;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Entities;

public partial class MassShape : Entity
{
    public Vector2 TotalVisForce
    {
        get
        {
            return _points.Aggregate(
                new Vector2(),
                (totalVisForce, p) => totalVisForce += p.VisForce
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
                (centerOfMass, p) => centerOfMass += p.Mass * p.Pos, 
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
                (centroid, p) => centroid += p.Pos,
                centroid => centroid / _points.Count
            );
        }
    }

    public Vector2 Vel => CenterOfMass - _lastCenterOfMass;

    public float Inertia
    {
        get
        {
            Vector2 COM = CenterOfMass;
            float inertia = 0f;
            foreach (var p in _points)
            {
                float distSq = Vector2.DistanceSquared(COM, p.Pos);
                inertia += p.Mass * distSq;
            }
            return inertia;
        }
    }

    public Vector2 Momentum => Mass * Vel;

    public float RotEnergy
    {
        get
        {
            float angVel = AngVel / Ctx.Substep;
            return 0.5f * Inertia * angVel * angVel;
        }
    }

    public float LinEnergy
    {
        get
        {
            var vel = Vel / Ctx.Substep;
            return 0.5f * Mass * vel.LengthSquared();
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
                area += (p1.Pos.Y + p2.Pos.Y) * (p1.Pos.X - p2.Pos.X);
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
            Vector2 pos = _points.First().Pos;
            Vector2 dir = pos - CenterOfMass;
            return MathF.Atan2(dir.Y, dir.X);
        }
    }

    public float AngVel
    {
        get
        {
            return Angle - _lastAngle;
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
                if (p.Pos.X - p.Radius <= minX)
                {
                    minX = p.Pos.X - p.Radius;
                }
                if (p.Pos.Y - p.Radius <= minY)
                {
                    minY = p.Pos.Y - p.Radius;
                }
                if (p.Pos.X + p.Radius >= maxX)
                {
                    maxX = p.Pos.X + p.Radius;
                }
                if (p.Pos.Y + p.Radius >= maxY)
                {
                    maxY = p.Pos.Y + p.Radius;
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
                if (p.Pos.X - p.Radius <= minX)
                {
                    minX = p.Pos.X - p.Radius;
                }
                if (p.Pos.Y - p.Radius <= minY)
                {
                    minY = p.Pos.Y - p.Radius;
                }
                if (p.Pos.X + p.Radius >= maxX)
                {
                    maxX = p.Pos.X + p.Radius;
                }
                if (p.Pos.Y + p.Radius >= maxY)
                {
                    maxY = p.Pos.Y + p.Radius;
                }
            }
            float margin = UnitConv.PixelsToMeters(1f);
            return new BoundingBox()
            {
                Max = new(maxX + margin, maxY + margin, 0f),
                Min = new(minX - margin, minY - margin, 0f)
            };
        }
    }
    
    public List<Constraint> _constraints;
    public List<PointMass> _points;
    public bool _toBeDeleted;
    public float _gasAmount;
    public bool _inflated;
    public bool _showInfo;
    private Vector2 _lastCenterOfMass;
    private PressureVis _pressureVis;
    private float _lastAngle;

    public MassShape(Context ctx, bool inflated = false) : base(ctx)
    {
        _inflated = inflated;
        _toBeDeleted = false;
        _points = new();
        _constraints = new();
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
            Constraint copyConstraint = new DistanceConstraint((DistanceConstraint) c);
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
            DrawRectangleLines(
                UnitConv.MetersToPixels(aabb.Min.X),
                UnitConv.MetersToPixels(aabb.Min.Y),
                UnitConv.MetersToPixels(aabb.Max.X - aabb.Min.X),
                UnitConv.MetersToPixels(aabb.Max.Y - aabb.Min.Y),
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
            Vector2 COM = UnitConv.MetersToPixels(CenterOfMass);
            Graphics.DrawArrow(COM, COM + Raymath.Vector2ClampValue(TotalVisForce, 0f, 150f), Color.Magenta);
        }
        if (_showInfo)
        {
            DrawInfo();
        }
    }

    public void ApplyForce(in Vector2 force)
    {
        foreach (PointMass p in _points)
        {
            p.ApplyForce(force);
        }
    }

    public void ApplyForceCOM(in Vector2 force)
    {
        foreach (PointMass p in _points)
        {
            p.ApplyForce(p.Mass * force);
        }
    }

    public void Move(in Vector2 translation)
    {
        foreach (PointMass p in _points)
        {
            p.Pos += translation;
            p.PrevPos = p.Pos;
        }
    }

    private void Inflate()
    {
        for (int i = 0; i < _points.Count; i++)
        {
            PointMass p1 = _points[i];
            PointMass p2 = _points[(i + 1) % _points.Count];
            Vector2 P1ToP2 = p2.Pos - p1.Pos;
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
                line._start.X = p1.Pos.X + 0.5f * P1ToP2.X;
                line._start.Y = p1.Pos.Y + 0.5f * P1ToP2.Y;
                line._end.X = p1.Pos.X + 0.5f * P1ToP2.X + force.X * PressureVis.VisForceMult;
                line._end.Y = p1.Pos.Y + 0.5f * P1ToP2.Y + force.Y * PressureVis.VisForceMult;
                line._start = UnitConv.MetersToPixels(line._start);
                line._end = UnitConv.MetersToPixels(line._end);
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
        Vector2 centroidViewPos = Ctx.Camera.ViewPos(UnitConv.MetersToPixels(Centroid));
        ImGui.Begin($"Body id {Id} info", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        ImGui.SetWindowPos(centroidViewPos + new Vector2(25f, 0f));
        ImGui.SetWindowSize(new (250f, 130f));
        ImGui.Text($"Mass: {Mass} kg");
        ImGui.Text($"Velocity: {Vel / Ctx.Substep:0.0} m/s");
        ImGui.Text($"Momentum: {Momentum / Ctx.Substep:0.0} kgm/s");
        ImGui.Text($"Moment of inertia: {Inertia:0} kgm^2");
        ImGui.Text($"Angular vel: {AngVel / Ctx.Substep * RAD2DEG:0} deg/s");
        ImGui.Text($"Linear energy: {LinEnergy:0.##} J");
        ImGui.Text($"Rot energy: {RotEnergy:0.##} J");
        ImGui.End();
        var centerOfMassIcon = Ctx.TextureManager.GetTexture("center_of_mass.png");
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