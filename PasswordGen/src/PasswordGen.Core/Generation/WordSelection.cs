using System;
using System.Linq;

namespace PasswordGen.Core.Generation
{
    /// <summary>La lista di parole effettivamente usata e gli avvisi per l'utente.</summary>
    public sealed class WordSelection
    {
        /// <summary>Sotto questo numero di parole si avvisa che la passphrase perde sicurezza.</summary>
        public const int RecommendedWords = 1000;

        public WordSelection(WordList list, WordSourceMode effective, string warning)
        {
            List = list;
            Effective = effective;
            Warning = warning ?? string.Empty;
        }

        public WordList List { get; private set; }

        /// <summary>La sorgente realmente usata (può differire da quella richiesta se il file non è utilizzabile).</summary>
        public WordSourceMode Effective { get; private set; }

        public string Warning { get; private set; }

        public double BitsPerWord
        {
            get { return Math.Log(List.Count, 2); }
        }

        /// <summary>Sceglie la lista da usare in base alla modalità richiesta e al file caricato; senza file utilizzabile ripiega sulla lista integrata.</summary>
        public static WordSelection Select(WordList builtin, WordFileResult custom, WordSourceMode requested)
        {
            var warning = string.Empty;
            var list = builtin;
            var effective = WordSourceMode.Builtin;

            if (requested != WordSourceMode.Builtin)
            {
                if (custom == null)
                {
                    warning = "Nessun file di parole caricato: uso la lista integrata.";
                }
                else if (custom.Error != null)
                {
                    warning = custom.Error + " Uso la lista integrata.";
                }
                else if (requested == WordSourceMode.CustomOnly && custom.Words.Count < CustomWordList.MinWordsCustomOnly)
                {
                    warning = "Il file ha solo " + custom.Words.Count + " parole valide: per usarlo da solo ne servono almeno "
                        + CustomWordList.MinWordsCustomOnly + ". Uso la lista integrata.";
                }
                else if (requested == WordSourceMode.Combined && custom.Words.Count < CustomWordList.MinWordsCombined)
                {
                    warning = "Il file non contiene parole valide: uso la lista integrata.";
                }
                else if (requested == WordSourceMode.CustomOnly)
                {
                    list = new WordList(custom.Words);
                    effective = WordSourceMode.CustomOnly;
                }
                else
                {
                    list = new WordList(builtin.Words.Concat(custom.Words));
                    effective = WordSourceMode.Combined;
                }
            }

            if (list.Count < RecommendedWords)
            {
                var size = "La lista ha " + list.Count + " parole (" + Math.Log(list.Count, 2).ToString("0.0") + " bit per parola): "
                    + "aggiungi altre parole o usa più parole nella passphrase.";
                warning = warning.Length == 0 ? size : warning + " " + size;
            }

            return new WordSelection(list, effective, warning);
        }
    }
}
