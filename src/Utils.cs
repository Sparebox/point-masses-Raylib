using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace PointMasses.Utils 
{
    public static class UnitConv
    {
        public const float PixelsPerMeter = 300f;

        public static float PtoM(float pixels)
        {
            return pixels / PixelsPerMeter;
        }

        public static int MtoP(float meters)
        {
            return (int) (PixelsPerMeter * meters);
        }

        public static Vector2 PtoM(in Vector2 pixels)
        {
            return new(pixels.X / PixelsPerMeter, pixels.Y / PixelsPerMeter);
        }

        public static Vector2 MtoP(in Vector2 meters)
        {
            return new(meters.X * PixelsPerMeter, meters.Y * PixelsPerMeter);
        }

        public static Vector3 MtoP(in Vector3 meters)
        {
            return new(meters.X * PixelsPerMeter, meters.Y * PixelsPerMeter, 0f);
        }
    }

    public static class Geometry
    {
        public static Vector2 ClosestPointOnLine(in Vector2 lineStart, in Vector2 lineEnd, in Vector2 point)
        {
            Vector2 startToPoint = point - lineStart;
            Vector2 startToEnd = lineEnd - lineStart;
            Vector2 startToEndNorm = Vector2.Normalize(startToEnd);
            float distOnLine = Vector2.Dot(startToPoint, startToEndNorm);
            if (distOnLine <= 0f)
            {
                return lineStart;
            }
            if (distOnLine * distOnLine > startToEnd.LengthSquared())
            {
                return lineEnd;
            }
            return lineStart + distOnLine * startToEndNorm;
        }
    }

    public static class Constants
    {
        public const float SpeedOfLight = 299_792_458f;
        public const float SpeedOfLightSq = SpeedOfLight * SpeedOfLight;
        public const float GravConstant = 6.67430e-11f;
        public const int PauseThresholdFPS = 15;
        public const int QuadTreeUpdateMs = 50;
        public const int NbodySystemUpdateMs = 50;
        // Point mass
        public const float RadiusPerMassRatio = 0.01f; // aka inverse density
        // Mass shape
        public const int ConstraintIterations = 3;
        public const float GasAmountMult = 1f;
        public const float GlobalRestitutionCoeffDefault = .3f;
        public const float GlobalKineticFrictionCoeffDefault = .1f;
        public const float GlobalStaticFrictionCoeffDefault = .0f;
        public const int BarnesHutMaxDepth = 10;
        public const float DefaultSpringDamping = 5e4f;
        // Pull tool
        public const float DefaultPullForceCoeff = 1e2f;
        // Wind tool
        public const int MaxWindForce = 100;
        public const int MinWindForce = 10;
        // Rotate tool
        public const float Torque = 1e2f;
        // Gravitywell tool
        public const float DefaultGravityWellConstant = 0.1f;
        public const float DefaultGravityWellMinDist = 0.01f;
    }

    public static class Graphics
    {
        public const float ArrowBranchLength = 10f;
        public const float ArrowAngle = 20f;

        public static void DrawArrow(float x0, float y0, float x1, float y1, Color color)
        {
            Vector2 start = new(x0, y0);
            Vector2 end = new(x1, y1);
            Vector2 dir = start - end;
            float lenSq = dir.LengthSquared();
            if (lenSq < 5f * 5f)
            {
                return;
            }
            dir = Vector2.Normalize(dir);
            float dirAngle = MathF.Atan2(dir.Y, dir.X);
            float radians = DEG2RAD * ArrowAngle;
            float angle1 = dirAngle + radians;
            float angle2 = dirAngle - radians;
            Vector2 branchA = new(ArrowBranchLength * MathF.Cos(angle1), ArrowBranchLength * MathF.Sin(angle1));
            Vector2 branchB = new(ArrowBranchLength * MathF.Cos(angle2), ArrowBranchLength * MathF.Sin(angle2));
            DrawLineV(start, end, color);
            DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchA.X), (int) (end.Y + branchA.Y), color);
            DrawLine((int) end.X, (int) end.Y, (int) (end.X + branchB.X), (int) (end.Y + branchB.Y), color);
        }

        public static void DrawArrow(in Vector2 start, in Vector2 end, Color color)
        {
            DrawArrow(start.X, start.Y, end.X, end.Y, color);
        }
    }

    public static class Rng
    {
        public static Random Gen { get; } = new Random();
    }

    public static class Perf
    {
        private const int HistoryLength = 10;
        private static readonly Stopwatch _stopwatch;
        private static readonly TimeSpan[] _history;
        private static int _historyIndex;
        private static int _lineStart;

        static Perf()
        {
            _stopwatch = new Stopwatch();
            _history = new TimeSpan[HistoryLength];
            _historyIndex = 0;
        }

        public static void PrintAvgMsSinceLast()
        {
            Update();
            float avgMilliSeconds = _history.Aggregate(
                0f, 
                (milliseconds, entry) => milliseconds + entry.Milliseconds,
                (milliseconds) => milliseconds / HistoryLength
            );
            AsyncConsole.WriteLine($"{avgMilliSeconds} avg ms");
        }

        public static void StartMeasure([CallerLineNumber] int lineNum = 0)
        {
            _lineStart = lineNum;
            _stopwatch.Restart();
        }

        public static void EndMeasure([CallerLineNumber] int lineNum = 0, [CallerMemberName] string caller = "")
        {
            _stopwatch.Stop();
            AsyncConsole.WriteLine($"{GetTime(): 0} - Measure from line {caller}:{_lineStart} to line {caller}:{lineNum} took {_stopwatch.ElapsedMilliseconds} ms");
        }

        private static TimeSpan Update()
        {
            TimeSpan elapsed = _stopwatch.Elapsed;
            _history[_historyIndex++ % HistoryLength] = elapsed;
            _stopwatch.Restart();
            return elapsed;
        }
    }

    public static class AsyncConsole
    {
        private static readonly BlockingCollection<string> _printQueue;

        static AsyncConsole()
        {
            _printQueue = new();
            var thread = new Thread(
                () => 
                {
                    for (;;) Console.WriteLine(_printQueue.Take());
                }
            );
            thread.IsBackground = true;
            thread.Start();
        }

        public static void WriteLine(string line)
        {
            _printQueue.Add(line);
        }
    }
}


namespace PointMasses.Entities
{
    public readonly struct CollisionData
    {
        public PointMass PointMassA { get; init; }
        public PointMass PointMassB { get; init; }
        public Vector2 Normal { get; init; }
        public float Separation { get; init; }
    }
}