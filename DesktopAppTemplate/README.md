# DesktopAppTemplate

Modello di applicazione desktop per .NET Framework 4.6.2+ con **due interfacce intercambiabili**
(WPF e Windows Forms) sopra la **stessa logica**, organizzata a **slice verticali**.

## Avvio

```
DesktopAppTemplate.exe --ui winforms          # interfaccia Windows Forms (predefinita: wpf)
DesktopAppTemplate.exe --read-only            # attività in sola lettura
DesktopAppTemplate.exe --data-folder D:\Dati  # cartella dei dati
DesktopAppTemplate.exe --help                 # elenco completo delle opzioni
DesktopAppTemplate.exe --version
```

`--help` e `--version` mostrano le informazioni ed escono senza aprire l'interfaccia. Se l'app è avviata da un prompt
il testo compare lì, altrimenti in una finestra di messaggio. Un argomento sconosciuto o un valore non valido
(es. `--ui boh`) viene segnalato con un messaggio chiaro e l'app esce con codice 2, senza aprire l'interfaccia.
Da Visual Studio: impostare gli *Application arguments* nelle proprietà di debug del progetto `DesktopAppTemplate.Host`.

## Configurazione e contesto

**Un parametro, tre modi per impostarlo.** Ogni parametro si dichiara una volta sola (`OptionDefinition`) e vale
sia in `App.config` (chiave `DataFolder`) sia da riga di comando (`--data-folder`, ricavato dalla chiave).
Le sorgenti si sovrappongono, dalla più debole alla più forte:

```
valori predefiniti  <  App.config  <  riga di comando
```

Dalla dichiarazione derivano da soli: la lettura da entrambe le sorgenti, la validazione e il testo di `--help`.
La pagina *Informazioni* mostra la configurazione in uso con l'origine di ogni valore (predefinito, App.config o riga di comando).

**Aggiungere un parametro** (3 passi):
1. nel modulo che lo usa, dichiarare l'opzione e leggerla nella sua classe `XxxSettings` (esempi: `UiSettings`, `StorageSettings`, `TaskSettings`);
2. aggiungere una riga in `Host/AppOptions.cs`;
3. iniettare `XxxSettings` dove serve (e, se vuoi, documentare la chiave in `App.config`).

**Il contesto** (`IAppContext`) è il punto unico da cui view model e handler ottengono ciò che serve:

| Parte | Contenuto | Cambia? |
|---|---|---|
| `Configuration` | configurazione risolta | no |
| `App` | nome, versione, runtime | no |
| `Environment` | interfaccia attiva, utente, computer | no |
| `Session` | stato di lavoro condiviso tra le pagine (valori tipizzati per chiave, evento `Changed`) | sì |

Regola: chi ha bisogno di poco chiede poco (es. `TaskSettings` o `IAppConfiguration`, non tutto il contesto):
così il contesto non diventa un "service locator" e le dipendenze restano visibili nei costruttori.

## Dati e database

Le attività si possono salvare in **SQLite** (predefinito, non richiede installare nulla), **SQL Server** oppure in un **file JSON**.
Si sceglie come per ogni altro parametro (App.config o riga di comando):

```
DesktopAppTemplate.exe --storage sqlite                               # predefinito: file <nome app>.db in %LocalAppData%\<nome app>
DesktopAppTemplate.exe --storage sqlserver --connection-string "Server=.;Database=Demo;Integrated Security=True"
DesktopAppTemplate.exe --storage file                                 # file JSON, senza database
DesktopAppTemplate.exe --data-access dapper                           # ado (predefinito) | dapper | ef | dal
```

Con SQL Server preferisci l'autenticazione di Windows (`Integrated Security=True`): una password in App.config è in chiaro.
La stringa di connessione non viene mai scritta nei messaggi dell'app.

### Più tecnologie, stessa slice
`--data-access` sceglie come si leggono e scrivono i dati. Le implementazioni di `ITaskRepository` usano le **stesse tabelle** e lo stesso SQL
(`TaskSql`), quindi i dati scritti con una si leggono con le altre e si possono confrontare sullo stesso database:

| Progetto | Tecnologia | Pacchetti |
|---|---|---|
| `Data.Ado` (predefinita) | ADO.NET puro tramite `IDbExecutor` | nessuno in più |
| `Data.Dapper` | Dapper (SQL tuo, meno codice di lettura) | Dapper |
| `Data.EntityFramework` | Entity Framework 6 (net462; per SQLite usa il driver `System.Data.SQLite`) | EntityFramework, System.Data.SQLite.EF6 |
| `Data.Dal` | libreria DAL esistente (vedi sotto) | il riferimento all'assembly di DAL |

Nei progetti reali tieni quella che preferisci ed elimina le altre: un progetto `Data.*`, il suo `ProjectReference` nell'Host e il `case` in `Host/StorageRegistration.cs`.

### Agganciare la libreria esistente (punti di aggancio)
I repository ADO.NET non aprono connessioni né creano comandi: usano solo interfacce piccole, che si possono sostituire una per una.
Dopo `AddDatabase` (in `Host/StorageRegistration.cs`) basta registrare la propria implementazione: **vale l'ultima registrazione**.

