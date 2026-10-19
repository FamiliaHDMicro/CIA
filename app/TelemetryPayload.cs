using System;
using System.Text.Json.Serialization;

namespace ControlIA.Core
{
    /// <summary>
    /// O que a CIA envia ao servidor. Valor nulo = "não consegui ler" e simplesmente NÃO é enviado
    /// (antes ia 0, o que fazia o painel achar que estava tudo zerado).
    /// Os nomes batem com o que o worker.js lê.
    /// </summary>
    public sealed class TelemetryPayload
    {
        // Preenchidos pelo ControlIACloudClient na hora de enviar.
        [JsonPropertyName("chave")] public string? Chave { get; set; }
        [JsonPropertyName("maquinaId")] public string? MaquinaId { get; set; }
        [JsonPropertyName("tecnicoCliente")] public string? TecnicoCliente { get; set; }

        [JsonPropertyName("evento")] public string Evento { get; set; } = "DIAGNOSTICO_CONCLUIDO";
        [JsonPropertyName("tipo")] public string Tipo { get; set; } = "relatorio_saude";
        [JsonPropertyName("timestampUtc")] public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("cpuPercentual")] public double? CpuPercentual { get; set; }
        [JsonPropertyName("memoriaDisponivelMb")] public double? MemoriaDisponivelMb { get; set; }
        [JsonPropertyName("discoLivreGb")] public double? DiscoLivreGb { get; set; }
    }
}
