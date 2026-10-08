# LegacyDemo

Piccola solution WinForms **volutamente legacy e non compilabile** (non fa parte di `SolutionDoctor.sln`): serve per provare
SolutionDoctor e come banco di prova della CI, che ne controlla il risultato nella finestra.

Contiene gli smell più tipici: `packages.config`, framework fuori supporto (net452), `System.Web`, accesso al database
dentro un event handler, SQL costruito per concatenazione, `Application.DoEvents()`, `InvokeRequired` e stato statico.

```
solutiondoctor analyze SolutionDoctor/samples/LegacyDemo -o demo.md
SolutionDoctor.App.exe SolutionDoctor\samples\LegacyDemo\LegacyDemo.sln
```
