# XiloVR — Design Técnico

> Documento 2/3 do SDD. Define **como** os requisitos de [requirements.md](requirements.md) serão atendidos.
> Os REQ-IDs entre colchetes ligam cada decisão ao requisito.

**Status:** rascunho para revisão · **Data:** 2026-10-27

---

## 1. Princípios

1. **Orquestração nova, assets antigos.** Shaders, partículas, sons, modelos e RenderTextures continuam em `Assets/xilogravuraStuff/`. O código novo fica em `Assets/Xilo/`. [C-2]
2. **Dados no Inspector.** Etapas, artes e pincéis são ScriptableObjects. [NFR-3]
3. **Eventos, não referências cruzadas.** Sistemas se comunicam por eventos C# expostos por poucos serviços de cena. [NFR-2]
4. **Um componente, uma responsabilidade.** Substitui os "god objects" atuais (`DynamicPainting`, `MenuController`, `Tool`).
5. **XRI padrão.** Grab, sockets e UI usam componentes do XR Interaction Toolkit com as input actions padrão dos controles. Sem input customizado.
6. **Código em inglês.** Namespaces `Xilo.*`, classes, métodos, variáveis, enums, assets, prefabs, pastas e logs em inglês. Textos de UI em português. Nomes de propriedades de shader (alguns em português) ficam isolados em `ShaderProps`. [C-6]

---

## 2. Estrutura de pastas e assemblies

```
Assets/
├── Xilo/
│   ├── Scripts/
│   │   ├── Xilo.Runtime.asmdef      (refs: XRI, InputSystem, TMP, URP)
│   │   ├── Core/        ExperienceFlow, StepMachine, StepDefinition, XiloLog, ShaderProps
│   │   ├── Tools/       XiloTool, ToolTipProbe, ToolHighlighter, ToolHomeReturn, ToolRegistry
│   │   ├── Painting/    MaskPainter, PaintableSurface, BrushProfile, CoverageMeter, CoverageCalculator
│   │   ├── Steps/       PaintStepHandler, InkPourHandler, RollerLoadHandler,
│   │   │                PaperPlacementHandler, RevealHandler, ArtSelectionHandler
│   │   ├── Objects/     RollerInk, PaperSheet
│   │   ├── UI/          StepPanel, ArtSelectionPanel, RevealPanel, MenuPanel
│   │   ├── Gallery/     PrintBaker, GalleryStorage, DryingLine, IGalleryUploader,
│   │   │                FirebaseStorageUploader, UploadQueue
│   │   └── Dev/         DebugStepHotkeys   (#if UNITY_EDITOR)
│   ├── Data/            Steps/*.asset, ArtCatalog.asset, Brushes/*.asset, GalleryConfig.asset
│   ├── Prefabs/         Tools/*, UI/*, DryingLine.prefab
│   └── Tests/EditMode/  Xilo.Tests.EditMode.asmdef
├── xilogravuraStuff/    (assets visuais mantidos: shaders, materiais, modelos, sons, RTs)
└── _Legacy/             (código e cenas fora do escopo VR)
```

---

## 3. Componentes

```
                 ┌──────────────────────────────────────────┐
                 │ ExperienceFlow (Room)                     │
                 │  - possui StepMachine                     │
                 │  - Next(), Back(), Restart(), Finish(save)│
                 └──────┬────────────────────────────┬──────┘
          StepChanged   │                            │ CanAdvanceChanged / Progress
          ┌─────────────▼─────────┐        ┌─────────▼────────┐
          │ IStepHandler (1/etapa)│        │ StepPanel (UI)   │
          │ Enter/Exit/Clear      │        │ texto, %, botões │
          └──┬────────────────┬───┘        └──────────────────┘
             │ usa            │ usa
   ┌─────────▼──────┐  ┌──────▼─────────┐     ┌────────────────┐
   │ ToolRegistry   │  │ MaskPainter    │◀────│ ToolTipProbe   │
   │ XiloTool[]     │  │ PaintableSurf. │     │ (contato)      │
   │ Highlighter    │  │ CoverageMeter  │     └────────────────┘
   └────────────────┘  └────────────────┘
```

### 3.1 Core

