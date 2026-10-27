# XiloVR — Requisitos (Spec)

> Documento 1/3 do Spec-Driven Development. Define **o quê** o protótipo deve fazer e **como validamos**.
> Próximos: [design.md](design.md) (como) → [tasks.md](tasks.md) (plano de execução).

**Status:** rascunho para revisão · **Data:** 2026-10-27

---

## 1. Contexto e objetivo

O XiloVR é uma experiência VR para o Museu Câmara Cascudo (UFRN) que simula o processo tradicional da xilogravura nordestina. Este protótipo atualiza o projeto existente para uma experiência **100% VR, com controles**, com pipeline completo (ainda que simplificado) e arquitetura modular.

### 1.1 Escopo desta rodada
- Pipeline completo e **simplificado**, da escolha da arte até o varal.
- Abordagem **híbrida**: reaproveitar o que já funciona (shaders, partículas, sons, modelos, pintura em RenderTexture), reescrevendo a orquestração com uma arquitetura nova.
- Alvo: **Meta Quest standalone (Android)**. Desenvolvimento e testes principalmente via **XR Interaction Simulator** no PC (não há headset disponível).
- Interação via **controles** (não hand tracking).

### 1.2 Fora de escopo
- Qualquer coisa de OpenCV, tracking por UDP, projeção, AR, Vuforia, joystick/ESP.
- Escolha de arte em formato de "caderno com páginas" (rodada futura).
- Modo de arte autoral (desenho livre). Os shaders expõem `autoral`/`mascaraArteAutoral`, mas o protótipo usa sempre a arte escolhida (`autoral = 0`).
- Locomoção (o usuário fica parado diante da bancada).
- **Animações / movimento de objetos.** Previsto para ainda esta semana (movimentos simples); nesta rodada só deixamos ganchos (§4.8).
- **Design visual dos painéis.** Os painéis são placeholders funcionais; a equipe faz o design à mão depois.
- Entrega ao visitante (QR code / link): decidimos depois (§8).
- Alterar shaders, shadergraphs ou sistemas de partículas.

### 1.3 Restrições fixas
| ID | Restrição |
|----|-----------|
| C-1 | Unity 6, URP 17.3, XR Interaction Toolkit 3.3.2, OpenXR 1.16. |
| C-2 | **Nenhum shader/shadergraph/partícula existente pode ser modificado.** O código novo só escreve nas propriedades que eles já expõem (§6). |
| C-3 | O resultado visual segue a arte escolhida (comportamento atual dos shaders). |
| C-4 | Tudo precisa ser testável no XR Interaction Simulator, sem headset. |
| C-5 | Diretrizes do CLAUDE.md: funções pequenas, try/catch explícito, logs de progresso, comentário curto no topo das funções, nada de comentários óbvios. |
| C-6 | **Código e nomenclatura em inglês:** classes, métodos, variáveis, namespaces, enums, nomes de assets/prefabs/pastas novos e logs. Documentação e textos de UI em português. |

---

## 2. Glossário

| Termo | Significado | Nome no código |
|-------|-------------|----------------|
| Matriz | Bloco de madeira que recebe desenho, entalhe, lixa e tinta. | `Matrix` |
| Acrílico (vidro) | Superfície onde a tinta é derramada e o rolo é carregado. | `Glass` |
| Goiva | Ferramenta de entalhe. | `Gouge` |
| Lixa | Ferramenta de acabamento. | `Sandpaper` |
| Pote de tinta | Recipiente usado para derramar tinta. | `InkPot` |
| Rolo | Rolo de tinta. | `Roller` |
| Barém | Ferramenta de fricção usada para imprimir no papel. | `Baren` |
| Máscara | RenderTexture onde o progresso de uma etapa é pintado e lido pelos shaders. | `Mask` |
| Cobertura | % da área-alvo de uma etapa já pintada na máscara. | `Coverage` |
| Ponta | Transform na extremidade da ferramenta usado para detectar contato. | `Tip` |
| Âncora do controle | Ponto de encaixe do controle XR onde a ferramenta fica presa. | `AttachTransform` |
| Varal | Exibição das gravuras salvas no lobby (Menu). | `DryingLine` |
| Gravura | Impressão final salva. | `Print` |

---

## 3. Fluxo da experiência

```
Menu (lobby + varal) ──Iniciar──▶ Room (oficina)
  Room: Escolha da arte → Lápis → Goiva → Lixa → Derramar tinta → Carregar rolo
        → Entintar matriz → Posicionar papel → Barém → Revelação → Salvar?
  ──Finalizar──▶ Menu (varal agora inclui a nova gravura)
```

