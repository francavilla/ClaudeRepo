using System;
using System.IO;
using System.Windows.Forms;

namespace Gestionale
{
    public partial class ClientiForm : Form
    {
        public static ClientiForm Corrente;

        public ClientiForm()
        {
            InitializeComponent();
            Corrente = this;
        }

        private void btnEsporta_Click(object sender, EventArgs e)
        {
            File.WriteAllText("clienti.csv", txtElenco.Text);
        }
    }
}
