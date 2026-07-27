# Ronaldinho — Proteção por Barra de Vida

**Versão 2.0.3 — Dibre a concorrência**

A visão geral exibe lado a lado os status das duas janelas. Use os menus laterais para abrir a configuração da janela selecionada.

Nesta versão, a **Configuração guiada** conduz todas as marcações e opções de cada janela. A reação pode parar após o primeiro teleporte ou percorrer uma rota de spots.

Programa portátil para Windows que monitora até duas janelas de forma independente. Ele reconhece a parte vermelha da barra de vida e executa a reação quando a vida restante fica abaixo da porcentagem configurada.

## Requisitos

- Windows 10 versão 2004 ou mais recente, ou Windows 11, 64 bits.
- Executar como administrador para que os cliques funcionem em programas também executados como administrador.
- A janela monitorada pode ficar coberta por outras janelas, mas não pode ficar minimizada.
- Esta distribuição já inclui o .NET necessário; não há instalador.

## Primeira execução

1. Extraia todo o conteúdo do ZIP para uma pasta comum.
2. Abra `Ronaldinho.exe`.
3. Confirme a solicitação de administrador do Windows.
4. Se o SmartScreen avisar que o arquivo não é reconhecido, confira se o arquivo veio de uma fonte confiável. O programa não possui assinatura digital.
5. Abra a janela do jogo ou programa que será monitorado.
6. No ControlarTela, clique em **Atualizar janelas**.
7. Na primeira execução, siga a **Configuração guiada**. Ela também pode ser reaberta pelo menu lateral.

As configurações são salvas somente para o usuário atual em:

`%LOCALAPPDATA%\ControlarTela\config.json`

## Atualizações automáticas

