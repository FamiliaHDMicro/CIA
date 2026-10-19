using System;
using System.Windows;
using System.Windows.Controls;
using ControlIA.Ferramentas;

namespace NexusAdminTool
{
    internal static class Dialogos
    {
        /// <summary>Caixinha pedindo um texto ou uma senha. Devolve null se cancelou.</summary>
        public static string? Pedir(Window? dono, string titulo, string mensagem, bool senha)
        {
            var janela = new Window
            {
                Title = titulo,
                Width = 440,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = dono != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            if (dono != null && dono.IsLoaded)
                janela.Owner = dono;

            var painel = new StackPanel { Margin = new Thickness(14) };
            painel.Children.Add(new TextBlock { Text = mensagem, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });

            Control campo = senha ? (Control)new PasswordBox() : new TextBox();
            painel.Children.Add(campo);

            var botoes = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var ok = new Button { Content = "OK", MinWidth = 90, Padding = new Thickness(12, 6, 12, 6), IsDefault = true, Margin = new Thickness(6, 0, 0, 0) };
            var cancelar = new Button { Content = "Cancelar", MinWidth = 90, Padding = new Thickness(12, 6, 12, 6), IsCancel = true, Margin = new Thickness(6, 0, 0, 0) };
            botoes.Children.Add(ok);
            botoes.Children.Add(cancelar);
            painel.Children.Add(botoes);
            janela.Content = painel;

            string? resultado = null;
            ok.Click += (_, _) =>
            {
                resultado = senha ? ((PasswordBox)campo).Password : ((TextBox)campo).Text;
                janela.DialogResult = true;
            };
            janela.Loaded += (_, _) => campo.Focus();
            janela.ShowDialog();
            return resultado;
        }

        /// <summary>Pede a senha do técnico (ou cria, na primeira vez). Devolve true se a bancada ficou ativa.</summary>
        public static bool EntrarNaBancada(Window? dono)
        {
            GestorDeModo gestor = App.GestorModo;
            if (gestor.Atual == Modo.Bancada)
                return true;

            if (!gestor.SenhaDefinida)
            {
                string? nova = Pedir(dono, "Senha do técnico",
                    "Ainda não existe senha do técnico nesta máquina.\nDefina agora a senha do técnico instalador (mínimo 6 caracteres). " +
                    "Ela libera o modo bancada.", true);
                if (nova == null) return false;

                string? repetida = Pedir(dono, "Senha do técnico", "Digite a mesma senha de novo para confirmar.", true);
                if (repetida == null) return false;
                if (nova != repetida)
                {
                    MessageBox.Show("As duas senhas não são iguais. Nada foi salvo.", "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                try { gestor.DefinirSenha(nova); }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                gestor.EntrarBancada(nova, out _);
                return gestor.Atual == Modo.Bancada;
            }

            string? senha = Pedir(dono, "Modo bancada", "Digite a senha do técnico instalador.", true);
            if (senha == null) return false;
            bool ok = gestor.EntrarBancada(senha, out string mensagem);
            if (!ok)
                MessageBox.Show(mensagem, "ControlIA", MessageBoxButton.OK, MessageBoxImage.Warning);
            return ok;
        }
    }
}