| # | Etapa (`StepId`) | Ferramenta | Superfície | Máscara / efeito |
|---|------------------|------------|------------|------------------|
| 0 | `ArtSelection` | — (UI) | Painel | Define `SketchTexture` na matriz e no papel |
| 1 | `Sketching` | Lápis | Matriz | `SketchMask` |
| 2 | `Carving` | Goiva | Matriz | `SculptMask` + partícula de lascas |
| 3 | `Sanding` | Lixa | Matriz | `SandpaperMask` + partícula de pó |
| 4 | `InkPouring` | Pote de tinta | Acrílico | `InkMask` (acrílico) |
| 5 | `RollerLoading` | Rolo | Acrílico | `isTinta` / `inkLevel` no rolo |
| 6 | `MatrixInking` | Rolo | Matriz | `PaintMask` |
| 7 | `PaperPlacement` | Folha de papel | Matriz (socket) | Papel encaixa sobre a matriz |
| 8 | `Pressing` | Barém | Papel | `PrintMask` (papel) |
| 9 | `Reveal` | — | Papel | Gravura final visível + opção de salvar |

---

## 4. Requisitos funcionais

Formato: **REQ-ID** e critérios de aceite (AC) no padrão *QUANDO / ENTÃO*.

### 4.1 Ferramentas e interação (TOOL)

**REQ-TOOL-1 — Empunhadura**
- AC1: QUANDO o usuário aponta/encosta o controle numa ferramenta e pressiona **Grip**, ENTÃO a ferramenta fica presa à âncora do controle (XRGrabInteractable).
- AC2: QUANDO solta o Grip, ENTÃO a ferramenta é liberada com física e repousa na bancada.
- AC3: QUANDO uma ferramenta cai fora da área útil (chão ou fora do alcance), ENTÃO volta à sua posição de origem na bancada em até 2 s.

**REQ-TOOL-2 — Aplicação por contato da ponta**
- AC1: Cada ferramenta tem uma ponta (`tip`). QUANDO a ponta está a ≤ `contactDistance` (padrão **1,5 cm**) de uma superfície pintável, ENTÃO o efeito é aplicado na coordenada UV do ponto de contato.
- AC2: Movimentos contínuos geram traço contínuo, interpolando entre amostras.
- AC3: QUANDO a ponta se afasta da superfície, ENTÃO o traço é interrompido e o som para.

**REQ-TOOL-3 — Ferramenta válida por etapa**
- AC1: Só a ferramenta da etapa atual produz efeito; as demais podem ser pegas, mas não pintam.
- AC2: A ferramenta da etapa atual recebe **destaque (outline)** com o `Outline.shadergraph` existente.
- AC3: QUANDO o usuário usa a ferramenta errada sobre uma superfície, ENTÃO nada é pintado e um log de debug registra a tentativa.

**REQ-TOOL-4 — Feedback**
- AC1: Enquanto pinta, a ferramenta toca seu `AudioSource` em loop; para ao interromper o contato.
- AC2: Goiva emite `lascasDeMadeira` e lixa emite `poDeMadeira` no ponto de contato enquanto pintam.
- AC3: Vibração (haptic) opcional no controle enquanto pinta.

### 4.2 Etapas e máquina de estados (STEP)

**REQ-STEP-1 — Fluxo linear guiado**
- AC1: A experiência é uma máquina de estados linear com as etapas da §3.
- AC2: Cada etapa é configurada por um asset (ScriptableObject): ferramenta, superfície, máscara, pincel, cobertura mínima, texto de instrução e áudio.

**REQ-STEP-2 — Cobertura como feedback, avanço manual**
- AC1: Nas etapas de pintura, a % de cobertura da área-alvo é calculada e exibida no painel da etapa.
- AC2: O botão **Próximo** só fica habilitado quando a cobertura ≥ `minCoverage` da etapa (configurável no Inspector).
- AC3: O avanço só acontece quando o usuário aperta **Próximo**; nunca é automático.
- AC4: Nas etapas sem pintura (`PaperPlacement`, `RollerLoading`), Próximo é habilitado ao cumprir a condição da etapa (folha encaixada / rolo carregado).

