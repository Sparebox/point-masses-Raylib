﻿using System.Numerics;
using Collision;
using Physics;
using static Raylib_cs.Raylib;

namespace Sim;

public class Loop 
{   
    public const float TimeStep = 1f / 60f;
    public const int WinW = 800;
    public const int WinH = 450;
    public const float PixelsPerMeter = 1f / 0.01f;

    public static bool GravityEnabled { get; set; }
    public static readonly Vector2 Gravity = new(0f, 9.81f * PixelsPerMeter);

    private static float _accumulator;

    public static void Main() 
    {
        InitWindow(WinW, WinH, "Point-masses");
        SetTargetFPS(165);

        PointMass mass = new(WinW / 2f, WinH / 2f, 10f);
        LineCollider line = new(0f, 300f, 800f, 450f);
        GravityEnabled = true;

        while (!WindowShouldClose())
        {
            _accumulator += GetFrameTime();
            while (_accumulator >= TimeStep)
            {
                mass.Update();
                _accumulator -= TimeStep;
            }
            Vector2 closestPoint = line.ClosestPointOnLine(mass.Pos);
            BeginDrawing();
            ClearBackground(Raylib_cs.Color.BLACK);
            mass.Draw();
            line.Draw();
            DrawLine((int) closestPoint.X, (int) closestPoint.Y, (int) mass.Pos.X, (int) mass.Pos.Y, Raylib_cs.Color.MAGENTA);
            EndDrawing();
        }

        CloseWindow();
    }
}