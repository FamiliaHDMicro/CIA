# ControlIA-reparo v2 (modo normal/bancada + ferramentas próprias)

Novidades: Central com 6 botões, limpeza própria, memória, inicialização, programas protegidos, discos/pen drive (bancada).
Envio ao servidor agora vem DESLIGADO e só envia com a sua confirmação a cada vez.

## Como usar (passo a passo)
1. Extraia este zip numa pasta nova (ex.: C:\Users\HDMICRO\Desktop\CIA\CLAUDE\ControlIA-reparo-v2). NÃO misture com a pasta antiga.
2. Dentro da pasta extraída, clique na barra de endereço do Explorador, digite powershell e dê Enter.
3. Digite: dotnet build   e dê Enter.
4. Se aparecer "0 Erro(s)", digite: dotnet run   e dê Enter. (Se o Windows pedir permissão de administrador, aceite.)
5. Se der erro no passo 3, copie o texto do erro e me mande.

## Como testar sem risco
- Limpeza: veja a lista SEM clicar em "Limpar os marcados". Para testar de verdade, marque só "Temporários do usuário".
- Memória e Inicialização: só leem. Nada é alterado.
- Programas: tente selecionar o Microsoft Edge. O botão Desinstalar deve ficar desativado (PROTEGIDO).
- Modo bancada: na primeira vez o programa pede para CRIAR a senha do técnico. Guarde-a.
- Discos e pen drive: NÃO use o botão "Preparar pen drive" agora. Só confira se o disco do Windows aparece como bloqueado.
