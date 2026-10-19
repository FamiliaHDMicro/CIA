using System;
using System.Windows;
using System.Windows.Threading;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    public partial class JanelaPrincipal : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };

        public JanelaPrincipal()
        {
            InitializeComponent();
            _timer.Tick += (_, _) => Atualizar();
            _timer.Start();
            Atualizar();
        }

        public void Atualizar()
        {
            bool bancada = App.GestorModo.Atual == Modo.Bancada;
            txtModo.Text = bancada
                ? "Modo BANCADA (técnico). Desliga sozinho depois de 15 minutos."
                : "Modo NORMAL. Eu só aviso e sugiro: nunca apago nem desligo nada sozinha.";
            btnBancada.Content = bancada ? "Sair do modo bancada" : "Entrar no modo bancada (senha do técnico)";
        }

        private void btnBancada_Click(object sender, RoutedEventArgs e)
        {
            if (App.GestorModo.Atual == Modo.Bancada)
                App.GestorModo.SairBancada();
            else
                Dialogos.EntrarNaBancada(this);
            Atualizar();
        }

        private void btnRelatorio_Click(object sender, RoutedEventArgs e) { new ModuloRelatorio { Owner = this }.Show(); }
        private void btnLimpeza_Click(object sender, RoutedEventArgs e) { new ModuloLimpeza { Owner = this }.Show(); }
        private void btnMemoria_Click(object sender, RoutedEventArgs e) { new ModuloMemoria { Owner = this }.Show(); }
        private void btnInicializacao_Click(object sender, RoutedEventArgs e) { new ModuloInicializacao { Owner = this }.Show(); }
        private void btnProgramas_Click(object sender, RoutedEventArgs e) { new ModuloProgramas { Owner = this }.Show(); }
        private void btnDiscos_Click(object sender, RoutedEventArgs e) { new ModuloDiscos { Owner = this }.Show(); }

        private void btnLicenca_Click(object sender, RoutedEventArgs e) { new LicencaWindow { Owner = this }.ShowDialog(); }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
