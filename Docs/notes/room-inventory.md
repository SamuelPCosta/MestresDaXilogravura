# Inventário da Room (Fase 0 · T0.2)

Levantado via MCP em 2026-10-27, antes de qualquer alteração na cena.

## Superfícies

| Objeto | Layer | Componentes | Material (shader) | Máscaras ligadas |
|--------|-------|-------------|-------------------|------------------|
| `Room/matriz` | `wood` | MeshCollider (`matriz`), XiloController | `Xilogravura` (XiloShader) | `1_SketchTexture`, `2_CarveTexture`, `3_SandTexture`, `5_PaintTexture` |
| `Room/glass` | `glass` | MeshCollider (`Glass`), Rigidbody, GlassController | `Glass` (GlassShader) | `4_InkTexture` |
| `Room/Paper/paper` | `paper` | MeshCollider (`paper`), PaperController, Animator, ShaderBaker | `Paper` (PaperShader) | nenhuma no asset (`PrintMask` é criada em runtime pelo PaperController) |
| `Room/Paper/paper/paper2` | Default | — | `PaperVerso` (PaperShaderVerso) | — |

> ⚠️ **Achado importante para o design:** na matriz e no acrílico, as RTs estão ligadas como **textura padrão dentro dos nós `SampleTexture2D` do shadergraph**, e não pelas propriedades expostas (`SketchMask`, `SculptMask`…). Ou seja, o shader lê diretamente os assets `1_Sketch`…`5_Paint`. Consequência: o `PaintableSurface` deve **pintar nesses assets de RT** (não basta `SetTexture` numa propriedade). A T3.2 precisa confirmar quais propriedades expostas o shader realmente usa (provavelmente o papel usa as expostas).

## Ferramentas (hoje filhas do XR Origin, sem grab)

`XR Origin (VR)/Camera Offset/OffsetTools/Tools/…` (layer `tools`), com Rigidbody, AudioSource e collider, **sem XRGrabInteractable**:

| Objeto | Collider | Extra |
|--------|----------|-------|
| `lapis` | Capsule | filho `pointer` (TrailRenderer) |
| `goiva` | Capsule | filho `goivaPonta.001` |
| `lixa` | Box | 2 materiais |
| `tinta` | Capsule | pote de tinta |
| `rolinho` | Capsule | InkRollerController; borracha em `RolinhoMetal.001/RolinhoBorracha.001` (Borracha shader) |
| `Baren` | Capsule | filho `Baren (1)` duplicado com Rigidbody próprio |
| `Cursor` | Circle2D | TrackingBtnController (OpenCV, legado) |

`OffsetTools` (ProjectionController) está desativado.

## Partículas (`Room/Particulas`)
`Particle SystemMadeira` (lascas), `Particle SystemPo` (pó, PoMadeira), `Particle SystemTinta` + `Particle SystemTintaPingos` (ParticulasTinta).

## XR / câmera
- `XR Origin (VR)`: XROrigin + InputActionManager, **só a Main Camera, sem controles**.
- `Room/CameraOffset` (desativado): câmera antiga com CameraMovement, collisionController, FixCamera, ShaderBaker, Volume (legado da projeção).
- `EventSystem`: InputSystemUIInputModule + XRUIInputModule (dois módulos ao mesmo tempo, revisar na Fase 1).
- Input actions vindas do sample **XRI 1.0.0-pre.3**; sprites do sample **2.5.2**. Há três versões de samples do XRI em `Assets/Samples` (1.0.0-pre.3, 2.5.2, 3.3.2).

## UI existente (legado, será substituída)
`Room/UI/Notebook` (menu com slider de pincel), `Room/UI/Folder` (seleção de arte ◀ ▶ + botão), `Room/UI/Center/*` (MenuDeCriacao, Undo/Refazer, SetSheet, Result, Restart/Salvar/Reiniciar), `CanvasOverlay`, `AudioSourceClick`.

## Objetos de cena com scripts legados
`Receivers` (PythonLauncher, UDPReceiver, AutoConnectJoystick), `ToolsController`, `DynamicPaint` (DynamicPainting), `MenuController`, `MarcadorController` (off).

## Dependências
- `_Course Library`: a Room usa o piso (`Room_Traditional_Floor` prefab/fbx, `Material_Wood_Dark`, `WoodFloor_Texture`). **A pasta foi mantida no lugar.**
- Menu: só Canvas screen-space, XR Origin sem controles e um Cube/Quad.

## Build
- Plataforma ativa: **StandaloneWindows64** (trocar para Android antes da Fase 7).
- Build Settings atualizado para: `Menu` (0), `Room` (1).