**`StepId`** (enum): `ArtSelection, Sketching, Carving, Sanding, InkPouring, RollerLoading, MatrixInking, PaperPlacement, Pressing, Reveal`.

**`ToolId`** (enum): `None, Pencil, Gouge, Sandpaper, InkPot, Roller, Paper, Baren`.

**`SurfaceId`** (enum): `Matrix, Glass, Paper`.

**`StepDefinition : ScriptableObject`** [REQ-STEP-1]
| Campo | Tipo | Uso |
|-------|------|-----|
| `id` | `StepId` | |
| `title`, `instruction` | `string` (pt-BR) | Painel |
| `toolId` | `ToolId` | Ferramenta ativa (`None` em ArtSelection/Reveal) |
| `surface` | `SurfaceId` | |
| `maskProperty` | `string` | Ex.: `ShaderProps.SculptMask` |
| `brush` | `BrushProfile` | Sprite, tamanho em UV, espaçamento |
| `coverageTarget` | `CoverageTarget` | `FullSurface`, `ArtDark`, `ArtLight` |
| `minCoverage` | `float 0..1` | Habilita Próximo [REQ-STEP-2] |
| `instructionClip`, `toolLoopClip` | `AudioClip` | Opcionais |

**`ShaderProps`** (static): constantes e `Shader.PropertyToID` para todos os nomes da §6 do requirements. É o único lugar com os nomes originais dos shaders.

