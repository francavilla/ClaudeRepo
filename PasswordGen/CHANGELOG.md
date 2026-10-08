# Changelog

Tutte le modifiche rilevanti di PasswordGen sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

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
