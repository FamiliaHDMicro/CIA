using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ControlIA.Ferramentas
{
    public sealed class ConsumoProcesso
    {
        public string Nome = "";
        public int Instancias;
        public long Bytes;
        public double CrescimentoMB;   // desde a leitura anterior
    }

    public sealed class RelatorioMemoria
    {
        public ulong TotalBytes, LivreBytes;
        public int PercentualUso;
        public string Resumo = "";
        public List<ConsumoProcesso> Maiores = new List<ConsumoProcesso>();
        public List<string> Sugestoes = new List<string>();
    }

    /// <summary>
    /// Substitui o "Liberar RAM": mostra quem consome e por quê. Não fecha nada, não esvazia nada.
    /// Chame Ler() de tempos em tempos: a partir da 2ª leitura ela aponta quem está crescendo.
    /// </summary>
    public sealed class DiagnosticoMemoria
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX
        {
            public uint dwLength; public uint dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile;
            public ulong ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);

        Dictionary<string, long> _anterior = new Dictionary<string, long>();

        public RelatorioMemoria Ler(int quantos = 8)
        {
            var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (!GlobalMemoryStatusEx(ref m))
                return new RelatorioMemoria { Resumo = "Não consegui ler a memória do computador." };

            var porNome = new Dictionary<string, ConsumoProcesso>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    long ws = p.WorkingSet64;
                    if (!porNome.TryGetValue(p.ProcessName, out var c))
                        porNome[p.ProcessName] = c = new ConsumoProcesso { Nome = p.ProcessName };
                    c.Instancias++; c.Bytes += ws;
                }
                catch { /* processo protegido ou já encerrado: ignora */ }
                finally { p.Dispose(); }
            }
            foreach (var c in porNome.Values)
                if (_anterior.TryGetValue(c.Nome, out long antes))
                    c.CrescimentoMB = (c.Bytes - antes) / 1048576.0;
            _anterior = porNome.ToDictionary(k => k.Key, v => v.Value.Bytes, StringComparer.OrdinalIgnoreCase);

            var r = new RelatorioMemoria
            {
                TotalBytes = m.ullTotalPhys, LivreBytes = m.ullAvailPhys, PercentualUso = (int)m.dwMemoryLoad,
                Maiores = porNome.Values.OrderByDescending(c => c.Bytes).Take(quantos).ToList()
            };
            r.Resumo = $"Memória em uso: {r.PercentualUso}% ({LimpezaDisco.Formatar((long)(r.TotalBytes - r.LivreBytes))} de {LimpezaDisco.Formatar((long)r.TotalBytes)}).";

            if (r.PercentualUso >= 85)
                r.Sugestoes.Add("A memória está muito cheia. Feche o que não está usando; se for sempre assim, a máquina precisa de mais RAM.");
            else if (r.PercentualUso >= 70)
                r.Sugestoes.Add("A memória está alta, mas ainda dá conta. Vale ficar de olho.");
            else
                r.Sugestoes.Add("Memória em situação normal. Não há nada para liberar.");

            foreach (var c in r.Maiores)
            {
                if (c.CrescimentoMB >= 200)
                    r.Sugestoes.Add($"{c.Nome} cresceu {c.CrescimentoMB:0} MB desde a última leitura. Se continuar subindo, pode estar com defeito.");
                if (c.Instancias >= 8 && c.Bytes > 1_500_000_000)
                    r.Sugestoes.Add($"{c.Nome} está com {c.Instancias} processos usando {LimpezaDisco.Formatar(c.Bytes)} juntos (comum em navegador com muitas abas).");
            }
            return r;
        }
    }
}
