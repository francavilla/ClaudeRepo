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

Le attività si salvano in un database, con **ADO.NET**: **SQLite** (predefinito, non richiede installare nulla) oppure **SQL Server**.

```
DesktopAppTemplate.exe                                                # SQLite: <nome app>.db in %LocalAppData%\<nome app>
DesktopAppTemplate.exe --storage sqlserver --connection-string "Server=.;Database=Demo;Integrated Security=True"
DesktopAppTemplate.exe --data-access dal                              # usa la libreria DAL (vedi sotto)
```

Con SQL Server preferisci l'autenticazione di Windows (`Integrated Security=True`): una password in App.config è in chiaro.
L'account deve poter creare tabelle al primo avvio, oppure si usa `--no-migrate` (vedi sotto) con lo schema creato da chi amministra il database.

### Come è fatto
```
slice (ITaskRepository)  →  AdoNetTaskRepository  →  IDbExecutor  →  AdoNetExecutor  →  connessione (IDbConnectionFactory)
```
Tutto sta nel progetto `Data`; i repository ADO.NET non aprono connessioni né creano comandi: usano solo `IDbExecutor`.
Gli SQL (`TaskSql`) sono scritti una volta sola con parametri `@Nome` e valgono per SQLite e SQL Server.

### Agganciare la libreria esistente (punti di aggancio)
Si sostituisce una interfaccia alla volta, registrandola dopo `AddDatabase` in `Host/StorageRegistration.cs` (**vale l'ultima registrazione**):

| Punto di aggancio | A cosa serve |
|---|---|
| `IDbExecutor` | esegue SQL con parametri nominati (`QueryAsync`, `ExecuteAsync`) |
| `IDbConnectionFactory` | come si ottiene una connessione |
| `ISqlDialect` | convenzione dei parametri (`@Nome`, `:Nome`...) |
| `AdoNetOptions.ConfigureCommand` | gancio su ogni comando prima dell'esecuzione (timeout, log delle query) |

### La libreria DAL (`--data-access dal`)
DAL ha una connessione unica (singleton) con locking, metodi che incapsulano quelli standard di ADO.NET e transazioni esplicite.
Il progetto `Data.Dal` la integra senza toccare i repository:

```
repository ADO.NET  →  DalExecutor  →  IDalGateway  →  DalGateway  →  libreria DAL
                       serializza        5 chiamate     UNICO FILE DA COMPLETARE
```

- **DAL simulata con nomi fittizi, già funzionante:** `DalGateway.cs` richiama `DalPlaceholder` (file `DalPlaceholder.cs`), che si comporta come DAL: una sola
  connessione per tutta l'app, protetta da blocco, metodi nella forma dei wrapper ADO.NET (`ExecuteReader`, `ExecuteNonQuery`, `BeginTransaction`, `Commit`,
  `Rollback`) e transazioni esplicite. Quindi `--data-access dal` funziona subito, su SQLite e SQL Server, senza la libreria vera.
- **Collegare la DAL vera in 4 passi:** (1) aggiungere in `Data.Dal.csproj` il riferimento all'assembly di DAL; (2) in `DalGateway.cs` sostituire le chiamate
  `_dal.` con quelle reali (es. `Dal.Instance.`); (3) se nomi o parametri di DAL sono diversi, adattare solo quelle righe (e, se DAL non usa `DbParameter`, il metodo
  `CreateParameters`); (4) eliminare `DalPlaceholder.cs` e il campo `_dal`. Serializzazione, transazioni e repository non cambiano.
  Mappa dei nomi fittizi: `ExecuteReader` → wrapper della SELECT con reader; `ExecuteNonQuery` → wrapper di INSERT/UPDATE/DELETE; `BeginTransaction`/`Commit`/`Rollback` →
  le chiamate esplicite di DAL.
- **Connessione singleton:** l'adattatore non apre, chiude né elimina connessioni. `DalLock` esegue **una sola operazione per volta** (attesa asincrona,
  chiamate a DAL fuori dal thread della UI: la finestra non si blocca).
- **Transazioni esplicite:** `ITransactionRunner.RunAsync(async ct => { ... })` apre la transazione, esegue il lavoro (i repository usati dentro ne fanno
  parte), conferma se riesce e annulla se solleva un'eccezione. Durante la transazione le altre operazioni attendono; una transazione richiesta dentro un'altra
  partecipa a quella esterna. Senza `RunAsync` ogni comando è confermato subito, come in DAL.
- Schema e migrazioni usano una connessione propria alla stessa stringa di connessione, separata da quella di DAL.

### Schema già esistente: `--no-migrate`
Normalmente l'app, all'avvio, **crea e aggiorna le tabelle** da sola (vedi sotto). Con `--no-migrate` (o `NoMigrate=true` in App.config) **non tocca lo schema**:
presume che le tabelle esistano già e non inserisce neppure le attività di esempio. Serve quando l'account con cui gira l'app **non ha il permesso di
creare o modificare tabelle** (tipico di SQL Server aziendale con `Integrated Security=True`), perché lo schema lo gestisce chi amministra il database.
All'avvio l'app controlla comunque che la tabella `Tasks` sia accessibile: se manca, mostra un errore chiaro ed esce con codice 3.

