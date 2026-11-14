// Varianta B: evita obstacolele
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class ObstacleScene : Scene
{
    private const string Left = "Schimba banda spre stanga";
    private const string Right = "Schimba banda spre dreapta";
    private const string Jump = "Sare peste obstacol";
    private const string Restart = "Joc nou";
    private const float LaneWidth = 2f;
    private const float LaneChangeTime = 0.16f;
    private const float PlayerZ = 3f;
    private const float SpawnZ = -24f;
    private const float EndZ = 8f;
    private const float MaxStep = 1f / 120f;

    private static readonly Vector3 PlayerSize = new(0.8f, 0.8f, 0.8f);
    private readonly Random _random = new();
    private readonly List<SceneObject> _obstacles = new();
    private readonly List<SceneObject> _markings = new();
    private SceneObject _player = null!;
    private Tween? _laneTween;
    private float _laneElapsed;
    private int _lane;
    private float _height;
    private float _verticalSpeed;
    private float _score;
    private float _record;
    private float _untilSpawn;
    private bool _gameOver;

    private float Speed => MathF.Min(6f + 0.22f * _score, 18f);
    private float SpawnInterval => MathF.Max(0.65f, 1.7f - 0.025f * _score);
    public override string Name => "Evita obstacolele";
    public override string Status =>
        $"{(_gameOver ? "Joc terminat · R pentru joc nou" : "In joc")} | Scor: {_score:F1} s | Record: {_record:F1} s";

    public override IEnumerable<string> ActionNames => new[] { Left, Right, Jump, Restart };

    public override void Load(Input input)
    {
        input.Bind(Left, Keys.Left, Keys.A);
        input.Bind(Right, Keys.Right, Keys.D);
        input.Bind(Jump, Keys.Space);
        input.Bind(Restart, Keys.R);

        CameraPosition = new Vector3(0f, 7f, 12f);
        CameraTarget = new Vector3(0f, 0f, -8f);

        Mesh cube = AddMesh(Shapes.Cube());
        SceneObject road = Box("Drum", cube, new Vector3(6f, 0.2f, 34f),
            new Vector3(0f, -0.1f, -8f), new Color4(0.25f, 0.27f, 0.32f, 1f));

        // Pozitiile marcajelor sunt locale fata de un pivot fara scalare.
        SceneObject roadPivot = AddObject("Pivot marcaje", null);
        roadPivot.Transform.Position = new Vector3(road.Transform.Position.X, 0f, 0f);
        for (int i = 0; i < 16; i++)
        {
            _markings.Add(Box("Marcaj", cube, new Vector3(0.07f, 0.02f, 0.9f),
                new Vector3(i % 2 == 0 ? -1f : 1f, 0.01f, 0f),
                new Color4(1f, 1f, 1f, 1f), roadPivot));
        }

        _player = AddObject("Pivot jucator", null);
        Box("Jucator", cube, PlayerSize, new Vector3(0f, PlayerSize.Y / 2f, 0f),
            new Color4(0.3f, 0.9f, 1f, 1f), _player);

        // Zece obiecte reutilizabile acopera numarul maxim simultan pe drum.
        for (int i = 0; i < 10; i++)
            _obstacles.Add(AddObject("Obstacol", cube));

        ResetRun();
    }

    private SceneObject Box(string name, Mesh mesh, Vector3 size, Vector3 position,
        Color4 tint, SceneObject? parent = null)
    {
        SceneObject box = AddObject(name, mesh, parent);
        box.Transform.Scale = size;
        box.Transform.Position = position;
        box.Tint = tint;
        return box;
    }

    private void ResetRun()
    {
        _score = 0f;
        _gameOver = false;
        _lane = 0;
        _laneTween = null;
        _laneElapsed = 0f;
        _height = 0f;
        _verticalSpeed = 0f;
        _untilSpawn = 0f;
        _player.Transform.Position = new Vector3(0f, 0f, PlayerZ);
        foreach (SceneObject obstacle in _obstacles)
            obstacle.Visible = false;

        for (int i = 0; i < _markings.Count; i++)
        {
            Vector3 position = _markings[i].Transform.Position;
            position.Z = SpawnZ + (i / 2) * 4f;
            _markings[i].Transform.Position = position;
        }
    }

    public override void Update(float dt, Input input)
    {
        if (dt <= 0f) return;
        if (input.WasPressed(Restart)) ResetRun();
        if (_gameOver) return;

        int turn = (input.WasPressed(Right) ? 1 : 0) - (input.WasPressed(Left) ? 1 : 0);
        int targetLane = Math.Clamp(_lane + turn, -1, 1);
        if (targetLane != _lane)
        {
            _lane = targetLane;
            _laneElapsed = 0f;
            _laneTween = new Tween(
                new Vector3(_player.Transform.Position.X, 0f, 0f),
                new Vector3(_lane * LaneWidth, 0f, 0f),
                LaneChangeTime,
                Easing.EaseOut,
                false);
        }

        if (input.WasPressed(Jump) && _height == 0f)
            _verticalSpeed = 7f;

        // Subpașii păstrează gravitația și coliziunile stabile la TimeScale mare.
        int steps = (int)MathF.Ceiling(dt / MaxStep);
        float step = dt / steps;
        for (int i = 0; i < steps && !_gameOver; i++)
            Advance(step);
    }

    private void Advance(float dt)
    {
        _score += dt;
        Vector3 position = _player.Transform.Position;
        if (_laneTween != null)
        {
            _laneElapsed += dt;
            _laneTween.Update(dt);
            position.X = _laneTween.Value.X;
            if (_laneElapsed >= LaneChangeTime)
            {
                position.X = _lane * LaneWidth;
                _laneTween = null;
            }
        }

        if (_height > 0f || _verticalSpeed > 0f)
        {
            _verticalSpeed -= PlayerController.Gravity * dt;
            _height += _verticalSpeed * dt;
            if (_height <= 0f)
            {
                _height = 0f;
                _verticalSpeed = 0f;
            }
        }
        position.Y = _height;
        _player.Transform.Position = position;

        _untilSpawn -= dt;
        if (_untilSpawn <= 0f)
        {
            SpawnObstacle();
            _untilSpawn += SpawnInterval;
        }

        float travel = Speed * dt;
        foreach (SceneObject marking in _markings)
        {
            Vector3 p = marking.Transform.Position;
            p.Z += travel;
            if (p.Z >= EndZ)
                p.Z -= EndZ - SpawnZ;
            marking.Transform.Position = p;
        }

        Vector3 playerCenter = position + new Vector3(0f, PlayerSize.Y / 2f, 0f);
        foreach (SceneObject obstacle in _obstacles)
        {
            if (!obstacle.Visible) continue;

            Vector3 p = obstacle.Transform.Position;
            p.Z += travel;
            obstacle.Transform.Position = p;
            if (p.Z >= EndZ)
                obstacle.Visible = false;
            else if (Overlaps(playerCenter, PlayerSize, p, obstacle.Transform.Scale))
                _gameOver = true;
        }
        _record = MathF.Max(_record, _score);
    }

    private void SpawnObstacle()
    {
        foreach (SceneObject obstacle in _obstacles)
        {
            if (obstacle.Visible) continue;
            bool tall = _random.Next(2) == 0;
            float height = tall ? 2.2f : 0.45f;
            obstacle.Transform.Scale = new Vector3(1.15f, height, 0.6f);
            obstacle.Transform.Position = new Vector3(_random.Next(-1, 2) * LaneWidth, height / 2f, SpawnZ);
            obstacle.Tint = tall ? new Color4(1f, 0.25f, 0.2f, 1f) : new Color4(1f, 0.75f, 0.2f, 1f);
            obstacle.Visible = true;
            return;
        }
    }

    private static bool Overlaps(Vector3 a, Vector3 sizeA, Vector3 b, Vector3 sizeB) =>
        MathF.Abs(a.X - b.X) < (sizeA.X + sizeB.X) / 2f &&
        MathF.Abs(a.Y - b.Y) < (sizeA.Y + sizeB.Y) / 2f &&
        MathF.Abs(a.Z - b.Z) < (sizeA.Z + sizeB.Z) / 2f;

    public override IEnumerable<string> Describe()
    {
        yield return Status;
        yield return $"Banda țintă: {_lane + 2}/3 | Înălțimea saltului: {_height:F2}";
        yield return $"Viteza lumii: {Speed:F2} unități/s | Interval obstacole: {SpawnInterval:F2} s";
    }
}
