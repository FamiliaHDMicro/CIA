using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ControlIA.Core;

namespace NexusAdminTool
{
    public partial class LicencaWindow : Window
    {
        private readonly CancellationTokenSource _cancelamento = new CancellationTokenSource();
        private bool _fechando;

        public LicencaWindow()
        {
            InitializeComponent();
            MostrarSituacao();
            txtChave.Focus();
        }

        private void MostrarSituacao()
        {
            string atual = UserSettings.LicenseKey;
            txtSituacao.Text = UserSettings.ValidarChaveLicenca(atual)
                ? "Licença registrada nesta máquina: " + atual
                : "Nenhuma licença ativada nesta máquina.";
        }

        private async void btnAtivar_Click(object sender, RoutedEventArgs e)
        {
            string chave = (txtChave.Text ?? string.Empty).Trim().ToUpperInvariant();

            if (!UserSettings.ValidarChaveLicenca(chave))
            {
                txtResultado.Text = "A chave precisa ter o formato XXXXX-XXXXX-XXXXX " +
                                    "(letras e números, com os dois traços). Confira e tente de novo.";
                return;
            }

            btnAtivar.IsEnabled = false;
            txtChave.IsEnabled = false;
            txtResultado.Text = "Conferindo com o servidor...";

            bool ok = false;
            try
            {
                ok = await LicenseManager.ProcessarAtivacaoAsync(chave, _cancelamento.Token);
            }
            catch (Exception ex)
            {
                if (_fechando)
                    return;
                txtResultado.Text = "Não consegui concluir a ativação:\n" + ex.Message;
            }

            if (_fechando)
                return;

            if (ok)
            {
                txtResultado.Text = "Licença ativada com sucesso.";
                MostrarSituacao();
                txtChave.Clear();
            }
            else if (string.IsNullOrEmpty(txtResultado.Text) || txtResultado.Text == "Conferindo com o servidor...")
            {
                txtResultado.Text = "A chave não foi aceita. Pode estar errada, vencida, revogada ou já usada em outra máquina. " +
                                    "Se a chave estiver certa, confira se este PC está com internet.";
            }

            btnAtivar.IsEnabled = true;
            txtChave.IsEnabled = true;
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _fechando = true;
            _cancelamento.Cancel();
            base.OnClosed(e);
        }
    }
}
