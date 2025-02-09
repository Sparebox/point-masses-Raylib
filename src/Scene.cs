#pragma warning disable CA1507 // Use nameof to express symbol names

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using PointMasses.Entities;
using PointMasses.Sim;
using PointMasses.Textures;
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

    public static Scene LoadDefaultScene(Vector2 winSize)
    {
        var winSizeMeters = UnitConv.PtoM(winSize);
        Context ctx = new(timeStep: 1f / 60f, 3, gravity: new(0f, 9.81f), winSize)
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
        ctx.SaveCurrentState();
        // Start quad tree update thread
        var quadTreeUpdateThread = new Thread(new ParameterizedThreadStart(QuadTree.ThreadUpdate), 0)
        {
            IsBackground = true
        };
        quadTreeUpdateThread.Start(ctx);
        return new Scene() { Ctx = ctx, Name = "Default_scene" };
    }

    public static Scene LoadFromFile(string filepath)
    {
        AsyncConsole.WriteLine("Loading scene from file");
        try
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                IncludeFields = true,
            };
            string json = File.ReadAllText(filepath);
            var rootNode = JsonNode.Parse(json);
            var substep = rootNode["Ctx"]["Substep"].Deserialize<float>();
            var substeps = rootNode["Ctx"]["_substeps"].Deserialize<int>();
            var gravity = new Vector2(rootNode["Ctx"]["Gravity"]["X"].Deserialize<float>(), rootNode["Ctx"]["Gravity"]["Y"].Deserialize<float>());
            var winSize = new Vector2(rootNode["Ctx"]["WinSize"]["X"].Deserialize<float>(), rootNode["Ctx"]["WinSize"]["Y"].Deserialize<float>());
            var massShapes = rootNode["Ctx"]["MassShapes"].AsArray();
            foreach (var shape in massShapes)
            {
                var castShape = shape.Deserialize<MassShape>(options);
            }
            var winSizeMeters = UnitConv.PtoM(winSize);
            var ctx = new Context(substep, substeps, gravity, winSize)
            {
                QuadTree = new(
                    new(winSizeMeters.X * 0.5f, winSizeMeters.Y * 0.5f),
                    new(winSizeMeters.X, winSizeMeters.Y),
                    1,
                    6
                ),
                QuadTreeLock = new ReaderWriterLockSlim(),
                Lock = new ReaderWriterLockSlim()
            };
            ctx.SaveCurrentState();
            var scene = new Scene()
            {
                Ctx = ctx,
                Name = rootNode["Name"].Deserialize<string>()
            };
            // Start quad tree update thread
            var quadTreeUpdateThread = new Thread(new ParameterizedThreadStart(QuadTree.ThreadUpdate), 0)
            {
                IsBackground = true
            };
            quadTreeUpdateThread.Start(ctx);
            return scene;
        }
        catch (Exception e)
        {
            AsyncConsole.WriteLine($"Could not load scene: {e.Message}");
            return null;
        }
    }

    public void SaveToFile(string filepath)
    {
        try
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.General)
            {
                WriteIndented = true,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
            };
            File.WriteAllText($"{filepath}\\{Name}.json", JsonSerializer.Serialize(this, options));
            AsyncConsole.WriteLine($"Saved scene to {filepath}");
        }
        catch (Exception e)
        {
            AsyncConsole.WriteLine($"Could not save scene: {e.Message}");
        }
    }

}