**REQ-STEP-3 — Voltar etapa**
- AC1: Existe o botão **Voltar**. QUANDO pressionado, ENTÃO o **progresso da etapa atual é limpo** (máscara/estado) e a etapa anterior volta a ser a atual, com sua ferramenta reativada e destacada.
- AC2: O progresso da etapa anterior é **preservado** (o usuário continua de onde parou).
- AC3: Voltar está desabilitado em `ArtSelection`.
- AC4: O que é limpo ao sair de cada etapa via Voltar:
  | Etapa que está sendo deixada | Limpa |
  |---|---|
  | Sketching / Carving / Sanding / MatrixInking | a máscara dela |
  | InkPouring | `InkMask` |
  | RollerLoading | tinta do rolo (`isTinta = 0`) |
  | PaperPlacement | papel sai do socket e volta para a pilha |
  | Pressing | `PrintMask` |
  | Reveal | nada (volta para Pressing) |

**REQ-STEP-4 — Reiniciar**
- AC1: O botão **Reiniciar** limpa todas as máscaras, reseta rolo e papel, devolve ferramentas às posições originais e volta para `ArtSelection`.

### 4.3 Escolha da arte (ART)

**REQ-ART-1 — Painel world-space**
- AC1: Um painel world-space mostra a arte atual com botões **◀ / ▶** para navegar e **Escolher** para confirmar, usando o `Assets/Prefabs/Button.prefab` existente.
- AC2: O catálogo inclui **todas** as artes de `xilogravuraStuff/Draws`, listadas num ScriptableObject de catálogo.
- AC3: QUANDO o usuário confirma, ENTÃO a textura escolhida é aplicada em `SketchTexture` (matriz e papel) e o fluxo avança para `Sketching`.

### 4.4 Etapas específicas

**REQ-INK-1 — Derramar tinta:** inclinar o pote além de `pourAngle` (padrão **60°**) sobre o acrílico pinta `InkMask` sob o bico do pote.

**REQ-INK-2 — Carregar o rolo:** QUANDO o rolo toca uma área com tinta no acrílico, ENTÃO `isTinta = 1` e `inkLevel` evolui como no `InkRollerController` atual. A condição da etapa é cumprida quando o rolo está carregado.

**REQ-INK-3 — Entintar a matriz:** o rolo carregado pinta `PaintMask` na matriz; rolo não carregado não pinta.

**REQ-PAPER-1 — Pegar e posicionar a folha:** a folha é pega da pilha ao lado da bancada. QUANDO solta perto da matriz, ENTÃO encaixa (socket) e toca o som de posicionamento. Sem animação nesta rodada.

**REQ-PRINT-1 — Barém:** esfregar o barém sobre o papel pinta `PrintMask`, revelando progressivamente a impressão (o shader do papel cruza com `SculptMask`/`PaintMask`).

**REQ-PRINT-2 — Revelação:** ao avançar de `Pressing`, toca o som de resultado e o painel oferece **Salvar** e **Finalizar sem salvar**.

### 4.5 Galeria e varal (GAL)

**REQ-GAL-1 — Salvar PNG**
- AC1: QUANDO o usuário escolhe Salvar, ENTÃO a impressão é "assada" (bake, a partir do `ShaderBake` atual) em PNG.
- AC2: O PNG é gravado em `Application.persistentDataPath/Prints/xilo_yyyyMMdd_HHmmss.png` (funciona no Quest e no Editor).
- AC3: Falhas de escrita são tratadas e registradas no log, sem travar a experiência.

**REQ-GAL-2 — Varal no Menu**
- AC1: Ao carregar o Menu, a pasta `Prints/` é varrida e as **10 gravuras mais recentes** (configurável) são penduradas no varal.
- AC2: Pasta vazia ou arquivo corrompido não quebram o Menu (o arquivo é ignorado e logado).

**REQ-GAL-3 — Disponibilizar online**
- AC1: Após salvar localmente, o PNG é enviado ao **Firebase Storage** via REST (sem SDK do Firebase).
- AC2: O upload é **assíncrono e não bloqueante**: sem internet, o arquivo fica numa fila e é reenviado depois (ex.: na próxima abertura do Menu).
- AC3: A integração fica atrás de uma interface, para permitir trocar o backend.

### 4.6 Cenas e sessão (SES)

**REQ-SES-1:** O Menu é o lobby: título, varal e botão **Iniciar** (carrega a Room).
**REQ-SES-2:** Finalizar na Room volta ao Menu.
**REQ-SES-3:** O usuário fica parado diante da bancada; todos os objetos ficam ao alcance do braço (sem locomoção). Snap turn é opcional.

### 4.7 Ferramentas de desenvolvimento (DEV)

