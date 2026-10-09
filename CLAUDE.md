# Istruzioni per Claude su questo repository

## Branch
- Lavorare e fare push sul branch **`main`**: dall'8 ottobre 2026 tutte le integrazioni avvengono lì, anche se la sessione ne assegna un altro (confermato dall'utente).
- **`Progetti_Claude` è congelato** allo stato di DesktopAppTemplate 4.2.2 (commit `b2d2eb3`): non aggiungervi altro.
- DesktopAppTemplate riparte dalla versione **1.0.0** (equivalente alla 4.2.2); le versioni 1.0.0 – 4.2.2 del changelog sono la cronologia interna dello sviluppo precedente.
- PasswordGen riparte dalla versione **1.0.0** (equivalente alla 1.9.1, release `passwordgen-v1.9.1` congelata); le versioni 1.0.0 – 1.9.1 del changelog sono la cronologia interna precedente.
  Il codice di versione Android (`ApplicationVersion`) è `(major*10000 + minor*100 + patch)*1000 + n`, con n = 999 nelle release e il numero del run (max 998) nelle build di prova della CI:
  deve solo crescere, altrimenti Android rifiuta l'aggiornamento come «downgrade». Le release 1.0.0-1.3.0 usano il vecchio schema `100000 + major*10000 + minor*100 + patch` (più basso, quindi compatibile).

## Versioni (obbligatorio a ogni modifica)
Ogni modifica a un'applicazione del repository deve aggiornare il suo numero di versione:

1. Aggiornare `<Version>` nel `Directory.Build.props` dell'applicazione
   (per ExeBuilder: `ExeBuilder/Directory.Build.props`, per PasswordGen: `PasswordGen/Directory.Build.props`,
   per SolutionDoctor: `SolutionDoctor/Directory.Build.props`), seguendo il Semantic Versioning:
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

- Le release di SolutionDoctor sono **una sola**, con due zip (interfaccia e riga di comando): workflow `Release SolutionDoctor`
  (`.github/workflows/release-solutiondoctor.yml`), tag `solutiondoctor-vX.Y.Z` (il prefisso evita il conflitto con i tag `vX.Y.Z` di ExeBuilder),
  file `SolutionDoctor-App-vX.Y.Z.zip` e `SolutionDoctor-Cli-vX.Y.Z.zip`, note prese dal `CHANGELOG.md`.
  Avvio manuale su `main` (rifiuta altri rami) o tag uguale a `<Version>`; con `dry_run` costruisce e impacchetta senza creare tag né release (prova, da qualsiasi ramo).
  Richiedono il .NET 8 (Desktop Runtime per l'interfaccia). La 1.0.0 (solo CLI) ha un unico zip `SolutionDoctor-v1.0.0.zip`; dalla 1.1.0 gli zip sono due.

- Le release di DesktopAppTemplate si creano con il workflow `Release DesktopAppTemplate` (`.github/workflows/release-desktopapptemplate.yml`):
  stesso funzionamento, ma il tag è `desktopapptemplate-vX.Y.Z`, lo zip `DesktopAppTemplate-vX.Y.Z.zip` e le note vengono da `DesktopAppTemplate/CHANGELOG.md`.

- Le release di PasswordGen sono **due, separate** (stessa versione di `PasswordGen/Directory.Build.props`, ma tag, titolo e file propri):
  - Windows: workflow `Release PasswordGen Desktop` (`.github/workflows/release-passwordgen-desktop.yml`), tag `passwordgen-desktop-vX.Y.Z`, zip `PasswordGen-Desktop-vX.Y.Z.zip`;
  - Android: workflow `Release PasswordGen Android` (`.github/workflows/release-passwordgen-android.yml`), tag `passwordgen-android-vX.Y.Z`, APK `PasswordGen-Android-vX.Y.Z.apk`.
  Stesso funzionamento delle altre release (avvio manuale su `main` o tag uguale a `<Version>`). Se non si precisa, «crea la release» vale per entrambe.
  I tag `passwordgen-vX.Y.Z` sono quelli delle release vecchie, con zip e APK insieme.

## CI
- `.github/workflows/ci-exebuilder.yml` compila ExeBuilder su Windows (XAML compreso) ed esegue i test
  a ogni push su `Progetti_Claude`/`main` e a ogni PR verso `main`; l'app compilata è un artifact del run (14 giorni).
- `.github/workflows/ci-solutiondoctor.yml` fa lo stesso per SolutionDoctor: compila tutto (app WPF compresa), esegue i test,
  pubblica CLI e app, prova la CLI su ExeBuilder e prova l'interfaccia (avvio e contenuto delle griglie su `SolutionDoctor/samples/LegacyDemo`).
- Dopo ogni push controllare l'esito della CI e correggere subito eventuali errori.

## Applicazioni
- `ExeBuilder/` — WPF .NET Framework 4.8 che compila in Release progetti e solution .NET/.NET Framework.
  La logica sta in `ExeBuilder.Core` (testata in `tests/ExeBuilder.Core.Tests`); la UI in `src/ExeBuilder` (MVVM, tema in `Themes/Theme.xaml`).
- `SolutionDoctor/` — CLI e interfaccia WPF (.NET 8) che analizzano solution WinForms legacy e producono un report di priorità di refactoring.
  Logica in `SolutionDoctor.Core` (netstandard2.0); logica della UI in `SolutionDoctor.Presentation` (netstandard2.0, senza WPF);
  test in `tests/SolutionDoctor.Core.Tests` e `tests/SolutionDoctor.Presentation.Tests` (anche contratto XAML ↔ ViewModel).
  `SolutionDoctor.App` (WPF, `net8.0-windows`) si compila solo su Windows: in locale su Linux testare i due progetti di test, non la `.sln`.

- `DesktopAppTemplate/` — modello di solution con architettura a slice, interfaccia WPF o Windows Forms (net462, scelta con `--ui wpf|winforms`).
  Logica in `Core`/`Features`/`Infrastructure` (netstandard2.0, testata in `tests/DesktopAppTemplate.Tests`); versione in `DesktopAppTemplate/Directory.Build.props`;
  CI in `.github/workflows/ci-desktopapptemplate.yml`.

- `PasswordGen/` — WPF .NET Framework 4.8 che genera password casuali, sicure e memorizzabili (parole italiane, sillabe, caratteri casuali)
  secondo una policy configurabile, con promemoria del cambio password mensile. Logica in `PasswordGen.Core` (testata in `tests/PasswordGen.Core.Tests`);
  UI in `src/PasswordGen` (MVVM, tema in `Themes/Theme.xaml`); versione in `PasswordGen/Directory.Build.props`;
  CI in `.github/workflows/ci-passwordgen.yml`.
  App Android (.NET MAUI, `net10.0-android`) in `PasswordGen/src/PasswordGen.Mobile`, stessa versione dell'app Windows; CI in `.github/workflows/ci-passwordgen-android.yml`.
  Il Core è `net48;netstandard2.0`: niente API solo-Windows fuori da `#if NETFRAMEWORK`.

## Stile del codice
- C# 7.3 (`LangVersion` in `Directory.Build.props`), commenti e messaggi in italiano.
- L'utente sviluppa principalmente in C# con .NET Framework e .NET Core.
