# EGC Laborator 02

Proiect .NET 8 cu OpenTK 4.9.4 si OpenGL 3.3 Core. Proiectul pastreaza clasele din anexa profesorului si completeaza problemele obligatorii P0-P5.

## Rulare

In folderul care contine `EgcLab02.csproj`:

```sh
dotnet restore
dotnet build -c Release
dotnet run
```

Comenzile sunt aceleasi in Visual Studio 2022 pe Windows si in terminal pe macOS. Shaderele sunt copiate langa executabil si sunt citite din `AppContext.BaseDirectory`. La pornire, consola afiseaza versiunea OpenGL, vendorul si rendererul oferite de driverul local.

## Taste

- `0` revine la culorile originale ale triunghiului; `1`, `2`, `3` aplica nuanta rosie, verde sau albastra.
- `A` da tuturor varfurilor aceeasi culoare; `R` reface culorile rgb initiale. `V` schimba pe rand culoarea unui singur varf si copiaza datele actualizate in VBO.
- `F` comuta intre Fill si Line; `M` parcurge Fill, Line si Point.
- sagetile deplaseaza triunghiul cu viteza raportata la timpul dintre cadre; peste deplasarea lor se vede si oscilatia orizontala. `T` comuta VSync pentru comparatia din P4.
- `D` comuta deformarea doar a varfului de sus si deformarea tuturor varfurilor. `X` aduce toate varfurile in acelasi punct; apasati din nou pentru a reveni.
- `Esc` inchide fereastra.

## Experimente P0-P5

- P0 porneste o fereastra OpenGL si afiseaza triunghiul cu varfuri rgb.
- P1 actualizeaza cele trei canale de fundal cu sinusoide defazate. Mutarea lui `ClearColor` doar in `OnLoad` ar pastra culoarea initiala, fiindca starea OpenGL nu s-ar mai actualiza.
- P2 arata interpolarea culorilor dintre varfuri; `A` face toate varfurile albe, iar `V` schimba individual culorile in galben, cyan si magenta. `R` reface culorile de baza.
- P3 arata ca modul de rasterizare este stare OpenGL si ramane activ pana cand este schimbat.
- P4 incepe cu offset-ul static (0.3, 0.2), apoi combina miscarea sinusoidala cu sagetile si delta time. `T` permite oprirea VSync pentru comparatie. Un pas fix pe cadru ar depinde de FPS; fragmentele din afara cubului NDC sunt eliminate la clipping.
- P5 muta varfurile individual pe CPU si apeleaza `UpdateVertices`; `D` compara miscarea unui singur varf cu miscarea separata a tuturor, iar `X` arata ca un triunghi cu aria zero nu se rasterizeaza ca suprafata. Fara `UpdateVertices`, VBO-ul GPU ramane cu datele vechi. Pentru triunghi, transferul este de 18 float-uri (72 octeti) pe cadru, pe langa uniformele folosite pentru deplasarea intregii forme.

Experimentele sunt explicate ca rezultate asteptate din pipeline-ul OpenGL; valorile driverului si aspectul efectiv se verifica pe calculatorul unde ruleaza programul.
