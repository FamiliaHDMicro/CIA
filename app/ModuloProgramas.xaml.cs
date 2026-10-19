using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    public sealed class LinhaPrograma
    {
        public ProgramaInstalado Programa { get; set; } = new ProgramaInstalado();
        public string Texto { get; set; } = "";
    }

    public partial class ModuloProgramas : Window
    {
        private bool _fechando;

        public ModuloProgramas()
        {
            InitializeComponent();
            Loaded += async (_, _) => await CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            txtResumo.Text = "Lendo os programas instalados...";
            try
            {
                List<ProgramaInstalado> programas = await Task.Run(() => ProtecaoProgramas.Listar());
                if (_fechando) return;

                lista.ItemsSource = programas.Select(p => new LinhaPrograma
                {
                    Programa = p,
                    Texto = $"[{Rotulo(p.Nivel)}] {p.Nome}" + (string.IsNullOrWhiteSpace(p.Fabricante) ? "" : " — " + p.Fabricante)
                }).ToList();
                txtResumo.Text = $"{programas.Count} programas. Os PROTEGIDOS (Edge, componentes do Windows, etc.) nunca podem ser removidos por aqui; " +
                                 "os de CUIDADO só no modo bancada.";
            }
            catch (Exception ex)
            {
                if (!_fechando) txtResumo.Text = "Não consegui ler os programas: " + ex.Message;
            }
        }

        private static string Rotulo(NivelProtecao n)
        {
            switch (n)
            {
                case NivelProtecao.Protegido: return "PROTEGIDO";
                case NivelProtecao.Cuidado: return "CUIDADO";
                default: return "livre";
            }
        }

        private void lista_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var linha = lista.SelectedItem as LinhaPrograma;
            btnDesinstalar.IsEnabled = linha != null && linha.Programa.Nivel != NivelProtecao.Protegido
                                       && !string.IsNullOrWhiteSpace(linha.Programa.Desinstalador);
            txtDetalhe.Text = linha == null ? "" : (linha.Programa.Motivo.Length > 0 ? linha.Programa.Motivo : "Pode ser desinstalado, com a sua confirmação.");
        }

        private void btnDesinstalar_Click(object sender, RoutedEventArgs e)
        {
            var linha = lista.SelectedItem as LinhaPrograma;
            if (linha == null) return;
            ProgramaInstalado p = linha.Programa;

            // Cuidado: pede a bancada antes de seguir.
            if (p.Nivel == NivelProtecao.Cuidado && App.GestorModo.Atual != Modo.Bancada)
            {
                MessageBox.Show("Este programa é de CUIDADO (driver ou segurança). Só o técnico pode remover, no modo bancada.",
                    "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (!Dialogos.EntrarNaBancada(this)) return;
            }

            var resp = MessageBox.Show($"Desinstalar \"{p.Nome}\"?\n\nVai abrir o desinstalador do próprio programa.",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            try
            {
                ProtecaoProgramas.Autorizar(p, resp == MessageBoxResult.Yes, App.GestorModo);
                Process.Start(new ProcessStartInfo("cmd.exe", "/c \"" + p.Desinstalador + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (InvalidOperationException) { /* o dono não confirmou */ }
            catch (Exception ex)
            {
                MessageBox.Show("Não consegui abrir o desinstalador:\n" + ex.Message, "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e) { Close(); }

        protected override void OnClosed(EventArgs e)
        {
            _fechando = true;
            base.OnClosed(e);
        }
    }
}
