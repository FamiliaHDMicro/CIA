using System.Windows;
using System.Windows.Threading;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    public partial class App : Application
    {
        /// <summary>Modo atual da CIA (normal ou bancada), compartilhado por todas as janelas.</summary>
        public static GestorDeModo GestorModo { get; } = new GestorDeModo();

        public App()
        {
            // Se algo inesperado acontecer, a CIA avisa em vez de fechar sem explicar.
            DispatcherUnhandledException += AoOcorrerErroInesperado;
        }

        private void AoOcorrerErroInesperado(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "Aconteceu um erro inesperado, mas continuo aqui:\n" + e.Exception.Message,
                "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