| Punto di aggancio | A cosa serve | Quando sostituirlo |
|---|---|---|
| `IDbExecutor` | esegue SQL con parametri nominati (`QueryAsync`, `ExecuteAsync`) | la libreria ha già un modo suo di eseguire query: si scrive un adattatore che la richiama |
| `IDbConnectionFactory` | come si ottiene una connessione | la libreria gestisce già connessioni e stringhe di connessione |
| `ISqlDialect` | convenzione dei parametri (`@Nome`, `:Nome`...) | database o libreria con segnaposto diversi |
| `AdoNetOptions.ConfigureCommand` | gancio su ogni comando prima dell'esecuzione (timeout, log delle query, regole della libreria) | serve solo personalizzare, senza sostituire |

Esempio: `services.AddSingleton<IDbExecutor, MiaLibreriaExecutor>();` e i repository ADO.NET usano la libreria senza altre modifiche
(il test `Il_repository_ADO_usa_solo_l_executor_quindi_si_puo_agganciare_una_libreria` ne mostra il principio).
Gli SQL del modello sono scritti con `@Nome`: il dialetto li adatta, quindi non vanno riscritti.

### La libreria DAL (`--data-access dal`)
Le applicazioni esistenti usano la libreria **DAL**: connessione unica (singleton) con locking, metodi che incapsulano quelli standard di ADO.NET,
transazioni esplicite. Il progetto `Data.Dal` la integra senza toccare i repository:

```
repository ADO.NET  →  DalExecutor  →  IDalGateway  →  (DalGateway)  →  libreria DAL
                       serializza        5 chiamate        UNICO FILE DA COMPLETARE
```

- **`DalGateway.cs` è l'unico file da completare**: cinque metodi (`ExecuteReader`, `ExecuteNonQuery`, `BeginTransaction`, `Commit`, `Rollback`) che
  richiamano i metodi reali di DAL. Qui si aggiunge anche il riferimento all'assembly di DAL (in `Data.Dal.csproj`). Finché non è completato,
  `--data-access dal` si ferma con un messaggio chiaro.
- **Connessione singleton:** l'adattatore non apre, chiude né elimina connessioni. `DalLock` esegue **una sola operazione per volta** (attesa asincrona, le
  chiamate a DAL girano fuori dal thread della UI: la finestra non si blocca). Se DAL espone un proprio oggetto di lock si può usare al posto del semaforo.
- **Transazioni esplicite:** `ITransactionRunner.RunAsync(async ct => { ... })` apre la transazione, esegue il lavoro (i repository usati dentro ne fanno parte),
  conferma se riesce e annulla se solleva un'eccezione. Durante la transazione le altre operazioni attendono; se ne viene richiesta una dentro un'altra,
  partecipa a quella esterna. Senza `RunAsync` ogni comando è confermato subito, come in DAL.
- **Schema e migrazioni** usano ancora una connessione propria alla stessa stringa di connessione (`--connection-string`), separata da quella di DAL.
- I test usano una "finta DAL" (connessione unica, transazioni esplicite, rilevamento di accessi sovrapposti): servono da esempio e da prova del comportamento.

### Schema del database e migrazioni
Lo schema si crea da solo all'avvio: gli script `Data/Scripts/<Database>/Vnnn_Nome.sql` (incorporati nell'assembly, uno per SQLite e uno per SQL Server)
non ancora applicati vengono eseguiti in ordine, ciascuno in una transazione; la versione corrente è nella tabella `SchemaVersion`.
Per cambiare lo schema si **aggiunge** uno script (`V002_...`), senza modificare quelli già applicati. Al primo avvio con database nuovo si inseriscono
le attività di esempio. Se il database non è raggiungibile l'app lo segnala e esce con codice 3.

Le colonne usano tipi semplici e uguali nei due database (testo e interi, data in formato ISO 8601): niente conversioni dipendenti dal driver.

### Salvataggio su file JSON (`--storage file`)
- Le attività stanno in `tasks.json` nella cartella dei dati (`--data-folder`, predefinita `%LocalAppData%\<nome app>`).
- Ogni modifica riscrive il file in modo sicuro (file temporaneo + sostituzione); se è danneggiato l'app mostra l'errore con il percorso e **non lo tocca**.
- È pensato per una sola istanza dell'app alla volta.

### Test sul database
I test girano su SQLite in un file temporaneo, con le stesse verifiche per le tre tecnologie. SQL Server **non è provato in CI**: i test
`SqlServerTests` si attivano solo se imposti la variabile d'ambiente `DESKTOPAPPTEMPLATE_SQLSERVER` con la connessione a un database di prova
(le tabelle `Tasks` e `SchemaVersion` vengono create se mancano; i test eliminano solo le righe che inseriscono).

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
  DesktopAppTemplate.Infrastructure  netstandard2.0  Implementazioni concrete (orologio, info app, archivio su file JSON)
  DesktopAppTemplate.Data            netstandard2.0  Accesso al database: connessioni, dialetto, executor ADO.NET, migrazioni
  DesktopAppTemplate.Data.Ado/.Dapper/.EntityFramework   Le tecnologie di accesso (repository delle attività)
  DesktopAppTemplate.Data.Dal        netstandard2.0  Adattatore per la libreria DAL (connessione singleton, transazioni esplicite)
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
5. **Persistenza**: i dati sono già su database (SQLite/SQL Server, con tre tecnologie a scelta) o su file JSON: vedi *Dati e database*.
   Per una nuova slice si aggiunge uno script di migrazione, uno `ITaskRepository`-like derivato da `IRepository<,>` e la sua implementazione.
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
