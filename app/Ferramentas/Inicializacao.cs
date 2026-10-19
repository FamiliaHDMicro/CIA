using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace ControlIA.Ferramentas
{
    public sealed class EntradaInicializacao
    {
        public string Nome = "", Comando = "", Origem = "", Sugestao = "";
        public bool ArquivoExiste;
    }

    /// <summary>
    /// Leitura real do que abre junto com o Windows. Só lê e sugere; no modo normal a CIA nunca desativa nada.
    /// </summary>
    public static class Inicializacao
    {
        const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static List<EntradaInicializacao> Ler()
        {
            var lista = new List<EntradaInicializacao>();
            LerChave(lista, RegistryHive.CurrentUser, RegistryView.Default, "Usuário (registro)");
            LerChave(lista, RegistryHive.LocalMachine, RegistryView.Registry64, "Todos os usuários (registro)");
            LerChave(lista, RegistryHive.LocalMachine, RegistryView.Registry32, "Todos os usuários (32 bits)");
            LerPasta(lista, Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Pasta Inicializar do usuário");
            LerPasta(lista, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Pasta Inicializar comum");
            return lista;
        }

        static void LerChave(List<EntradaInicializacao> lista, RegistryHive hive, RegistryView view, string origem)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var k = baseKey.OpenSubKey(Run);
                if (k == null) return;
                foreach (string nome in k.GetValueNames())
                {
                    string cmd = Convert.ToString(k.GetValue(nome)) ?? "";
                    lista.Add(Avaliar(nome, cmd, origem));
                }
            }
            catch { /* sem permissão para esta chave: segue */ }
        }

        static void LerPasta(List<EntradaInicializacao> lista, string pasta, string origem)
        {
            try
            {
                if (!Directory.Exists(pasta)) return;
                foreach (string f in Directory.GetFiles(pasta))
                {
                    if (Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                    lista.Add(Avaliar(Path.GetFileNameWithoutExtension(f), f, origem));
                }
            }
            catch { }
        }

        static string ExtrairExe(string cmd)
        {
            cmd = Environment.ExpandEnvironmentVariables(cmd.Trim());
            if (cmd.StartsWith("\""))
            {
                int fim = cmd.IndexOf('"', 1);
                return fim > 1 ? cmd.Substring(1, fim - 1) : cmd.Trim('"');
            }
            int i = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            return i >= 0 ? cmd.Substring(0, i + 4) : cmd;
        }

        static EntradaInicializacao Avaliar(string nome, string cmd, string origem)
        {
            string exe = ExtrairExe(cmd);
            bool existe = false;
            try { existe = File.Exists(exe); } catch { }
            var e = new EntradaInicializacao { Nome = nome, Comando = cmd, Origem = origem, ArquivoExiste = existe };
            string baixo = exe.ToLowerInvariant();

            if (!existe)
                e.Sugestao = "Aponta para um arquivo que não existe mais. Provavelmente é resto de programa removido e pode sair da lista.";
            else if (baixo.Contains(@"\temp\") || baixo.Contains(@"\appdata\local\temp"))
                e.Sugestao = "Abre a partir de uma pasta temporária. É incomum: vale conferir com o técnico.";
            else if (baixo.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows).ToLowerInvariant()))
                e.Sugestao = "Item do próprio Windows. Mantenha.";
            else
                e.Sugestao = "Programa instalado. Se você não usa no dia a dia, pode tirar da inicialização para o PC ligar mais rápido.";
            return e;
        }
    }
}
