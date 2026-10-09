# Changelog

Tutte le modifiche rilevanti di PasswordGen sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

## [2.0.0] - 2026-10-09

### Modificato
- **Nuova versione maggiore, uguale per l'app Windows e per quella Android.** Il cambiamento di comportamento che la giustifica è la regola aziendale delle ultime 20 password
  (descritta nella 1.7.0): lo storico è sempre attivo, non si può disattivare né cancellare voce per voce, e la nuova password non può essere identica a una delle ultime 20.
- Raccoglie anche le modifiche delle versioni 1.5.1 – 1.7.0 qui sotto (schede «Genera» e «Impostazioni», tipo di password con riquadri su Android, pulsante del blocco chiaro).
  Quelle versioni non sono state pubblicate come release.
- Il codice di versione Android passa a 20000000 + n (con n = 999 nelle release): continua a crescere, quindi l'aggiornamento dalle versioni precedenti è accettato.

## [1.7.0] - 2026-10-09

### Aggiunto
- **Regola aziendale delle ultime 20 password.** La nuova password non può essere identica (maiuscole e minuscole contano) a una delle ultime 20 usate: «Ho cambiato la password» rifiuta una proposta già presente nello storico.
- **Verifica una password** (scheda «Genera», Windows e Android): scrivi una password e l'app dice se rispetta le regole della policy e se è già tra le ultime 20. Resta solo in memoria.
- **Azzera storico** con avviso: l'unico modo per svuotare lo storico, pensato per chi cambia azienda o account.

### Modificato
- Lo storico conserva le **ultime 20** password (prima 12) ed è **sempre attivo**: tolti l'interruttore «Conserva le password scelte», «Cancella tutto lo storico» e l'eliminazione della singola voce, che avrebbero reso inaffidabile il controllo.
  Lo storico già salvato resta valido. La sincronizzazione non richiede più di attivare lo storico.

## [1.6.2] - 2026-10-09

### Corretto
- **Android: il pulsante del blocco in «Impostazioni» era ancora troncato («Imposta un PIN o una»).** L'etichetta è ora più corta: «Imposta PIN o password» oppure, se già impostato, «Cambia PIN o password» (stesso testo su Windows).

## [1.6.1] - 2026-10-09

### Corretto
- **Android: le voci del tipo di password mostravano «Microsoft.Maui.Controls.VerticalStackLayout» al posto del testo.** I pulsanti di opzione di MAUI non gestiscono un contenuto composto;
  le tre voci sono ora riquadri da toccare, con titolo ed esempio, il cerchio pieno (●) e lo sfondo evidenziato sulla voce scelta.

## [1.6.0] - 2026-10-09

### Modificato
- **Le impostazioni sono in una scheda a parte, «Impostazioni».** La scheda «Genera» si occupa solo di generare e mostrare le password:
  - Windows: «Genera» contiene il tipo di password (con gli esempi), la password attuale facoltativa, le proposte e la cronologia; in «Impostazioni» ci sono lunghezza,
    numero di proposte, parole, regole della policy, blocco dell'app, promemoria, storico e sincronizzazione.
  - Android: nasce la terza scheda «Impostazioni» (accanto a «Genera» e alla cronologia) con le stesse sezioni.
- Gli avvisi (promemoria, attiva il blocco, scelta della proposta usata) restano visibili in alto da qualsiasi scheda.

## [1.5.3] - 2026-10-09

