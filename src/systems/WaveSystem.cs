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

    public void AddWaveInstance(Vector2 start, Vector2 end, uint yRes, float freq, float amp, float phase, bool showInfo, Context ctx)
    {
        _instances.Add(new WaveInstance(start, end, yRes, freq, amp, phase, showInfo, ctx));
    }

    private class WaveInstance
    {
        public const int FontSize = 20;

        private float _frequency;
        private float _phaseOffset;
        private float _amplitude;
        private float _currentPhase;
        private float _vel;

        private readonly Context _ctx;
        private readonly Vector2 _startPos;
        private readonly Vector2[] _points;
        private readonly float _spacing;
        private readonly uint _res;
        private readonly bool _showInfo;

        public WaveInstance(Vector2 start, Vector2 end, uint res, float freq, float amp, float phase, bool showInfo, Context ctx)
        {
            _frequency = freq;
            _amplitude = amp;
            _phaseOffset = phase;
            _showInfo = showInfo;
            _ctx = ctx;
            _res = res;
            _startPos = start;
            _spacing = Vector2.Distance(start, end) / (res - 1);
            _points = new Vector2[res];
            CreatePoints();
        }

        public void Update()
        {
            float angle = 0f;
            for (uint i = 0; i < _res; i++)
            {
                float yOffset = _amplitude * MathF.Sin(_frequency * angle + _phaseOffset + _currentPhase);
                _points[i].Y = _startPos.Y + yOffset;
                angle += MathF.Tau / _res;
                _currentPhase += _vel * _ctx._timestep;
            }
        }

        public void Draw()
        {
            if (_showInfo)
            {
                ImGui.Begin("Wave properties", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
                ImGui.SetWindowPos(GetWorldToScreen2D(UnitConv.MtoP(_startPos) - new Vector2(0f, UnitConv.MtoP(_amplitude) + 200f), _ctx._camera));
                ImGui.SetWindowSize(new (350f, 150f));
                ImGui.Text($"Y resolution: {_res}");
                ImGui.DragFloat("Frequency [Hz]", ref _frequency);
                ImGui.DragFloat("Amplitude [m]", ref _amplitude);
                ImGui.DragFloat("Phase [rad]", ref _phaseOffset);
                ImGui.DragFloat("Velocity [m/s]", ref _vel);
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
    }
}

