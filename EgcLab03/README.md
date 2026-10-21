# EGC Laborator 03

Proiectul porneste de la sablonul profesorului si completeaza problemele obligatorii P1-P5. Foloseste .NET 8, OpenTK 4.9.4, OpenGL 3.3 Core si GLSL 330.

## Rulare

In folderul care contine `EgcLab03.csproj`:

```sh
dotnet restore
dotnet build -c Release
dotnet run
```

Comenzile functioneaza in Visual Studio 2022 pe Windows si in terminal pe macOS. Shaderele se copiaza in output si se citesc prin `AppContext.BaseDirectory`. La pornire, consola afiseaza versiunea OpenGL, vendorul si rendererul driverului local.

## Taste

- `Tab` schimba forma: triunghi, dreptunghi, poligon, cerc, inel, grila.
- `F` comuta intre contur si plin. Cand dreptunghiul este selectat: `A` da tuturor colturilor aceeasi culoare, `R` pune rosu jos si albastru sus, iar `B` reface culorile rgb galben initiale.
- sagetile deplaseaza forma; `0`-`3` schimba nuanta.
- `+` si `-` schimba numarul de laturi al poligonului (3-64).
- `W` porneste/opreste unda din vertex shader; `C` comuta intre culoarea din VBO si culoarea calculata din pozitie.
- `P` pune pe pauza timpul animatiei; fereastra, comenzile, actualizarea titlului si FPS-ul continua.
- `M` schimba modul de desenare al inelului; `Esc` inchide fereastra.

## Probleme obligatorii

P1 foloseste un dreptunghi cu patru varfuri si sase indici. P2 construieste un poligon regulat cu centru si culori HSV. P3 foloseste acelasi generator cu 64 de laturi, fara corectie de aspect, pentru a observa elipsa din fereastra 800 x 600. P4 schimba nuanta fundalului cu viteza 0.05, saturatia 0.5 si luminozitatea 0.35; la pornire sunt afisate in consola si cele patru valori de verificare HSV din fisa. P5 calculeaza unda si culoarea din pozitie in vertex shader; timpul ingheata la pauza, iar VBO-ul nu se modifica.

Aspectul si valorile OpenGL depind de driver; proiectul trebuie rulat local pentru verificarea vizuala.
