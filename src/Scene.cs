using System.Numerics;
using Newtonsoft.Json;
using PointMasses.Entities;
using PointMasses.Input;
using PointMasses.Sim;
using PointMasses.Systems;
using PointMasses.Utils;
using static Raylib_cs.Raylib;

namespace PointMasses;

public class Scene
{
    public Context Ctx { get; set; }
    public string Name { get; set; } = "untitled_scene";

    public Scene(float timeStep, int substeps, Vector2 gravity, Vector2 winSize)
    {
        Ctx = new Context(timeStep, substeps, gravity, winSize);
    }

    public Scene() {}

    public void Update()
    {
        if (Ctx._simPaused)
        {
            return;
        }
        if (GetFPS() < Constants.PauseThresholdFPS) // Pause if running too slow
        {
            AsyncLogger.Warn("Running too slow. Pausing sim");
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

    public static Scene LoadEmptyScene(Vector2 winSize)
    {
        var winSizeMeters = UnitConv.PtoM(winSize);
        Context ctx = new(timestep: 1f / 60f, 3, gravity: new(0f, 9.81f), winSize)
        {
            QuadTree = new(
                new Vector2(winSizeMeters.X * 0.5f, winSizeMeters.Y * 0.5f),
                new Vector2(winSizeMeters.X, winSizeMeters.Y),
                1,
                6
            )
        };
        ctx.LineColliders = new() {
            new(0f, 0f, winSizeMeters.X, 0f, ctx),
            new(0f, 0f, 0f, winSizeMeters.Y, ctx),
            new(winSizeMeters.X, 0f, winSizeMeters.X, winSizeMeters.Y, ctx),
            new(0f, winSizeMeters.Y, winSizeMeters.X, winSizeMeters.Y, ctx)
        };
        return new Scene() { Ctx = ctx, Name = "Default scene" };
    }

    public static Scene LoadFromFile(string filepath)
    {
        AsyncLogger.Info($"Loading scene from file path: {filepath}");
        try
        {
            string json = File.ReadAllText(filepath);
            var scene = JsonConvert.DeserializeObject<Scene>(json);
            var winSize = scene.Ctx.WinSize;
            scene.Ctx.QuadTree = new(
                new Vector2(winSize.X * 0.5f, winSize.Y * 0.5f),
                new Vector2(winSize.X, winSize.Y),
                1,
                6
            );
            foreach (var shape in scene.Ctx.MassShapes)
            {
                shape.SetContext(scene.Ctx);
                foreach (var pointMass in shape._points)
                {
                    pointMass.SetContext(scene.Ctx);
                }
                foreach (var constraint in shape._constraints)
                {
                    constraint.PointA = shape._points.Where(p => p.Id == constraint.PointA.Id).Single();
                    constraint.PointB = shape._points.Where(p => p.Id == constraint.PointB.Id).Single();
                    constraint.Ctx = scene.Ctx;
                }
            }
            
            return scene;
        }
        catch (Exception e)
        {
            AsyncLogger.Fatal($"Could not load scene: {e.Message}");
            return null;
        }
    }

    public void SaveToFile(string filepath = "scenes" )
    {
        try
        {
            string json = JsonConvert.SerializeObject(this);
            File.WriteAllText(Path.Combine(filepath, $"{Name}.json"), json);
            AsyncLogger.Info($"Saved scene as {Name}.json");
        }
        catch (Exception e)
        {
            AsyncLogger.Error($"Could not save scene: {e.Message}");
        }
    }

    public void DeleteFile()
    {
        try
        {
            File.Delete(Path.Combine("scenes", $"{Name}.json"));
            AsyncLogger.Info($"Deleted scene {Name}");
        }
        catch (Exception e)
        {
            AsyncLogger.Error($"Could not delete scene: {e.Message}");
        }
    }

    public void Init()
    {
        Ctx.SaveCurrentState();
        QuadTree.StartUpdateThread(Ctx);
        Ctx.GetSystem<NbodySystem>().StartUpdateThread();
        InputManager.InputEnabled = true;
    }

    public void Destroy()
    {
        QuadTree.ShutdownUpdateThread();
        Ctx.GetSystem<NbodySystem>().ShutdownUpdateThread();
        InputManager.InputEnabled = false;
    }
}

