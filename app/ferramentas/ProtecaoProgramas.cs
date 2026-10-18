using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace ControlIA.Ferramentas
{
    public enum NivelProtecao
    {
        Livre,      // pode desinstalar com confirmação do dono
        Cuidado,    // só no modo bancada
        Protegido   // nunca por aqui
    }

    public sealed class ProgramaInstalado
    {
        public string Nome = "", Fabricante = "", Desinstalador = "", Motivo = "";
        public NivelProtecao Nivel;
    }

    /// <summary>Classifica os programas instalados para a lista de desinstalação não oferecer o que não deve.</summary>
    public static class ProtecaoProgramas
    {
        static readonly string[] Protegidos =
        {
            "microsoft edge", "webview2", "windows", "microsoft .net", ".net runtime", ".net desktop",
            "visual c++", "directx", "controlia", "nexusadmin"
        };
        static readonly string[] Cuidado =
        {
            "driver", "nvidia", "amd ", "intel(r)", "intel ", "realtek", "chipset", "bios", "firmware",
            "antivirus", "defender", "security"
        };

        public static NivelProtecao Classificar(string nome, bool componenteDeSistema, out string motivo)
        {
            string n = (nome ?? "").ToLowerInvariant();
            motivo = "";
            if (componenteDeSistema) { motivo = "Componente de sistema."; return NivelProtecao.Protegido; }
            if (Protegidos.Any(n.Contains)) { motivo = "Faz parte do Windows ou é necessário para outros programas."; return NivelProtecao.Protegido; }
            if (Cuidado.Any(n.Contains)) { motivo = "Driver ou segurança: remover pode quebrar o computador."; return NivelProtecao.Cuidado; }
            return NivelProtecao.Livre;
        }

        public static List<ProgramaInstalado> Listar()
        {
            var lista = new List<ProgramaInstalado>();
            Ler(lista, RegistryHive.LocalMachine, RegistryView.Registry64);
            Ler(lista, RegistryHive.LocalMachine, RegistryView.Registry32);
            Ler(lista, RegistryHive.CurrentUser, RegistryView.Default);
            return lista.GroupBy(p => p.Nome).Select(g => g.First()).OrderBy(p => p.Nome).ToList();
        }

        static void Ler(List<ProgramaInstalado> lista, RegistryHive hive, RegistryView view)
        {
            try
            {
                using var b = RegistryKey.OpenBaseKey(hive, view);
                using var u = b.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                if (u == null) return;
                foreach (string sub in u.GetSubKeyNames())
                {
                    using var k = u.OpenSubKey(sub);
                    if (k == null) continue;
                    string nome = Convert.ToString(k.GetValue("DisplayName")) ?? "";
                    if (string.IsNullOrWhiteSpace(nome)) continue;
                    bool sistema = Convert.ToInt32(k.GetValue("SystemComponent") ?? 0) == 1;
                    var p = new ProgramaInstalado
                    {
                        Nome = nome,
                        Fabricante = Convert.ToString(k.GetValue("Publisher")) ?? "",
                        Desinstalador = Convert.ToString(k.GetValue("UninstallString")) ?? ""
                    };
                    p.Nivel = Classificar(nome, sistema, out p.Motivo);
                    lista.Add(p);
                }
            }
            catch { }
        }

        /// <summary>Decide se a desinstalação pode seguir agora. Lança exceção se não puder.</summary>
        public static void Autorizar(ProgramaInstalado p, bool donoConfirmou, GestorDeModo modo)
        {
            if (p.Nivel == NivelProtecao.Protegido)
                throw new UnauthorizedAccessException($"'{p.Nome}' é protegido. {p.Motivo}");
            if (p.Nivel == NivelProtecao.Cuidado) modo.Exigir(Modo.Bancada);
            if (!donoConfirmou)
                throw new InvalidOperationException("É preciso a confirmação do dono para desinstalar.");
        }
    }
}
