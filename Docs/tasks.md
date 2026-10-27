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
- [x] **T1.7** (ajustes) Gravidade desligada no rig; `RigStartPose` mantém a pose do `Camera Offset` da cena a cada carregamento (no headset descarta pitch/altura); botão "Crie sua arte" mantido no Menu.
- [x] **T2.1** `XiloTool` + `ToolId`; ferramentas de `LegacyTools` movidas para `Desk/Tools` (Pencil, Gouge, Sandpaper, InkPot, Roller, Baren) com `XRGrabInteractable` kinematic (attach = filho `Attach` existente), `Rigidbody` kinematic e probes. [REQ-TOOL-1]
- [x] **T2.2** `ToolHomeReturn`: volta deslizando à origem ao soltar; `SnapHome` no Restart. [REQ-TOOL-1]
- [x] **T2.3** `ToolRegistry` + `ToolHighlighter` (`Outline.mat` existente) ligados ao `ExperienceFlow`. [REQ-TOOL-3]
- [x] **T2.4** `ToolTipProbe` com gizmos e múltiplos `tipSamples`: lápis/goiva/pote = 1 probe na ponta (`pointer`); lixa/rolo/barém = 2 probes nas faces do eixo mais fino. Validado: todos acertam a matriz (MeshCollider + UV). [REQ-TOOL-2]
  - Calibrar direção dos probes do rolo e do barém olhando os gizmos (filhos `Probes/ProbeA|B`), se necessário.
- [x] **T2.5** Som (`AudioSource` existente), partículas (goiva → `Particle SystemMadeira`, lixa → `Particle SystemPo`) e haptics em `BeginUse/EndUse`. [REQ-TOOL-4]
- ⚠️ **Escala/alcance:** a câmera está a ~1,77 m do plano da Desk; no headset (1:1) nada fica ao alcance do braço (~0,6 m). Decidir antes da validação em headset.

**Pronto quando:** dá para pegar e soltar cada ferramenta com o controle simulado, a ferramenta da etapa aparece destacada e o gizmo da ponta acusa contato com as superfícies.

## Fase 3 — Pintura e cobertura
- [x] **T3.1** `BrushProfile` (SO, 6 assets em `Xilo/Data/Brushes`) + `MaskPainter` (extraído do `DynamicPainting`, usa `Hidden_DrawSprite.mat`, limite de stamps/frame). Traço contínuo validado (linha de UV 0,26→0,74 sem falhas). [REQ-TOOL-2 AC2]
- [x] **T3.2** `PaintableSurface` na matriz (Sketch/Sculpt/Sandpaper/Paint → RTs 1,2,3,5), acrílico (Ink → RT 4) e papel (lê Sketch/Sculpt/Paint da matriz + `PrintMask` criada em runtime). Material instanciado em runtime (os `.mat` não mudam). `Workbench` aplica a arte (`SketchTexture`, `autoral = 0`); `previewArt = coco` até a Fase 4.
- [x] **T3.3** `CoverageCalculator` (6 testes) + `CoverageMeter` (AsyncGPUReadback 128², a cada 0,25 s; alvos da arte construídos uma vez). [REQ-STEP-2, NFR-1]
- [x] **T3.4** Alvos: as artes são linhas pretas (13–20%) em fundo branco → Sketching = `ArtDark`, Carving = `ArtLight`, Sanding = `FullSurface`. Ajustar `minCoverage` jogando.
- [x] **T3.5** `PaintStepHandler` em Sketching, Carving e Sanding. Validado: Próximo libera ao atingir o mínimo; Voltar limpa só a etapa atual (Carving 100%→0%, Sketching mantido em 100%). [REQ-STEP-2/3]
- [x] **T3.6** Atalhos F7 (preencher) / F8 (limpar) via `StepHandlerBase.DebugFill/ClearProgress`. [REQ-DEV-2]

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
