# Changelog

Tutte le modifiche rilevanti di DesktopAppTemplate sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

## [3.1.0] - 2026-10-08

### Aggiunto
- Adattatore per la libreria DAL (`DesktopAppTemplate.Data.Dal`, `--data-access dal`): `DalExecutor` (implementa `IDbExecutor`) serializza l'uso della
  connessione singleton con `DalLock` (attesa asincrona, chiamate fuori dal thread della UI); `DalTransactionRunner` gestisce le transazioni esplicite
  (conferma/annulla, le operazioni interne ne fanno parte, le transazioni annidate partecipano a quella esterna).
- `ITransactionRunner` in `Core.Data`: contratto per eseguire un gruppo di operazioni in una transazione.
- `DalGateway`: unico file da completare con le chiamate reali di DAL (cinque metodi); `IDalGateway` ne definisce il contratto.
- Test con una "finta DAL" a connessione unica: contratto comune dei repository, serializzazione, parametri, transazioni (conferma, annullamento,
  attesa delle altre operazioni, annidamento) e segnalazione di DAL non collegata.

## [3.0.1] - 2026-10-08

### Corretto
- Il progetto `Data.EntityFramework` non compilava: mancava il riferimento diretto al driver `System.Data.SQLite.Core`.

## [3.0.0] - 2026-10-08

### Aggiunto
- Accesso al database SQLite e SQL Server con tre tecnologie selezionabili (`--data-access ado|dapper|ef`): ADO.NET (predefinita), Dapper ed Entity Framework 6,
  in progetti separati (`Data.Ado`, `Data.Dapper`, `Data.EntityFramework`) con tabelle e SQL comuni.
- Punti di aggancio per una libreria esistente: `IDbExecutor`, `IDbConnectionFactory`, `ISqlDialect` e `AdoNetOptions.ConfigureCommand`;
  i repository ADO.NET usano solo `IDbExecutor`. Vale l'ultima registrazione nel contenitore.
- Progetto `DesktopAppTemplate.Data`: fabbrica di connessioni, dialetto SQL, executor ADO.NET, migrazioni dello schema con script `Vnnn_Nome.sql`
  per database (tabella `SchemaVersion`, una transazione per script), riga `TaskRow` e SQL delle attività condivisi.
- Parametri `--storage sqlite|sqlserver|file`, `--data-access ado|dapper|ef`, `--connection-string` (chiavi `Storage`, `DataAccess`, `ConnectionString`),
  con controllo incrociato (SQL Server richiede la stringa di connessione).
- `IRepository<TEntity, TId>` generico: `ITaskRepository` ne deriva.
- Preparazione dell'archivio all'avvio (migrazioni e attività di esempio solo a database nuovo); errore chiaro con codice di uscita 3 se il database non è raggiungibile.
- Test: contratto comune ai tre repository su SQLite, scambio di dati tra le tecnologie, migrazioni, executor e punti di aggancio, impostazioni;
  test su SQL Server attivabili con la variabile `DESKTOPAPPTEMPLATE_SQLSERVER` (non eseguiti in CI).

### Modificato
- **Incompatibile:** l'archivio predefinito è SQLite (file `<nome app>.db` nella cartella dei dati) e non più il file JSON; per il file JSON usare `--storage file`.
- **Incompatibile:** `AddInfrastructure()` non registra più `ITaskRepository`: l'archivio si sceglie in `Host/StorageRegistration.cs` (`AddFileStorage()` o `AddDatabase` + una tecnologia).
- `StorageSettings` comprende archiviazione, tecnologia di accesso e stringa di connessione.

## [2.0.1] - 2026-10-08

### Corretto
- `TaskListViewModel` non riceveva `TaskSettings` (costruttore non aggiornato): la compilazione dei test falliva e la modalità `--read-only` non avrebbe funzionato.
- I glifi delle icone nei sorgenti sono scritti come sequenze `\uXXXX` invece che come caratteri speciali invisibili.

## [2.0.0] - 2026-10-08

### Aggiunto
- Configurazione unificata: ogni parametro si dichiara una sola volta (`OptionDefinition`) e vale in `App.config` e da riga di comando
  (`--data-folder` ricavato da `DataFolder`), con priorità predefinito < App.config < riga di comando, validazione e origine di ogni valore.
