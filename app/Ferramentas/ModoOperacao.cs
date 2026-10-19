using System;
using System.IO;
using System.Security.Cryptography;

namespace ControlIA.Ferramentas
{
    public enum Modo { Normal, Bancada }

    /// <summary>
    /// Controla os dois modos da CIA. Normal = só avisa e sugere.
    /// Bancada = uso técnico, liberado só com a senha do técnico instalador.
    /// A senha nunca é guardada em texto: só um hash PBKDF2 com sal.
    /// </summary>
    public sealed class GestorDeModo
    {
        const int Iteracoes = 200_000;
        const int MaxTentativas = 5;
        static readonly TimeSpan Bloqueio = TimeSpan.FromMinutes(5);
        static readonly TimeSpan TempoMaximoBancada = TimeSpan.FromMinutes(15);

        readonly string _arquivo;
        Modo _modo = Modo.Normal;
        DateTime _entrou;
        int _erros;
        DateTime _bloqueadoAte = DateTime.MinValue;

        public GestorDeModo(string? arquivoSenha = null)
        {
            _arquivo = arquivoSenha ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ControlIA", "senha.dat");
        }

        public bool SenhaDefinida => File.Exists(_arquivo);

        /// <summary>Modo atual. A bancada se desliga sozinha após 15 minutos.</summary>
        public Modo Atual
        {
            get
            {
                if (_modo == Modo.Bancada && DateTime.UtcNow - _entrou > TempoMaximoBancada)
                    _modo = Modo.Normal;
                return _modo;
            }
        }

        public void DefinirSenha(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6)
                throw new ArgumentException("A senha precisa ter pelo menos 6 caracteres.");
            byte[] sal = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(senha, sal, Iteracoes, HashAlgorithmName.SHA256, 32);
            Directory.CreateDirectory(Path.GetDirectoryName(_arquivo)!);
            File.WriteAllText(_arquivo, $"{Iteracoes}:{Convert.ToBase64String(sal)}:{Convert.ToBase64String(hash)}");
        }

        /// <summary>Tenta entrar na bancada. Devolve true se a senha confere.</summary>
        public bool EntrarBancada(string? senha, out string mensagem)
        {
            mensagem = "";
            if (!SenhaDefinida) { mensagem = "Nenhuma senha de técnico foi definida ainda."; return false; }
            if (DateTime.UtcNow < _bloqueadoAte)
            {
                mensagem = $"Muitas tentativas. Tente de novo após {_bloqueadoAte.ToLocalTime():HH:mm}.";
                return false;
            }
            string[] p = File.ReadAllText(_arquivo).Trim().Split(':');
            bool ok = false;
            if (p.Length == 3 && int.TryParse(p[0], out int it))
            {
                byte[] sal = Convert.FromBase64String(p[1]);
                byte[] esperado = Convert.FromBase64String(p[2]);
                byte[] calc = Rfc2898DeriveBytes.Pbkdf2(senha ?? "", sal, it, HashAlgorithmName.SHA256, esperado.Length);
                ok = CryptographicOperations.FixedTimeEquals(calc, esperado);
            }
            if (ok)
            {
                _erros = 0; _modo = Modo.Bancada; _entrou = DateTime.UtcNow;
                mensagem = "Modo bancada ativado (desliga sozinho em 15 minutos).";
                return true;
            }
            _erros++;
            if (_erros >= MaxTentativas) { _bloqueadoAte = DateTime.UtcNow + Bloqueio; _erros = 0; }
            mensagem = "Senha incorreta.";
            return false;
        }

        public void SairBancada() { _modo = Modo.Normal; }

        public void Exigir(Modo necessario)
        {
            if (necessario == Modo.Bancada && Atual != Modo.Bancada)
                throw new UnauthorizedAccessException("Esta ação só é liberada no modo bancada, com a senha do técnico.");
        }
    }
}
