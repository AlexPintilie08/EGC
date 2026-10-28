using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// GameWindow apelează automat metodele On... de mai jos, în ordinea descrisă în laborator.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private Triangle _triangle = null!;
    private Quad _quad = null!;

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private Color4 _clearColor = new(0.2f, 0.3f, 0.3f, 1.0f);   // culoarea de fundal
    private Color4 _tint = Color4.White;                          // nuanța triunghiului (alb = neschimbat)
    private Vector3 _offset = Vector3.Zero;                       // deplasarea triunghiului
    private Vector3 _manualOffset = new(0.3f, 0.2f, 0.0f);                  // deplasarea controlata de sageti
    private PolygonMode _polygonMode = PolygonMode.Fill;            // Fill, Line sau Point
    private int _vertexEditIndex;                                  // varful modificat la urmatoarea apasare V
    private int _vertexColorIndex;                                 // culoarea atribuita la urmatoarea apasare V
    private bool _deformAllVertices = true;                         // false = doar varful de sus se misca
    private bool _collapsed;                                       // toate varfurile sunt in acelasi punct
    private bool _showQuad;                                        // false = triunghi, true = dreptunghi

    // Contoare pentru exemplul cu bucla principală (afișate în titlul ferestrei).
    private readonly string _baseTitle;
    private double _elapsed;     // timpul total scurs (secunde)
    private double _secondTimer; // cronometru pentru actualizarea titlului o dată pe secundă
    private int _updates;        // câte OnUpdateFrame în ultima secundă
    private int _frames;         // câte OnRenderFrame în ultima secundă

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

        // Informații despre driverul și placa grafică folosite (vezi Problema 1 din Laboratorul 01).
        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Vendor  : {GL.GetString(StringName.Vendor)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");
        Console.WriteLine("Apasa H pentru lista comenzilor.");

        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        GL.PointSize(8.0f);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");
        _triangle = new Triangle();
        _quad = new Quad();
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // args.Time = secundele scurse de la apelul anterior (delta time).
        _elapsed += args.Time;
        _updates++;

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        if (KeyboardState.IsKeyPressed(Keys.H))
        {
            PrintHelp();
        }

        if (KeyboardState.IsKeyPressed(Keys.Tab))
        {
            _showQuad = !_showQuad;
        }

        // P2: nuanta generala a triunghiului. Alb pastreaza culorile varfurilor.
        if (KeyboardState.IsKeyPressed(Keys.D0)) _tint = Color4.White;
        if (KeyboardState.IsKeyPressed(Keys.D1)) _tint = new Color4(1.0f, 0.3f, 0.3f, 1.0f);
        if (KeyboardState.IsKeyPressed(Keys.D2)) _tint = new Color4(0.3f, 1.0f, 0.3f, 1.0f);
        if (KeyboardState.IsKeyPressed(Keys.D3)) _tint = new Color4(0.3f, 0.3f, 1.0f, 1.0f);

        // P2: schimba culoarea unui singur varf si copiaza tabloul actualizat in VBO.
        if (KeyboardState.IsKeyPressed(Keys.A))
        {
            for (int i = 0; i < 3; i++) _triangle.SetColor(i, 1.0f, 1.0f, 1.0f);
            _triangle.UpdateVertices();
        }
        if (KeyboardState.IsKeyPressed(Keys.R))
        {
            _triangle.SetColor(0, 1.0f, 0.0f, 0.0f);
            _triangle.SetColor(1, 0.0f, 1.0f, 0.0f);
            _triangle.SetColor(2, 0.0f, 0.0f, 1.0f);
            _triangle.UpdateVertices();
        }
        if (KeyboardState.IsKeyPressed(Keys.V))
        {
            Color4[] colors = { Color4.Yellow, Color4.Cyan, Color4.Magenta };
            Color4 color = colors[_vertexColorIndex];
            _triangle.SetColor(_vertexEditIndex, color.R, color.G, color.B);
            _triangle.UpdateVertices();
            _vertexEditIndex = (_vertexEditIndex + 1) % 3;
            _vertexColorIndex = (_vertexColorIndex + 1) % colors.Length;
        }

        // P3: F comuta Fill/Line; M parcurge Fill, Line si Point.
        if (KeyboardState.IsKeyPressed(Keys.F))
        {
            _polygonMode = _polygonMode == PolygonMode.Line ? PolygonMode.Fill : PolygonMode.Line;
        }
        if (KeyboardState.IsKeyPressed(Keys.M))
        {
            _polygonMode = _polygonMode switch
            {
                PolygonMode.Fill => PolygonMode.Line,
                PolygonMode.Line => PolygonMode.Point,
                _ => PolygonMode.Fill
            };
        }
        if (KeyboardState.IsKeyPressed(Keys.T))
        {
            VSync = VSync == VSyncMode.On ? VSyncMode.Off : VSyncMode.On;
        }
        if (KeyboardState.IsKeyPressed(Keys.D)) _deformAllVertices = !_deformAllVertices;
        if (KeyboardState.IsKeyPressed(Keys.X)) _collapsed = !_collapsed;

        // P4: deplasare la viteza constanta cu delta time, plus oscilatia orizontala.
        float dt = (float)args.Time;
        const float speed = 1.0f;
        if (KeyboardState.IsKeyDown(Keys.Left)) _manualOffset.X -= speed * dt;
        if (KeyboardState.IsKeyDown(Keys.Right)) _manualOffset.X += speed * dt;
        if (KeyboardState.IsKeyDown(Keys.Down)) _manualOffset.Y -= speed * dt;
        if (KeyboardState.IsKeyDown(Keys.Up)) _manualOffset.Y += speed * dt;
        _offset = new Vector3(_manualOffset.X + 0.5f * MathF.Sin((float)_elapsed),
                              _manualOffset.Y, _manualOffset.Z);

        // P1: fiecare canal RGB oscileaza cu alta faza.
        float time = (float)_elapsed;
        _clearColor = new Color4(
            0.5f + 0.5f * MathF.Sin(time),
            0.5f + 0.5f * MathF.Sin(time + 2.0f * MathF.PI / 3.0f),
            0.5f + 0.5f * MathF.Sin(time + 4.0f * MathF.PI / 3.0f),
            1.0f);

        // P5: fiecare varf este deformat individual in RAM, apoi tabloul se copiaza in VBO.
        if (_collapsed)
        {
            _triangle.SetPosition(0, 0.0f, 0.0f);
            _triangle.SetPosition(1, 0.0f, 0.0f);
            _triangle.SetPosition(2, 0.0f, 0.0f);
        }
        else if (_deformAllVertices)
        {
            _triangle.SetPosition(0, -0.5f + 0.12f * MathF.Sin(time),
                                     -0.5f + 0.10f * MathF.Sin(time * 1.3f));
            _triangle.SetPosition(1,  0.5f + 0.12f * MathF.Sin(time * 1.4f + 2.0f),
                                     -0.5f + 0.10f * MathF.Sin(time * 0.9f + 1.0f));
            _triangle.SetPosition(2,  0.08f * MathF.Sin(time * 0.7f),
                                      0.5f + 0.30f * MathF.Sin(time));
        }
        else
        {
            _triangle.SetPosition(0, -0.5f, -0.5f);
            _triangle.SetPosition(1, 0.5f, -0.5f);
            _triangle.SetPosition(2, 0.0f, 0.5f + 0.3f * MathF.Sin(time));
        }
        _triangle.UpdateVertices();

        // Exemplul bucla principală: o dată pe secundă afișăm în titlu câte
        // actualizări și câte cadre desenate au avut loc.
        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            Title = $"{_baseTitle} | update/s: {_updates} | render/s: {_frames}";
            _updates = 0;
            _frames = 0;
            _secondTimer -= 1.0;
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche din back buffer. GL.ClearColor SETEAZĂ starea (culoarea de ștergere),
        //    GL.Clear o FOLOSEȘTE. Aici o setăm la fiecare cadru pentru că vom putea s-o schimbăm (P1).
        GL.ClearColor(_clearColor.R, _clearColor.G, _clearColor.B, _clearColor.A);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Activăm programul shader și îi trimitem uniformele.
        _shader.Use();
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);
        _shader.SetVector3("uOffset", _offset);
        _shader.SetColor("uTint", _tint);

        // 3. Setam rasterizarea si desenam triunghiul. PolygonMode este stare OpenGL.
        GL.PolygonMode(TriangleFace.FrontAndBack, _polygonMode);
        if (_showQuad)
        {
            _quad.Draw();
        }
        else
        {
            _triangle.Draw();
        }

        // 4. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        _triangle.Dispose();
        _quad.Dispose();
        _shader.Dispose();

        base.OnUnload();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("EGC - LABORATOR 02");
        Console.WriteLine("LISTA COMENZILOR");
        Console.WriteLine("========================================");
        Console.WriteLine("H              - afiseaza aceasta lista");
        Console.WriteLine("ESC            - inchide aplicatia");
        Console.WriteLine("TAB            - comuta intre triunghi si dreptunghi");
        Console.WriteLine("SAGETI         - deplaseaza forma selectata");
        Console.WriteLine("0 - 3          - schimba nuanta generala a formei");
        Console.WriteLine("A              - face albe toate varfurile triunghiului");
        Console.WriteLine("R              - reface culorile RGB ale triunghiului");
        Console.WriteLine("V              - schimba pe rand culoarea unui varf triunghiular");
        Console.WriteLine("F              - comuta intre afisarea plina si contur");
        Console.WriteLine("M              - parcurge modurile plin, contur si puncte");
        Console.WriteLine("T              - comuta VSync");
        Console.WriteLine("D              - comuta intre deformarea tuturor varfurilor si doar varful de sus");
        Console.WriteLine("X              - strange sau reface triunghiul in centru");
        Console.WriteLine("========================================");
    }
}
