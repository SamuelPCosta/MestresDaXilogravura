# Projeto Mestres da xilogravura
## Explicando o projeto
# XiloVR - Experience Mechanics & Workflow

## 1. Visão Geral da Experiência
O **XiloVR** é uma experiência de realidade virtual imersiva desenvolvido para o Museu Câmara Cascudo (UFRN) com o objetivo de preservar, ensinar e simular o processo tradicional de xilogravura nordestina. A experiência mapeia interações físicas do mundo real para o ambiente virtual utilizando os *controllers*, reproduzindo cada etapa artesanal.

---

## 2. Sistema de Interação e Gerenciamento de Ferramentas (*Tool Handling*)
O núcleo da mecânica baseia-se na seleção, empunhadura e troca dinâmica de ferramentas tridimensionais:

* **Empunhadura (Grip):** Ao pressionar o botão de *Grip* do controle, a ferramenta selecionada (goiva, rolo de tinta, folha de papel) é afixada à âncora da mão (*Hand Anchor*).
* **Descarte:** O usuário pode soltar as ferramentas sobre as superfícies de trabalho, disparando eventos de física para repousá-las na bancada.

---

## 3. Etapas do Processo Produtivo (*Pipeline*)

### Etapa 1: Preparação e Entintagem da Matriz
1. **Escolha da Arte:** O usuário seleciona uma arte de referência.
2. **Transferência do Rascunho:** O usuário pega o lápis e transfere o desenho para a matriz.
3. **Entalhe:** O usuário pega a goiva e entalha a madeira.
4. **Acabamento:** O usuário pega a lixa e lixa a matriz esculpida.
5. **Derramamento de Tinta:** O usuário pega o pote de tinta e derrama no acrílico.
6. **Carga do Rolo:** O usuário pega o rolo de tinta e passa no acrílico (um shader dinâmico atualiza a textura do rolo para indicar que ele está carregado).
7. **Aplicação na Matriz:** O usuário movimenta o rolo uniformemente sobre a matriz de madeira. A fricção transfere a tinta para os relevos da matriz, calculados via shader.

### Etapa 2: Posicionamento do Papel
1. **Seleção da Folha:** O usuário pega uma folha de papel na pilha posicionada ao lado da bancada.
2. **Posicionamento:** A folha é alinhada e colocada sobre a matriz entintada.

### Etapa 3: Impressão e Prensagem
1. **Ferramenta de Prensagem:** O usuário pega o barém.
2. **Fricção:** O movimento de esfregar sobre o dorso do papel transfere a tinta das áreas em relevo da matriz para o papel. O sistema rastreia a área percorrida para revelar progressivamente a imagem impressa.
3. **Revelação:** Conforme a pressão é aplicada, a textura final da xilogravura vai se tornando visível.

### Etapa 4: Remoção e Exposição
1. **Finalização:** O processo de impressão é concluído.
2. **Secagem e Exibição:** A obra concluída é colocada no varal de secagem virtual.

## Diretrizes de Código
- Estamos utilizando Unity 6.
- Escreva funções pequenas e focadas.
- Trate erros de forma explícita com blocos try/catch.
- Utilize o log para mostrar o progresso.
- Não adicione comentários óbvios no código; deixe o código autoexplicativo.
- Escreva comentários curtos no começo das funcoes explicando o fluxo geral.