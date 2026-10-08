# Changelog

Tutte le modifiche rilevanti di ExeBuilder sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

## [1.1.1] - 2026-10-08

### Modificato
- Dimensione predefinita della finestra aumentata del 15% (da 1200×920 a 1380×1058).
  All'avvio la finestra viene ridotta, se necessario, per restare entro l'area utile dello schermo.

## [1.1.0] - 2026-10-08

### Aggiunto
- Numero di versione visibile nella barra del titolo e nell'intestazione (badge `vX.Y.Z`).
- Selezione di uno, alcuni o tutti i progetti eseguibili di una solution (caselle di spunta,
  "Seleziona tutti"/"Nessuno"); build in sequenza, una sottocartella per progetto, stato per riga e riepilogo finale.
- Svuotamento della cartella di output prima di ogni build, con controlli di sicurezza
  (radice del disco, cartelle di sistema/profilo, cartelle dei sorgenti).
- Deduzione di `SolutionDir` quando si sceglie un progetto senza solution (`.sln` nelle cartelle superiori
  o cartella `packages\` degli `HintPath`): necessario al restore di `packages.config`.
- Nuova interfaccia in stile Fluent: intestazione, card, icone, messaggi colorati, console scura per il log.
- Note informative nel piano di build (es. da dove è stata ricavata `SolutionDir`).

### Modificato
- Applicazione rinominata da BuildExe a **ExeBuilder** (cartelle, progetti, namespace, eseguibile).
- L'output di MSBuild/dotnet viene letto in UTF-8 (lettere accentate corrette nel log).

### Corretto
- Eccezione "ItemsControl è incoerente con l'origine elementi" durante lo scorrimento automatico del log.
- Il pannello di analisi non seguiva il progetto quando si cliccava la casella di spunta della riga.
- Restore NuGet fallito ("Non è stata trovata alcuna soluzione") per progetti classici con `packages.config`.

## [1.0.0] - 2026-10-07

### Aggiunto
- Prima versione: analisi statica di progetti e solution, scelta automatica tra `dotnet publish`
  e MSBuild in base al target framework, build Release in una cartella di output, log in tempo reale,
  annullamento.
