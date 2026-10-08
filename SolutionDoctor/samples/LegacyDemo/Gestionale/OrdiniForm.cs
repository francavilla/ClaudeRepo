using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Gestionale
{
    // Esempio di form "all in uno": interfaccia, accesso ai dati e logica nello stesso handler.
    public partial class OrdiniForm : Form
    {
        public OrdiniForm()
        {
            InitializeComponent();
        }

        private void btnCerca_Click(object sender, EventArgs e)
        {
            using (var cn = new SqlConnection(Dati.Db.ConnectionString))
            using (var cmd = new SqlCommand("SELECT * FROM Ordini WHERE Cliente LIKE '%" + txtCerca.Text + "%'", cn))
            {
                cn.Open();
                Application.DoEvents();
            }
        }

        private void Aggiorna()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(Aggiorna));
            }
        }
    }
}
