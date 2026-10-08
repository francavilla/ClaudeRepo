# SolutionDoctor

Strumento a riga di comando che **analizza una solution Windows Forms legacy e produce un piano di refactoring
ordinato per priorità**. Non modifica nulla: legge i file di progetto e i sorgenti C#, e scrive un report Markdown.

Risponde alla domanda più difficile di un refactoring: *da dove comincio, e in che ordine?*

## Uso

```
solutiondoctor analyze <percorso> [opzioni]
```

`<percorso>` può essere un `.sln`, un `.csproj` o una cartella (se contiene un solo `.sln` viene usato quello,
altrimenti si analizzano tutti i `.csproj` sotto la cartella).

| Opzione | Effetto |
|---|---|
| `-o`, `--output <file>` | scrive il report su file (altrimenti su standard output) |
| `--fail-on <livello>` | codice di uscita 1 se esistono problemi di gravità ≥ `info`/`low`/`medium`/`high` |
| `--no-git` | non usa la cronologia git per la priorità |
| `--months <n>` | finestra della cronologia git in mesi (predefinito 12) |

Codici di uscita: `0` ok, `1` soglia `--fail-on` superata, `2` errore.

Esempio, anche in CI per impedire che gli smell più gravi crescano:

```
solutiondoctor analyze MiaApp.sln -o report.md --fail-on high
```

## Cosa contiene il report

- **Riepilogo**: progetti, form, problemi per gravità.
- **Da dove cominciare**: le classi con più problemi, pesate per quanto spesso cambiano.
- **Progetti**: framework, formato, pacchetti.
- **Ordine di migrazione**: dalle foglie agli eseguibili, con grafo Mermaid e segnalazione dei cicli.
- **Problemi per regola**, con file e riga, e **come intervenire**.

### Regole

| Id | Gravità | Cosa rileva |
|---|---|---|
| `WF001` | bassa → alta | form/user control con 400 / 800 / 1500+ righe di code-behind (Designer escluso) |
| `WF002` | media | `SqlConnection`/`SqlCommand`/…, `File.*`, `Directory.*` dentro un event handler |
| `WF003` | media | `Application.DoEvents()` |
| `WF004` | alta | comando SQL costruito per concatenazione, interpolazione o `string.Format` |
| `WF005` | bassa/media | campi e proprietà automatiche `static` modificabili |
| `WF006` | info | `InvokeRequired` sparso (una segnalazione per file) |
| `WF007` | bassa/media | event handler di oltre 50 / 150 righe |
| `PJ001` | media | `packages.config` in uso |
| `PJ002` | bassa | `.csproj` non SDK-style |
| `PJ003` | bassa → alta | `System.Web`, `System.Runtime.Remoting`, `System.Data.OracleClient`, `System.EnterpriseServices`, WCF, riferimenti COM |
| `PJ004` | media | .NET Framework precedente alla 4.6.2 |

Le regole `WF*` si applicano ai soli progetti WinForms (`UseWindowsForms` o riferimento a `System.Windows.Forms`);
le regole `PJ*` a tutti i progetti.

### Priorità

```
priorità = Σ pesi dei problemi della classe × (1 + min(commit recenti, 50) / 10)
pesi: alta 10, media 4, bassa 2, info 1
```

A parità di problemi vince il codice che si modifica più spesso, dove il refactoring rende di più.
I commit si contano su file e relativo `.Designer.cs`.

## Limiti noti

- L'analisi è **solo sintattica** (Roslyn senza compilazione): funziona anche su solution che non compilano
  su questa macchina, ma non risolve i tipi. Un comando SQL costruito in una variabile e passato per nome
  (`new SqlCommand(sql, cn)`) non viene riconosciuto; una costante con nome concatenata può dare un falso positivo.
- Il riconoscimento delle form si basa sul nome del tipo base (`Form`, `UserControl`, `*Form`, `*UserControl`)
  o sulla chiamata a `InitializeComponent()`.
- Solo progetti C# (`.csproj`).

## Sviluppo

- `src/SolutionDoctor.Core` — logica (netstandard2.0, nessuna dipendenza da UI o console), testata in
  `tests/SolutionDoctor.Core.Tests`.
- `src/SolutionDoctor.Cli` — riga di comando (.NET 8).
- C# 7.3 (`LangVersion` in `Directory.Build.props`), commenti e messaggi in italiano.

```
dotnet test SolutionDoctor/SolutionDoctor.sln -c Release
dotnet run --project SolutionDoctor/src/SolutionDoctor.Cli -- analyze <percorso>
```

Per eseguire il programma serve il runtime .NET 8. Prossimo passo previsto: interfaccia WPF che riusa il Core.
