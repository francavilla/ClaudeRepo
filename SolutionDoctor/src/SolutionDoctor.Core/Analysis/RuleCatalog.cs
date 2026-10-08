using System.Collections.Generic;
using System.Linq;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>Elenco delle regole note: identificativo, significato e rimedio.</summary>
    public static class RuleCatalog
    {
        public const string FormTooLarge = "WF001";
        public const string DataAccessInHandler = "WF002";
        public const string DoEvents = "WF003";
        public const string ConcatenatedSql = "WF004";
        public const string MutableStaticState = "WF005";
        public const string ScatteredInvokeRequired = "WF006";
        public const string LongHandler = "WF007";

        public const string PackagesConfig = "PJ001";
        public const string ClassicProjectFormat = "PJ002";
        public const string MigrationBlockers = "PJ003";
        public const string OutdatedFramework = "PJ004";

        public static readonly IReadOnlyList<RuleInfo> All = new List<RuleInfo>
        {
            new RuleInfo(FormTooLarge, "Form o user control di grandi dimensioni",
                "Spezzare in UserControl per area funzionale ed estrarre la logica in servizi/presenter."),
            new RuleInfo(DataAccessInHandler, "Accesso a database o file dentro un event handler",
                "Spostare l'accesso ai dati in un repository dietro interfaccia; l'handler deve solo delegare."),
            new RuleInfo(DoEvents, "Uso di Application.DoEvents()",
                "Sintomo di lavoro bloccante sul thread UI: usare async/await con IProgress<T> e CancellationToken."),
            new RuleInfo(ConcatenatedSql, "Comando SQL costruito per concatenazione",
                "Rischio di SQL injection: usare parametri (SqlParameter). Le costanti con nome possono essere falsi positivi."),
            new RuleInfo(MutableStaticState, "Stato statico modificabile",
                "Stato globale nascosto: sostituire con un servizio iniettato o un contesto applicativo esplicito."),
            new RuleInfo(ScatteredInvokeRequired, "InvokeRequired sparso nel codice",
                "Centralizzare il marshalling verso la UI (SynchronizationContext o async/await)."),
            new RuleInfo(LongHandler, "Event handler troppo lungo",
                "La logica sta nell'handler: Extract Method, poi Extract Class verso un servizio testabile."),
            new RuleInfo(PackagesConfig, "Il progetto usa packages.config",
                "Migrare a PackageReference (Visual Studio: tasto destro su packages.config)."),
            new RuleInfo(ClassicProjectFormat, "Il .csproj non è nel formato SDK-style",
                "Convertire a SDK-style: prerequisito per il multi-targeting (net48;net8.0-windows)."),
            new RuleInfo(MigrationBlockers, "Riferimenti che bloccano o complicano la migrazione a .NET moderno",
                "Verificare in anticipo l'alternativa (System.Web, Remoting, COM, OracleClient, WCF client)."),
            new RuleInfo(OutdatedFramework, "Framework di destinazione fuori supporto (< .NET Framework 4.6.2)",
                "Aggiornare almeno a net48 prima di qualsiasi altra migrazione.")
        };

        public static RuleInfo Find(string id)
        {
            return All.FirstOrDefault(r => r.Id == id);
        }
    }
}