- Testo di `--help` generato dalle opzioni dichiarate; il catalogo delle opzioni sta in `Host/AppOptions.cs`.
- Contesto applicativo `IAppContext` (configurazione, informazioni, ambiente, stato di sessione `ISessionState` con notifica dei cambiamenti).
- Classi `UiSettings`, `StorageSettings`, `TaskSettings` che leggono la configurazione per ciascun modulo.
- Opzione dimostrativa `--read-only` / chiave `ReadOnly`: le attività si vedono ma non si modificano (comandi e casella disabilitati in entrambe le UI).
- La pagina Informazioni mostra utente, computer e la configurazione in uso con l'origine di ogni valore.

### Modificato
- **Incompatibile:** un argomento sconosciuto o un valore non valido (es. `--ui boh`) ora è un errore (codice di uscita 2, messaggio chiaro) invece di essere ignorato.
- **Incompatibile:** `AddCore()` diventa `AddMediator()` + `AddAppContext(configuration)`; `AddInfrastructure()` non riceve più la cartella dati
  (la legge dalla configurazione); `UiSelector` e `CommandLine` sono sostituiti da `UiSettings` e dal nuovo parser.
- `TaskListViewModel` riceve `TaskSettings`; `AboutViewModel` riceve `IAppContext`.

## [1.3.0] - 2026-10-08

### Aggiunto
- Persistenza su file: le attività sono salvate in `tasks.json` (JSON) in `%LocalAppData%\<nome app>` e ricaricate all'avvio.
  Cartella configurabile con la chiave `DataFolder` di `App.config`.
- `JsonFileTaskRepository`: scrittura sicura (file temporaneo + sostituzione), accesso thread-safe, errore chiaro
  (senza modificare il file) se il contenuto non è valido, attività di esempio solo al primo avvio.
- Test su salvataggio, ricarica, concorrenza, file non valido e cartella dati.

### Modificato
- `AddInfrastructure` accetta la cartella dei dati; l'archivio in memoria resta per i test.

## [1.2.0] - 2026-10-08

### Aggiunto
- Opzioni da riga di comando `--help` (`-h`, `/?`) e `--version` (`-v`): mostrano aiuto e versione ed escono senza aprire l'interfaccia
  (nel prompt se disponibile, altrimenti in una finestra di messaggio).
- Icona dell'applicazione (`assets/app.ico`, rigenerabile con `tools/make_icon.py`) per l'`.exe` e per le finestre WPF e Windows Forms.
- Numero di versione nel titolo della finestra (es. "DesktopAppTemplate v1.2.0") in entrambe le interfacce.

## [1.1.0] - 2026-10-08

### Aggiunto
- `New-Project.ps1`: crea un nuovo progetto dal template (rinomina namespace, assembly, file e cartelle, nuovi GUID).
- Sezione "Adattarlo a un nuovo progetto" nel README.
- La CI verifica che il progetto generato da `New-Project.ps1` compili.

### Modificato
- Le view Windows Forms (`MainForm`, `TaskListView`, `AboutView`) usano il designer di Visual Studio
  (classi `partial` con `.Designer.cs`, costruttore senza parametri e metodo `Bind`), invece di essere costruite a mano.
- I componenti grafici condivisi (`CardPanel`, `AccentButton`, `NavButton`) sono pubblici e utilizzabili dalla casella degli strumenti.

## [1.0.1] - 2026-10-08

### Corretto
- `App.config` dell'Host non era valido (un commento XML conteneva `--`) e impediva la compilazione dell'eseguibile.

## [1.0.0] - 2026-10-08

### Aggiunto
- Solution `DesktopAppTemplate` con architettura a slice verticali: `Core`, `Features`, `Infrastructure`,
  due interfacce (`UI.WinForms`, `UI.Wpf`) e `Host` (composition root).
- Scelta dell'interfaccia all'avvio: argomento `--ui wpf|winforms` oppure impostazione `Ui` in `App.config`.
- Mediator minimale con validazione, registrazione automatica di handler e validatori, base MVVM (`ObservableObject`, comandi sincroni e asincroni).
- Slice di esempio "Attività" (aggiungi, elenca con filtro, completa, elimina) e pagina "Informazioni".
- Layout moderno in entrambe le UI: menu laterale scuro, contenuto a card, stessa tavolozza di colori.
- Test xUnit su mediator, slice, view model e selezione dell'interfaccia.
