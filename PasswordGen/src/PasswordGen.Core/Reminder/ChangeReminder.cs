using System;

namespace PasswordGen.Core.Reminder
{
    public enum ReminderStatus
    {
        /// <summary>Promemoria disattivato.</summary>
        Disabled = 0,

        /// <summary>Nessun cambio registrato: non si può calcolare la scadenza.</summary>
        NeverRecorded = 1,

        Ok = 2,
        DueSoon = 3,
        Expired = 4
    }

    public sealed class ReminderState
    {
        public ReminderState(ReminderStatus status, int? daysRemaining, string message)
        {
            Status = status;
            DaysRemaining = daysRemaining;
            Message = message;
        }

        public ReminderStatus Status { get; private set; }

        /// <summary>Giorni alla scadenza (negativi se già scaduta); null se non calcolabile.</summary>
        public int? DaysRemaining { get; private set; }

        public string Message { get; private set; }

        /// <summary>True se conviene avvisare l'utente (in scadenza o scaduta).</summary>
        public bool ShouldAlert
        {
            get { return Status == ReminderStatus.DueSoon || Status == ReminderStatus.Expired; }
        }
    }

    /// <summary>Calcolo della scadenza della password in base alla data dell'ultimo cambio.</summary>
    public static class ChangeReminder
    {
        public static ReminderState Evaluate(bool enabled, DateTime? lastChange, int validityDays, int warnDays, DateTime today)
        {
            if (!enabled)
            {
                return new ReminderState(ReminderStatus.Disabled, null, "Promemoria disattivato.");
            }

            if (!lastChange.HasValue)
            {
                return new ReminderState(ReminderStatus.NeverRecorded, null,
                    "Dopo aver cambiato la password premi \"Ho cambiato la password\": ti avviserò prima della scadenza.");
            }

            var elapsed = (today.Date - lastChange.Value.Date).Days;
            var remaining = validityDays - elapsed;

            if (remaining < 0)
            {
                var late = -remaining;
                return new ReminderState(ReminderStatus.Expired, remaining,
                    "La password è scaduta da " + late + (late == 1 ? " giorno" : " giorni") + ": cambiala subito.");
            }

            if (remaining == 0)
            {
                return new ReminderState(ReminderStatus.DueSoon, remaining, "La password scade oggi.");
            }

            if (remaining == 1)
            {
                return new ReminderState(ReminderStatus.DueSoon, remaining, "La password scade domani.");
            }

            var status = remaining <= warnDays ? ReminderStatus.DueSoon : ReminderStatus.Ok;
            return new ReminderState(status, remaining, "La password scade tra " + remaining + " giorni.");
        }
    }
}
