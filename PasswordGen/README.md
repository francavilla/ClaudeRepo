# PasswordGen

Applicazione desktop **WPF per .NET Framework 4.8** che genera password **casuali, sicure e facili da ricordare**,
pensata per chi deve cambiare password ogni mese per policy aziendale.

Tutto avviene sul computer: nessuna connessione di rete. Le password non vengono salvate, salvo quelle che scegli tu di conservare nello **storico** (facoltativo, cifrato: vedi sotto).

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
6. Dopo il cambio, premere **Ho cambiato la password**: l'app chiede **quale proposta** hai usato (è preselezionata
   l'ultima copiata) e la registra nello **storico** con numero progressivo e data. Il banner in alto mostra i giorni alla scadenza.
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

## Lista di parole personale

Nella card *Parole della passphrase* (visibile con il tipo «Parole italiane») si può caricare un file di testo con le parole da usare:
una parola per riga, `#` per i commenti. L'app le porta in minuscolo, toglie gli accenti e scarta duplicati, parole con
caratteri diversi da a-z e parole più corte di 4 o più lunghe di 9 lettere, indicando quante ne ha scartate.

| Modalità | Requisito | Quando |
|---|---|---|
| Solo la lista integrata | - | Predefinita (1074 parole, ~10,1 bit per parola) |
| Aggiungi il mio file alla lista integrata | almeno 1 parola valida | Pochi termini in più, senza perdere sicurezza |
| Solo il mio file | almeno **300** parole valide | Altra lingua o vocabolario tutto tuo |

L'entropia mostrata tiene conto della lista in uso: con 400 parole ogni parola vale ~8,6 bit invece di ~10,1, quindi servono
più parole per la stessa sicurezza (sotto le 1000 parole compare un avviso). Evita nomi di familiari, date e soprannomi:
sono facili da indovinare per chi ti conosce. Se il file manca o non è valido, l'app usa la lista integrata e lo segnala.

## Blocco dell'app

