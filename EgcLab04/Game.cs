using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R
    private Mesh? _satelliteMesh;

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)
    private Vector2? _pivot;
    private float _orbitAngleDegrees;

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;   // swap-ul bufferelor așteaptă reîmprospătarea monitorului
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");
        Console.WriteLine("Apasa H pentru lista comenzilor.");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        // Scena: aceleași forme din laboratorul 3, acum plasate în lume prin Transform2D.
        // Depth (z) contează doar în perspectivă (tasta O, secțiunea 7.2).
        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });

        _satelliteMesh = Shapes.Triangle();
    }

    /// <summary>Adaugă un obiect în scenă și reține transformarea lui inițială (pentru tasta R).</summary>
    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);      // proiecția se adaptează la noul raport de aspect
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        if (KeyboardState.IsKeyPressed(Keys.H)) PrintHelp();

        bool selectionChanged = KeyboardState.IsKeyPressed(Keys.Tab);
        if (selectionChanged) _selected = (_selected + 1) % _scene.Count;

        bool projectionChanged = KeyboardState.IsKeyPressed(Keys.O);
        if (projectionChanged) _camera.IsPerspective = !_camera.IsPerspective;

        if (KeyboardState.IsKeyPressed(Keys.F)) _wireframe = !_wireframe;

        bool resetRequested = KeyboardState.IsKeyPressed(Keys.R);
        if (resetRequested) ResetScene();

        if (selectionChanged || projectionChanged || resetRequested)
        {
            _titleDirty = true;
        }

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        float dt = (float)args.Time;
        _orbitAngleDegrees = (_orbitAngleDegrees + 90.0f * dt) % 360.0f;
        SceneObject sel = _scene[_selected];
        bool shiftDown = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
        float speedMultiplier = shiftDown ? 3.0f : 1.0f;

        const float moveSpeed = 2.0f;   // unități de lume pe secundă
        float moveStep = moveSpeed * dt * speedMultiplier;
        if (KeyboardState.IsKeyDown(Keys.Left))  sel.Transform.Position.X -= moveStep;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveStep;
        if (KeyboardState.IsKeyDown(Keys.Down))  sel.Transform.Position.Y -= moveStep;
        if (KeyboardState.IsKeyDown(Keys.Up))    sel.Transform.Position.Y += moveStep;

        float rotationStep = 90.0f * dt * speedMultiplier;
        if (KeyboardState.IsKeyDown(Keys.Q)) sel.Transform.Rotation += rotationStep;
        if (KeyboardState.IsKeyDown(Keys.E)) sel.Transform.Rotation -= rotationStep;

        float scaleFactor = MathF.Pow(1.5f, dt * speedMultiplier);
        if (KeyboardState.IsKeyDown(Keys.Z))
        {
            sel.Transform.Scale *= 1.0f / scaleFactor;
        }
        if (KeyboardState.IsKeyDown(Keys.X))
        {
            sel.Transform.Scale *= scaleFactor;
        }
        // T2: inmultirea pastreaza aceeasi schimbare relativa indiferent de marimea curenta a obiectului.
        sel.Transform.Scale.X = Math.Clamp(sel.Transform.Scale.X, 0.1f, 10.0f);
        sel.Transform.Scale.Y = Math.Clamp(sel.Transform.Scale.Y, 0.1f, 10.0f);

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            sel.Transform.Position = ScreenToWorld(MouseState.Position, sel.Transform.Depth);
        }

        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _pivot = ScreenToWorld(MouseState.Position);
        }

        // T4: pozitia descrie orbita in jurul pivotului, iar rotatia proprie pastreaza orientarea fata de pivot.
        if (_pivot.HasValue && KeyboardState.IsKeyDown(Keys.P))
        {
            float pivotRotation = 90.0f * dt * speedMultiplier;
            Vector2 pivot = _pivot.Value;
            Matrix4 aroundPivot =
                Matrix4.CreateTranslation(-pivot.X, -pivot.Y, 0.0f)
              * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(pivotRotation))
              * Matrix4.CreateTranslation(pivot.X, pivot.Y, 0.0f);
            Vector4 rotatedPosition = new Vector4(sel.Transform.Position.X, sel.Transform.Position.Y, 0.0f, 1.0f) * aroundPivot;
            sel.Transform.Position = new Vector2(rotatedPosition.X, rotatedPosition.Y);
            sel.Transform.Rotation += pivotRotation;
        }

        // --- Camera: tastatura și rotița (secțiunea 6.2) -----------------------------

        float camSpeed = 3.0f / _camera.Zoom;                    // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);   // un pas al rotiței = ±10 %
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        // --- Titlul ferestrei: obiectul selectat, proiecția, cadre pe secundă --------

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche din back buffer.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare și programul shader.
        SetWireframe(_wireframe);
        _shader.Use();

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // T5 (facultativ): satelitul urmeaza obiectul selectat, inclusiv cand selectia se schimba cu Tab.
        if (_satelliteMesh is not null)
        {
            // T5: orbita este locala parintelui, deci scalarea parintelui mareste distanta efectiva a satelitului.
            Matrix4 localTransform =
                Matrix4.CreateScale(0.3f, 0.3f, 1.0f)
              * Matrix4.CreateTranslation(1.2f, 0.0f, 0.0f)
              * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(_orbitAngleDegrees));
            Matrix4 satelliteModel = localTransform * _scene[_selected].Transform.ModelMatrix;
            _shader.SetMatrix4("uModel", satelliteModel);
            _shader.SetColor("uTint", Color4.White);
            _satelliteMesh.Draw();
        }

        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _satelliteMesh?.Dispose();
        _shader.Dispose();

        base.OnUnload();
    }

    /// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
    private Vector2 ScreenToWorld(Vector2 mousePixels, float worldDepth = 0.0f)
    {
        float width = Math.Max(ClientSize.X, 1);
        float height = Math.Max(ClientSize.Y, 1);
        // T3: pixelii au y crescator in jos, iar NDC are y crescator in sus, de aceea folosim 1 - 2*y/H.
        float ndcX = 2.0f * mousePixels.X / width - 1.0f;
        float ndcY = 1.0f - 2.0f * mousePixels.Y / height;

        Matrix4 viewProjection = _camera.ViewMatrix * _camera.ProjectionMatrix;
        Matrix4 inverseViewProjection = Matrix4.Invert(viewProjection);

        if (!_camera.IsPerspective)
        {
            Vector4 world = new Vector4(ndcX, ndcY, 0.0f, 1.0f) * inverseViewProjection;
            if (MathF.Abs(world.W) > float.Epsilon)
            {
                world /= world.W;
            }
            return new Vector2(world.X, world.Y);
        }

        // In perspectiva, pixelul defineste o raza; intersectam raza cu planul z=Depth al obiectului.
        Vector4 nearHomogeneous = new Vector4(ndcX, ndcY, -1.0f, 1.0f) * inverseViewProjection;
        Vector4 farHomogeneous = new Vector4(ndcX, ndcY, 1.0f, 1.0f) * inverseViewProjection;
        Vector3 nearPoint = nearHomogeneous.Xyz / nearHomogeneous.W;
        Vector3 farPoint = farHomogeneous.Xyz / farHomogeneous.W;
        Vector3 rayDirection = farPoint - nearPoint;

        if (MathF.Abs(rayDirection.Z) <= float.Epsilon)
        {
            return new Vector2(nearPoint.X, nearPoint.Y);
        }

        float distance = (worldDepth - nearPoint.Z) / rayDirection.Z;
        Vector3 worldPoint = nearPoint + rayDirection * distance;
        return new Vector2(worldPoint.X, worldPoint.Y);
    }

    /// <summary>Tasta R: obiectele revin la transformările inițiale, camera la starea inițială.</summary>
    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
        _pivot = null;
        _orbitAngleDegrees = 0.0f;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("EGC - LABORATOR 04");
        Console.WriteLine("LISTA COMENZILOR");
        Console.WriteLine("========================================");
        Console.WriteLine("H              - afiseaza aceasta lista");
        Console.WriteLine("ESC            - inchide aplicatia");
        Console.WriteLine("TAB            - selecteaza urmatorul obiect; satelitul il urmeaza");
        Console.WriteLine("SAGETI         - deplaseaza obiectul selectat");
        Console.WriteLine("Q / E          - roteste obiectul selectat");
        Console.WriteLine("Z / X          - micsoreaza / mareste obiectul selectat");
        Console.WriteLine("SHIFT          - miscare, rotatie si scalare de 3 ori mai rapide");
        Console.WriteLine("W / A / S / D  - deplaseaza camera");
        Console.WriteLine("SCROLL         - zoom camera");
        Console.WriteLine("CLICK STANGA   - muta obiectul sub cursor");
        Console.WriteLine("CLICK DREAPTA  - stabileste pivotul in lumea scenei");
        Console.WriteLine("P              - roteste obiectul selectat in jurul pivotului");
        Console.WriteLine("O              - schimba proiectia ortografica / perspectiva");
        Console.WriteLine("F              - schimba contur / plin");
        Console.WriteLine("R              - reseteaza scena si camera");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}
