using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ControlIA.Ferramentas
{
    public enum RiscoLimpeza { Baixo, Medio }

    public sealed class ItemLimpeza
    {
        public string Id { get; init; } = "";
        public string Nome { get; init; } = "";
        public string Explicacao { get; init; } = "";
        public string Pasta { get; init; } = "";
        public string Padrao { get; init; } = "*";       // "*" = recursivo; outro = só nessa pasta
        public int IdadeMinimaDias { get; init; }
        public RiscoLimpeza Risco { get; init; }
        public bool ExigeBancada { get; init; }
        public long Bytes { get; set; }
        public int Arquivos { get; set; }
    }

    public sealed class ResultadoLimpeza
    {
        public long BytesLiberados;
        public int Apagados, Falhas;
        public List<string> Pulados = new List<string>();
    }

    /// <summary>
    /// Limpeza própria da CIA: varre, explica e sugere. Só apaga o que o dono confirmou,
    /// só dentro de pastas conhecidas (lista fixa abaixo) e nunca segue atalhos/links.
    /// </summary>
    public static class LimpezaDisco
    {
        static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public static List<ItemLimpeza> Alvos()
        {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string pd = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return new List<ItemLimpeza>
            {
                new ItemLimpeza { Id="temp-usuario", Nome="Temporários do usuário",
                    Explicacao="Arquivos que programas deixam para trás. Só os com mais de 2 dias.",
                    Pasta=Path.GetTempPath(), IdadeMinimaDias=2, Risco=RiscoLimpeza.Baixo },
                new ItemLimpeza { Id="temp-windows", Nome="Temporários do Windows",
                    Explicacao="Sobras de instalações e atualizações. Só os com mais de 2 dias.",
                    Pasta=Path.Combine(win,"Temp"), IdadeMinimaDias=2, Risco=RiscoLimpeza.Baixo, ExigeBancada=true },
                new ItemLimpeza { Id="miniaturas", Nome="Cache de miniaturas",
                    Explicacao="Imagens pequenas que o Explorer guarda. O Windows recria sozinho.",
                    Pasta=Path.Combine(Local,"Microsoft","Windows","Explorer"), Padrao="thumbcache_*.db", Risco=RiscoLimpeza.Baixo },
                new ItemLimpeza { Id="wer", Nome="Relatórios de erro do Windows",
                    Explicacao="Registros de travamentos antigos enviados à Microsoft.",
                    Pasta=Path.Combine(pd,"Microsoft","Windows","WER"), IdadeMinimaDias=7, Risco=RiscoLimpeza.Baixo, ExigeBancada=true },
                new ItemLimpeza { Id="cache-chrome", Nome="Cache do Google Chrome",
                    Explicacao="Páginas e imagens guardadas para abrir mais rápido. Não apaga senhas nem histórico.",
                    Pasta=Path.Combine(Local,"Google","Chrome","User Data","Default","Cache","Cache_Data"), Risco=RiscoLimpeza.Baixo },
                new ItemLimpeza { Id="cache-edge", Nome="Cache do Microsoft Edge",
                    Explicacao="Páginas e imagens guardadas para abrir mais rápido. Não apaga senhas nem histórico.",
                    Pasta=Path.Combine(Local,"Microsoft","Edge","User Data","Default","Cache","Cache_Data"), Risco=RiscoLimpeza.Baixo },
                new ItemLimpeza { Id="winupdate", Nome="Cache do Windows Update",
                    Explicacao="Pacotes de atualização já baixados. Limpeza profunda: o Windows baixa de novo se precisar.",
                    Pasta=Path.Combine(win,"SoftwareDistribution","Download"), Risco=RiscoLimpeza.Medio, ExigeBancada=true },
            };
        }

        static List<FileInfo> Arquivos(ItemLimpeza a)
        {
            var resultado = new List<FileInfo>();
            try
            {
                if (!Directory.Exists(a.Pasta)) return resultado;
                var opt = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                    RecurseSubdirectories = a.Padrao == "*"
                };
                DateTime limite = DateTime.Now.AddDays(-a.IdadeMinimaDias);
                foreach (var f in new DirectoryInfo(a.Pasta).EnumerateFiles(a.Padrao, opt))
                {
                    try { if (f.LastWriteTime <= limite) resultado.Add(f); } catch { }
                }
            }
            catch { /* pasta sumiu ou sem permissão: devolve o que deu */ }
            return resultado;
        }

        /// <summary>Só mede. Não apaga nada.</summary>
        public static List<ItemLimpeza> Varrer(CancellationToken ct = default)
        {
            var alvos = Alvos();
            foreach (var a in alvos)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var f in Arquivos(a))
                {
                    ct.ThrowIfCancellationRequested();
                    try { a.Bytes += f.Length; a.Arquivos++; } catch { }
                }
            }
            return alvos.Where(a => a.Arquivos > 0).OrderByDescending(a => a.Bytes).ToList();
        }

        /// <summary>
        /// Apaga os itens escolhidos (por Id). Exige confirmação do dono (donoConfirmou=true,
        /// vindo de uma caixa de diálogo) e respeita o modo bancada nos itens profundos.
        /// Os caminhos vêm da lista fixa Alvos(), nunca do chamador.
        /// </summary>
        public static ResultadoLimpeza Apagar(IEnumerable<string> ids, bool donoConfirmou, GestorDeModo modo)
        {
            if (!donoConfirmou)
                throw new InvalidOperationException("A CIA nunca apaga sozinha: é preciso a confirmação do dono.");
            var r = new ResultadoLimpeza();
            var mapa = Alvos().ToDictionary(a => a.Id);
            foreach (string id in ids.Distinct())
            {
                if (!mapa.TryGetValue(id, out var a)) { r.Pulados.Add($"{id}: item desconhecido"); continue; }
                if (a.ExigeBancada && modo.Atual != Modo.Bancada)
                { r.Pulados.Add($"{a.Nome}: só no modo bancada"); continue; }
                foreach (var f in Arquivos(a))
                {
                    try { long t = f.Length; f.Delete(); r.BytesLiberados += t; r.Apagados++; }
                    catch { r.Falhas++; }
                }
            }
            return r;
        }

        public static string Formatar(long bytes)
        {
            string[] u = { "B", "KB", "MB", "GB", "TB" };
            double v = bytes; int i = 0;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return $"{v:0.#} {u[i]}";
        }
    }
}
