using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    public sealed class LinhaLimpeza
    {
        public bool Marcado { get; set; }
        public ItemLimpeza Item { get; set; } = new ItemLimpeza();
        public string Texto { get; set; } = "";
    }

    public partial class ModuloLimpeza : Window
    {
        private readonly CancellationTokenSource _cancelamento = new CancellationTokenSource();
        private bool _fechando;

        public ModuloLimpeza()
        {
            InitializeComponent();
            Loaded += async (_, _) => await VarrerAsync();
        }

        private async Task VarrerAsync()
        {
            btnVarrer.IsEnabled = false;
            btnLimpar.IsEnabled = false;
            lista.ItemsSource = null;
            txtResumo.Text = "Estou olhando o que dá para limpar... aguarde.";
            try
            {
                List<ItemLimpeza> itens = await Task.Run(() => LimpezaDisco.Varrer(_cancelamento.Token));
                if (_fechando) return;

                bool bancada = App.GestorModo.Atual == Modo.Bancada;
                var linhas = itens.Select(i => new LinhaLimpeza
                {
                    Item = i,
                    // Sugestão: já vêm marcados só os itens leves que podem ser limpos agora.
                    Marcado = i.Risco == RiscoLimpeza.Baixo && (!i.ExigeBancada || bancada),
                    Texto = $"{i.Nome} — {LimpezaDisco.Formatar(i.Bytes)} em {i.Arquivos} arquivos" +
                            (i.ExigeBancada && !bancada ? "  [só no modo bancada]" : "") +
                            "\n" + i.Explicacao
                }).ToList();

                lista.ItemsSource = linhas;
                long total = itens.Sum(i => i.Bytes);
                txtResumo.Text = itens.Count == 0
                    ? "Não achei nada que valha a pena limpar agora."
                    : $"Posso liberar até {LimpezaDisco.Formatar(total)}. Marque o que quer limpar. Eu nunca apago nada sem a sua confirmação.";
                btnLimpar.IsEnabled = itens.Count > 0;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!_fechando) txtResumo.Text = "Não consegui concluir a varredura: " + ex.Message;
            }
            finally
            {
                if (!_fechando) btnVarrer.IsEnabled = true;
            }
        }

        private async void btnVarrer_Click(object sender, RoutedEventArgs e) { await VarrerAsync(); }

        private async void btnLimpar_Click(object sender, RoutedEventArgs e)
        {
            var marcados = (lista.ItemsSource as IEnumerable<LinhaLimpeza>)?.Where(l => l.Marcado).ToList()
                           ?? new List<LinhaLimpeza>();
            if (marcados.Count == 0)
            {
                MessageBox.Show("Marque pelo menos um item.", "ControlIA", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            long total = marcados.Sum(m => m.Item.Bytes);
            var lis = new StringBuilder();
            foreach (var m in marcados) lis.AppendLine("• " + m.Item.Nome);
            var resp = MessageBox.Show(
                $"Vou apagar até {LimpezaDisco.Formatar(total)} destes itens:\n\n{lis}\nConfirma?",
                "Confirmar limpeza", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resp != MessageBoxResult.Yes) return;

            btnLimpar.IsEnabled = false;
            btnVarrer.IsEnabled = false;
            txtResumo.Text = "Limpando...";
            var ids = marcados.Select(m => m.Item.Id).ToList();
            ResultadoLimpeza r = await Task.Run(() => LimpezaDisco.Apagar(ids, true, App.GestorModo));
            if (_fechando) return;

            var msg = new StringBuilder();
            msg.AppendLine($"Liberei {LimpezaDisco.Formatar(r.BytesLiberados)} ({r.Apagados} arquivos).");
            if (r.Falhas > 0) msg.AppendLine($"{r.Falhas} arquivos estavam em uso e ficaram.");
            foreach (string p in r.Pulados) msg.AppendLine("Pulei: " + p);
            MessageBox.Show(msg.ToString(), "ControlIA", MessageBoxButton.OK, MessageBoxImage.Information);
            await VarrerAsync();
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e) { Close(); }

        protected override void OnClosed(EventArgs e)
        {
            _fechando = true;
            _cancelamento.Cancel();
            base.OnClosed(e);
        }
    }
}
