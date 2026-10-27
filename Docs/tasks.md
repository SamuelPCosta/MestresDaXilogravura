# XiloVR — Plano de Tarefas

> Documento 3/3 do SDD. Execução incremental do [design.md](design.md). Cada task cita os REQs que atende.
> Regra: ao fim de **cada fase**, as cenas rodam no XR Interaction Simulator sem erros e `git diff -- "*.shader" "*.shadergraph"` está vazio.

**Status:** rascunho para revisão · **Data:** 2026-10-27
Legenda: `[ ]` pendente · `[~]` em andamento · `[x]` concluída

---

## Fase 0 — Preparação e limpeza
- [x] **T0.1** Criar a branch `feature/vr-prototype`.
- [x] **T0.2** Auditar a `Room.unity` via MCP: ferramentas, superfícies (layers `wood`, `glass`, `paper`, `newArt`), colliders, materiais e RTs ligados. Registrar em `Docs/notes/room-inventory.md`.
- [x] **T0.3** Verificar dependências da `Room`/`Menu` com a `_Course Library` (GUIDs dos scripts). → A Room usa o piso dela; **pasta mantida no lugar**.
- [x] **T0.4** Mover o legado para `Assets/_Legacy/` (design §4), preservando os `.meta`. Build Settings: `Menu` (0), `Room` (1). Todos os scripts antigos de `xilogravuraStuff/Scripts` + `DynamicPainting.cs` foram para `_Legacy/Scripts` (os que serão portados servem de referência lá). O `AddVuforiaEnginePackage.cs` (`[InitializeOnLoad]`) foi desativado com `#if XILO_LEGACY`.
  - [ ] **Pendente:** `Assets/Packages/WebSocketSharp-netstandard.1.0.1` (DLL bloqueada pelo Unity aberto). Mover para `_Legacy/Packages/` com o Unity fechado.
- [x] **T0.5** Resolver os erros de compilação causados pela limpeza. → Nenhum erro; 0 scripts faltando na Room, no Menu e nos prefabs.
- [x] **T0.6** Criar a estrutura `Assets/Xilo/` + `Xilo.Runtime.asmdef` + `Xilo.Tests.EditMode.asmdef`.

**Pronto quando:** o projeto compila, a Room abre e nenhum asset visual foi alterado.

## Fase 1 — Core e simulador
- [x] **T1.0** Desligar o Meta XR Simulator: `InitManagerOnStart = false` no **Standalone** (o runtime OpenXR ativo do Windows é o Meta XR Simulator). Android não foi alterado.
  - ⚠️ **Android não tem loader XR configurado** (lista vazia). Habilitar OpenXR + perfil Meta Quest antes da T7.2.
- [x] **T1.1** `XiloLog` + `ShaderProps`. [REQ-DEV-3, C-6]
- [x] **T1.2** `StepId`, `StepDefinition` (SO), `StepMachine` + testes EditMode (8/8 passando). [REQ-STEP-1/3/4]
- [x] **T1.3** `IStepHandler`, `StepHandlerBase`, `StepContext`, `ExperienceFlow` com `StubStepHandler` (logam Enter/Exit/Clear) + eventos `OnStepEntered/Exited`. [REQ-STEP-1, REQ-ANIM-1]
- [x] **T1.4** `XR Origin (XR Rig)` do XRI 3.3.2 (controles, Near-Far + Poke) + **XR Interaction Simulator** na Room e no Menu; locomoção desligada (Move, Teleport, Climb, Jump), Snap Turn mantido. `EditorOnlySimulator` remove o simulador em build/headset real e reinicia o `XRInputModalityManager` quando os controles simulados ficam rastreados (corrige controles invisíveis). [REQ-DEV-1, REQ-SES-3]
  - Room: objetos legados desativados (Receivers/PythonLauncher/UDP, ToolsController, DynamicPaint, MenuController, CanvasOverlay, `Room/UI`) e componentes legados das superfícies desligados. Ferramentas antigas em `LegacyTools` (inativo) para a Fase 2. XR Origin antigo desativado.
  - Menu: Canvas convertido para world-space com `TrackedDeviceGraphicRaycaster`; `MenuPanel` mínimo no botão Iniciar (carrega a Room). Botão "Create" legado oculto.
- [x] **T1.5** `DebugStepHotkeys`: **F5** próximo (força), **F6** voltar, **F9** reiniciar (o simulador usa N/C/R; F7/F8 ficam para a Fase 3). [REQ-DEV-2]
- [x] **T1.6** 10 assets `StepDefinition` em `Xilo/Data/Steps/` com textos provisórios em pt-BR.

**Pronto quando:** no simulador, N/B/R percorrem as etapas e os logs mostram as transições.

## Fase 2 — Ferramentas
- [ ] **T2.1** `XiloTool` + `ToolId`; configurar os prefabs de lápis, goiva, lixa, pote de tinta, rolo e barém com `XRGrabInteractable` (attach transform para o controle), `Rigidbody` e `tip`. [REQ-TOOL-1]
- [ ] **T2.2** `ToolHomeReturn`. [REQ-TOOL-1 AC3]
- [ ] **T2.3** `ToolRegistry` + `ToolHighlighter` (Outline existente). [REQ-TOOL-3]
- [ ] **T2.4** `ToolTipProbe` com gizmos de debug e suporte a múltiplos `tipSamples`. [REQ-TOOL-2]
- [ ] **T2.5** Som, partículas e vibração opcional em `BeginUse/EndUse`. [REQ-TOOL-4]