**REQ-DEV-1:** O **XR Interaction Simulator** (sample do XRI 3.3.2, já importado) é **adicionado às cenas Menu e Room**, ativo só no Editor e quando não há headset conectado.
**REQ-DEV-2:** Atalhos de debug só no Editor: pular etapa, preencher a máscara atual, limpar a máscara atual, reiniciar.
**REQ-DEV-3:** Toda transição de etapa, pegar/soltar ferramenta, salvar e upload gera log com prefixo `[XiloVR]`.

### 4.8 Ganchos para movimento futuro (ANIM)

**REQ-ANIM-1:** Momentos-chave expõem `UnityEvent`s para plugar movimentos simples depois, sem mexer no fluxo: `OnStepEntered`, `OnStepExited`, `OnPaperPlaced`, `OnPaperRemoved`, `OnPrintRevealed`, `OnRollerLoaded`. Nesta rodada eles só disparam sons e logs.

---

## 5. Requisitos não funcionais

| ID | Requisito |
|----|-----------|
| NFR-1 | **Performance no Quest:** 72 fps estáveis no Quest 2. Pintura e cobertura não podem bloquear a main thread (cobertura via `AsyncGPUReadback` em textura reduzida, no máximo ~4×/s). |
| NFR-2 | **Modularidade:** sistemas desacoplados por eventos; nenhum singleton global com `DontDestroyOnLoad` para gameplay. |
| NFR-3 | **Configurabilidade:** etapas, artes, thresholds, distâncias de contato e tamanhos de pincel ajustáveis no Inspector. |
| NFR-4 | **Robustez:** erros de IO/rede/referências ausentes são tratados e logados; a experiência continua. |
| NFR-5 | **Idioma:** código e nomenclatura em inglês; textos de UI em português (pt-BR) (C-6). |
| NFR-6 | **Shaders intocados** (C-2). |
| NFR-7 | **Painéis de UI são placeholders funcionais:** layout simples, só o prefab de botão é obrigatório. |

---

## 6. Contrato com os shaders existentes (somente leitura)

> Os nomes de propriedades dos shaders ficam como estão (alguns em português), porque os shaders não podem ser alterados.

| Shader | Propriedades que o código escreve |
|--------|-----------------------------------|
| `XiloShader` / `XiloShaderFigure` (matriz) | `SketchTexture`, `SketchMask`, `SculptMask`, `SandpaperMask`, `PaintMask`, `PrintMask`, `Resultado`, `autoral`, `mascaraArteAutoral` |
| `PaperShader` (papel) | `SketchTexture`, `SketchMask`, `SculptMask`, `PaintMask`, `PrintMask`, `autoral`, `mascaraArteAutoral` |
| `GlassShader` (acrílico) | `InkMask` |
| `Borracha` (rolo) | `isTinta`, `inkLevel` |
| `ArtShader` | `SketchMask` |
| `Outline` | `_Color`, `_Size` |
| `Hidden/DrawSprite` (pincel) | `_SpriteTex`, `_Pos`, `_Scale`, `_Rotation` |

No código, esses nomes ficam centralizados numa classe de constantes (`ShaderProps`), para que o resto do código só use identificadores em inglês.

As RenderTextures existentes em `xilogravuraStuff/PainterAssets/RenderTextures/` (`1_Sketch`, `2_Carve`, `3_Sand`, `4_Ink`, `5_Paint`) são reaproveitadas.

---

## 7. Critérios de aceite do protótipo (Definition of Done)

1. No simulador, é possível ir do Menu até salvar uma gravura e vê-la no varal, sem erros no console.
2. Voltar funciona em todas as etapas de 1 a 9: limpa a etapa atual e reativa a ferramenta anterior.
3. Reiniciar deixa a Room idêntica ao estado inicial.
4. Nenhum arquivo de shader, shadergraph ou partícula aparece modificado no `git status`.
5. O build Android gera sem erros (validação em headset quando houver um disponível).
6. O upload online funciona com internet e entra na fila sem internet.
7. Todo código novo usa nomenclatura em inglês.

---

## 8. Decisões e pendências

**Decidido**
- Voltar limpa só a etapa atual (REQ-STEP-3).
- Backend: Firebase Storage via REST.
- Limite do varal: 10.
- Catálogo: todas as artes de `xilogravuraStuff/Draws`.
- Calibração inicial: `pourAngle = 60°`, `contactDistance = 1,5 cm` (ajustar no headset).
- Sem câmera secundária para o bake.

**Para depois**
1. Entrega ao visitante (QR code / link).
2. Textos finais da UI e design dos painéis.
3. Movimento simples de objetos (ainda esta semana), plugado nos ganchos do REQ-ANIM-1.
