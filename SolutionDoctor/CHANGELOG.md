# Changelog

Tutte le modifiche rilevanti di SolutionDoctor sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

## [1.1.0] - 2026-10-08

### Aggiunto
- Interfaccia grafica WPF (`SolutionDoctor.App`, .NET 8, solo Windows): scelta di file `.sln`/`.csproj` o cartella
  (pulsanti, trascinamento, argomento da riga di comando che avvia subito l'analisi), opzioni sulla cronologia git,
  analisi annullabile con avanzamento e riepilogo a riquadri (progetti, form, problemi per gravità).
- Quattro sezioni del risultato: **Da dove cominciare** (backlog ordinabile con i problemi della classe selezionata),
  **Problemi** (filtri per gravità e regola, ricerca, dettaglio con «come intervenire», apri file / mostra nella cartella /
  copia percorso), **Progetti**, **Ordine di migrazione** (con avviso sui riferimenti circolari).
- Salvataggio e copia del report Markdown dalla finestra; ultimo percorso e opzioni ricordati in `%APPDATA%\SolutionDoctor\settings.txt`.
- Libreria `SolutionDoctor.Presentation` (netstandard2.0, senza WPF): ViewModel, comandi, righe da mostrare e servizi
  astratti, compilabile e testabile anche su Linux.
- Core: `SolutionAnalyzer.Analyze(path, options, progress, cancellationToken)` con avanzamento e annullamento
  (l'overload precedente resta invariato).
- Test di contratto XAML ↔ ViewModel: binding, risorse (anche l'ordine di definizione), gestori di evento, `x:Static`,
  voci della solution; poiché il XAML si compila solo su Windows, rilevano in anticipo gli errori che altrimenti
  emergerebbero a runtime.
- Solution di esempio `samples/LegacyDemo` (WinForms legacy, volutamente non compilabile) per provare lo strumento
  e come banco di prova della CI.
- Script `tools/make_icon.py` (Python + Pillow) e icona dell'applicazione.

### Modificato
- Il workflow `Release SolutionDoctor` (tag `solutiondoctor-vX.Y.Z`) pubblica ora due zip, interfaccia
  (`SolutionDoctor-App-vX.Y.Z.zip`) e riga di comando (`SolutionDoctor-Cli-vX.Y.Z.zip`); rifiuta di rilasciare da rami diversi
  da `main` e ha la modalità `dry_run`, che costruisce e impacchetta senza creare tag né release.
- La CI compila anche l'app WPF (XAML compreso), ne pubblica l'artifact e la prova davvero: avvia la finestra sulla solution
  di esempio, apre le quattro sezioni tramite UI Automation e controlla righe e testi delle griglie (quindi anche i binding delle celle).
- ADR 0002: la UI è WPF su `net8.0-windows` (non .NET Framework 4.8) con i ViewModel in una libreria separata.

## [1.0.0] - 2026-10-08

### Aggiunto
- Prima versione, a riga di comando: `solutiondoctor analyze <.sln | .csproj | cartella>`.
- Inventario di solution e progetti C# (classici e SDK-style): framework di destinazione, formato del progetto,
  pacchetti (`packages.config` e `PackageReference`), assembly e riferimenti COM, riferimenti tra progetti.
- Ordine di migrazione consigliato (dalle dipendenze verso gli eseguibili), rilevamento dei riferimenti circolari
  e grafo Mermaid nel report.
- Regole sui progetti: `PJ001` packages.config, `PJ002` formato non SDK-style, `PJ003` riferimenti che complicano
  la migrazione (System.Web, Remoting, OracleClient, EnterpriseServices, WCF, COM), `PJ004` framework fuori supporto.
- Regole sul codice WinForms, con analisi sintattica Roslyn: `WF001` form di grandi dimensioni, `WF002` accesso a
  database/file negli event handler, `WF003` `Application.DoEvents()`, `WF004` SQL costruito per concatenazione,
  `WF005` stato statico modificabile, `WF006` `InvokeRequired` sparso, `WF007` event handler lungo.
- Backlog "Da dove cominciare": classi ordinate per punteggio dei problemi × frequenza di modifica
  (commit degli ultimi mesi dalla cronologia git; senza git la priorità usa solo i problemi).
- Report Markdown (su file con `-o` o su standard output).
- Opzioni `--fail-on <info|low|medium|high>` (codice di uscita 1 sopra soglia, per la CI), `--no-git`, `--months`.
