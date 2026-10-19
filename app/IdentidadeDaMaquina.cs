using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace ControlIA.Core
{
    /// <summary>
    /// Um código estável que identifica ESTA máquina para a licença.
    /// Sai do identificador do Windows já embaralhado (SHA-256): o valor original nunca sai daqui.
    /// </summary>
    public static class IdentidadeDaMaquina
    {
        private static string? _cache;

        public static string Obter()
        {
            if (_cache != null)
                return _cache;

            string bruto = LerMachineGuid() ?? Environment.MachineName;
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("ControlIA|" + bruto));
            _cache = Convert.ToHexString(hash).Substring(0, 32);
            return _cache;
        }

        private static string? LerMachineGuid()
        {
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using RegistryKey? chave = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                string? valor = chave?.GetValue("MachineGuid")?.ToString();
                return string.IsNullOrWhiteSpace(valor) ? null : valor;
            }
            catch
            {
                return null;
            }
        }
    }
}
