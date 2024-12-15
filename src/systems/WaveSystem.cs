using System.Numerics;
using ImGuiNET;
using PointMasses.Entities;
using PointMasses.Sim;
using PointMasses.Utils;
using Raylib_cs;
using static Raylib_cs.Raylib;

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

    public void AddWaveInstance(Vector2 start, Vector2 end, float mass, uint res, float freq, float amp, float phase, bool showInfo, Context ctx)
    {
        _instances.Add(new WaveInstance(start, end, mass, res, freq, amp, phase, showInfo, ctx));
    }

    private class WaveInstance
    {
        public const int FontSize = 20;

        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        public float Phase { get; set; }

        private readonly Context _ctx;
        private readonly Vector2 _startPos;
        private readonly Vector2[] _points;
        private readonly float _spacing;
        private readonly uint _res;
        private readonly uint _framesPerRevolution;
        private readonly float[] _yPosArray;
        private readonly float _pointRadius;
        private readonly bool _showInfo;
        private uint _animFrameIndex;

        public WaveInstance(Vector2 start, Vector2 end, float mass, uint resolution, float freq, float amp, float phase, bool showInfo, Context ctx)
        {
            Frequency = freq;
            Amplitude = amp;
            Phase = phase;
            _showInfo = showInfo;
            _ctx = ctx;
            _pointRadius = PointMass.MassToRadius(mass);
            _framesPerRevolution = (uint) MathF.Ceiling(1f / _ctx._timestep);
            _animFrameIndex = 0;
            _yPosArray = new float[_framesPerRevolution];
            _startPos = start;
            _res = resolution;
            PrecalculatePositions();
            _spacing = Vector2.Distance(start, end) / (resolution - 1);
            _points = new Vector2[resolution];
            CreatePoints();
        }

        public void Update()
        {
            for (uint i = 0; i < _res; i++)
            {
                float yOffset = _yPosArray[(_animFrameIndex + i) % _framesPerRevolution];
                _points[i].Y = _startPos.Y + yOffset;
            }
            _animFrameIndex++;
            _animFrameIndex %= _framesPerRevolution;
        }

        public void Draw()
        {
            if (_showInfo)
            {
                ImGui.Begin("Wave properties", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
                ImGui.SetWindowPos(GetWorldToScreen2D(UnitConv.MtoP(_startPos) - new Vector2(0f, UnitConv.MtoP(Amplitude) + 200f), _ctx._camera));
                ImGui.SetWindowSize(new (200f, 150f));
                ImGui.Text($"Time: {_animFrameIndex * _ctx._timestep} s");
                ImGui.Text($"Frame count: {_framesPerRevolution}");
                ImGui.Text($"Frame index: {_animFrameIndex}");
                ImGui.Text($"Frequency: {Frequency} Hz");
                ImGui.End();
            }
            for (uint i = 0; i < _points.Length - 1; i++)
            {
                DrawLineV(UnitConv.MtoP(_points[i]), UnitConv.MtoP(_points[i + 1]), Color.White);
                if (i % 2 == 0)
                {
                    DrawLine(UnitConv.MtoP(_points[i].X), UnitConv.MtoP(_startPos.Y), UnitConv.MtoP(_points[i + 1].X), UnitConv.MtoP(_startPos.Y), Color.Red);
                }
            }
        }

        private void CreatePoints()
        {
            Vector2 nextPoint = _startPos;
            for (uint i = 0; i < _res; i++)
            {
                _points[i] = nextPoint;
                nextPoint.X += _spacing;
            }
        }

        private void PrecalculatePositions()
        {   
            float angle = 0f;
            for (uint i = 0; i < _framesPerRevolution; i++)
            {
                _yPosArray[i] = Amplitude * MathF.Sin(Frequency * angle + Phase);
                angle += MathF.Tau / _framesPerRevolution;
            } 
        }
    }
}

