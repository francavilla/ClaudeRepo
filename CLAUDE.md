# Istruzioni per Claude su questo repository

## Branch
- Lavorare e fare push sempre sul branch **`Progetti_Claude`**.

## Versioni (obbligatorio a ogni modifica)
Ogni modifica a un'applicazione del repository deve aggiornare il suo numero di versione:

1. Aggiornare `<Version>` nel `Directory.Build.props` dell'applicazione
   (per ExeBuilder: `ExeBuilder/Directory.Build.props`), seguendo il Semantic Versioning:
   - **PATCH** (1.1.0 → 1.1.1): correzione di bug senza nuove funzioni;
   - **MINOR** (1.1.0 → 1.2.0): nuove funzioni compatibili;
   - **MAJOR** (1.1.0 → 2.0.0): cambiamenti incompatibili (formato, comportamento, requisiti).
2. Aggiungere la voce in cima al `CHANGELOG.md` dell'applicazione (sezioni Aggiunto / Modificato / Corretto),
   con la data del giorno.
3. Indicare la nuova versione nel messaggio di commit e nella risposta all'utente.

Più modifiche nella stessa richiesta = un solo incremento di versione.

## Release
- Le release di ExeBuilder si creano con il workflow `Release ExeBuilder`:
  - avvio manuale (Actions → Run workflow, o API `workflow_dispatch` su `main`): usa `<Version>` e crea il tag `vX.Y.Z`;
  - oppure pubblicando un tag `vX.Y.Z` uguale a `<Version>` (dalla sessione cloud il push dei tag non è consentito).
- Il workflow `.github/workflows/release-exebuilder.yml` verifica la versione, esegue i test, compila su Windows
  e crea la release GitHub con lo zip e le note prese dal `CHANGELOG.md`.

## Applicazioni
- `ExeBuilder/` — WPF .NET Framework 4.8 che compila in Release progetti e solution .NET/.NET Framework.
  La logica sta in `ExeBuilder.Core` (testata in `tests/ExeBuilder.Core.Tests`); la UI in `src/ExeBuilder` (MVVM, tema in `Themes/Theme.xaml`).

## Stile del codice
- C# 7.3 (`LangVersion` in `Directory.Build.props`), commenti e messaggi in italiano.
- L'utente sviluppa principalmente in C# con .NET Framework e .NET Core.
