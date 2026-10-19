using System.Threading;
using System.Threading.Tasks;

namespace ControlIA.Core
{
    public class LicenseManager
    {
        public static async Task<bool> ProcessarAtivacaoAsync(string? chave, CancellationToken ct = default)
        {
            chave = chave?.Trim().ToUpperInvariant();

            // Confere o formato antes de usar a rede (evita requisições inúteis).
            if (string.IsNullOrEmpty(chave) || !UserSettings.ValidarChaveLicenca(chave))
                return false;

            ResultadoLicenca resultado = await ControlIACloudClient.ValidarLicencaOnlineAsync(chave, ct);

            switch (resultado)
            {
                case ResultadoLicenca.Valida:
                    UserSettings.LicenseKey = chave;
                    return true;

                case ResultadoLicenca.Invalida:
                    // O servidor respondeu que não vale: rejeita na hora.
                    return false;

                default:
                    // Sem conexão: só libera com comprovante offline assinado. Hoje isso nega (falha fechada).
                    if (UserSettings.ValidarAssinaturaOffline(chave))
                    {
                        UserSettings.LicenseKey = chave;
                        return true;
                    }

                    return false;
            }
        }
    }
}
