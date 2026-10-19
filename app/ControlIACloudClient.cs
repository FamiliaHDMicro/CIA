using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ControlIA.Core
{
    public enum ResultadoLicenca
    {
        Valida,
        Invalida,
        SemConexao   // não deu para saber (internet fora, servidor fora do ar)
    }

    /// <summary>
    /// Conversa da CIA com o servidor (worker.js). Só duas rotas: licença e avisos.
    /// Todo JSON é montado pelo System.Text.Json (nunca por texto colado), então aspas ou
    /// símbolos em nomes não quebram nem permitem injetar dados.
    /// </summary>
    public static class ControlIACloudClient
    {
        private const string BaseUrl = "https://control-ia.hdmicro-ml.workers.dev";

        private static readonly HttpClient Client = CriarCliente();

        private static readonly JsonSerializerOptions OpcoesJson = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static HttpClient CriarCliente()
        {
            var cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd("ControlIA-CIA/0.2");
            return cliente;
        }

        // --- 1. AVISOS / TELEMETRIA ---
        public static async Task<bool> EnviarTelemetriaAsync(TelemetryPayload payload, CancellationToken ct = default, bool consentimentoConfirmado = false)
        {
            if (payload == null || !(consentimentoConfirmado || UserSettings.SendTelemetryToCloud))
                return false;

            try
            {
                string chave = UserSettings.LicenseKey;
                payload.Chave = string.IsNullOrEmpty(chave) ? null : chave;
                payload.MaquinaId = IdentidadeDaMaquina.Obter();
                payload.TecnicoCliente = UserSettings.NomeTecnico;

                string json = JsonSerializer.Serialize(payload, OpcoesJson);
                using var conteudo = new StringContent(json, Encoding.UTF8, "application/json");
                using HttpResponseMessage resposta = await Client
                    .PostAsync(BaseUrl + "/api/telemetria", conteudo, ct)
                    .ConfigureAwait(false);

                return resposta.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Sem internet, tempo esgotado ou cancelado: os dados ficam só nesta máquina.
                return false;
            }
        }

        // --- 2. VALIDAÇÃO DE LICENÇA ---
        public static async Task<ResultadoLicenca> ValidarLicencaOnlineAsync(string chave, CancellationToken ct = default)
        {
            try
            {
                string json = JsonSerializer.Serialize(new { chave, maquinaId = IdentidadeDaMaquina.Obter() });
                using var conteudo = new StringContent(json, Encoding.UTF8, "application/json");
                using HttpResponseMessage resposta = await Client
                    .PostAsync(BaseUrl + "/api/validar-licenca", conteudo, ct)
                    .ConfigureAwait(false);

                int codigo = (int)resposta.StatusCode;

                // O servidor respondeu e disse "não": formato ruim, inexistente, revogada, expirada, outra máquina.
                if (codigo == 400 || codigo == 402 || codigo == 403 || codigo == 404 || codigo == 409)
                    return ResultadoLicenca.Invalida;

                if (codigo != 200)
                    return ResultadoLicenca.SemConexao;

                string corpo = await resposta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(corpo);
                bool valida = doc.RootElement.TryGetProperty("valida", out JsonElement v) && v.ValueKind == JsonValueKind.True;
                return valida ? ResultadoLicenca.Valida : ResultadoLicenca.Invalida;
            }
            catch (Exception)
            {
                return ResultadoLicenca.SemConexao;
            }
        }
    }
}
