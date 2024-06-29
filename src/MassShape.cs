using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Physics;

public partial class MassShape
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

    public float Mass
    {
        get 
        { 
            if (_mass.HasValue)
            {
                return _mass.Value;
            }
            _mass = _points.Select(p => p.Mass).Sum();
            return _mass.Value;
        } 
    }

    public Vector2 CenterOfMass
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

    public Vector2 Centroid
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

    public float AngularMomentum
    {
        get
        {
            float angularMomentum = 0f;
            foreach (var point in _points)
            {
                Vector2 momentum = (Vel - point.Vel) * point.Mass;
                Vector2 radius = point.Pos - CenterOfMass;
                Vector3 cross = Vector3.Cross(new(radius, 0f), new(momentum, 0f));
                angularMomentum += float.Sign(cross.Z) * cross.Length();
            }
            return angularMomentum;
        }
    }

    public float RotEnergy
    {
        get
        {
            float angVel = AngVel / _context.SubStep;
            return 0.5f * Inertia * angVel * angVel;
        }
    }

    public float LinEnergy
    {
        get
        {
            return 0.5f * Mass * Vel.LengthSquared() / _context.SubStep;
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

    public float Angle => _angle;

    public float AngVel
    {
        get
        {
            if (_points.Count == 1)
            {
                return 0f;
            }
            return AngularMomentum / Inertia;
        }
    }

    public BoundingBox Aabb
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
    
    public const float GasAmountMult = 1f;
    public uint Id { get; init; }
    public List<Constraint> _constraints;
    public List<PointMass> _points;
    public bool _toBeDeleted;
    public float _gasAmount;
    public bool _inflated;
    private readonly Context _context;
    private static uint _idCounter;
    private Vector2 _lastCenterOfMass;
    private PressureVis _pressureVis;
    private float? _mass;
    private float _angle;

    public MassShape(Context context, bool inflated = false) 
    {
        _context = context;
        _inflated = inflated;
        Id = _idCounter++;
        _toBeDeleted = false;
        _points = new();
        _constraints = new();
    }

    // Copy constructor
    public MassShape(in MassShape shape)
    {
        _context = shape._context;
        _inflated = shape._inflated;
        _gasAmount = shape._gasAmount;
        _mass = shape._mass;
        Id = _idCounter++;
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

    public void Update()
    {
        if (!_points.Any())
        {
            _toBeDeleted = true;
            return;
        }
        _lastCenterOfMass = CenterOfMass;
        _angle += AngVel;
        _angle %= 2f * MathF.PI;
        foreach (Constraint c in _constraints)
        {
            c.Update();
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

    public void Draw()
    {
        foreach (Constraint c in _constraints)
        {
            c.Draw();
        }
        foreach (PointMass p in _points)
        {
            p.Draw();
        }
        if (_context._drawAABBS)
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
        if (_context._drawForces)
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
            Vector2 totalVisForce = UnitConv.MetersToPixels(TotalVisForce);
            totalVisForce = Raymath.Vector2ClampValue(totalVisForce, 0f, 150f);
            Graphics.DrawArrow(COM, COM + totalVisForce, Color.Magenta);
        }
        if (_context._drawBodyInfo)
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
            Vector2 force = faceLength * GasAmountMult * _gasAmount / Volume / 2f * normal;
            p1.ApplyForce(force);
            p2.ApplyForce(force);
            if (_context._drawForces)
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

    private bool CheckLineCollision(PointMass otherPoint)
    {
        for (int i = 0; i < _points.Count; i++)
        {
            Vector2 startPos = _points[i].Pos;
            Vector2 endPos = _points[(i + 1) % _points.Count].Pos;
            Vector2 towardsClosestPoint = Geometry.ClosestPointOnLine(startPos, endPos, otherPoint.Pos) - otherPoint.Pos;
            float distSq = towardsClosestPoint.LengthSquared();
            if (distSq <= otherPoint.Radius * otherPoint.Radius)
            {
                return true;
            }
        }
        return false;
    }

    private void HandleLineCollisions(MassShape otherShape)
    {
        if (_points.Count == 1 && otherShape._points.Count == 1)
        {
            return;
        }
        var thisAABB = Aabb;
        foreach (var point in otherShape._points)
        {
            if (!CheckCollisionBoxes(point.AABB, thisAABB))
            {
                continue;
            }
            if (CheckLineCollision(point))
            {
                HandleLineCollision(point);
            }
        }
    }

    private void HandlePointOnPointCollisions(MassShape otherShape)
    {
        foreach (var pointA in _points)
        {
            if (!CheckCollisionBoxes(pointA.AABB, otherShape.Aabb))
            {
                continue;
            }
            foreach (var pointB in otherShape._points)
            {
                var collisionResult = pointA.CheckPointToPointCollision(pointB);
                if (collisionResult.HasValue)
                {
                    PointMass.HandlePointToPointCollision(collisionResult.Value, _context);
                }
            }
        }
    }

    public static void HandleCollisions(Context context)
    {
        foreach (var shapeA in context.MassShapes)
        {
            var nearShapes = context.GetMassShapes(shapeA.AabbMargin);
            foreach (var shapeB in nearShapes)
            {
                if (shapeA.Equals(shapeB))
                {
                    continue;
                }
                shapeA.HandlePointOnPointCollisions(shapeB);
                shapeA.HandleLineCollisions(shapeB);
            }
        }
    }

    private void HandleLineCollision(PointMass pointMass)
    {
        (PointMass closestA, PointMass closestB, Vector2 closestPoint) = FindClosestPoints(pointMass.Pos);
        Vector2 pointToClosest = closestPoint - pointMass.Pos;
        float totalOffset = pointMass.Radius - pointToClosest.Length();
        if (totalOffset == 0f)
        {
            return;
        }
        float lineLen = Vector2.Distance(closestA.Pos, closestB.Pos);
        if (lineLen == 0f)
        {
            return;
        }
        float distToB = Vector2.Distance(closestPoint, closestB.Pos);
        float aOffset = distToB / lineLen * totalOffset;
        float bOffset = totalOffset - aOffset;
        var normal = Vector2.Normalize(pointToClosest);
        Vector2 avgVel = (closestA.Vel + closestB.Vel) / 2f;
        Vector2 preVel = pointMass.Vel;
        Vector2 closestApreVel = closestA.Vel;
        Vector2 closestBpreVel = closestB.Vel;
        Vector2 relVel = preVel - avgVel;
        pointMass.Pos += totalOffset * -normal;
        closestA.Pos += aOffset * normal;
        closestB.Pos += bOffset * normal;
        // Apply impulse
        float combinedMass = closestA.Mass + closestB.Mass;
        float impulseMag = -(1f + _context._globalRestitutionCoeff) * Vector2.Dot(relVel, normal) / (1f / combinedMass + pointMass.InvMass);
        Vector2 impulse = impulseMag * normal;
        pointMass.Vel = preVel + impulse * pointMass.InvMass;
        closestA.Vel = closestApreVel - impulse * 0.5f / (combinedMass - closestB.Mass);
        closestB.Vel = closestBpreVel - impulse * 0.5f / (combinedMass - closestA.Mass);
        // Apply friction
        pointMass.ApplyFriction(-normal);
    }

    private (PointMass closestA, PointMass closestB, Vector2 closestPoint) FindClosestPoints(Vector2 pos)
    {
        float closestDistSq = float.MaxValue;
        PointMass closestA = null;
        PointMass closestB = null;
        Vector2 closestPoint = new();
        for (int i = 0; i < _points.Count; i++)
        {
            PointMass lineStart = _points[i];
            PointMass lineEnd = _points[(i + 1) % _points.Count];
            Vector2 pointOnLine = Geometry.ClosestPointOnLine(lineStart.Pos, lineEnd.Pos, pos);
            float distSq = Vector2.DistanceSquared(pointOnLine, pos);
            if (distSq < closestDistSq)
            {
                closestA = lineStart;
                closestB = lineEnd;
                closestDistSq = distSq;
                closestPoint = pointOnLine;
            }
        }
        return (closestA, closestB, closestPoint);
    }

    private void DrawInfo()
    {
        ImGui.Begin(string.Format("Body {0} info", Id), ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        ImGui.SetWindowPos(UnitConv.MetersToPixels(Centroid) + new Vector2(25f, 0f));
        ImGui.SetWindowSize(new (250f, 130f));
        ImGui.Text(string.Format("Mass: {0} kg", Mass));
        ImGui.Text(string.Format("Velocity: {0:0.0} m/s", Vel / _context.SubStep));
        ImGui.Text(string.Format("Momentum: {0:0.0} kgm/s", Momentum / _context.SubStep));
        ImGui.Text(string.Format("Angular momentum: {0:0.0} Js", AngularMomentum / _context.SubStep));
        ImGui.Text(string.Format("Moment of inertia: {0:0} kgm^2", Inertia));
        ImGui.Text(string.Format("Angular vel: {0:0} deg/s", AngVel / _context.SubStep * RAD2DEG));
        ImGui.Text(string.Format("Angle: {0:0} deg", Angle * RAD2DEG));
        ImGui.Text(string.Format("Linear energy: {0:0.##} J", LinEnergy));
        ImGui.Text(string.Format("Rot energy: {0:0.##} J", RotEnergy));
        ImGui.End();
        Vector2 offset = new(-_context.TextureManager.CenterOfMassIcon.Width / 2f, -_context.TextureManager.CenterOfMassIcon.Height / 2f);
        DrawTextureEx(_context.TextureManager.CenterOfMassIcon, UnitConv.MetersToPixels(Centroid) + 0.5f * offset, 0f, 0.5f, Color.White);
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