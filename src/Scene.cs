using System;
using System.Numerics;
using PointMasses.Entities;
using PointMasses.Sim;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace point_masses;

public class Scene
{
    public Context Ctx { get; set; }
    public string Name { get; set; }

    public Scene(float timeStep, int substeps, Vector2 gravity, Vector2 winSize)
    {
        Ctx = new Context(timeStep, substeps, gravity, winSize);
    }

    public Scene() {}

    public void Update()
    {
        if (GetFPS() < Constants.PauseThresholdFPS) // Pause if running too slow
        {
            AsyncConsole.WriteLine("Running too slow. Pausing sim");
            Ctx._simPaused = true;
        }
        Ctx._accumulator += GetFrameTime();
        while (Ctx._accumulator >= Ctx._timestep)
        {
            for (int i = 0; i < Ctx._substeps; i++)
            {
                foreach (MassShape s in Ctx.MassShapes)
                {
                    s.Update();
                }
                foreach (var system in Ctx.SubStepSystems)
                {
                    system.Update();
                }
            }
            foreach (var system in Ctx.Systems)
            {
                system.Update();
            }
            // Remove deleted mass shapes if any deleted
            Ctx.Lock.EnterUpgradeableReadLock();
            if (Ctx.MassShapes.Where(s => s._toBeDeleted).Any())
            {
                if (Ctx.Lock.TryEnterWriteLock(0)) // Do not block the main thread if the lock is unavailable
                {
                    Ctx.MassShapes.RemoveAll(s => s._toBeDeleted);
                    Ctx.Lock.ExitWriteLock();
                }
            }
            Ctx.Lock.ExitUpgradeableReadLock();
            Ctx._accumulator -= Ctx._timestep;
        }
    }

    public void Draw()
    {
        BeginMode2D(Ctx._camera);
        foreach (MassShape s in Ctx.MassShapes)
        {
            s.Draw();
        }
        foreach (var l in Ctx.LineColliders)
        {
            l.Draw();
        }
        foreach (var system in Ctx.Systems)
        {
            system.Draw();
        }
        foreach (var substepSystem in Ctx.SubStepSystems)
        {
            substepSystem.Draw();
        }
        if (Ctx._drawQuadTree)
        {
            Ctx.QuadTree.Draw();
        }
        EndMode2D();
    }

}

