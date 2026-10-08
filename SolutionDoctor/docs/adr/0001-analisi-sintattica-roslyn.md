# ADR 0001 — Analisi sintattica con Roslyn, senza MSBuildWorkspace

- Stato: accettata
- Data: 2026-10-08

## Contesto
SolutionDoctor deve analizzare solution WinForms **legacy**: spesso non compilano sulla macchina di analisi
(Developer Pack mancante, pacchetti non ripristinabili, controlli di terze parti non installati, `packages/` assente).
`MSBuildWorkspace` carica la semantica completa, ma richiede un MSBuild funzionante e la risoluzione di tutti i riferimenti.

## Decisione
- I file di progetto si leggono come XML (`ProjectReader`), senza caricare MSBuild.
- I sorgenti si analizzano con **solo l'albero sintattico** di Roslyn (`CSharpSyntaxTree.ParseText`), senza compilazione.
- Il Core è `netstandard2.0`: lo usano la CLI e la UI WPF (entrambe .NET 8, vedi ADR 0002).

## Conseguenze
- Funziona su qualsiasi solution, anche non compilabile, ed è veloce e deterministico.
- Le regole si basano sui nomi dei tipi e non sulla loro risoluzione: un tipo omonimo può dare falsi positivi e
  un valore passato per variabile (es. SQL costruito altrove) può sfuggire. I limiti sono dichiarati nel README.
- Se in futuro servono regole semantiche (flusso dei dati, uso reale dei tipi), si aggiungerà un secondo livello
  opzionale basato su `MSBuildWorkspace`, lasciando invariato quello sintattico.
