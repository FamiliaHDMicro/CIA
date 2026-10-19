using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ControlIA.Core;
using Microsoft.Win32;

namespace NexusAdminTool
{
    public partial class ModuloRelatorio : Window
    {
        private readonly HardwareDiagnostic _diagnostic = new HardwareDiagnostic();
        private readonly CancellationTokenSource _cancelamento = new CancellationTokenSource();
        private string _relatorioTexto = string.Empty;
        private bool _fechando;

        public ModuloRelatorio()
        {
            InitializeComponent();
            Loaded += async (_, _) => await CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            DefinirBotoes(false);
            txtRelatorio.Text = "Estou sentindo as minhas peças... aguarde alguns segundos.";

            try
            {
                // A leitura é lenta (sensores, WMI, log do Windows): roda fora da tela para ela não congelar.
                string texto = await Task.Run(() => _diagnostic.GerarRelatorioCompleto());
                if (_fechando)
                    return;

                _relatorioTexto = texto;
                txtRelatorio.Text = texto;
                DefinirBotoes(true);
            }
            catch (Exception ex)
            {
                if (_fechando)
                    return;

                txtRelatorio.Text = "Não consegui concluir a leitura das minhas peças:\n" + ex.Message;
            }
        }

        private void DefinirBotoes(bool pronto)
        {
            btnEnviarCloud.IsEnabled = pronto;
            btnExportarTxt.IsEnabled = pronto;
        }

        private static double? Arredondar(double? valor, int casas)
        {
            return valor.HasValue ? Math.Round(valor.Value, casas) : (double?)null;
        }

        private async void btnEnviarCloud_Click(object sender, RoutedEventArgs e)
        {
            SinaisVitais? sinais = _diagnostic.UltimosSinais;
            if (sinais == null)
            {
                MessageBox.Show(
                    "Aguarde a leitura terminar antes de enviar.",
                    "ControlIA", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // O envio só acontece se a pessoa confirmar a cada vez (consentimento), mesmo com o envio automático desligado.

            MessageBoxResult confirmacao = MessageBox.Show(
                "Serão enviados ao servidor ControlIA somente: uso de CPU, memória livre e espaço livre no disco, " +
                "junto com a sua licença e um código embaralhado desta máquina (para o servidor saber de quem é o aviso). " +
                "Nada além disso. Deseja enviar?",
                "Confirmar envio", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmacao != MessageBoxResult.Yes)
                return;

            btnEnviarCloud.IsEnabled = false;
            try
            {
                var payload = new TelemetryPayload
                {
                    Tipo = "relatorio_saude",
                    TimestampUtc = DateTime.UtcNow,
                    // Se a leitura falhou, vai vazio (null). Nunca 0.
                    CpuPercentual = Arredondar(sinais.CpuUsoPercent, 1),
                    MemoriaDisponivelMb = Arredondar(sinais.MemoriaLivreGb * 1024, 0),
                    DiscoLivreGb = Arredondar(sinais.DiscoSistemaLivreGb, 1)
                };

                bool enviado = await ControlIACloudClient.EnviarTelemetriaAsync(payload, _cancelamento.Token, consentimentoConfirmado: true);
                if (_fechando)
                    return;

                MessageBox.Show(
                    enviado
                        ? "Enviado com sucesso."
                        : "Não consegui enviar agora. Os dados continuam somente nesta máquina.",
                    "ControlIA", MessageBoxButton.OK,
                    enviado ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            finally
            {
                if (!_fechando)
                    btnEnviarCloud.IsEnabled = true;
            }
        }

        private void btnExportarTxt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_relatorioTexto))
            {
                MessageBox.Show(
                    "O relatório ainda não está pronto.",
                    "ControlIA", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialogo = new SaveFileDialog
            {
                Filter = "Texto (*.txt)|*.txt",
                FileName = $"CIA-sinais-vitais-{DateTime.Now:yyyyMMdd-HHmm}.txt"
            };

            if (dialogo.ShowDialog(this) != true)
                return;

            try
            {
                File.WriteAllText(dialogo.FileName, _relatorioTexto, Encoding.UTF8);
                MessageBox.Show(
                    "Relatório salvo.",
                    "ControlIA", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Não foi possível salvar o arquivo:\n" + ex.Message,
                    "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnVoltar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _fechando = true;
            _cancelamento.Cancel();
            _diagnostic.Dispose();
            base.OnClosed(e);
        }
    }
}
