# CIA — ControlIA

A CIA é a assistente que vive dentro do PC: sente as peças, avisa e sugere. Ela só avisa e sugere;
não apaga, não fecha e não desliga nada sozinha.

## Estrutura
| Pasta | O que tem |
|---|---|
| `app/` | Programa para Windows (C# / WPF, .NET 10). Abre como Administrador. |
| `worker/` | Servidor na Cloudflare: licenças, avisos e painel do dono. |
| `docs/` | Savepoints, pendências e anotações de projeto. |

## Compilar o app (no PC)
1. Instale o .NET 10 SDK.
2. Abra o PowerShell **como Administrador**, entre na pasta `app` e rode `dotnet build`.
3. Abra `app\bin\Debug\net10.0-windows\ControlIA.exe` (clique em Sim no aviso do Administrador).

## Regras do repositório
- Nenhuma senha, chave de licença real ou token entra aqui (veja `.gitignore`).
- O envio de dados ao servidor vem **desligado** e só acontece com confirmação a cada vez.