**Pronto quando:** dá para pegar e soltar cada ferramenta com o controle simulado, a ferramenta da etapa aparece destacada e o gizmo da ponta acusa contato com as superfícies.

## Fase 3 — Pintura e cobertura
- [ ] **T3.1** `BrushProfile` (SO) + `MaskPainter` (extraído do `DynamicPainting`). [REQ-TOOL-2 AC2]
- [ ] **T3.2** `PaintableSurface` com `MaskBinding` na matriz, no acrílico e no papel; checar MeshColliders/UVs.
- [ ] **T3.3** `CoverageCalculator` (puro, com testes) + `CoverageMeter` (AsyncGPUReadback 128²). [REQ-STEP-2, NFR-1]
- [ ] **T3.4** Calibrar o `CoverageTarget` de cada etapa observando o `XiloShader` (sem alterá-lo).
- [ ] **T3.5** `PaintStepHandler` genérico para Sketching, Carving e Sanding, com `ClearProgress`. [REQ-STEP-2/3]
- [ ] **T3.6** Atalhos F/C (preencher/limpar máscara). [REQ-DEV-2]

**Pronto quando:** lápis → goiva → lixa funcionam na matriz com o visual atual, a cobertura sobe e Voltar limpa a etapa atual.

## Fase 4 — UI (placeholders)
- [ ] **T4.1** `StepPanel` (Voltar/Próximo/Reiniciar + barra + texto) com `Button.prefab`; layout simples. [REQ-STEP-2/3/4, NFR-7]
- [ ] **T4.2** `ArtCatalog` (SO, todas as artes de `Draws/`) + `ArtSelectionPanel` + `ArtSelectionHandler`. [REQ-ART-1]
- [ ] **T4.3** Reiniciar completo: máscaras, ferramentas, rolo e papel. [REQ-STEP-4]

**Pronto quando:** o fluxo arte → lápis → goiva → lixa é controlado só pelo painel, com Voltar funcionando.

## Fase 5 — Tinta, papel e impressão
- [ ] **T5.1** `InkPourHandler` (ângulo + raycast do bico → `InkMask`). [REQ-INK-1]
- [ ] **T5.2** `RollerInk` (port do `InkRollerController`) + `RollerLoadHandler`. [REQ-INK-2]
- [ ] **T5.3** MatrixInking com a trava `RollerInk.IsLoaded`. [REQ-INK-3]
- [ ] **T5.4** `PaperSheet` (port do `PaperController`, sem Animator) + socket na matriz + `PaperPlacementHandler`, incluindo o Voltar (papel volta para a pilha). [REQ-PAPER-1, REQ-ANIM-1]
- [ ] **T5.5** Pressing com o barém → `PrintMask`; ligar as máscaras da matriz no papel. [REQ-PRINT-1]
- [ ] **T5.6** `RevealHandler` + `RevealPanel`. [REQ-PRINT-2]

**Pronto quando:** o pipeline completo roda no simulador até a revelação.

## Fase 6 — Galeria e varal
- [ ] **T6.1** `PrintBaker` (port do `ShaderBake`), validando a saída no Editor. [REQ-GAL-1]
- [ ] **T6.2** `GalleryStorage` + testes (limite 10). [REQ-GAL-1/2]
- [ ] **T6.3** `DryingLine` + `MenuPanel` (Iniciar) + Finalizar → Menu. [REQ-GAL-2, REQ-SES-1/2]
- [ ] **T6.4** `IGalleryUploader` + `UploadQueue` (+ testes de persistência). [REQ-GAL-3]
- [ ] **T6.5** `FirebaseStorageUploader` + regras do Storage documentadas em `Docs/notes/firebase.md`. **Dependência da equipe:** projeto Firebase + bucket. [REQ-GAL-3]

**Pronto quando:** salvar → voltar ao Menu → a gravura aparece no varal (e no bucket, com internet).

## Fase 7 — Fechamento
- [ ] **T7.1** Passada de performance: limitar stamps/frame, avaliar ping-pong, checar no Profiler. [NFR-1]
- [ ] **T7.2** Build Android sem erros. [DoD 5]
- [ ] **T7.3** Rodar o checklist manual abaixo e atualizar este documento.

---

## Checklist manual (simulador)
1. O Menu abre; o varal mostra as gravuras existentes (até 10) ou fica vazio, sem erros.
2. Iniciar → a Room carrega e o painel de arte aparece.
3. Navegar ◀ / ▶, Escolher → o desenho aparece na matriz.
4. Em cada etapa de 1 a 8: a ferramenta certa fica destacada; a errada não pinta; a % sobe; Próximo habilita no mínimo configurado.
5. Voltar em pelo menos 3 etapas diferentes (incluindo Pressing → PaperPlacement): a etapa atual é limpa e a anterior mantém o progresso.
6. Derrubar uma ferramenta no chão → ela volta para a bancada.
7. Revelação → Salvar → Menu → a gravura nova aparece no varal.
8. Sem internet: a gravura é salva localmente e fica na fila; com internet: sobe para o backend.
9. Reiniciar no meio do fluxo → a Room fica igual ao estado inicial.
10. O `git status` não mostra shaders, shadergraphs ou partículas modificados.

---

## Futuro (fora desta rodada)
- Movimento simples de objetos plugado nos ganchos do REQ-ANIM-1 (ainda esta semana).
- Design visual dos painéis e textos finais da UI (equipe, à mão).
- Escolha de arte em formato de "caderno".
- Entrega ao visitante (QR code / link).
