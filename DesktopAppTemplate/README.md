# DesktopAppTemplate

Modello di applicazione desktop per .NET Framework 4.6.2+ con **due interfacce intercambiabili**
(WPF e Windows Forms) sopra la **stessa logica**, organizzata a **slice verticali**.

## Avvio

```
DesktopAppTemplate.exe --ui wpf        # interfaccia WPF
DesktopAppTemplate.exe --ui winforms   # interfaccia Windows Forms
```

Altre opzioni: `--help` (o `-h`, `/?`) mostra l'aiuto e `--version` (o `-v`) la versione, senza aprire l'interfaccia.
Se l'app è avviata da un prompt il testo compare lì, altrimenti in una finestra di messaggio.

Senza argomento vale `Ui` in `App.config` (predefinito `wpf`). Da Visual Studio: impostare gli
*Application arguments* nelle proprietà di debug del progetto `DesktopAppTemplate.Host`.

## Dati su file

Le attività sono salvate in un file JSON (`tasks.json`), per impostazione predefinita in
`%LocalAppData%\DesktopAppTemplate` (cartella dell'utente: non servono permessi di amministratore).
Per cambiarla: chiave `DataFolder` in `App.config` (sono ammesse variabili come `%USERPROFILE%`).

- Al primo avvio, se il file non esiste, parte con alcune attività di esempio (`SampleTasks`); poi vale solo il file.
- Ogni modifica riscrive il file in modo sicuro (file temporaneo + sostituzione): un'interruzione non lo lascia a metà.
- Se il file è danneggiato l'app mostra l'errore con il percorso e **non lo tocca**: si corregge o si elimina per ripartire da zero.
- È pensato per una sola istanza dell'app alla volta.
- Il formato su disco (`TaskRecord`) è separato dal modello di dominio e porta un numero di versione (`Version`).

Per passare a un database basta un'altra implementazione di `ITaskRepository` registrata in
`InfrastructureServiceCollectionExtensions`: slice e view model non cambiano.

## Icona e versione

L'icona (`assets/app.ico`) è usata dall'`.exe` e dalle finestre di entrambe le interfacce; si rigenera con
`python tools/make_icon.py` (richiede Pillow) o si sostituisce con un'icona propria. Il numero di versione si imposta
solo in `Directory.Build.props` (`<Version>`) e compare nel titolo della finestra, nel menu laterale, in *Informazioni*
e nelle proprietà dell'`.exe`.

## Struttura

```
src/
  DesktopAppTemplate.Core            netstandard2.0  Mediator, validazione, MVVM, astrazioni (nessuna UI)
  DesktopAppTemplate.Features        netstandard2.0  Slice: richieste, handler, validatori, view model
  DesktopAppTemplate.Infrastructure  netstandard2.0  Implementazioni concrete (orologio, archivio su file JSON, info app)
  DesktopAppTemplate.UI.Wpf          net462          Solo view XAML + tema
  DesktopAppTemplate.UI.WinForms     net462          Solo view Windows Forms (con designer)
  DesktopAppTemplate.Host            net462          Composition root: DI e scelta della UI
tests/
  DesktopAppTemplate.Tests           xUnit
```

Dipendenze (sempre verso l'interno): `Host → UI/Infrastructure → Features → Core`.
`Features` non conosce né le UI né l'Infrastructure: dichiara le interfacce (es. `ITaskRepository`)
che l'Infrastructure implementa.

## Slice

Ogni funzione sta in una cartella di `Features` e contiene tutto ciò che serve:

```
Features/Tasks/
  AddTask/AddTask.cs        AddTaskCommand + AddTaskValidator + AddTaskHandler
  ListTasks/ListTasks.cs    ListTasksQuery + risultato + handler
  ToggleTask/, RemoveTask/
  TaskListViewModel.cs      stato e comandi della pagina (condivisi da WPF e Windows Forms)
```

I view model parlano solo con `IMediator`: `await _mediator.Send(new AddTaskCommand(titolo))`.
Handler e validatori sono registrati automaticamente (`AddHandlersFromAssembly`).

### Aggiungere una funzione

1. Creare `Features/<Funzione>/<CasoDUso>/<CasoDUso>.cs` con request, handler ed eventuale validator.
2. Creare il view model derivando da `PageViewModel` e registrarlo in `FeaturesServiceCollectionExtensions`
   (`services.AddSingleton<PageViewModel, MioViewModel>()`): la voce nel menu compare da sola.
3. Aggiungere la view: in WPF un `DataTemplate` in `MainWindow.xaml`, in Windows Forms una riga in `MainForm.CreateView`.

## Windows Forms con il designer

`MainForm`, `TaskListView` e `AboutView` seguono il modello classico: file `.cs` (collegamento al view model)
+ `.Designer.cs` (aspetto), modificabili con il designer di Visual Studio. Ogni view ha un costruttore senza parametri
(per il designer) e un metodo `Bind(viewModel)` che la collega alla logica a runtime. Le voci del menu laterale e le pagine,
che dipendono dai view model registrati, vengono create nel codice di `MainForm`.

## Adattarlo a un nuovo progetto

1. **Creare il progetto**: `.\New-Project.ps1 -NewName GestioneOrdini` (PowerShell). Copia il template in una nuova cartella,
   rinomina namespace, assembly, file e cartelle e assegna nuovi GUID ai progetti. Il template non viene modificato.
2. **Togliere l'esempio** (se non serve): eliminare `Features/Tasks`, la sua riga in `FeaturesServiceCollectionExtensions`,
   `Infrastructure/InMemoryTaskRepository.cs` con la sua registrazione e i test delle attività.
   Tenere `Core`, `Shell`, `About` e le UI: sono l'infrastruttura riutilizzabile.
3. **Aggiungere le proprie funzioni** come slice (vedi sopra). Una funzione = una cartella; nessun altro file da modificare
   oltre alla riga di registrazione della pagina e alla view.
4. **Una sola interfaccia?** Eliminare il progetto `UI.*` non necessario, il suo `ProjectReference` nell'Host e il ramo
   corrispondente in `Program.cs`.
5. **Persistenza**: i dati sono già su file JSON (`JsonFileTaskRepository`); per un database sostituire
   l'implementazione del repository nell'Infrastructure, le slice non cambiano.
6. **Versione e changelog**: `<Version>` in `Directory.Build.props` e `CHANGELOG.md`.

Regole per mantenerlo scalabile: ogni dipendenza punta verso `Core`; le view non contengono logica; un caso d'uso
è sempre un `IRequest` con il suo handler; nuovi servizi si dichiarano come interfaccia in `Core` o nella slice che li usa.

### Verso un template di Visual Studio

La struttura è pensata per diventare, in futuro, un template di Visual Studio (`.vstemplate`, multi-progetto):
il nome del template compare sempre come un unico token (`DesktopAppTemplate`) in namespace, assembly, riferimenti, XAML e `.sln`,
senza percorsi assoluti né dipendenze dalla cartella in cui si trova. `New-Project.ps1` fa già oggi la stessa sostituzione;
per il template basterà sostituire quel token con `$safeprojectname$` e descrivere i sei progetti nel `.vstemplate`.
Per non complicare la conversione: non usare il nome del template in modo "creativo" (es. abbreviazioni o concatenazioni) e
non aggiungere file generati o dipendenti dalla macchina.

## SOLID in pratica

- **S** — un handler = un caso d'uso; le view mostrano, i view model orchestrano, l'Infrastructure persiste.
- **O** — una nuova slice o una nuova UI si aggiungono senza modificare il codice esistente (a parte la registrazione).
- **L/I** — interfacce piccole e mirate (`IClock`, `IDialogService`, `IUiShell`, `ITaskRepository`).
- **D** — tutto dipende da astrazioni; l'unico punto che conosce i tipi concreti è `Host/Program.cs`.

## Compilazione e test

Richiede Windows (WPF e Windows Forms) con .NET SDK 6+ o Visual Studio 2019+.

```
dotnet test tests/DesktopAppTemplate.Tests
dotnet build DesktopAppTemplate.sln -c Release
```

L'eseguibile si trova in `src/DesktopAppTemplate.Host/bin/Release/net462/`.