Procedura con SQL Server:
1. chi amministra il database esegue, **in ordine**, gli script `Data/Scripts/SqlServer/V001_CreateTasks.sql` e `V002_CreateLog.sql` (e i successivi);
2. l'app si avvia con `--no-migrate` (o con `NoMigrate=true`): serve solo lettura e scrittura sulle tabelle.

Se un giorno si vuole tornare alle migrazioni automatiche su quel database, bisogna prima registrare le versioni già applicate, altrimenti l'app
proverebbe a ricreare le tabelle:
```sql
CREATE TABLE dbo.SchemaVersion (Version INT NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY, Script NVARCHAR(200) NOT NULL, AppliedAt NVARCHAR(40) NOT NULL);
INSERT INTO dbo.SchemaVersion (Version, Script, AppliedAt) VALUES (1, 'V001_CreateTasks.sql', '2026-10-08T00:00:00'), (2, 'V002_CreateLog.sql', '2026-10-08T00:00:00');
```

### Schema del database e migrazioni
Lo schema si crea da solo all'avvio: gli script `Data/Scripts/<Database>/Vnnn_Nome.sql` (incorporati nell'assembly) non ancora applicati vengono eseguiti
in ordine, ciascuno in una transazione; la versione corrente è nella tabella `SchemaVersion`. Per cambiare lo schema si **aggiunge** uno script
(`V002_...`), senza modificare quelli già applicati. Con database nuovo si inseriscono le attività di esempio. Se il database non è raggiungibile l'app
lo segnala ed esce con codice 3. Le colonne usano tipi semplici e uguali nei due database (testo e interi, data in formato ISO 8601).

### Test sul database
I test girano su SQLite in un file temporaneo. SQL Server **non è provato in CI**: i test `SqlServerTests` si attivano solo se imposti la variabile
d'ambiente `DESKTOPAPPTEMPLATE_SQLSERVER` con la connessione a un database di prova (le tabelle `Tasks` e `SchemaVersion` vengono create se mancano;
i test eliminano solo le righe che inseriscono).

## Logging

Si usa l'interfaccia standard di .NET: ovunque basta chiedere un `ILogger<T>` nel costruttore e scrivere `_logger.LogInformation("...")`.
Le destinazioni sono due, attivabili da configurazione (App.config o riga di comando), da sole o insieme:

```
DesktopAppTemplate.exe                                   # predefinito: solo file, livello Information
DesktopAppTemplate.exe --log-targets db                  # solo database (tabella Log)
DesktopAppTemplate.exe --log-targets file,db --log-level debug
DesktopAppTemplate.exe --log-targets none                # nessun log
```

| Opzione (chiave App.config) | Significato | Predefinito |
|---|---|---|
| `--log-targets` (`LogTargets`) | `file`, `db`, `file,db`, `none` | `file` |
| `--log-level` (`LogLevel`) | `trace`, `debug`, `information`, `warning`, `error`, `critical`, `none` | `information` |
| `--log-folder` (`LogFolder`) | cartella dei file di log | sottocartella `logs` della cartella dei dati |
| `--log-retention-days` (`LogRetentionDays`) | giorni di conservazione (file e righe nel database) | `30` |

- **File:** uno al giorno, `app-AAAAMMGG.log`, leggibile da altri programmi mentre l'app gira; i file più vecchi della conservazione si eliminano
  da soli. Formato: `2026-10-08 09:30:15.123 [INF] Categoria - Messaggio`, con l'eccezione completa sotto.
- **Database:** tabella `Log` (data, livello, categoria, messaggio, eccezione, utente, computer), creata dalle migrazioni nello stesso database dei dati.
  Passa da `IDbExecutor`, quindi funziona anche con la libreria DAL. Le righe più vecchie della conservazione si eliminano all'avvio.
- **Il log non rompe né rallenta l'app:** chi registra un messaggio lo mette solo in coda; un thread in background lo scrive a gruppi. Se la coda
  si riempie i messaggi nuovi si scartano; se il database non risponde, i messaggi finiscono nel file di ripiego `app-fallback-AAAAMMGG.log`.
  Alla chiusura la coda viene svuotata.
- **Ordine all'avvio:** il database scrive solo dopo le migrazioni (`IDeferredStart`); i messaggi dell'avvio restano in coda e non si perdono.
- **Cosa viene registrato da solo:** avvio e arresto con la configurazione in uso, risultato delle migrazioni, ogni richiesta del mediator
  (Debug: eseguita, con la durata; Warning: non valida; Error: fallita, con l'eccezione), errori non gestiti delle finestre (WPF e Windows Forms)
  e le eccezioni non gestite del processo.
- Limite: in caso di arresto anomalo del processo i messaggi ancora in coda possono andare persi (quelli già scritti restano).

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
  DesktopAppTemplate.Infrastructure  netstandard2.0  Implementazioni concrete (orologio, info app, impostazioni di archiviazione)
  DesktopAppTemplate.Data            netstandard2.0  Accesso al database con ADO.NET: connessioni, dialetto, executor, migrazioni, repository
  DesktopAppTemplate.Data.Dal        netstandard2.0  Adattatore per la libreria DAL (connessione singleton, transazioni esplicite)
  DesktopAppTemplate.Logging         netstandard2.0  Logging su file e/o database (ILogger<T>, scrittura in background)
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
5. **Persistenza**: i dati sono già su database (SQLite/SQL Server, ADO.NET): vedi *Dati e database*.
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
