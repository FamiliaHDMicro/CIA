using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ControlIA.Core
{
    /// <summary>
    /// Configurações guardadas nesta máquina, em %LocalAppData%\ControlIA\configuracoes.json.
    /// Nada daqui vai para a nuvem sem o técnico confirmar.
    /// </summary>
    public static class UserSettings
    {
        private static readonly Regex FormatoChave =
            new Regex("^[A-Z0-9]{5}(-[A-Z0-9]{5}){2}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly object Trava = new object();

        private static readonly string PastaDados = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ControlIA");

        private static readonly string ArquivoDados = Path.Combine(PastaDados, "configuracoes.json");

        internal sealed class Dados
        {
            public bool EnviarTelemetria { get; set; } = false;
            public string NomeTecnico { get; set; } = "Técnico ControlIA";
            public string ChaveLicenca { get; set; } = string.Empty;
        }

        // Precisa ficar DEPOIS dos campos acima (o C# inicia na ordem em que aparecem).
        private static readonly Dados Atual = Carregar();

        public static bool SendTelemetryToCloud
        {
            get { lock (Trava) { return Atual.EnviarTelemetria; } }
            set { lock (Trava) { Atual.EnviarTelemetria = value; Salvar(); } }
        }

        public static string NomeTecnico
        {
            get { lock (Trava) { return Atual.NomeTecnico; } }
            set { lock (Trava) { Atual.NomeTecnico = (value ?? string.Empty).Trim(); Salvar(); } }
        }

        public static string LicenseKey
        {
            get { lock (Trava) { return Atual.ChaveLicenca; } }
            set { lock (Trava) { Atual.ChaveLicenca = (value ?? string.Empty).Trim().ToUpperInvariant(); Salvar(); } }
        }

        /// <summary>Confere só o FORMATO (XXXXX-XXXXX-XXXXX). Quem decide se vale é o servidor.</summary>
        public static bool ValidarChaveLicenca(string? chave)
        {
            return !string.IsNullOrEmpty(chave) && FormatoChave.IsMatch(chave);
        }

        /// <summary>
        /// Validação sem internet. Por segurança ela NEGA por padrão: só vai liberar quando o
        /// servidor passar a emitir um comprovante assinado que esta máquina consiga conferir.
        /// </summary>
        public static bool ValidarAssinaturaOffline(string? chave)
        {
            return false;
        }

        private static Dados Carregar()
        {
            try
            {
                if (File.Exists(ArquivoDados))
                {
                    Dados? lido = JsonSerializer.Deserialize<Dados>(File.ReadAllText(ArquivoDados));
                    if (lido != null)
                        return lido;
                }
            }
            catch
            {
                // Arquivo ausente ou corrompido: começa com o padrão.
            }

            return new Dados();
        }

        private static void Salvar()
        {
            try
            {
                Directory.CreateDirectory(PastaDados);
                string temporario = ArquivoDados + ".tmp";
                File.WriteAllText(temporario, JsonSerializer.Serialize(Atual, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temporario, ArquivoDados, true);
            }
            catch
            {
                // Se não der para salvar, o programa segue com o valor em memória.
            }
        }
    }
}
