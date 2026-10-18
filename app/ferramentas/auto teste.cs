using System;
using System.Linq;
using System.Text;

namespace ControlIA.Ferramentas
{
    /// <summary>Roda tudo em modo SOMENTE LEITURA e devolve um texto. Não apaga, não fecha, não desinstala nada.</summary>
    public static class AutoTeste
    {
        public static string Executar()
        {
            var s = new StringBuilder();
            s.AppendLine("== LIMPEZA (só medir) ==");
            try
            {
                foreach (var i in LimpezaDisco.Varrer())
                    s.AppendLine($"{i.Nome}: {LimpezaDisco.Formatar(i.Bytes)} em {i.Arquivos} arquivos" + (i.ExigeBancada ? " [bancada]" : ""));
            }
            catch (Exception e) { s.AppendLine("Falha: " + e.Message); }

            s.AppendLine("\n== MEMÓRIA ==");
            try
            {
                var m = new DiagnosticoMemoria().Ler();
                s.AppendLine(m.Resumo);
                foreach (var c in m.Maiores) s.AppendLine($"  {c.Nome} x{c.Instancias}: {LimpezaDisco.Formatar(c.Bytes)}");
                foreach (var t in m.Sugestoes) s.AppendLine("  > " + t);
            }
            catch (Exception e) { s.AppendLine("Falha: " + e.Message); }

            s.AppendLine("\n== INICIALIZAÇÃO ==");
            try
            {
                foreach (var e in Inicializacao.Ler())
                    s.AppendLine($"{e.Nome} [{e.Origem}] -> {e.Sugestao}");
            }
            catch (Exception e) { s.AppendLine("Falha: " + e.Message); }

            s.AppendLine("\n== PROGRAMAS (classificação) ==");
            try
            {
                var l = ProtecaoProgramas.Listar();
                s.AppendLine($"Livres: {l.Count(p => p.Nivel == NivelProtecao.Livre)}  |  Cuidado: {l.Count(p => p.Nivel == NivelProtecao.Cuidado)}  |  Protegidos: {l.Count(p => p.Nivel == NivelProtecao.Protegido)}");
                foreach (var p in l.Where(p => p.Nivel == NivelProtecao.Protegido).Take(10)) s.AppendLine("  protegido: " + p.Nome);
            }
            catch (Exception e) { s.AppendLine("Falha: " + e.Message); }

            s.AppendLine("\n== DISCOS ==");
            try
            {
                foreach (var d in DiscoSeguro.Listar())
                    s.AppendLine($"Disco {d.Numero}: {d.Modelo} ({LimpezaDisco.Formatar((long)d.Tamanho)}) removível={d.Removivel} sistema={d.EhDoSistema} letras={string.Join(",", d.Letras)}");
            }
            catch (Exception e) { s.AppendLine("Falha: " + e.Message); }
            return s.ToString();
        }
    }
}
