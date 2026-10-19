using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    public partial class ModuloMemoria : Window
    {
        private readonly DiagnosticoMemoria _diagnostico = new DiagnosticoMemoria();
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        private bool _lendo;
        private bool _fechando;

        public ModuloMemoria()
        {
            InitializeComponent();
            txt.Text = "Lendo a memória...";
            _timer.Tick += async (_, _) => await AtualizarAsync();
            Loaded += async (_, _) => { await AtualizarAsync(); _timer.Start(); };
        }

        private async Task AtualizarAsync()
        {
            if (_lendo || _fechando) return;
            _lendo = true;
            try
            {
                RelatorioMemoria r = await Task.Run(() => _diagnostico.Ler());
                if (_fechando) return;

                var s = new StringBuilder();
                s.AppendLine(r.Resumo);
                s.AppendLine();
                s.AppendLine("Quem mais usa:");
                foreach (var c in r.Maiores)
                {
                    string cresceu = Math.Abs(c.CrescimentoMB) >= 1 ? $"  ({(c.CrescimentoMB > 0 ? "+" : "")}{c.CrescimentoMB:0} MB)" : "";
                    s.AppendLine($"  {c.Nome} (x{c.Instancias}): {LimpezaDisco.Formatar(c.Bytes)}{cresceu}");
                }
                s.AppendLine();
                s.AppendLine("O que eu acho:");
                foreach (string t in r.Sugestoes) s.AppendLine("  • " + t);
                s.AppendLine();
                s.AppendLine("Atualizo sozinha a cada 5 segundos. Eu não fecho nada: quem decide é você.");
                txt.Text = s.ToString();
            }
            catch (Exception ex)
            {
                if (!_fechando) txt.Text = "Não consegui ler a memória: " + ex.Message;
            }
            finally { _lendo = false; }
        }

        private async void btnAtualizar_Click(object sender, RoutedEventArgs e) { await AtualizarAsync(); }
        private void btnFechar_Click(object sender, RoutedEventArgs e) { Close(); }

        protected override void OnClosed(EventArgs e)
        {
            _fechando = true;
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
