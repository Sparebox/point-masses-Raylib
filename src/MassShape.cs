using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using Sim;
using Utils;
using static Raylib_cs.Raylib;

namespace Physics;

public class MassShape
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

    public float Mass
    {
        get 
        { 
            if (_mass.HasValue)
            {
                return (float) _mass;
            }
            _mass = _points.Select(p => p.Mass).Sum();
            return (float) _mass;
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

    public Vector2 Vel
    {
        get
        {
            Vector2 vel = (CenterOfMass - _lastCenterOfMass) / _context.SubStep;
            return vel;
        }
    }

    public float AngularMass
    {
        get
        {
            if (IsRigid && _angularMass.HasValue) 
            {
                return (float) _angularMass;
            }
            Vector2 COM = CenterOfMass;
            float angularMass = 0f;
            foreach (var p in _points)
            {
                float distSq = Vector2.DistanceSquared(COM, p.Pos);
                angularMass += p.Mass * distSq;
            }
            if (IsRigid)
            {
                _angularMass = angularMass;
            }
            return angularMass;
        }
        set => _angularMass = value;
    }

    public Vector2 Momentum
    {
        get
        {
            return Mass * Vel;
        }
    }

    public float AngularMomentum
    {
        get
        {
            return AngularMass * AngVel;
        }
    }

    public bool IsRigid
    {
        get
        {
            if (_isRigid.HasValue) 
            {
                return _isRigid.Value;
            }
            foreach (var c in _constraints)
            {
                if (c.GetType() == typeof(SpringConstraint))
                {
                    _isRigid = false;
                    return _isRigid.Value;
                }
            }
            _isRigid = true;
            return _isRigid.Value;
        }
    }

    public float RotEnergy
    {
        get
        {
            float angVel = AngVel;
            return 0.5f * AngularMass * angVel * angVel;
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
                area += (p1.Pos.Y + p2.Pos.Y) * (p1.Pos.X - p2.Pos.X);
            }
            return area / 2f;
        }
    }

    public float Angle
    {
        get
        {
            if (!_points.Any() || _points.Count == 1)
            {
                return 0f;
            }
            Vector2 com = CenterOfMass;
            Vector2 pos = _points.First().Pos;
            Vector2 dir = pos - com;
            return MathF.Atan2(dir.Y, dir.X);
        }
    }

    public float AngVel
    {
        get
        {
            if (_points.Count == 1)
            {
                return 0f;
            }
            return (Angle - _lastAngle) / _context.SubStep;
        }
    }

    public BoundingBox AABB
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
    
    public const float GasAmountMult = 1f;
    private readonly Context _context;
    private readonly bool _inflated;
    private static int _idCounter;

    public int Id { get; init; }
    public List<Constraint> _constraints;
    public List<PointMass> _points;
    public bool _toBeDeleted;
    private Vector2 _lastCenterOfMass;
    private float _lastAngle;
    private PressureVis _pressureVis;
    private float? _mass;
    private float? _angularMass;
    private float _gasAmount;
    private bool? _isRigid;
    private const float SpringDamping = 5e4f;

    public MassShape(Context context, bool inflated) 
    {
        _context = context;
        _inflated = inflated;
        Id = _idCounter++;
        _toBeDeleted = false;
    }

    // Copy constructor
    public MassShape(in MassShape shape)
    {
        _context = shape._context;
        _inflated = shape._inflated;
        _gasAmount = shape._gasAmount;
        _isRigid = shape._isRigid;
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
            Constraint copyConstraint = null;
            if (c.GetType() == typeof(SpringConstraint))
            {
                copyConstraint = new SpringConstraint((SpringConstraint) c);
            }
            if (c.GetType() == typeof(RigidConstraint))
            {
                copyConstraint = new RigidConstraint((RigidConstraint) c);
            }
            copyConstraint.PointA = _points.Where(p => p.Equals(copyConstraint.PointA)).First();
            copyConstraint.PointB = _points.Where(p => p.Equals(copyConstraint.PointB)).First();
            _constraints.Add(copyConstraint);
        }
    }

    public void Update()
    {
        _lastAngle = Angle;
        if (!_points.Any())
        {
            _toBeDeleted = true;
            return;
        }
        if (_context._drawBodyInfo)
        {
            _lastCenterOfMass = CenterOfMass;
        }
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
            BoundingBox aabb = AABB;
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
                    Graphics.DrawArrow(line._start, line._end, Color.Magenta);
                }
            }
            Vector2 COM = CenterOfMass;
            Vector2 totalVisForce = TotalVisForce;
            Graphics.DrawArrow(COM, COM + totalVisForce * 1e-2f, Color.Magenta);
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
                _pressureVis._lines[i] = line;
            }
        }
    }

    public void DeletePoints(List<int> ids)
    {
        foreach (var id in ids)
        {
            DeletePoint(id);
        }
    }

    public void DeletePoint(int id)
    {
        List<int> constraintsToDelete = new();
        int? pointToDelete = null;
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
        var thisAABB = AABB;
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

    private void HandlePointOnPointCollisions(MassShape otherShape, BoundingBox otherAABB)
    {
        foreach (var pointA in _points)
        {
            if (!CheckCollisionBoxes(pointA.AABB, otherAABB))
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
            var nearShapes = context.QuadTree.QueryShapes(shapeA.AABB);
            foreach (var shapeB in nearShapes)
            {
                if (shapeA.Equals(shapeB))
                {
                    continue;
                }
                shapeA.HandlePointOnPointCollisions(shapeB, shapeB.AABB);
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
        closestA.Vel = closestApreVel - impulse / 2f / (combinedMass - closestB.Mass);
        closestB.Vel = closestBpreVel - impulse / 2f / (combinedMass - closestA.Mass);
        // Apply friction
        pointMass.ApplyFriction(-normal);
    }

    private (PointMass, PointMass, Vector2) FindClosestPoints(Vector2 pos)
    {
        float closestDistSq = float.MaxValue;
        PointMass closestA = null;
        PointMass closestB = null;
        Vector2 closestPoint = new();
        for (int i = 0; i < _points.Count; i++)
        {
            PointMass lineStart = _points[i];
            PointMass lineEnd = _points[(i + 1) % _points.Count];
            Vector2 pointOnLine = Utils.Geometry.ClosestPointOnLine(lineStart.Pos, lineEnd.Pos, pos);
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
        ImGui.Text(string.Format("Velocity: {0:0.0} m/s", Vel));
        ImGui.Text(string.Format("Momentum: {0:0.0} kgm/s", Momentum));
        ImGui.Text(string.Format("Angular momentum: {0:0.0} Js", AngularMomentum));
        ImGui.Text(string.Format("Moment of inertia: {0:0} kgm^2", AngularMass));
        ImGui.Text(string.Format("Angular vel: {0:0} deg/s", AngVel * RAD2DEG));
        ImGui.Text(string.Format("Angle: {0:0} deg", Angle * RAD2DEG));
        ImGui.Text(string.Format("Linear energy: {0:0.##} J", LinEnergy));
        ImGui.Text(string.Format("Rot energy: {0:0.##} J", RotEnergy));
        ImGui.End();
        Vector2 offset = new(-_context.TextureManager._centerOfMassIcon.Width / 2f, -_context.TextureManager._centerOfMassIcon.Height / 2f);
        DrawTextureEx(_context.TextureManager._centerOfMassIcon, UnitConv.MetersToPixels(Centroid) + 0.5f * offset, 0f, 0.5f, Color.White);
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

    // Shape constructors

    public static MassShape SoftBall(float x, float y, float radius, float mass, int res, float stiffness, float gasAmount, Context context)
    {
        float angle = MathF.PI / 2f;
        MassShape s = new(context, true)
        {
            _points = new(),
            _constraints = new(),
            _gasAmount = gasAmount
        };
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, context));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new SpringConstraint(s._points[i], s._points[(i + 1) % res], stiffness, SpringDamping));
        }
        return s;
    }

    public static MassShape HardBall(float x, float y, float radius, float mass, int res, Context context)
    {
        float angle = MathF.PI / 2f;
        MassShape s = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };
        // Points
        for (int i = 0; i < res; i++)
        {
            float x0 = radius * MathF.Cos(angle);
            float y0 = radius * MathF.Sin(angle);
            s._points.Add(new(x0 + x, y0 + y, mass / res, false, context));
            angle += 2f * MathF.PI / res;
        }
        // Constraints
        List<int> visitedPoints = new();
        for (int i = 0; i < res; i++)
        {
            s._constraints.Add(new RigidConstraint(s._points[i], s._points[(i + 1) % res]));
            if (!visitedPoints.Contains(i))
            {
                int nextIndex = res % 2 == 0 ? (i + res / 2 - 1) % res : (i + res / 2) % res;
                s._constraints.Add(new RigidConstraint(s._points[i], s._points[nextIndex]));
                visitedPoints.Add(i);
            }
        }
        
        return s;
    }

    public static MassShape Chain(float x0, float y0, float x1, float y1, float mass, int res, (bool, bool) pins, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };
        Vector2 start = new(x0, y0);
        Vector2 end = new(x1, y1);
        float len = Vector2.Distance(start, end);
        float spacing = len / (res - 1);
        Vector2 dir = (end - start) / len;
        // Points
        for (int i = 0; i < res; i++)
        {
            bool pinned;
            if (pins.Item1 && pins.Item2)
            {
                pinned = i == 0 || i == res - 1;
            } else if (pins.Item1)
            {
                pinned = i == 0;
            } else if (pins.Item2)
            {
                pinned = i == res - 1;
            } else
            {
                pinned = false;
            }
            c._points.Add(new(start.X + i * spacing * dir.X, start.Y + i * spacing * dir.Y, mass / res, pinned, context));
        }
        // Constraints
        for (int i = 0; i < res - 1; i++)
        {
            c._constraints.Add(new RigidConstraint(c._points[i], c._points[i + 1]));
        }
        return c;
    }

    public static MassShape Cloth(float x, float y, float width, float height, float mass, int res, float stiffness, Context context)
    {
        float metersPerConstraintW = width / res;
        float metersPerConstraintH = height / res;
        MassShape c = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };
        // Points
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                bool pinned = (col == 0 || col == res - 1) && row == 0;
                c._points.Add(new(x + col * metersPerConstraintW, y + row * metersPerConstraintH, mass, pinned, context));
            }
        }
        // Constraints
        for (int col = 0; col < res; col++)
        {
            for (int row = 0; row < res; row++)
            {
                if (col != res - 1)
                {
                    if (stiffness == 0f)
                    {
                        c._constraints.Add(new RigidConstraint(c._points[col * res + row], c._points[(col + 1) * res + row]));
                    } else
                    {
                        c._constraints.Add(new SpringConstraint(c._points[col * res + row], c._points[(col + 1) * res + row], stiffness, SpringDamping));
                    }
                }
                if (row != res - 1)
                {
                    if (stiffness == 0f)
                    {
                        c._constraints.Add(new RigidConstraint(c._points[col * res + row], c._points[col * res + row + 1]));
                    } else 
                    {
                        c._constraints.Add(new SpringConstraint(c._points[col * res + row], c._points[col * res + row + 1], stiffness, SpringDamping));
                    }
                }
            }
        }
        return c;
    }

    public static MassShape Pendulum(float x, float y, float length, float mass, int order, Context context)
    {
        if (order < 1)
        {
            return null;
        }
        MassShape c = new(context, false)
        {
            _points = new(),
            _constraints = new()
        };

        // Points
        for (int i = 0; i < order + 1; i++)
        {
            c._points.Add(new PointMass(x, y, mass / (order + 1), i == 0, context));
            y += length / (order + 1);
        }
        // Constraints
        for (int i = 0; i < order; i++)
        {
            c._constraints.Add(new RigidConstraint(c._points[i], c._points[i + 1]));
        }
        return c;
    }

    public static MassShape Box(float x, float y, float size, float mass, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new() 
            {
                new(x - size / 2f, y - size / 2f, mass / 4f, false, context),
                new(x - size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y - size / 2f, mass / 4f, false, context)
            },
            _constraints = new()
        };
        c._constraints.Add(new RigidConstraint(c._points[0], c._points[1]));
        c._constraints.Add(new RigidConstraint(c._points[1], c._points[2]));
        c._constraints.Add(new RigidConstraint(c._points[2], c._points[3]));
        c._constraints.Add(new RigidConstraint(c._points[3], c._points[0]));
        c._constraints.Add(new RigidConstraint(c._points[0], c._points[2]));
        return c;
    }

    public static MassShape SoftBox(float x, float y, float size, float mass, float stiffness, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new() 
            {
                new(x - size / 2f, y - size / 2f, mass / 4f, false, context),
                new(x - size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y + size / 2f, mass / 4f, false, context),
                new(x + size / 2f, y - size / 2f, mass / 4f, false, context)
            },
            _constraints = new()
        };
        c._constraints.Add(new SpringConstraint(c._points[0], c._points[1], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[1], c._points[2], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[2], c._points[3], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[3], c._points[0], stiffness, SpringDamping));
        c._constraints.Add(new SpringConstraint(c._points[0], c._points[2], stiffness, SpringDamping));
        return c;
    }

    public static MassShape Particle(float x, float y, float mass, Context context)
    {
        MassShape c = new(context, false)
        {
            _points = new() { new(x, y, mass, false, context) },
            _constraints = new()
        };
        return c;
    }
}