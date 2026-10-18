using System;
using System.Collections.Generic;
using System.IO;
using System.Management;

namespace ControlIA.Ferramentas
{
    public sealed class DiscoFisico
    {
        public uint Numero;
        public string Modelo = "", Interface = "", TipoMidia = "";
        public ulong Tamanho;
        public bool Removivel, EhDoSistema;
        public List<string> Letras = new List<string>();
    }

    /// <summary>
    /// Trava de segurança das operações destrutivas (partição, pen drive de reprodução).
    /// Esta classe NÃO formata nada: ela só diz se a operação pode seguir.
    /// Regras: só na bancada, nunca no disco do sistema, só em disco removível,
    /// e o técnico digita a confirmação mostrando o disco.
    /// </summary>
    public static class DiscoSeguro
    {
        public static List<DiscoFisico> Listar()
        {
            var discos = new List<DiscoFisico>();
            string sistema = (Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\").TrimEnd('\\').ToUpperInvariant();

            using var busca = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject d in busca.Get())
            {
                var disco = new DiscoFisico
                {
                    Numero = Convert.ToUInt32(d["Index"] ?? 0),
                    Modelo = Convert.ToString(d["Model"]) ?? "",
                    Interface = Convert.ToString(d["InterfaceType"]) ?? "",
                    TipoMidia = Convert.ToString(d["MediaType"]) ?? "",
                    Tamanho = Convert.ToUInt64(d["Size"] ?? 0UL)
                };
                disco.Removivel = disco.Interface.Equals("USB", StringComparison.OrdinalIgnoreCase)
                               || disco.TipoMidia.IndexOf("Removable", StringComparison.OrdinalIgnoreCase) >= 0;

                string id = (Convert.ToString(d["DeviceID"]) ?? "").Replace("\\", "\\\\");
                try
                {
                    using var parts = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{id}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                    foreach (ManagementObject part in parts.Get())
                    {
                        if (part["BootPartition"] is bool b && b) disco.EhDoSistema = true;
                        string pid = (Convert.ToString(part["DeviceID"]) ?? "").Replace("\\", "\\\\");
                        using var logs = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{pid}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                        foreach (ManagementObject l in logs.Get())
                        {
                            string letra = (Convert.ToString(l["DeviceID"]) ?? "").ToUpperInvariant();
                            if (letra.Length > 0) disco.Letras.Add(letra);
                            if (letra == sistema) disco.EhDoSistema = true;
                        }
                    }
                }
                catch { disco.EhDoSistema = true; /* na dúvida, trata como disco do sistema */ }
                discos.Add(disco);
            }
            return discos;
        }

        public static string TextoDeConfirmacao(DiscoFisico d) => $"APAGAR DISCO {d.Numero}";

        /// <summary>Lança exceção se a operação destrutiva não puder seguir.</summary>
        public static void AutorizarOperacaoDestrutiva(DiscoFisico d, GestorDeModo modo, string? textoDigitado, bool exigirRemovivel = true)
        {
            modo.Exigir(Modo.Bancada);
            if (d.EhDoSistema)
                throw new UnauthorizedAccessException("Este disco contém o Windows (ou não deu para ter certeza). Operação bloqueada.");
            if (exigirRemovivel && !d.Removivel)
                throw new UnauthorizedAccessException("Este disco não é removível. Operação bloqueada.");
            if (!string.Equals(textoDigitado?.Trim(), TextoDeConfirmacao(d), StringComparison.Ordinal))
                throw new InvalidOperationException($"Confirmação incorreta. Digite exatamente: {TextoDeConfirmacao(d)}");
        }
    }
}
