// Autoturism
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class CarScene : Scene
{
    private const string DriveForward = "Masina: inainte";
    private const string DriveBack = "Masina: marsarier";
    private const string SteerLeft = "Masina: vireaza stanga";
    private const string SteerRight = "Masina: vireaza dreapta";
    private const string ResetCar = "Masina: Reset";

    private const float Acceleration = 5f;
    private const float Braking = 9f;
    private const float SteeringSpeed = 85f;
    private const float AreaLimit = 11f;

    private SceneObject _car = null!;
    private float _speed;
    private float _heading;

    public override string Name => "Autoturism";
    public override string Status => $"Viteza: {_speed:F1} unitati/s";

    public override IEnumerable<string> ActionNames =>
        new[] { DriveForward, DriveBack, SteerLeft, SteerRight, ResetCar };

    public override void Load(Input input)
    {
        input.Bind(DriveForward, Keys.Up);
        input.Bind(DriveBack, Keys.Down);
        input.Bind(SteerLeft, Keys.Left);
        input.Bind(SteerRight, Keys.Right);
        input.Bind(ResetCar, Keys.R);

        CameraPosition = new Vector3(0f, 20f, 22f);
        CameraTarget = Vector3.Zero;

        Mesh cube = AddMesh(Shapes.Cube());
        Part("Platforma", cube, new Vector3(26f, 0.1f, 26f),
            new Vector3(0f, -0.05f, 0f), new Color4(0.35f, 0.35f, 0.35f, 1f));

        _car = AddObject("Pivot masina", null);
        Part("Caroseria", cube, new Vector3(1.5f, 0.45f, 2.8f),
            new Vector3(0f, 0.6f, 0f), new Color4(1f, 0.25f, 0.2f, 1f), _car);

        foreach (float x in new[] { -0.8f, 0.8f })
        {
            foreach (float z in new[] { -0.85f, 0.85f })
            {
                Part("Roata", cube, new Vector3(0.3f, 0.5f, 0.55f),
                    new Vector3(x, 0.25f, z), new Color4(0.1f, 0.1f, 0.1f, 1f), _car);
            }
        }

        Reset();
    }

    private SceneObject Part(string name, Mesh mesh, Vector3 size, Vector3 position,
        Color4 color, SceneObject? parent = null)
    {
        SceneObject part = AddObject(name, mesh, parent);
        part.Transform.Scale = size;
        part.Transform.Position = position;
        part.Tint = color;
        return part;
    }

    private void Reset()
    {
        _speed = 0f;
        _heading = 0f;
        _car.Transform.Position = Vector3.Zero;
        _car.Transform.Rotation = Vector3.Zero;
    }

    public override void Update(float dt, Input input)
    {
        if (dt <= 0f) return;
        if (input.WasPressed(ResetCar))
        {
            Reset();
            return;
        }

        float throttle = input.Axis(DriveBack, DriveForward);
        float steering = input.Axis(SteerRight, SteerLeft);
        float targetSpeed = throttle * (throttle < 0f ? 3f : 6f);

        // Pentru miscare stabila
        int steps = (int)MathF.Ceiling(dt / (1f / 120f));
        float step = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            float rate = throttle == 0f || _speed * throttle < 0f ? Braking : Acceleration;
            _speed += Math.Clamp(targetSpeed - _speed, -rate * step, rate * step);

            // La marsarier, sensul la virare se inverseaza.
            _heading += steering * SteeringSpeed * (_speed / 6f) * step;
            _heading %= 360f;
            float angle = MathHelper.DegreesToRadians(_heading);
            Vector3 forward = new Vector3(-MathF.Sin(angle), 0f, -MathF.Cos(angle));
            Vector3 position = _car.Transform.Position + forward * _speed * step;
            if (MathF.Abs(position.X) > AreaLimit || MathF.Abs(position.Z) > AreaLimit)
                _speed = 0f;

            position.X = Math.Clamp(position.X, -AreaLimit, AreaLimit);
            position.Z = Math.Clamp(position.Z, -AreaLimit, AreaLimit);
            _car.Transform.Position = position;
        }
        _car.Transform.Rotation = new Vector3(0f, _heading, 0f);
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 position = _car.Transform.Position;
        yield return $"Pozitie: X = {position.X:F2}, Z = {position.Z:F2}";
        yield return $"Orientare: {_heading:F1}°";
        yield return Status;
    }
}