### Modificato
- **Pulsante del blocco chiaro e non più troncato.** Su Android l'etichetta «Imposta o cambia PIN/password» era tagliata a «Imposta o» dallo spazio ristretto.
  Ora il pulsante occupa tutta la larghezza e dice «Imposta un PIN o una password dell'app» oppure, se già impostato, «Cambia PIN o password dell'app»
  (stesso testo anche nell'app Windows).

## [1.5.2] - 2026-10-09

### Modificato
- **Android: la scelta del tipo di password è ora evidente.** Al posto del menu a tendina (che sembrava una semplice riga di testo) ci sono tre voci con il pulsante di opzione,
  ciascuna con un esempio: «Parole italiane (consigliata)», «Sillabe pronunciabili» e «Caratteri casuali», come nell'app Windows. La voce scelta ha il cerchio pieno.

## [1.5.1] - 2026-10-09

### Corretto
- Le note della release Windows e il README dicevano «nessuna connessione di rete»: non è più vero se si attiva la sincronizzazione su Google Drive. Ora dicono che la rete
  serve solo per quella funzione facoltativa. Nessuna modifica al programma.

## [1.5.0] - 2026-10-09

### Aggiunto
- **App Windows: sincronizzazione direttamente su Google Drive**, come su Android. In «Sincronizzazione e backup» → «Imposta...» → «Il mio Google Drive» si apre il browser,
  si accede con l'account Google e l'app usa il file cifrato `PasswordGen-sync.pgx` nel Drive (ambito `drive.file`: vede solo i file creati dall'app). L'app non vede mai
  la password di Google; il token di rinnovo è cifrato con DPAPI (`%AppData%\PasswordGen\google.key`). Il ritorno dal browser arriva a un piccolo server locale su 127.0.0.1
  (porta libera, una sola richiesta). Se l'accesso scade o viene revocato, «Sincronizza ora» lo rifà.
- **Richiede un client OAuth «App desktop»** di Google Cloud: l'ID client è nel progetto (`GoogleDesktopClientId`), la chiave arriva in compilazione dal secret
  `GOOGLE_DESKTOP_CLIENT_SECRET` della CI. Senza, l'opzione dice che Google Drive non è configurato e resta la sincronizzazione con un file.
- `PasswordGen.Core`: `LoopbackReceiver`, interfaccia `IGoogleDriveService` condivisa tra le due app, chiave del client e indirizzo di ritorno variabile in `GoogleOAuthClient` (con test).
- Test del ViewModel Windows sulla sincronizzazione su Drive (accesso, file nuovo ed esistente, due PC, accesso scaduto, disattivazione).

### Modificato
- «Imposta...» chiede prima dove sincronizzare (Google Drive o un file).
- Gli errori di Google Drive riportano anche il motivo scritto da Google (per esempio API non abilitata o ambito non concesso).

## [1.4.0] - 2026-10-08

### Aggiunto
- **App Windows: PIN o password dell'app**, come su Android. Attivando il blocco si sceglie tra Windows Hello, un PIN (4-12 cifre) e una password dell'app (6-64 caratteri).
  La schermata di blocco ha la casella del PIN/password e il pulsante «Sblocca con Windows Hello»; dopo 5 errori scatta un'**attesa crescente**
  (30 secondi, 1, 2, 4 minuti... fino a un'ora) che sopravvive alla chiusura dell'app e ha il conto alla rovescia. Del PIN/password si conserva solo un hash
  (PBKDF2-SHA256, 150.000 iterazioni) in `%AppData%\PasswordGen\lock.dat`, cifrato con DPAPI.
- Il blocco ora si può usare **anche sui PC senza Windows Hello**, con un PIN o una password dell'app.
- Pulsanti «Imposta o cambia PIN/password» e «Rimuovi PIN/password» nella card del blocco; disattivare il blocco, cambiare o togliere il PIN chiede di
  confermare l'identità (con Windows Hello, o con il PIN/password se Hello non c'è).
- Test del ViewModel Windows sul blocco: attivazione con Hello, PIN o password, sblocco, attesa crescente, disattivazione, cambio e rimozione.

### Corretto
- Windows: una finestra di dialogo dell'app (scelta di un file, PIN, conferme) non fa più scattare il blocco quando il tempo scelto è «Subito».

### Modificato
- Build più pulita: tolti gli ultimi avvisi del compilatore Android (`OpenableColumns` obsoleta, `CreateConfirmDeviceCredentialIntent`) e dell'azione `upload-artifact` (Node.js 24).
- Le due app hanno ora le **stesse funzioni**: restano solo le differenze della piattaforma (impronta e Google Drive diretto su Android; Windows Hello e avvio con Windows su Windows).
- Card «Blocco dell'app» e schermata di blocco di Windows riscritte per le tre modalità di sblocco.

## [1.3.2] - 2026-10-08

### Modificato
- **Android:** le chiamate alla finestra di impronta/volto (`BiometricPrompt`, disponibile da Android 9) sono ora dichiarate come tali nel codice: spariscono gli avvisi
  del compilatore (CA1416) dalla build. Il comportamento non cambia: sotto Android 9 l'app propone direttamente il PIN.
- **Workflow di GitHub:** `checkout`, `setup-dotnet` e `setup-java` passano alla versione 5, che usa Node.js 24 (spariscono gli avvisi «Node.js 20 is deprecated»).

## [1.3.1] - 2026-10-08

### Corretto
- **Android: l'aggiornamento a volte veniva rifiutato** e bisognava disinstallare e reinstallare. Le build di prova della CI avevano come codice di versione
  il numero del run (piccolo), le release un numero molto più grande: installare una build di prova sopra una release sembrava un «downgrade» ad Android.
  Ora il codice è `versione*1000 + n` (999 nelle release, il numero del run nelle prove): le release sono sempre più alte delle prove della stessa versione
  e ogni versione nuova supera tutte le precedenti. Vale per le build da questa versione in poi: **la prima volta** sopra la 1.3.0 si installa normalmente
  (il nuovo codice è molto più alto del vecchio).

## [1.3.0] - 2026-10-08

### Aggiunto
- **App Android: sincronizzazione direttamente su Google Drive.** In «Sincronizzazione e backup» → «Imposta...» → «Il mio Google Drive» si accede con l'account Google
  (OAuth 2.0 con PKCE nel browser del telefono: l'app non vede mai la password) e l'app crea nel Drive il file cifrato `PasswordGen-sync.pgx`.
  Il file resta cifrato con la frase segreta; l'accesso è limitato ai file creati dall'app (ambito `drive.file`). Il token di rinnovo è nel Keystore.
  Sul PC lo stesso file compare nella cartella di Google Drive per desktop e si sceglie con «Imposta...» come un normale file.
- `PasswordGen.Core`: `GoogleOAuthClient`, `GoogleAccessTokenProvider`, `GoogleDriveStorage` (con test su un Google finto).

### Modificato
- **APK molto più piccola (da ~64 a ~36 MB):** in Release solo l'architettura ARM a 64 bit. Gli assembly venivano copiati una volta per architettura
  (~25 MB ciascuna); i telefoni solo a 32 bit non sono più supportati, i modelli recenti e tutti quelli con impronta e Android 9+ sono a 64 bit.
- L'app Android ha ora il permesso di usare la rete, **solo** per l'accesso facoltativo a Google Drive: senza attivarlo non invia né riceve nulla.
- Le note delle release Android dicono che la rete serve a Google Drive.

## [1.2.0] - 2026-10-08

### Modificato
- **APK più piccola:** in Release solo le architetture ARM a 64 e 32 bit (niente x86/x64 degli emulatori).
- Sostituiti i metodi obsoleti `DisplayAlert` e `DisplayActionSheet` con `DisplayAlertAsync` e `DisplayActionSheetAsync`.
- Aprire il selettore di file non fa più scattare il blocco dell'app Android.
- **Release separate per Windows e Android:** due workflow (`Release PasswordGen Desktop` e `Release PasswordGen Android`), due release con tag
  `passwordgen-desktop-vX.Y.Z` e `passwordgen-android-vX.Y.Z`, titoli e file distinti (`PasswordGen-Desktop-v….zip`, `PasswordGen-Android-v….apk`).
  Si può pubblicare una sola delle due. Le release già esistenti (`passwordgen-vX.Y.Z`, zip e APK insieme) restano com'erano.

### Aggiunto
- **Sincronizzazione tra app Windows e Android** con un file cifrato condiviso (per esempio in Google Drive): storico, data dell'ultimo cambio e durata
  della password. Il file è cifrato con una frase segreta (AES-256 + HMAC, chiave PBKDF2-SHA256); l'unione non cancella mai nulla di locale.
- **Esporta / importa** lo storico in un file cifrato con frase segreta, utile anche per cambiare telefono.
- **App Windows:** card «Sincronizzazione e backup» (Imposta, Sincronizza ora, Disattiva, Esporta, Importa); sincronizza da sola all'avvio e dopo ogni cambio
  registrato. La frase segreta si salva cifrata con DPAPI.
- **App Android:** stessa card «Sincronizzazione e backup»; il file si sceglie o si crea con il selettore di documenti di Android (anche in Google Drive),
  l'accesso resta valido dopo la chiusura dell'app. La frase segreta si salva cifrata con la chiave del Keystore.
- **Avviso per attivare il blocco** (Windows Hello / impronta o PIN), che si può chiudere con «Non ora».
- Test del ViewModel dell'app Windows (`PasswordGen.App.Tests`): cambio password e storico, avviso del blocco, sincronizzazione, esportazione e importazione.
- `PasswordGen.Core`: `PassphraseProtector`, `ExchangeFile`, `SyncEngine`, `FileSyncStorage`, `SyncPassphraseStore`, `PasswordHistory.Merge` (con test).

## [1.0.0] - 2026-10-08

### Modificato
- **Nuova numerazione:** da `main` PasswordGen riparte dalla versione **1.0.0**, equivalente alla 1.9.1 (stesso codice). Le versioni dalla 1.0.0 alla 1.9.1
  riportate qui sotto sono la cronologia interna dello sviluppo precedente; la release `passwordgen-v1.9.1` resta pubblicata ma congelata.
- Il codice di versione Android delle release ora parte da 110000 (`100000 + major*10000 + minor*100 + patch`), così gli APK 1.x nuovi
  si installano sopra quelli delle vecchie release (che arrivavano a 10701) senza essere scambiati per un downgrade.

## [1.9.1] - 2026-10-08

### Corretto
- La build Android (netstandard2.0) della 1.9.0 non compilava: `Rfc2898DeriveBytes` con SHA-256 non esiste in quel target. L'hash del PIN/password
  usa ora un PBKDF2-HMAC-SHA256 implementato con `HMACSHA256` (stesso algoritmo e stesso risultato su tutte le piattaforme).

## [1.9.0] - 2026-10-08

### Aggiunto
- **App Android: PIN o password dell'app.** Alla prima attivazione del blocco si sceglie come sbloccare: impronta o PIN del telefono,
  PIN dell'app (4-12 cifre) oppure password dell'app (6-64 caratteri), da ripetere due volte. Poi si può sbloccare in tre modi:
  impronta o volto, PIN/password dell'app, PIN del telefono.
  - Il PIN o la password non sono salvati: resta solo un hash (PBKDF2-SHA256, sale casuale, 150.000 iterazioni) in un file cifrato con la
    chiave del Keystore.
  - **Attesa crescente**: dopo 5 errori consecutivi l'app aspetta 30 secondi, poi 1, 2, 4 minuti... fino a un'ora. L'attesa e il conteggio
    sopravvivono alla chiusura dell'app e non si azzerano riaprendola; sulla schermata di blocco il conto alla rovescia scende ogni secondo.
  - Pulsanti «Imposta o cambia PIN/password» e «Rimuovi PIN/password» nella card del blocco; disattivare il blocco, cambiare o togliere
    il PIN chiede di confermare l'identità.
  - Il blocco si può attivare anche su telefoni senza blocco schermo, purché si scelga un PIN o una password dell'app.
- `PasswordGen.Core`: `LockCredential`, `LockCredentialStore` e `LockCredentialManager` (con test): hash, regole, attesa crescente, archivio cifrato.

## [1.8.1] - 2026-10-08

### Corretto
- **App Android: sblocco con PIN, sequenza o password.** Su alcuni telefoni la finestra dell'impronta non offriva alternative e non c'era modo
  di sbloccare con il PIN. Ora la schermata di blocco ha due pulsanti, «Sblocca con impronta» e «Usa PIN o password» (apre la schermata
  del telefono), la finestra dell'impronta ha il suo pulsante «Usa PIN o password» e, se l'impronta non è utilizzabile (non registrata,
  sensore occupato, troppi tentativi), l'app passa da sola al PIN.
- Con il tempo di blocco «Subito», la schermata del PIN non fa più bloccare di nuovo l'app appena si rientra.

## [1.8.0] - 2026-10-08

### Aggiunto
- **App Windows: blocco con Windows Hello** (PIN, impronta o volto). Con il blocco attivo una schermata copre l'app all'avvio e dopo il
  tempo scelto in secondo piano (subito, 30 secondi, 1 minuto, 5 minuti) finché non ti autentichi. Card «Blocco dell'app» con
  casella di attivazione, tempo e pulsante «Blocca adesso».
  - Attivare o disattivare il blocco richiede Windows Hello: chi trova il PC sbloccato non può toglierlo.
  - La finestra è esclusa da screenshot, registrazioni e condivisione dello schermo mentre il blocco è attivo (Windows 10 2004 o successivo).
  - Se Windows Hello non è più configurato all'avvio, il blocco si disattiva da solo con un avviso, così non resti chiuso fuori.
  - Quando l'app si blocca, la password attuale viene svuotata e quelle dello storico tornano mascherate.
  - L'app non vede mai PIN, impronta o volto: riceve solo l'esito da Windows.

### Corretto
- La casella «Conserva le password scelte» (storico) e «Ricordamelo all'accesso a Windows» non tornavano allo stato precedente quando
  l'operazione veniva annullata o falliva (WPF ignorava la notifica sincrona).

## [1.7.1] - 2026-10-08

### Aggiunto
- App Android, blocco dell'app: pulsante **«Blocca adesso»** (si può provare subito, senza aspettare il tempo in secondo piano).
- Quando l'autenticazione non riesce, l'app mostra il **motivo** (messaggio del sistema Android) sulla schermata di blocco e nella barra di stato.
- Dopo l'attivazione, la barra di stato spiega quando il blocco scatterà.

### Corretto
- La schermata di blocco all'avvio veniva mostrata una volta sola e un eventuale errore era ignorato in silenzio: ora si riprova più volte
  finché la finestra principale è pronta.

## [1.7.0] - 2026-10-08

### Aggiunto
- **App Android: blocco con impronta, volto o PIN.** Con il blocco attivo l'app chiede l'autenticazione del telefono all'avvio e dopo
  il tempo scelto in secondo piano (subito, 30 secondi, 1 minuto, 5 minuti); finché non ti autentichi una schermata copre tutto.
  L'autenticazione è quella di sistema (`BiometricPrompt`): l'app non vede mai impronta o PIN, riceve solo l'esito.
  - Attivare o disattivare il blocco richiede di autenticarsi: chi trova il telefono sbloccato non può toglierlo.
  - Con il blocco attivo vengono bloccati screenshot, registrazioni dello schermo e l'anteprima tra le app recenti.
  - Disponibile con un blocco schermo impostato e Android 9 o successivo; altrimenti l'interruttore resta disattivato.
- `PasswordGen.Core`: `AppLockState`, logica del blocco indipendente dalla piattaforma (con test), pronta per Windows Hello.

### Modificato
- L'app Android richiede anche il permesso di usare impronta/PIN (`USE_BIOMETRIC`, nessuna conferma all'installazione).

## [1.6.0] - 2026-10-08

### Aggiunto
- **App Android, tappa 3**: file di parole personale per le passphrase, come nell'app Windows (card «Parole della passphrase»).
  - Si sceglie un file di testo (una parola per riga, 4-9 lettere, `#` per i commenti): l'app lo copia nella memoria privata,
    lo normalizza (minuscole, senza accenti, senza duplicati) e mostra quante parole ha accettato e perché ha scartato le altre.
  - Tre modalità: solo la lista integrata, aggiungi il mio file (basta una parola valida), solo il mio file (almeno 300 parole).
  - Riepilogo con i bit per parola e avviso sotto le 1000 parole; se il file manca o è troppo corto si usa la lista integrata.
  - Il file precedente resta intatto finché quello nuovo non è valido.

## [1.5.0] - 2026-10-08

### Aggiunto
- **App Android, tappa 2**: storico delle password e promemoria con notifiche.
  - Scheda *Storico*: con «Ho cambiato la password» l'app chiede quale proposta è stata usata e la registra con numero
    progressivo, data e tipo (ultime 12). Password mascherate, con mostra/nascondi, copia (con cancellazione automatica degli appunti)
    ed elimina; interruttore per disattivare lo storico e pulsante per cancellarlo (con conferma). Le nuove proposte evitano le varianti
    di quelle passate.
  - Lo storico è cifrato (AES-256 + HMAC) con una chiave generata sul telefono e custodita nel Keystore di Android (`SecureStorage`):
    la chiave non sta mai nei file dell'app.
  - Promemoria: banner con i giorni alla scadenza e **notifica giornaliera** (verso le 9) quando mancano al massimo 5 giorni o la
    password è scaduta. Si riattiva da sola dopo il riavvio del telefono.
  - Password attuale e password dello storico vengono nascoste quando l'app va in secondo piano.
- `PasswordGen.Core`: `AesHmacProtector`, cifratura portabile (AES-256-CBC + HMAC-SHA256) con test, usata dall'app Android.

### Modificato
- L'app Android richiede ora due permessi: notifiche (Android 13+, richiesto all'attivazione del promemoria) e riavvio del telefono
  (per ripianificare il promemoria). Nessun accesso alla rete.

## [1.4.0] - 2026-10-08

### Aggiunto
- **App Android** (prima tappa, .NET MAUI, cartella `src/PasswordGen.Mobile`): le tre tipologie di password (parole italiane,
  sillabe, caratteri casuali), policy configurabile, numero di proposte regolabile, password attuale facoltativa e
  copia negli appunti segnalata come sensibile e cancellata dopo 30 secondi. L'APK è allegata alla release
  (Android 7.0 o successivo, nessun permesso). Storico, promemoria con notifiche e file di parole arriveranno nelle tappe successive.
- Il workflow di release compila e allega anche l'APK (firma con la chiave fissa se i secret `ANDROID_KEYSTORE_*` sono configurati).

### Modificato
- `PasswordGen.Core` ora è compilato sia per `net48` (app Windows e test) sia per `netstandard2.0` (app Android).
  `DpapiProtector` resta disponibile solo su Windows.

## [1.3.0] - 2026-10-08

### Aggiunto
- **File di parole personale** per le passphrase (card «Parole della passphrase»): una parola per riga, 4-9 lettere,
  `#` per i commenti. Le parole vengono portate in minuscolo, senza accenti e senza duplicati; l'app mostra quante ne ha
  accettate e perché ha scartato le altre.
- Tre modalità: solo la lista integrata, **aggiungi il mio file alla lista integrata** (basta una parola valida)
  oppure **solo il mio file** (almeno 300 parole valide).
- Riepilogo della lista in uso con il valore in bit di ogni parola e avviso quando la lista ha meno di 1000 parole.
- Con il file mancante, illeggibile o troppo corto l'app ripiega sulla lista integrata e lo segnala.
- Il percorso del file e la modalità scelta vengono ricordati tra un avvio e l'altro.

## [1.2.0] - 2026-10-08

### Aggiunto
- **Storico delle password** (facoltativo): premendo «Ho cambiato la password» l'app chiede quale proposta è stata usata
  (preselezionata l'ultima copiata) e la registra con numero progressivo, data e tipo. Ultime 12 voci.
- Scheda *Storico* con password mascherate: mostra/nascondi, copia (con cancellazione automatica degli appunti) ed elimina;
  pulsante per cancellare tutto lo storico.
- Le nuove proposte evitano le varianti di tutte le password dello storico, non solo dell'ultima.
- Il pulsante «Ho cambiato la password» è sempre visibile, anche con il promemoria disattivato.

### Modificato
- Le password dello storico sono salvate in `%AppData%\PasswordGen\history.dat`, cifrato con DPAPI (utente corrente).
  Disattivando lo storico il file viene cancellato (con conferma).

## [1.1.0] - 2026-10-08

### Aggiunto
- Numero di proposte regolabile dall'interfaccia (da 1 a 20, predefinito 6), salvato nelle preferenze.

## [1.0.0] - 2026-10-08

### Aggiunto
- Prima versione: generatore di password casuali, sicure e facili da ricordare per la rotazione mensile imposta dalle policy aziendali.
- Tre tipi di password: **parole italiane** (passphrase di 3-8 parole da una lista di oltre mille parole, con cifre e simbolo finali),
  **sillabe pronunciabili** e **caratteri casuali**.
- Policy configurabile (predefinita: almeno 10 caratteri con maiuscole, minuscole, numeri e caratteri speciali), con opzione
  per evitare i caratteri ambigui (0 O 1 l I) e limite ai caratteri uguali consecutivi.
- Sei proposte per volta, con entropia in bit e giudizio (Debole / Accettabile / Buona / Ottima).
- Password attuale facoltativa (solo in memoria): le nuove proposte sono sensibilmente diverse dalla precedente.
- Copia negli appunti con esclusione dalla cronologia di Windows e cancellazione automatica dopo 30 secondi o alla chiusura.
- Promemoria del cambio password: banner con i giorni alla scadenza, pulsante «Ho cambiato la password» e, a scelta,
  avvio con Windows che apre la finestra solo quando la password sta per scadere.
- Preferenze salvate in `%AppData%\PasswordGen\settings.json` (mai le password).