**`StepMachine`** (classe C# pura, com testes) [REQ-STEP-1..4]
```csharp
public sealed class StepMachine {
    public StepId Current { get; }
    public event Action<StepId, StepId> StepChanged;   // (from, to)
    public bool CanGoBack => Current != StepId.ArtSelection;
    public bool CanAdvance { get; set; }               // definido pelo handler ativo
    public bool TryAdvance();
    public bool TryGoBack();
    public void Reset();
}
```

**`ExperienceFlow : MonoBehaviour`** (um por Room): cria a `StepMachine`, mapeia `StepId → IStepHandler`, repassa progresso para a UI e expõe `Next()`, `Back()`, `Restart()`, `Finish(bool save)`. Expõe `UnityEvent<StepId> OnStepEntered / OnStepExited`. [REQ-ANIM-1]

- `Next()`: `current.Exit()` → avança → `next.Enter()`.
- `Back()`: `current.ClearProgress()` → `current.Exit()` → volta → `previous.Enter()` (progresso anterior mantido). [REQ-STEP-3]
- `Restart()`: `ClearProgress()` em todos os handlers, ferramentas para a origem, `StepMachine.Reset()`. [REQ-STEP-4]

**`IStepHandler`**
```csharp
public interface IStepHandler {
    StepId Id { get; }
    event Action<float> ProgressChanged;   // 0..1
    event Action<bool> CanAdvanceChanged;
    void Enter(StepContext context);
    void Exit();
    void ClearProgress();                  // usado por Back (etapa atual) e Restart (todas)
}
```

**`XiloLog`**: wrapper de `Debug.Log` com prefixo `[XiloVR]` e categoria. [REQ-DEV-3]

### 3.2 Ferramentas (via controle)

**`XiloTool : MonoBehaviour`** (no prefab da ferramenta, junto do `XRGrabInteractable`)
- `ToolId id`, `Transform tip`, `AudioSource loopAudio`, `ParticleSystem contactParticles` (opcional).
- `IsHeld` (via `selectEntered/selectExited`), `IsActiveForStep`, `HoldingInteractor` (para haptics).
- Encaixe: `XRGrabInteractable` com `attachTransform` próprio por ferramenta, para a ferramenta ficar natural na âncora do controle. [REQ-TOOL-1]
- `BeginUse(Vector3 point)` / `EndUse()`: som, partículas (as existentes `lascasDeMadeira` e `poDeMadeira`, reposicionadas no ponto de contato) e vibração opcional via `SendHapticImpulse` do interactor. [REQ-TOOL-4]

**`ToolTipProbe`** [REQ-TOOL-2]
- Em `FixedUpdate`, enquanto `IsHeld && IsActiveForStep`: `Physics.Raycast` a partir de `tip.position - tip.forward * backOffset`, na direção `tip.forward`, com alcance `backOffset + contactDistance`, filtrado pela LayerMask da superfície da etapa.
- O hit precisa ser `MeshCollider` (necessário para `RaycastHit.textureCoord`).
- Emite `Contact(PaintableSurface surface, Vector2 uv, Vector3 point)` e `ContactLost()`.
- Ferramentas largas (rolo, barém, lixa): vários `tipSamples[]` distribuídos na largura, cada um gerando seu traço.

**`ToolHighlighter`** [REQ-TOOL-3]: adiciona/remove um material de outline (`Outline.shadergraph` existente) como material extra no renderer da ferramenta ativa.

**`ToolHomeReturn`** [REQ-TOOL-1 AC3]: guarda a pose de origem; se a ferramenta solta ficar abaixo de `floorY` ou fora de `Bounds`, ou no `Restart`, teleporta de volta e zera a velocidade do Rigidbody (sem animação nesta rodada).

**`ToolRegistry`**: lista das ferramentas da cena, `Get(ToolId)`, `SetActiveTool(ToolId)` → atualiza `IsActiveForStep` + destaque.

### 3.3 Pintura (reaproveita o núcleo do `DynamicPainting`)

**`MaskPainter`** (serviço de cena): extração direta de `DrawSprite` e `DrawInterpolationLine` do `DynamicPainting.cs`, mesmo material `Hidden/DrawSprite`.
```csharp
public void Stamp(RenderTexture mask, BrushProfile brush, Vector2 uv);
public void Line(RenderTexture mask, BrushProfile brush, Vector2 fromUv, Vector2 toUv);
public void Clear(RenderTexture mask, Color color);
```
- Cada stamp hoje faz 2 blits de tela cheia (RT → temp → RT). Para o Quest: agrupar por frame e limitar a `maxStampsPerFrame`. Otimizações sem mudar shader (ex.: ping-pong entre dois RTs fixos em vez de `GetTemporary`) ficam na task de performance.

**`PaintableSurface`** (matriz, acrílico, papel)
- `SurfaceId id`, `Renderer targetRenderer`, `MeshCollider meshCollider`.
- `MaskBinding[]`: `{ string property; RenderTexture mask; }`. Na inicialização faz `material.SetTexture(property, mask)`, ligando as RTs existentes (`1_Sketch` … `5_Paint`) ou RTs criadas em runtime (o `PrintMask` do papel, como o `PaperController` faz hoje).
- `GetMask(string property)`, `ClearMask(string property)`, `ClearAll()`.
- O papel também recebe as máscaras da matriz (`SketchMask`, `SculptMask`, `PaintMask`).
- ⚠️ **Achado da Fase 0** ([room-inventory.md](notes/room-inventory.md)): na matriz e no acrílico, as RTs estão ligadas como textura padrão dentro dos nós do shadergraph, não pelas propriedades expostas. O `MaskBinding` dessas superfícies, portanto, **aponta para os próprios assets de RT** (`1_Sketch`…`5_Paint`) e pinta neles; `SetTexture` só é usado onde a propriedade exposta é de fato lida (papel). Confirmar na T3.2.

**`CoverageMeter`** [REQ-STEP-2, NFR-1]
- A cada `interval` (0,25 s): `Graphics.Blit` da máscara ativa para um RT de 128×128 + `AsyncGPUReadback.Request`.
- O alvo (`CoverageTarget`) é pré-computado uma vez ao escolher a arte: amostra a arte em 128×128 e gera um `bool[]` (`ArtDark`: luminância < 0,5; `ArtLight`: o inverso; `FullSurface`: tudo).
- `coverage = paintedTargetPixels / targetPixels`, calculado pela classe pura `CoverageCalculator` (testes EditMode).
- Plataformas sem `AsyncGPUReadback`: loga um aviso e retorna 1 (Próximo liberado), para nunca travar o usuário.

> ⚠️ O `CoverageTarget` correto de cada etapa depende de como o `XiloShader` lê cada máscara (a goiva remove as áreas claras ou as escuras?). Calibrado na T3.4 observando o shader, sem alterá-lo.

### 3.4 Handlers de etapa

| Handler | Etapas | Lógica | `ClearProgress()` |
|---------|--------|--------|-------------------|
| `ArtSelectionHandler` | ArtSelection | Mostra o `ArtSelectionPanel`; ao Escolher: `SketchTexture` na matriz/papel, pré-computa os alvos de cobertura e avança. | — |
| `PaintStepHandler` (genérico) | Sketching, Carving, Sanding, MatrixInking, Pressing | Escuta o `ToolTipProbe` da ferramenta da etapa → `MaskPainter.Line` na máscara da etapa; `BeginUse/EndUse`; `CoverageMeter` → Progress/CanAdvance. | Limpa a máscara dela |
| `InkPourHandler` | InkPouring | Se o ângulo do pote em relação a `Vector3.up` passar de `pourAngle` (60°): raycast para baixo a partir do bico → stamp em `InkMask`. Cobertura `FullSurface` numa sub-região do acrílico. | Limpa `InkMask` |
| `RollerLoadHandler` | RollerLoading | Contato do rolo com o acrílico **em pixel com tinta** (via readback do `InkMask`) → `RollerInk.Load()` (port do `InkRollerController`: `isTinta`, corrotina de `inkLevel`). CanAdvance = carregado. Dispara `OnRollerLoaded`. | `RollerInk.ResetInk()` |
| `PaintStepHandler` + trava do `RollerInk` | MatrixInking | Como o genérico, mas só pinta se `RollerInk.IsLoaded`. | Limpa `PaintMask` |
| `PaperPlacementHandler` | PaperPlacement | `XRSocketInteractor` sobre a matriz aceita só o papel. Ao encaixar: desabilita o grab do papel, `PaperSheet.Place()` (som), CanAdvance = true, dispara `OnPaperPlaced`. | Solta do socket, papel volta para a pilha (teleporte), dispara `OnPaperRemoved` |
| `RevealHandler` | Reveal | `PaperSheet.ShowResult()` (som, define `Resultado`), mostra o `RevealPanel` (Salvar / Finalizar sem salvar), dispara `OnPrintRevealed`. | — |

Os parâmetros do `Animator` atual do papel (`isPrint`, `isResult`) **não** são usados nesta rodada. O `PaperSheet` expõe os ganchos `UnityEvent` onde o movimento futuro será plugado. [REQ-ANIM-1]

### 3.5 UI (placeholders funcionais)

Todos os painéis são Canvas world-space com `TrackedDeviceGraphicRaycaster`, operados pelo raio do controle (UI interaction do XRI). O layout é simples; **só os botões precisam obrigatoriamente ser instâncias do `Assets/Prefabs/Button.prefab`**. A equipe faz o design à mão depois, então os scripts dos painéis dependem só de referências serializadas (textos, slider, botões), não de hierarquia nem de estilo. [NFR-7]

| Painel | Conteúdo |
|--------|----------|
| `StepPanel` | Título, instrução, barra de cobertura (`Slider` não interativo), **Voltar**, **Próximo**, **Reiniciar**. Só escuta eventos do `ExperienceFlow`. |
| `ArtSelectionPanel` | `Image` da arte atual + **◀ / ▶ / Escolher**. Fonte: `ArtCatalog` (`List<ArtEntry { string displayName; Texture2D texture; }>`, todas as artes de `xilogravuraStuff/Draws`). Emite `ArtChosen(ArtEntry)`, para poder ser trocado pelo "caderno" sem mexer no handler. |
| `RevealPanel` | **Salvar** / **Finalizar sem salvar** → `ExperienceFlow.Finish(save)`. |
| `MenuPanel` | Título + **Iniciar**. |

### 3.6 Galeria [REQ-GAL-*]

**`PrintBaker`** (port do `ShaderBaker`)
- Mantém a técnica atual (`GL.LoadOrtho` + `material.SetPass(0)` + `Graphics.DrawMeshNow` sobre o mesh deslocado do papel) para renderizar o material do papel num RT e ler para um `Texture2D`.
- Mudanças: sem `Update`/flag `bake`; método `Texture2D Bake()`; parâmetros (tamanho, crop, offset) no `GalleryConfig`.
- **Sem câmera secundária.** Se a saída divergir no Quest (Vulkan), as correções ficam dentro do próprio baker (ex.: buscar o índice do pass com `material.FindPass`, `GL.invertCulling`/flip de Y, formato sRGB do RT).

**`GalleryStorage`** (classe pura + IO)
- `string Save(Texture2D print)` → `persistentDataPath/Prints/xilo_yyyyMMdd_HHmmss.png`, com try/catch e log.
- `IReadOnlyList<string> ListRecent(int count)` ordenada por data de escrita (count = 10, do `GalleryConfig`).
- `Texture2D Load(string path)` → `ImageConversion.LoadImage`; arquivos inválidos são ignorados e logados.

**`DryingLine`** (Menu): 10 pregadores (Transforms); instancia um quad com `Texture2D` para cada gravura recente.

**Upload online** [REQ-GAL-3]
```csharp
public interface IGalleryUploader {
    Task<bool> UploadAsync(string localPath, CancellationToken cancellationToken);
}
```
- **`FirebaseStorageUploader`**: `UnityWebRequest` POST para `https://firebasestorage.googleapis.com/v0/b/{bucket}/o?uploadType=media&name=prints/{file}` com `Content-Type: image/png`. Sem SDK do Firebase.
- Segurança: regras do Storage permitindo **apenas create** em `prints/`, `contentType == image/png` e tamanho < 5 MB. Bucket no `GalleryConfig`.
- **`UploadQueue`**: `pending_uploads.json` em `persistentDataPath`. Após salvar: enfileira e tenta enviar. No início do Menu: reenvia os pendentes. Falha → mantém na fila e loga.

### 3.7 Cenas

| Cena | Conteúdo |
|------|----------|
| `Menu.unity` | XR Origin (controles), XR Interaction Simulator, `DryingLine`, `MenuPanel`, `UploadQueue.Flush()` no início. |
| `Room.unity` | XR Origin parado diante da bancada (controles), XR Interaction Simulator, `ExperienceFlow`, superfícies, ferramentas, pilha de papel, `StepPanel`, `ArtSelectionPanel`, `RevealPanel`. |

Troca de cena via `SceneManager.LoadSceneAsync`. Nenhum objeto de gameplay com `DontDestroyOnLoad` (o `GrabController` atual é removido). [NFR-2]

### 3.8 Dev / simulador [REQ-DEV-*]

- O prefab do **XR Interaction Simulator** (`Assets/Samples/XR Interaction Toolkit/3.3.2/XR Interaction Simulator`) é **colocado nas duas cenas**, configurado para rodar só no Editor e se desligar quando houver um dispositivo XR real ativo.
- **`DebugStepHotkeys`** (só no Editor): `F5` próximo (ignora cobertura), `F6` voltar, `F7` preencher a máscara atual, `F8` limpar a máscara atual, `F9` reiniciar, `F10` soltar todas as ferramentas. Teclas de função porque o simulador já usa letras (N, C, R…).
- Simulador: `G` = grip do controle direito; `Shift + G` = grip do esquerdo (soltar **G antes do Shift**, senão o grip esquerdo fica preso — usar `F10` se acontecer); `[` / `]` alternam a manipulação dos controles; `Tab` cicla os dispositivos.
- Grab: os casters do Near-Far incluem a layer `tools`; as ferramentas usam `farAttachMode = Near` (o raio puxa a ferramenta até o controle).
- XR no Editor: `InitManagerOnStart` desligado no Standalone para o runtime OpenXR do sistema (Meta XR Simulator) não subir no Play Mode. O alvo real (Android) mantém a inicialização.
- Com `backgroundBehavior = ResetAndDisableNonBackgroundDevices`, os controles simulados só ficam rastreados com a **Game view em foco**.

---

## 4. Mapa de reaproveitamento

| Existente | Destino (nome novo em inglês) | Ação |
|-----------|-------------------------------|------|
| `PainterAssets/DynamicPainting.cs` (DrawSprite / InterpolationLine) | `MaskPainter` | Extrair métodos |
| `PainterAssets/Shader/DrawSprite.shader`, `Brushes/*` | mesmo lugar | Manter |
| `PainterAssets/RenderTextures/*` | `PaintableSurface.MaskBinding` | Manter |
| `Objects/InkRollerController.cs` | `RollerInk` | Port |
| `Objects/PaperController.cs` (setTextures, sons de posicionar/resultado, reset) | `PaperSheet` + `PaintableSurface` | Port (sem Animator) |
| `ShaderBake.cs` | `PrintBaker` | Port + refactor |
| `Tool.cs` (som/partículas) | `XiloTool` | Port parcial (sem UDP) |
| Shaders, materiais, partículas, sons, modelos, animações | — | **Não mexer** |
| `Assets/Prefabs/Button.prefab` | UI | Reusar |
| `OpenCVTracking/`, `Mode/*`, `Tool.cs`, `ToolsController`, `GrabController`, `JoystickController`, `AutoConnectJoystick`, `MarcadorController`, `Inputs/*`, `MenuController`, `XiloController`, `GlassController`, `NewArtController`, `FixCamera`, `collisionController` | `_Legacy/` | Mover |
| `Editor/Migration/AddVuforiaEnginePackage.cs`, `QCAR/`, `WSATestCertificate.pfx` | `_Legacy/` | Mover |
| `Scenes/roomxiloProjection*`, `Scenes/Testes` | `_Legacy/Scenes` | Mover e tirar do Build Settings |
| `_Course Library/` | avaliar | Mover se a Room não depender dele |

> Os scripts movidos para `_Legacy/` **continuam compilando** (o Unity compila tudo dentro de `Assets/`). Se algum quebrar depois de removidas as dependências, ele ganha um `#if XILO_LEGACY` ou é excluído. Mover pelo Unity/MCP preserva os `.meta` (GUIDs), então as referências nas cenas não quebram.
>
> Assets existentes com nome em português dentro de `xilogravuraStuff/` (ex.: partículas `lascasDeMadeira`, `poDeMadeira`) **não são renomeados nesta rodada**, para não quebrar referências. Todo asset novo já nasce em inglês.

---

## 5. Tratamento de erros e logs

- Toda operação de IO (`GalleryStorage`, `UploadQueue`), rede (`IGalleryUploader`) e bake fica em try/catch; a falha retorna `false`/`null` e gera `XiloLog.Error`.
- Referências obrigatórias ausentes no Inspector: validação no `Awake` → `XiloLog.Error` e o componente se desabilita.
- Logs de progresso (em inglês): `Step: Carving → Sanding`, `Back: cleared Carving`, `Tool grabbed: Gouge`, `Coverage Carving: 40%` (a cada 10%), `Saved: <path>`, `Upload ok/failed: <file>`.

---

## 6. Estratégia de testes

| Nível | O quê | Como |
|-------|-------|------|
| EditMode | `StepMachine` (avançar, voltar, reset, limites), `CoverageCalculator`, `GalleryStorage` (nome, ordenação, limite 10, arquivo corrompido), `UploadQueue` (persistência) | Unity Test Framework |
| PlayMode manual | Pipeline completo no XR Interaction Simulator | Checklist no tasks.md |
| Verificação de shaders | `git diff --stat -- "*.shader" "*.shadergraph"` vazio | A cada fase |
| Build | Build Android sem erros | Fim do protótipo |

---

## 7. Riscos

| Risco | Mitigação |
|-------|-----------|
| Blits de tela cheia em 2048² a cada stamp pesam no Quest | Limitar stamps por frame; ping-pong; reduzir RTs para 1024 se preciso (asset de RT, não shader) |
| `SetPass` do `PrintBaker` renderiza diferente no Vulkan | Corrigir dentro do baker (pass lookup, flip de Y, espaço de cor); sem câmera extra |
| O simulador reproduz mal o rolar do rolo e a inclinação do pote | Atalhos de debug + tolerâncias generosas; calibrar no headset depois |
| `textureCoord` exige MeshCollider com UVs corretas | Validar os colliders da matriz, do acrílico e do papel na T3.2 |
| Configuração do Firebase dentro do build | Regras restritivas (só create, png, tamanho); nenhuma chave de admin no cliente |
