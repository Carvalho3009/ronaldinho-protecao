# Ronaldinho — Proteção por Barra de Vida

**Versão 1.1.0 — Dibre a concorrência**

Programa portátil para Windows que monitora até duas janelas de forma independente. Ele reconhece a parte vermelha da barra de vida e pode executar uma sequência de teleporte e spots quando a vida cair além do limite configurado.

## Requisitos

- Windows 10 versão 2004 ou mais recente, ou Windows 11, 64 bits.
- Executar como administrador para que os cliques funcionem em programas também executados como administrador.
- A janela monitorada pode ficar coberta por outras janelas, mas não pode ficar minimizada.
- Esta distribuição já inclui o .NET necessário; não há instalador.

## Primeira execução

1. Extraia todo o conteúdo do ZIP para uma pasta comum.
2. Abra `ControlarTela.exe`.
3. Confirme a solicitação de administrador do Windows.
4. Se o SmartScreen avisar que o arquivo não é reconhecido, confira se o arquivo veio de uma fonte confiável. O programa não possui assinatura digital.
5. Abra a janela do jogo ou programa que será monitorado.
6. No ControlarTela, clique em **Atualizar janelas**.

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

1. Selecione a janela correta na lista.
2. Mantenha **Proteção ativa nesta janela** marcada para monitorá-la.
3. Use **Executar em segundo plano** para não mover o mouse nem entregar o foco ao jogo.

A proteção pode ser ativada ou desativada individualmente durante a sessão. Desativar uma janela preserva o tempo e a posição atual da sequência; a outra continua funcionando.

### 2. Marque a barra de vida cheia

Este passo precisa ser feito com a vida realmente em 100%.

1. Recupere completamente a vida.
2. Clique em **Marcar barra de vida**.
3. Arraste o retângulo exatamente sobre a barra, incluindo toda a extensão que o vermelho ocupa quando cheia.
4. Confirme a marcação.
5. Confira se o visualizador mostra **Vida: 100%**.

O programa mede somente a largura vermelha. Números brancos, fundo, moldura e faixas de outras cores são ignorados. Se a leitura estiver incorreta, remarque a barra com a vida cheia.

### 3. Marque o item de teleporte

1. Clique em **Marcar item de teleporte**.
2. Clique na posição do item que deve ser usado quando a vida cair.

### 4. Defina o limite

Em **Reagir quando a vida cair pelo menos**, informe a queda permitida.

Exemplo: limite de `40%` executa a reação quando a vida chegar aproximadamente a `60%` ou menos.

## Usando somente o teleporte

Desmarque **Após teleportar, escolher um spot**. Quando o limite for atingido, o programa clicará somente no item de teleporte.

## Configurando spots

Se quiser escolher um spot após usar o item de teleporte:

1. Marque **Após teleportar, escolher um spot**.
2. Abra manualmente a janela de spots no jogo.
3. Clique em **Marcar janela de spots** e selecione a área usada para reconhecer essa janela.
4. Clique em **Marcar botão Abrir spots** e marque o botão que abre o menu.
5. Clique em **Marcar botão Teleportar** e marque o botão de confirmação.
6. Use **Adicionar spot** para cadastrar cada posição desejada.
7. Marque na lista somente os spots que participarão da sequência.
8. Defina quantas vezes a lista completa será repetida.

Os spots podem ser ativados ou desativados durante a sessão. A sequência ignora os spots desmarcados.

Em **Opções avançadas**, ajuste **Tentativas no botão Teleportar**. Após cada clique, o programa verifica se a janela de spots fechou. Se ela continuar aberta por stagger ou lag, o botão é acionado novamente usando o **Intervalo entre cliques**. Se todas as tentativas falharem, a proteção daquela janela pausa sem avançar o spot ou o ciclo.

## Tempo da sessão

Defina o limite de horas e minutos para cada janela. O contador considera apenas o tempo em que aquela proteção está ativa. Ao terminar, a janela é desativada automaticamente.

## Iniciando

1. Deixe as janelas monitoradas abertas e não minimizadas.
2. Clique em **Iniciar proteção**.
3. Acompanhe em cada aba o estado, o tempo, a vida estimada e o próximo spot.
4. Use **Parar proteção** para interromper todas as janelas.

Se a barra desaparecer ou a captura parar, somente aquela janela entra em **Procurando barra**. O contador e as ações ficam pausados, e uma nova tentativa ocorre a cada 5 segundos. Quando o vermelho reaparecer, a rotina volta automaticamente do ponto em que parou.

## Solução de problemas

### O clique não funciona

- Feche o programa e abra `ControlarTela.exe` como administrador.
- Confirme que a janela escolhida ainda é a correta.
- Use os botões de teste antes de iniciar a proteção.

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
