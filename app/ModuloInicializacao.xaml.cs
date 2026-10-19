using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    public partial class ModuloInicializacao : Window
    {
        private bool _fechando;

        public ModuloInicializacao()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LerAsync();
        }

        private async Task LerAsync()
        {
            txt.Text = "Lendo o que abre junto com o Windows...";
            try
            {
                List<EntradaInicializacao> lista = await Task.Run(() => Inicializacao.Ler());
                if (_fechando) return;

                var s = new StringBuilder();
                s.AppendLine($"Encontrei {lista.Count} itens. Eu só mostro e sugiro; não desativo nada.");
                s.AppendLine();
                foreach (var e in lista)
                {
                    s.AppendLine(e.Nome + "   [" + e.Origem + "]");
                    s.AppendLine("  Comando: " + e.Comando);
                    s.AppendLine("  Minha sugestão: " + e.Sugestao);
                    s.AppendLine();
                }
                txt.Text = s.ToString();
            }
            catch (Exception ex)
            {
                if (!_fechando) txt.Text = "Não consegui ler a inicialização: " + ex.Message;
            }
        }

        private async void btnLer_Click(object sender, RoutedEventArgs e) { await LerAsync(); }
        private void btnFechar_Click(object sender, RoutedEventArgs e) { Close(); }

        protected override void OnClosed(EventArgs e)
        {
            _fechando = true;
            base.OnClosed(e);
        }
    }
}