Ao abrir, o programa consulta a versão mais recente publicada em
[`Carvalho3009/ronaldinho-protecao`](https://github.com/Carvalho3009/ronaldinho-protecao/releases).
Quando houver uma versão nova, escolha **Sim** para baixar, substituir o executável e reiniciar automaticamente.
Também é possível usar **Verificar atualização** no topo da tela.

## Configurando cada janela

Repita estes passos nas abas **Janela 1** e **Janela 2** que desejar usar.

### 1. Escolha a janela

1. Clique em **Janela** no menu lateral e selecione a janela correta na lista.
2. Ative **Proteção ativa** para monitorá-la.
3. Use **Segundo plano** para manter a captura ativa com a janela coberta. No momento de cada clique, o Ronaldinho recupera o foco do jogo para garantir o comando.

A proteção pode ser ativada ou desativada individualmente durante a sessão. Desativar uma janela preserva o tempo e a posição atual da sequência; a outra continua funcionando.

### 2. Marque a barra de vida cheia

Este passo precisa ser feito com a vida realmente em 100%.

1. Recupere completamente a vida.
2. Clique em **Barra de vida** no menu lateral e use **Marcar barra**.
3. Arraste o retângulo exatamente sobre a barra, incluindo toda a extensão que o vermelho ocupa quando cheia.
4. Confirme a marcação.
5. Confira se o visualizador mostra **Vida: 100%**.

O programa mede somente a largura vermelha. Números brancos, fundo, moldura e faixas de outras cores são ignorados. Se a leitura estiver incorreta, remarque a barra com a vida cheia.

### 3. Configure o teleporte

1. Clique em **Teleporte** no menu lateral.
2. Use **Marcar Safe** e/ou **Marcar Random** para registrar cada posição.
3. Em **Usar teleporte**, escolha qual deles será clicado quando a vida cair.

### 4. Defina o limite de vida

Em **Reagir com vida abaixo de**, informe a porcentagem de vida restante que dispara a reação.

Exemplo: limite de `40%` executa a reação quando a vida ficar abaixo de aproximadamente `40%`.

## Usando somente o teleporte

Em **Rota de spots**, escolha **Parar após teleporte**. Quando o limite for atingido, o programa:

1. clica no item Safe ou Random escolhido;
2. aguarda o tempo configurado após o teleporte;
3. pausa somente aquela janela.

## Configurando spots

Em **Rota de spots**, escolha **Rotação de spots** e configure:

1. **Menu de spots**: a área visual que confirma que o menu abriu.
2. **Ícone Abrir spots**: o ponto exato onde o programa deve clicar.
3. **Ícone NPC**: o ponto exato onde o programa deve clicar quando **Abrir spots** ainda não aparece.
4. **Botão Teleportar**: o ponto do botão de confirmação.
5. **Botão Auto**: o ponto acionado depois da espera no novo spot.
6. **Marcar Safe**: obrigatório porque, ao terminar todos os ciclos, a próxima reação leva ao Safe e pausa a janela.
7. Os spots ativos, sua ordem e o número de repetições da rota.

Para substituir toda a rota de uma vez, use **Reiniciar spots**. Após a confirmação, clique no primeiro spot; o programa abrirá imediatamente a marcação seguinte. Continue clicando nos spots na ordem desejada e pressione `Esc` para concluir. Se `Esc` for pressionado antes do primeiro ponto, a rota anterior será mantida.

Os spots podem ser ativados ou desativados durante a sessão. A sequência ignora os spots desmarcados.

O fluxo clica no teleporte, aguarda o tempo configurado, procura **Abrir spots**, tenta o **NPC** quando necessário, confirma o fechamento do menu, espera no destino, clica em **Auto** e volta a observar a vida. Se uma etapa exceder as tentativas, somente aquela janela pausa.

Em **Configurações**, todos os limites e tempos do fluxo podem ser ajustados, incluindo tentativas, semelhança visual, espera após o teleporte inicial, espera após NPC, nova tentativa do menu e espera após chegar ao spot.

## Tempo da sessão

No menu lateral **Sessão**, defina as horas e os minutos de cada janela. O contador considera apenas o tempo em que aquela proteção está ativa. Ao terminar, a janela é desativada automaticamente. A visão geral mostra o tempo ativo e o restante no cartão **Sessão**.

## Iniciando

1. Deixe as janelas monitoradas abertas e não minimizadas.
2. Clique em **Iniciar proteção**.
3. Acompanhe na **Visão geral** o estado, o tempo, a vida estimada e a próxima reação das duas janelas.
4. Use **Parar proteção** para interromper todas as janelas.

Se a barra desaparecer ou a captura parar, somente aquela janela entra em **Procurando barra**. O contador e as ações ficam pausados, e uma nova tentativa ocorre a cada 5 segundos. Quando o vermelho reaparecer, a rotina volta automaticamente do ponto em que parou.

## Solução de problemas

### O clique não funciona

- Feche o programa e abra `Ronaldinho.exe` como administrador.
- Confirme que a janela escolhida ainda é a correta.
- Em **Configurações**, use os botões de teste antes de iniciar a proteção.

### A captura não encontra a barra

- Restaure a janela monitorada; ela não pode ficar minimizada.
- Clique em **Atualizar janelas** e selecione novamente a janela correta.
- Remarque somente a barra vermelha com a vida cheia.
- Não use uma marcação que inclua outra área vermelha fora da barra.

### A porcentagem está incorreta

- Recupere a vida até 100% e refaça **Marcar barra de vida**.
- Confira se a seleção cobre toda a largura vermelha cheia.
- Não inicie usando uma configuração copiada de outro computador ou resolução.

### O mouse fica preso no Parsec

O bloqueio é controlado pelo Parsec no computador cliente. Pressione `Ctrl+Alt+Z` no computador original para liberar temporariamente o mouse. `Ctrl+Shift+W` alterna o Parsec para o modo janela.

## Observações

- As posições dependem da resolução, escala e layout de cada janela. Configure novamente ao trocar de computador ou resolução.
- O ZIP não inclui as configurações e posições do computador de quem o criou.
- Verifique as regras e os termos do programa ou jogo monitorado antes de usar automação.
