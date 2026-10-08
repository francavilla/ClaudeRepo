# ADR 0002 — UI WPF su .NET 8 con i ViewModel in una libreria separata

- Stato: accettata
- Data: 2026-10-08

## Contesto
SolutionDoctor ha una CLI (.NET 8) e serve un'interfaccia grafica che riusi il Core. `ExeBuilder` è WPF su .NET Framework 4.8,
ma SolutionDoctor ha vincoli diversi:
- il Core usa Roslyn (`Microsoft.CodeAnalysis.CSharp`), che su .NET 8 funziona senza ulteriori accorgimenti, mentre su
  .NET Framework richiede ridirezioni di assembly e dipendenze transitive delicate;
- la CLI richiede già il runtime .NET 8: la UI non aggiunge un requisito nuovo;
- un progetto WPF si compila solo su Windows (il resto del codice si sviluppa e si prova anche su Linux).

## Decisione
- `SolutionDoctor.App` è WPF su `net8.0-windows`.
- Tutta la logica della finestra (ViewModel, comandi, righe, filtri, servizi astratti) sta in `SolutionDoctor.Presentation`,
  `netstandard2.0` e senza riferimenti a WPF. I servizi che toccano il sistema (finestre di dialogo, appunti, shell) sono interfacce
  implementate in `SolutionDoctor.App`.
- L'app è composta a mano in `App.OnStartup` (nessun container DI): le dipendenze sono poche.
- Il XAML non usa convertitori personalizzati: il ViewModel espone già booleani e testi pronti, così c'è meno codice
  che non si può compilare fuori da Windows.

## Come si verifica ciò che non si compila qui
Il XAML e il code-behind si compilano solo su Windows, quindi:
1. i ViewModel hanno test unitari completi (input, analisi, annullamento, filtri, salvataggio, impostazioni);
2. `XamlContractTests` legge i file XAML e controlla binding (anche nei template delle griglie, con il tipo della riga),
   risorse e loro ordine di definizione, gestori di evento, `x:Static`, riferimenti ai file e voci della solution;
   un test verifica il verificatore stesso su un XAML volutamente sbagliato;
3. la CI compila l'app su Windows e ne prova l'avvio: la finestra deve comparire e completare l'analisi di una solution.

## Conseguenze
- Spostare la UI su un altro host (Avalonia, MAUI, web) riusa `Presentation` e `Core` senza modifiche.
- Distribuire l'interfaccia richiede il .NET 8 Desktop Runtime; una pubblicazione autonoma (`SelfContained`) è possibile in futuro.
- Gli errori puramente visivi (layout, colori, allineamenti) restano rilevabili solo guardando la finestra su Windows.
