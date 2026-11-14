using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class OrbitCamera : Camera
{
    private const float MaxElevation = 89f;
    private const float MinDistance = 2f;
    private const float MaxDistance = 30f;
    private const float MouseSensitivity = 0.2f; // grade pe pixel
    private const float RotationSpeed = 90f;     // grade pe secundă
    private const float DistanceStep = 0.5f;     // unități per pas al rotiței

    public Vector3 Target { get; private set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }
    public float Elevation { get; private set; }

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "Orbita";

    public override Vector3 Position
    {
        get
        {
            float azimuthRadians = MathHelper.DegreesToRadians(Azimuth);
            float elevationRadians = MathHelper.DegreesToRadians(Elevation);
            float cosElevation = MathF.Cos(elevationRadians);

            Vector3 offsetDirection = new Vector3(
                cosElevation * MathF.Cos(azimuthRadians),
                MathF.Sin(elevationRadians),
                cosElevation * MathF.Sin(azimuthRadians));

            return Target + Distance * offsetDirection;
        }
    }

    public override Vector3 Front => Vector3.Normalize(Target - Position);

    public override Matrix4 ViewMatrix =>
        Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public override void Update(
        KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation -= mouseDelta.Y * MouseSensitivity;
        }

        float rotationStep = RotationSpeed * dt;

        if (keyboard.IsKeyDown(Keys.Left))
            Azimuth -= rotationStep;

        if (keyboard.IsKeyDown(Keys.Right))
            Azimuth += rotationStep;

        if (keyboard.IsKeyDown(Keys.Up))
            Elevation += rotationStep;

        if (keyboard.IsKeyDown(Keys.Down))
            Elevation -= rotationStep;

        Azimuth %= 360f;
        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);

        Distance = Math.Clamp(
            Distance - mouse.ScrollDelta.Y * DistanceStep,
            MinDistance,
            MaxDistance);
    }

    public override void Reset()
    {
        Target = Vector3.Zero;
        Distance = 8f;
        Azimuth = 60f;
        Elevation = 25f;
        FieldOfView = 60f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 position = Position;

        yield return $"Țintă     = ({Target.X:F2}, {Target.Y:F2}, {Target.Z:F2})";
        yield return $"Distanță  = {Distance:F2}";
        yield return $"Azimut    = {Azimuth:F1}°";
        yield return $"Elevație  = {Elevation:F1}°";
        yield return $"Poziție   = ({position.X:F2}, {position.Y:F2}, {position.Z:F2})";
    }
}