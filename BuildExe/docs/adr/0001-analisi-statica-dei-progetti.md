# ADR 0001 – Analisi statica dei file di progetto invece di caricare MSBuild

- **Stato:** accettata

## Contesto

BuildExe deve capire, prima della build, per quale .NET o .NET Framework è scritto un progetto
e quindi quale toolchain usare. Le alternative:

1. **Microsoft.Build + Microsoft.Build.Locator**: valutazione completa e fedele del progetto.
2. **Analisi statica dell'XML** del progetto, di `Directory.Build.props` e degli import locali,
   con un valutatore minimo di proprietà e condizioni.

## Decisione

Opzione 2.

- L'opzione 1 richiede che sulla macchina ci sia proprio l'MSBuild giusto. Per valutare un progetto
  SDK-style da un processo .NET Framework serve MSBuild di Visual Studio con i resolver SDK
  allineati, e i conflitti di versione degli assembly MSBuild caricati in-process sono un problema noto.
  BuildExe invece deve funzionare *proprio* quando la toolchain manca, per poterlo dire all'utente.
- Le informazioni necessarie (`TargetFramework(s)`, `TargetFrameworkVersion`, `OutputType`, `Sdk`,
  `UseWPF`) sono quasi sempre dichiarate in chiaro nel progetto o in `Directory.Build.props`.
- È deterministica e veloce: una solution di centinaia di progetti si analizza in millisecondi.

## Conseguenze

- Le property function e i file degli SDK non vengono valutati. Il valutatore lo segnala con un
  avviso invece di indovinare.
- La decisione (`BuildPlanner`) è una funzione pura di `ProjectInfo` + `Toolchains`: è completamente
  testabile senza Visual Studio né SDK installati.
- Se in futuro servirà una fedeltà totale, si potrà aggiungere un `IProjectAnalyzer` basato su
  Microsoft.Build senza modificare planner e UI.
