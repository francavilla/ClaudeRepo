# PasswordGen

Applicazione desktop **WPF per .NET Framework 4.8** che genera password **casuali, sicure e facili da ricordare**,
pensata per chi deve cambiare password ogni mese per policy aziendale.

Tutto avviene sul computer: nessuna connessione di rete, nessun salvataggio delle password.

## Uso

1. Avviare `PasswordGen.exe`: compaiono sei proposte (**F5** o *Genera nuove proposte* per averne altre; il numero, da 1 a 20, si regola nella card *Proposte*).
2. Scegliere il **tipo di password**:

   | Tipo | Esempio | Quando |
   |---|---|---|
   | Parole italiane (consigliata) | `Lampo-Cavallo-Nebbia-Fiume47!` | Password da digitare a mano e da ricordare |
   | Sillabe pronunciabili | `Bamelo-Tirusa-Pevono83=` | Si «leggono» come parole inventate |
   | Caratteri casuali | `k7Q#mP2v!xR4tw9N` | Quando basta copiarla |

3. Impostare le **regole della policy** (predefinite: almeno 10 caratteri, maiuscole, minuscole, numeri e caratteri speciali).
4. Facoltativo: scrivere la **password attuale**. Resta solo in memoria, non viene salvata, e le proposte saranno
   sensibilmente diverse (molte policy vietano una semplice variante della precedente).
5. **Copia** sulla proposta scelta: gli appunti si svuotano dopo 30 secondi o alla chiusura dell'app, e la password
   non entra nella cronologia di Windows (Win+V).
6. Dopo il cambio, premere **Ho cambiato la password**: il banner in alto mostra i giorni alla scadenza.
   Con *Ricordamelo anche all'accesso a Windows* l'app parte con Windows (`PasswordGen.exe /promemoria`)
   e apre la finestra **solo** se la password scade entro 5 giorni o è già scaduta.

## Quanto è sicura

Ogni scelta usa il generatore crittografico del sistema (`RandomNumberGenerator`) con campionamento senza bias.
L'entropia mostrata è il logaritmo del numero di password possibili con le scelte fatte:

| Password | Entropia indicativa |
|---|---|
| 4 parole + separatore, 2 cifre, simbolo | ~ 52 bit (Accettabile) |
| 5 parole | ~ 62 bit (Buona) |
| 6 parole | ~ 72 bit (Buona) |
| 16 caratteri casuali | ~ 98 bit (Ottima) |

Giudizio: sotto 40 bit *Debole*, 40-59 *Accettabile*, 60-79 *Buona*, da 80 *Ottima*.
Per una password che cambia ogni mese e protegge un accesso con blocco dopo i tentativi falliti, 4-5 parole sono adeguate.
Per segreti da conservare a lungo (cifratura di file, password manager) usare almeno 6 parole o caratteri casuali.

La lista delle parole è in `src/PasswordGen.Core/Words/it.txt` (minuscole, senza accenti, 4-9 lettere, una per riga): si può
ampliare, il numero di bit si adegua da solo.

## Impostazioni

`%AppData%\PasswordGen\settings.json`: tipo e lunghezze, regole della policy, durata della password e data dell'ultimo cambio.
Non contiene mai password.

## Struttura

- `src/PasswordGen.Core` — logica senza dipendenze dalla UI: generatori, policy, somiglianza con la password precedente,
  promemoria, impostazioni.
- `src/PasswordGen` — interfaccia WPF (MVVM, tema in `Themes/Theme.xaml`).
- `tests/PasswordGen.Core.Tests` — test xUnit del Core.
- `tools/make_icon.py` — rigenera l'icona (Python + Pillow).

## Compilare

```
dotnet test tests/PasswordGen.Core.Tests/PasswordGen.Core.Tests.csproj -c Release
dotnet publish src/PasswordGen/PasswordGen.csproj -c Release -o publish/PasswordGen
```

Richiede Windows con .NET SDK (per compilare la parte WPF). La versione si imposta in `Directory.Build.props`.
