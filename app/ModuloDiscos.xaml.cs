using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    public sealed class LinhaDisco
    {
        public DiscoFisico Disco { get; set; } = new DiscoFisico();
        public string Texto { get; set; } = "";
    }

    public partial class ModuloDiscos : Window
    {
        private bool _fechando;

        public ModuloDiscos()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                // Este módulo é só da bancada.
                if (App.GestorModo.Atual != Modo.Bancada && !Dialogos.EntrarNaBancada(this))
                {
                    Close();
                    return;
                }
                await CarregarAsync();
            };
        }

        private async Task CarregarAsync()
        {
            try
            {
                List<DiscoFisico> discos = await Task.Run(() => DiscoSeguro.Listar());
                if (_fechando) return;
                lista.ItemsSource = discos.Select(d => new LinhaDisco
                {
                    Disco = d,
                    Texto = $"Disco {d.Numero}: {d.Modelo} — {LimpezaDisco.Formatar((long)d.Tamanho)}\n" +
                            $"   Letras: {(d.Letras.Count == 0 ? "(nenhuma)" : string.Join(", ", d.Letras))}   |   " +
                            (d.EhDoSistema ? "DISCO DO WINDOWS (bloqueado)" : d.Removivel ? "removível (pode ser preparado)" : "fixo (bloqueado)")
                }).ToList();
            }
            catch (Exception ex)
            {
                if (!_fechando) txtResumo.Text = "Não consegui listar os discos: " + ex.Message;
            }
        }

        private async void btnAtualizar_Click(object sender, RoutedEventArgs e) { await CarregarAsync(); }

        private async void btnPreparar_Click(object sender, RoutedEventArgs e)
        {
            var linha = lista.SelectedItem as LinhaDisco;
            if (linha == null)
            {
                MessageBox.Show("Clique primeiro no pen drive na lista.", "ControlIA", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (App.GestorModo.Atual != Modo.Bancada && !Dialogos.EntrarNaBancada(this))
                return;

            DiscoFisico d = linha.Disco;
            string frase = DiscoSeguro.TextoDeConfirmacao(d);
            string? digitado = Dialogos.Pedir(this, "Confirmar — apaga tudo",
                $"VAI APAGAR TUDO neste disco:\n\nDisco {d.Numero}: {d.Modelo}\nTamanho: {LimpezaDisco.Formatar((long)d.Tamanho)}\n" +
                $"Letras: {(d.Letras.Count == 0 ? "(nenhuma)" : string.Join(", ", d.Letras))}\n\n" +
                $"Para continuar, digite exatamente:\n{frase}", false);
            if (digitado == null) return;

            try
            {
                string saida = await Task.Run(() => PenDrive.Preparar(d, App.GestorModo, digitado, "CIA"));
                MessageBox.Show("Terminou. Resposta do DiskPart:\n\n" + saida, "ControlIA", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ControlIA — bloqueado ou falhou", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            await CarregarAsync();
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e) { Close(); }

        protected override void OnClosed(EventArgs e)
        {
            _fechando = true;
            base.OnClosed(e);
        }
    }
}
