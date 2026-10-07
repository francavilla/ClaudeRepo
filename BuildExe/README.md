# BuildExe

Applicazione desktop **WPF per .NET Framework 4.8** che crea l'eseguibile di un progetto .NET
in configurazione **Release**, scegliendo da sola lo strumento di build corretto
**in base alle sole informazioni contenute nel progetto**.

## Uso

1. Avviare `BuildExe.exe` (o `BuildExe.exe "C:\percorso\Progetto.csproj"`).
2. Indicare un **progetto** (`.csproj`, `.vbproj`, `.fsproj`) o una **solution** (`.sln`, `.slnx`):
   con *Sfoglia...*, scrivendo il percorso o trascinando il file sulla finestra.
3. Con una solution compare l'elenco dei **progetti eseguibili** (`OutputType` Exe/WinExe; librerie,
   test e progetti web sono esclusi). Spuntare quelli da compilare: uno, alcuni, oppure tutti con
   *Seleziona tutti*. Se la solution ha un solo eseguibile è già spuntato.
   Selezionando una riga, il pannello di analisi mostra i dettagli di quel progetto.
4. Controllare l'analisi: formato del progetto, target framework (scelta per ogni progetto multi-target),
   strumento scelto e comando esatto.
5. Indicare la **cartella di output**: assoluta, o relativa alla cartella della solution/del progetto.
   Con più progetti selezionati ognuno finisce in una propria sottocartella `<output>\<NomeProgetto>`.
   **La cartella di output viene svuotata prima di ogni build**, così contiene solo il risultato
   dell'ultima build. Per sicurezza BuildExe rifiuta la radice di un disco, le cartelle di sistema
   e del profilo utente, e qualunque cartella che coincida con quella del progetto/della solution
   o la contenga. Se un file è in uso (per esempio l'eseguibile è aperto) la build non parte.
6. Premere **Crea eseguibile** (o **Crea N eseguibili**). I progetti vengono compilati in sequenza;
   la colonna *Stato* e il riepilogo finale indicano le build riuscite e quelle fallite.
   Il log mostra l'output di MSBuild/dotnet (errori in rosso); *Annulla* interrompe la build in corso
   e salta le successive.

## Come viene scelta la build

| Progetto | Esempio | Strumento | Comando |
|---|---|---|---|
| SDK-style, .NET 5+ / .NET Core | `net8.0-windows`, `netcoreapp3.1` | .NET SDK di major ≥ al target | `dotnet publish -c Release -f <tfm> -o <out>` |
| SDK-style, .NET Framework | `net48`, `net472` | .NET SDK (≥ 3 per WPF/WinForms) | `dotnet publish -c Release -f net48 -o <out>` |
| SDK-style con `COMReference` | | MSBuild.exe 16+ | `MSBuild -restore -t:Publish ...` |
| Classico (non SDK-style) | `<TargetFrameworkVersion>v4.8` | MSBuild.exe di VS/Build Tools (vswhere) | `MSBuild -restore -t:Build -p:Configuration=Release -p:OutDir=<out>\` |

Informazioni lette dal progetto:
`Sdk`, `TargetFramework`/`TargetFrameworks`, `TargetFrameworkIdentifier`/`TargetFrameworkVersion`,
`OutputType`, `AssemblyName`, `UseWPF`/`UseWindowsForms`, `ProjectTypeGuids` (progetti web),
`COMReference`, `packages.config`. Vengono letti anche `Directory.Build.props` e gli `Import` locali,
come farebbe MSBuild. Le condizioni (`'$(Configuration)|$(Platform)' == 'Release|AnyCPU'`, `Exists(...)`,
`and`/`or`, `Choose/When`) sono valutate simulando `Configuration=Release`.

Controlli eseguiti prima della build:
- il progetto deve produrre un eseguibile (non una libreria né un progetto web);
- è installato un .NET SDK di versione sufficiente, con il link per scaricarlo se manca;
- per i progetti classici: MSBuild 15.5+ e il **targeting pack** della versione del Framework (MSB3644);
- `packages.config` → `-p:RestorePackagesConfig=true` (MSBuild 16.5+);
- viene sempre passato `SolutionDir` quando è noto, come fa Visual Studio. Lo si prende dalla solution scelta;
  se è stato scelto un progetto da solo, dalla `.sln` delle cartelle superiori che lo contiene o, in mancanza,
  dalla cartella `packages\` indicata negli `HintPath`. È indispensabile per il restore di `packages.config`.

Opzioni solo per .NET Core/.NET 5+: **self-contained**, **file singolo**, **runtime** (`win-x64`, `win-x86`, `win-arm64`).

## Struttura

```
BuildExe.sln
src/
  BuildExe.Core/        net48 - nessuna dipendenza dalla UI
    Model/              TargetFramework, ProjectInfo, Toolchains, BuildOptions, BuildPlan
    Analysis/           ProjectAnalyzer (valutazione statica MSBuild), ConditionEvaluator, SolutionParser
    Planning/           BuildPlanner: logica di decisione pura, completamente testata
    Toolchain/          ToolchainLocator: dotnet --list-sdks, vswhere, Reference Assemblies
    Execution/          ProcessRunner (output in streaming, annullamento), BuildService, CommandLine,
                        OutputFolder (svuotamento sicuro della cartella di output)
  BuildExe/             WPF net48 - MVVM senza librerie esterne
    ViewModels/         MainViewModel, ProjectItemViewModel (un progetto dell'elenco)
    Services/           finestre di dialogo, selettore cartelle moderno (IFileOpenDialog)
tests/
  BuildExe.Core.Tests/  xUnit - 74 test
docs/adr/               decisioni architetturali
```

## Requisiti

- **Per compilare BuildExe**: Visual Studio 2019 16.8+ o 2022 (carico di lavoro *Sviluppo desktop .NET*).
- **Per eseguirlo**: Windows 10/11 con .NET Framework 4.8.
- **Per compilare i progetti selezionati**: il .NET SDK e/o Visual Studio/Build Tools richiesti dal progetto;
  BuildExe indica cosa manca.

## Limiti noti

- Le *property function* MSBuild (`$([System.IO.Path]::Combine(...))`, `$([MSBuild]::...)`) non sono valutate:
  la condizione viene considerata falsa e compare un avviso.
- `Directory.Build.targets` e i file importati dagli SDK non vengono letti: le proprietà impostate lì
  (raro per `TargetFramework`/`OutputType`) non sono viste.
- `global.json` viene rispettato da `dotnet`, ma non è usato per scegliere l'SDK.
