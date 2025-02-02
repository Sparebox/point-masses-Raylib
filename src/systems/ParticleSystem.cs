using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses.Systems;

public class ParticleSystem : ISystem
{
    public Vector2 SpawnPos { get; set; }
    public Vector2 SpawnVel { get; set; }

    private readonly Particle[] _particlePool;

    public ParticleSystem(int particleCount, float particleRadiusM, float lifeTimeSeconds, Vector2 spawnPos, Vector2 spawnVel)
    {
        _particlePool = new Particle[particleCount];
        SpawnPos = spawnPos;
        SpawnVel = spawnVel;
        for (int i = 0; i < particleCount; i++)
        {
            _particlePool[i] = new Particle(lifeTimeSeconds, particleRadiusM);
        }
    }

    public void Update()
    {
        foreach (var particle in _particlePool)
        {
            particle.Update();
            if (particle.IsDead)
            {
                particle.Pos = SpawnPos;
                particle.Vel = SpawnVel + UnitConv.PtoM(new Vector2(Rng.Gen.NextSingle(), Rng.Gen.NextSingle()));
                particle.Resurrect();
            }
        }
    }

    public void UpdateInput() {}

    public void Draw()
    {
        foreach (var particle in _particlePool)
        {
            particle.Draw();
        }
    }
}

internal class Particle
{
    public float LifeTimeSeconds { get; init; }
    public Vector2 Pos { get; set; }
    public Vector2 PrevPos { get; set; }
    public Vector2 Vel
    {
        get { return Pos - PrevPos;}
        set { PrevPos = Pos - value; }
    }
    public float Radius { get; set; }
    public bool IsDead { get; private set; }

    private readonly Stopwatch _stopwatch;

    public Particle(float lifeTimeSeconds, float radius)
    {
        LifeTimeSeconds = lifeTimeSeconds;
        Radius = radius;
        IsDead = true;
        Pos = new Vector2();
        PrevPos = new Vector2();
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void Update()
    {
        if (IsDead)
        {
            return;
        }
        if (_stopwatch.ElapsedMilliseconds > LifeTimeSeconds * 1e3)
        {
            Kill();
        }
        Vector2 vel = Vel;
        PrevPos = Pos;
        Pos += vel;
    }

    public void Draw()
    {
        if (IsDead)
        {
            return;
        }
        DrawCircleLinesV(UnitConv.MtoP(Pos), UnitConv.MtoP(Radius), Color.White);
    }

    public void Resurrect()
    {
        IsDead = false;
        _stopwatch.Restart();
    }

    public void Kill()
    {
        IsDead = true;
        _stopwatch.Stop();
    }
}