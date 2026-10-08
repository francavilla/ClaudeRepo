# Changelog

Tutte le modifiche rilevanti di DesktopAppTemplate sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

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
