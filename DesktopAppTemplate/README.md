# DesktopAppTemplate

Modello di applicazione desktop per .NET Framework 4.6.2+ con **due interfacce intercambiabili**
(WPF e Windows Forms) sopra la **stessa logica**, organizzata a **slice verticali**.

## Avvio

```
DesktopAppTemplate.exe --ui wpf        # interfaccia WPF
DesktopAppTemplate.exe --ui winforms   # interfaccia Windows Forms
```

Senza argomento vale `Ui` in `App.config` (predefinito `wpf`). Da Visual Studio: impostare gli
*Application arguments* nelle proprietà di debug del progetto `DesktopAppTemplate.Host`.

## Struttura

```
src/
  DesktopAppTemplate.Core            netstandard2.0  Mediator, validazione, MVVM, astrazioni (nessuna UI)
  DesktopAppTemplate.Features        netstandard2.0  Slice: richieste, handler, validatori, view model
  DesktopAppTemplate.Infrastructure  netstandard2.0  Implementazioni concrete (orologio, archivio, info app)
  DesktopAppTemplate.UI.Wpf          net462          Solo view XAML + tema
  DesktopAppTemplate.UI.WinForms     net462          Solo view Windows Forms
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
