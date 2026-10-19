using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ControlIA.Ferramentas
{
    /// <summary>
    /// Prepara (apaga e formata) um pen drive. Só na bancada, só disco removível que não seja o do Windows,
    /// e só com a confirmação digitada. O disco é lido de novo na hora para garantir que o número ainda é o mesmo.
    /// </summary>
    public static class PenDrive
    {
        public static string Preparar(DiscoFisico escolhido, GestorDeModo modo, string? textoDigitado, string rotulo = "CIA")
        {
            // Lê os discos DE NOVO: se o número mudou de dono desde que a lista foi mostrada, não segue.
            DiscoFisico? atual = DiscoSeguro.Listar().FirstOrDefault(d =>
                d.Numero == escolhido.Numero && d.Modelo == escolhido.Modelo && d.Tamanho == escolhido.Tamanho);
            if (atual == null)
                throw new InvalidOperationException("O disco mudou desde que a lista foi mostrada. Atualize a lista e tente de novo.");

            DiscoSeguro.AutorizarOperacaoDestrutiva(atual, modo, textoDigitado, exigirRemovivel: true);

            string nome = new string((rotulo ?? "CIA").ToUpperInvariant().Where(char.IsLetterOrDigit).Take(11).ToArray());
            if (nome.Length == 0) nome = "CIA";

            // FAT32 é o mais compatível, mas o DiskPart só formata FAT32 até 32 GB.
            string sistema = atual.Tamanho <= 32UL * 1024 * 1024 * 1024 ? "fat32" : "exfat";
            string script =
                $"select disk {atual.Numero}\r\nclean\r\ncreate partition primary\r\nformat fs={sistema} quick label={nome}\r\nassign\r\nexit\r\n";

            string arquivo = Path.Combine(Path.GetTempPath(), "cia-diskpart-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(arquivo, script, Encoding.ASCII);
            try
            {
                var psi = new ProcessStartInfo("diskpart.exe", "/s \"" + arquivo + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi) ?? throw new InvalidOperationException("Não consegui iniciar o DiskPart.");
                string saida = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                if (!p.WaitForExit(180_000))
                {
                    try { p.Kill(); } catch { }
                    throw new TimeoutException("O DiskPart demorou demais e foi interrompido.");
                }
                return saida;
            }
            finally
            {
                try { File.Delete(arquivo); } catch { }
            }
        }
    }
}
