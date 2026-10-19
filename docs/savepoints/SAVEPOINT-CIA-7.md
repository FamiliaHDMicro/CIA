# SAVEPOINT CIA 7 — 13/10/2026 (complementa os savepoints 3, 4, 5 e 6)

## Decisões e entendimentos
- Luis prefere que Claude execute direto (vale para este trabalho). Instruções sempre passo a passo, com cliques exatos.
- O PC do Luis tem o .NET 10 (não o 8). O projeto mira `net10.0-windows`.
- O ControlIA pede Administrador (app.manifest). `dotnet run` num PowerShell comum falha com "A operação solicitada requer elevação". Para abrir: dois cliques em `bin\Debug\net10.0-windows\ControlIA.exe` e clicar em Sim.
- A base de trabalho é sempre o v2/v3. Os arquivos soltos do projeto pequeno (UserSettings com envio ligado) são de uma versão antiga; não usar.

## Entregas da rodada 7
1. Tela de licença (`LicencaWindow`): campo da chave (17 caracteres), confere formato, valida no servidor via LicenseManager.
2. Botão "Licença (digitar a chave)" na Central.
3. ControlIA-reparo-v3 = v2 + tela de licença + csproj corrigido (System.Management 10.0.2, alvo net10.0-windows).

## Estado testado no PC do Luis
- `dotnet build`: 0 erros (resta o aviso NU1510, inofensivo).
- Central abre em "Modo NORMAL" com 6 botões + Licença.
- Teste 1 da licença passou (chave errada recusada com a mensagem certa).
- 18/10/2026: Teste 3 (Meus sinais vitais) passou. Temperatura de CPU/placa-mãe "não consegui ler" e chipset "não identificado" ficam como melhoria futura.

## Servidor (Cloudflare)
- Worker `control-ia.hdmicro-ml.workers.dev`; D1 `uso_telemetria`; painel `/admin`.
- Licença antiga `[licença antiga removida]` está em formato velho: não funciona, vence em 01/11/2026.
- 18/10/2026: o painel recusava a senha porque o código exigia 12+ caracteres. A constante `SENHA_MINIMA` agora é 8 (em `worker/worker.js`; precisa ser colada no Cloudflare para valer).

## Pendências de Luis
1. Colar o `worker/worker.js` novo no Cloudflare e redefinir o segredo `ADMIN_TOKEN` (8+ caracteres).
2. Teste 2 da licença: gerar licença no `/admin`, ativar na tela de licença.
3. Teste 4: "Programas instalados" (Edge deve aparecer PROTEGIDO).
4. Teste 5: "Limpeza do disco" só em "Temporários do usuário".
5. Não clicar em "Preparar pen drive" nem "Organizar Downloads" por enquanto.
6. Gerar o instalador (`GerarInstalador.bat`) depois que Claude ajustar para .NET 10.
7. Desinstalar o ControlIA NexusAdmin 1.0 antes de instalar o novo.
8. Apagar a linha de teste: `DELETE FROM uso_telemetria WHERE tecnico_nome = 'TESTE';`

## Pendências de Claude
1. Ajustar `ControlIA.iss` / `GerarInstalador.bat` para .NET 10 (caminho `net10.0-windows`; embutir ou exigir o runtime). Precisa receber os dois arquivos.
2. Corrigir erros dos testes 2 a 5.
3. Mensagem da tela de licença separar "chave recusada" de "sem internet" (muda o retorno do LicenseManager).
4. Reconhecimento das peças (nome e chipset), cobrança de troca, diário e voz da CIA.
5. Cópia em quantidade dos pen drives de reprodução comercial.
6. Camadas 2 a 4 da caixa de areia e worker de testes separado.
7. Worker: licenças no D1 em vez do KV, limite de tentativas de senha e de requisições, Cron Trigger diário.

## Observações
- A senha do técnico (modo bancada) fica só como hash em `C:\ProgramData\ControlIA\senha.dat`.
- Configurações da máquina: `%LocalAppData%\ControlIA\configuracoes.json` (inclui a chave de licença).
