using System.Numerics;
using PointMasses.Entities;
using PointMasses.Sim;

namespace PointMasses.Systems;

public class WaveSystem : ISystem
{
    private readonly Context _ctx;
    private readonly List<WaveInstance> _instances;

    public WaveSystem(Context ctx)
    {
        _ctx = ctx;
        _instances = new();
    }

    public void Update()
    {
        foreach (WaveInstance instance in _instances)
        {
            instance.Update();
        }
    }

    public void Draw()
    {
        foreach (WaveInstance instance in _instances)
        {
            instance.Draw();
        }
    }

    public void UpdateInput() {}

    public void AddWaveInstance(Vector2 start, Vector2 end, bool pinned, float mass, int res, float freq, float amp, float phase, Context ctx)
    {
        _instances.Add(new WaveInstance(start, end, pinned, mass, res, freq, amp, phase, ctx));
    }

    private class WaveInstance
    {
        public MassShape Vibrator { get; init; }
        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        public float Phase { get; set; }

        private readonly Vector2 _restStartPos;
        private readonly Vector2 _restEndPos;
        private readonly float[] _yPosList;
        private readonly uint _framesPerRevolution;
        private uint _animFrameIndex;

        public WaveInstance(Vector2 start, Vector2 end, bool pinned, float mass, int resolution, float freq, float amp, float phase, Context ctx)
        {
            Frequency = freq;
            Amplitude = amp;
            Phase = phase;
            _framesPerRevolution = (uint) MathF.Ceiling(MathF.Tau / ctx.Substep);
            _animFrameIndex = 0;
            _yPosList = new float[_framesPerRevolution];
            Vibrator = MassShape.Chain(start.X, start.Y, end.X, end.Y, mass, 0.5f, resolution, (true, pinned), ctx);
            _restStartPos = start;
            _restEndPos = end;
            PrecalculatePositions(ctx);
        }

        public void Update()
        {
            float yPos = _yPosList[_animFrameIndex];
            Vibrator._points.First().Pos = new(_restStartPos.X, _restStartPos.Y + yPos);
            Vector2 lastPos = Vibrator._points.Last().Pos;
            Vibrator._points.Last().Pos = new(_restEndPos.X, lastPos.Y);
            Vibrator.Update();
            _animFrameIndex++;
            _animFrameIndex %= _framesPerRevolution;
        }

        public void Draw()
        {
            Vibrator.Draw();
        }

        private void PrecalculatePositions(Context ctx)
        {
            float angle = 0f;
            uint frameIndex = 0;
            for (;;)
            {
                _yPosList[frameIndex] = Amplitude * MathF.Sin(Frequency * angle + Phase);
                angle += ctx.Substep;
                frameIndex++;
                if (angle >= MathF.Tau)
                {
                    break;
                }
            } 
        }
    }
}

