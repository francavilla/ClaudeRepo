namespace PasswordGen.Services
{
    /// <summary>
    /// Vero mentre l'app mostra una propria finestra di dialogo (scelta di un file, PIN, conferme): la finestra principale perde il fuoco e
    /// lo riprende, e questo non va contato come uscita ai fini del blocco.
    /// </summary>
    public static class ExternalActivity
    {
        private static int _depth;

        public static bool IsActive
        {
            get { return _depth > 0; }
        }

        public static void Enter()
        {
            _depth++;
        }

        public static void Leave()
        {
            if (_depth > 0)
            {
                _depth--;
            }
        }
    }
}