Nella card *Blocco dell'app* si può richiedere di sbloccare l'app all'avvio e dopo il tempo scelto in secondo piano. Attivandolo si sceglie come sbloccare:
- **Windows Hello** (PIN, impronta o volto): deve essere configurato (Impostazioni, Account, Opzioni di accesso). L'app non riceve mai PIN o impronta, solo l'esito.
- **PIN dell'app** (4-12 cifre) o **password dell'app** (6-64 caratteri): anche su PC senza Windows Hello. Se si imposta un PIN/password, si può sbloccare anche con Windows Hello (se c'è).
  Si conserva solo un hash (PBKDF2-SHA256) in `%AppData%\PasswordGen\lock.dat`, cifrato con DPAPI. Dopo 5 errori scatta un'**attesa crescente**
  (30 secondi, 1, 2, 4 minuti... fino a un'ora), che resta anche se si chiude e riapre l'app.
- Per disattivare il blocco, cambiare o togliere il PIN/password si conferma l'identità (Windows Hello, o il PIN/password se Hello non c'è).
- Con il blocco attivo la finestra non compare negli screenshot né nelle condivisioni dello schermo. Se né Windows Hello né un PIN/password sono utilizzabili, il blocco si disattiva da solo con un avviso.

## Storico delle password

Facoltativo (attivo di default, si disattiva nella card *Storico delle password*). Conserva le **ultime 12** password usate,
ciascuna con numero progressivo (#1, #2, ... mai riutilizzato), data e tipo. Nella scheda *Storico* le password sono mascherate:
si possono mostrare, copiare (con la stessa cancellazione automatica dagli appunti) o eliminare. Le nuove proposte evitano
le varianti di quelle dello storico.

- Il file `%AppData%\PasswordGen\history.dat` è cifrato con **DPAPI** (ambito utente): si apre solo con lo stesso account Windows
  sullo stesso computer. Non protegge da un programma malevolo che gira con il tuo account.
- Disattivando lo storico, o con *Cancella tutto lo storico*, il file viene eliminato.
- Se nella scelta indichi «Nessuna», viene registrata solo la data (senza password).

## Sincronizzazione e backup

Per avere lo stesso storico sul PC e sul telefono (storico, data dell'ultimo cambio e durata della password) le due app condividono un
**file cifrato** `PasswordGen-sync.pgx`: ognuna lo legge, unisce il contenuto con i propri dati (senza cancellare nulla di locale) e lo
riscrive se c'è qualcosa di nuovo. Succede all'avvio, dopo ogni «Ho cambiato la password» e con «Sincronizza ora».

- Il file è cifrato con una **frase segreta** scelta da te (almeno 8 caratteri, AES-256 con controllo di integrità, chiave PBKDF2-SHA256).
  Non si può recuperare: senza la frase il file non si apre. Su ogni dispositivo la frase è conservata cifrata (DPAPI su Windows, Keystore su Android).
- **Windows**: «Imposta...» e scegli un file, per esempio nella cartella di Google Drive per desktop.
- **Android**: «Imposta...» e scegli *Il mio Google Drive* (accesso con l'account Google; l'app crea il file nel tuo Drive e vede solo i file che ha creato),
  oppure un file esistente o nuovo scelto con il selettore di documenti.
- Per cominciare conviene attivare la sincronizzazione **prima sul telefono con Google Drive**: il file compare poi nella cartella di Drive sul PC.
- **Esporta / Importa** usano lo stesso formato cifrato, una volta sola: backup o cambio telefono.
- Un cambio che elimini su un dispositivo può ricomparire dopo la sincronizzazione (l'unione non cancella mai nulla).

## Impostazioni

`%AppData%\PasswordGen\settings.json`: tipo e lunghezze, regole della policy, durata della password e data dell'ultimo cambio.
Non contiene mai password: quelle dello storico stanno nel file cifrato `history.dat`.

## App Android (in sviluppo)

Nella cartella `src/PasswordGen.Mobile` c'è l'app Android (.NET MAUI), che riusa lo stesso Core.
- **Tappa 1**: generazione delle tre tipologie di password, policy, numero di proposte, password attuale facoltativa e copia sicura
  negli appunti (segnalata come sensibile, cancellata dopo 30 secondi).
- **Tappa 2**: scheda *Storico* (ultime 12 password, cifrate con una chiave del Keystore di Android) e promemoria con notifica
  giornaliera quando la password sta per scadere.
- **PIN o password dell'app** (versione 1.9.0): alla prima attivazione del blocco si sceglie impronta o PIN del telefono, un PIN (4-12 cifre) o una
  password dell'app; dopo 5 errori scatta un'attesa crescente (30 secondi, 1, 2, 4 minuti... fino a un'ora). Si conserva solo un hash.
- **Blocco dell'app**: impronta, volto o PIN all'avvio e dopo un po' in secondo piano; blocca anche screenshot e anteprima delle app recenti.
- **Tappa 3**: file di parole personale (card *Parole della passphrase*): carichi un file di testo, che viene copiato nella
  memoria privata dell'app, con le stesse modalità e gli stessi limiti dell'app Windows (almeno 300 parole per «solo il mio file»).

- L'APK è allegata a ogni release (`PasswordGen-Android-vX.Y.Z.apk`, nella release `passwordgen-android-vX.Y.Z`); le build di prova si scaricano anche dall'artifact del workflow *CI PasswordGen Android*. Per installarlo sul telefono servono le «origini sconosciute».
- Senza un keystore fisso l'APK è firmato con una chiave di debug diversa a ogni build: per **aggiornare** l'app senza disinstallarla
  (e perdere le impostazioni) servirà una chiave fissa, da fornire come secret del repository.
- L'APK di Release contiene solo l'architettura ARM a 64 bit (circa 36 MB); non si installa su telefoni solo a 32 bit.
- Non c'è nella `PasswordGen.sln` perché richiede il workload Android: si compila con `dotnet publish` (vedi il workflow).
- Permessi Android: notifiche e riavvio del telefono (promemoria), impronta/PIN (blocco dell'app); rete solo per l'accesso facoltativo a Google Drive.
  Il backup di Android è disattivato: impostazioni e storico non lasciano il telefono.

## Struttura

- `src/PasswordGen.Core` — logica senza dipendenze dalla UI: generatori, policy, somiglianza con la password precedente,
  promemoria, impostazioni.
- `src/PasswordGen` — interfaccia WPF (MVVM, tema in `Themes/Theme.xaml`).
- `src/PasswordGen.Mobile` — app Android (.NET MAUI).
- `tests/PasswordGen.Core.Tests` — test xUnit del Core.
- `tools/make_icon.py` — rigenera l'icona (Python + Pillow).

## Compilare

```
dotnet test tests/PasswordGen.Core.Tests/PasswordGen.Core.Tests.csproj -c Release
dotnet publish src/PasswordGen/PasswordGen.csproj -c Release -o publish/PasswordGen
```

Richiede Windows con .NET SDK (per compilare la parte WPF). La versione si imposta in `Directory.Build.props`.
