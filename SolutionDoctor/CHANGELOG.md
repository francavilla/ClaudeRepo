# Changelog

Tutte le modifiche rilevanti di SolutionDoctor sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

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
