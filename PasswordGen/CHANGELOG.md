# Changelog

Tutte le modifiche rilevanti di PasswordGen sono documentate qui.
Formato: [Keep a Changelog](https://keepachangelog.com/it-IT/1.1.0/) — versioni: [Semantic Versioning](https://semver.org/lang/it/).
Il numero di versione si imposta in un solo punto: `<Version>` in `Directory.Build.props`.

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
