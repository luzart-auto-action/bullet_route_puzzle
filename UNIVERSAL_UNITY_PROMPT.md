# UNIVERSAL UNITY GAME PROMPT
# Dung cho bat ky game Unity nao - Chi can dinh kem GDD

> **Cach dung**:
> 1. Copy TOAN BO prompt nay
> 2. Dinh kem file GDD cua game moi
> 3. Paste vao Claude Code (hoac Claude.ai)
> 4. Done - AI se tao toan bo project

---

## PROMPT

Toi la Unity developer. Toi co kha nang lam Prefabs, setup Inspector, keo doi tuong trong scene.
Project Unity da co san **DOTween** va **Odin Inspector**.

Hay doc GDD dinh kem va tao cho toi TOAN BO game nay.

---

## I. YEU CAU KIEN TRUC (BAT BUOC)

### 1. Patterns & Principles
- Code theo **SOLID**, dung **Design Patterns** nhung de doc, de sua
- **Service Locator**: Cac manager dang ky vao ServiceLocator.Register<T>(), truy cap qua ServiceLocator.Get<T>()
- **EventBus** (static pub/sub): Giao tiep giua cac he thong bang events, KHONG reference truc tiep
- **State Machine**: GameStateManager quan ly trang thai game (MainMenu, Loading, Setup/Playing, Simulating, Win, Fail, Paused)
- **Command Pattern**: Cho undo/redo player actions
- **Factory Pattern**: Tao objects tu prefabs qua Factory class
- **Object Pool**: Cho cac object spawn/despawn nhieu (dan, FX, ...)
- **ScriptableObject**: Tat ca config, level data, ... deu la ScriptableObject

### 2. Manager Architecture (Orchestrator + Workers)

```
GameManager (Orchestrator - DUY NHAT subscribe user-action events)
    ├── GameStateManager (State Machine)
    ├── LevelManager (Worker - CHI expose public methods, KHONG subscribe events)
    ├── [GameLogic Managers] (Workers - tuy theo game)
    ├── UIManager (Panel registry)
    ├── InputManager
    ├── AudioManager (SFX + Music + PlayerPrefs volume)
    ├── FXManager (Particle pool by name)
    └── [Timer/Score/etc] (Workers)
```

**QUY TAC VANG**:
- **GameManager la orchestrator DUY NHAT** subscribe cac events: PlayButton, Reset, NextLevel, GoHome, LevelCompleted, LevelFailed, TimerExpired
- **Workers (LevelManager, etc.)** KHONG subscribe bat ky event nao. Chi expose public methods de GameManager goi
- Ly do: Tranh double-handler bug (2 managers cung xu ly 1 event -> chay 2 lan)

### 3. Namespace & Folder Structure

```
Assets/_Game/
├── Art/
│   ├── Materials/          <- .mat files (tu dong tao boi MaterialFactory tool)
│   ├── Models/             <- 3D models
│   └── Textures/
├── Prefabs/
│   ├── [GameObjects]/      <- Prefabs theo loai (enemy, bullet, tile, ...)
│   ├── FX/                 <- ParticleSystem prefabs
│   └── UI/
├── Scenes/
├── ScriptableObjects/
│   ├── Configs/            <- GameConfig.asset
│   └── Levels/             <- Level_01.asset, Level_02.asset, ...
└── Scripts/
    ├── Core/               <- GameManager, EventBus, GameEvents, Enums, ServiceLocator, ObjectPool
    ├── GameState/           <- GameStateManager + cac state classes
    ├── Level/               <- LevelManager, LevelData
    ├── [GameLogic]/         <- Cac he thong gameplay chinh (tuy game)
    ├── Command/             <- ICommand, CommandManager, concrete commands
    ├── Input/               <- InputManager
    ├── UI/                  <- UIPanel, UIManager, cac panel cu the
    ├── Data/                <- GameConfig, PlayerProgressData
    ├── Audio/               <- AudioManager
    ├── CameraSystem/        <- CameraController
    ├── FX/                  <- FXManager, FXSpawnPoint
    ├── Animation/           <- DOTweenAnimator (utility)
    └── Editor/              <- Tat ca editor tools
```

Moi folder co namespace rieng: `[GameName].[FolderName]` (vd: `MyGame.Core`, `MyGame.UI`)

---

## II. UI SYSTEM (QUAN TRONG NHAT - NHIEU BUG NHAT)

### 1. Cac man hinh bat buoc

| Man hinh | Loai | Mo ta |
|----------|------|-------|
| **UIMainMenu** | UIPanel | Nut Play, Level text, Settings button |
| **PopupSettings** | UIPanel | Slider SFX Volume + Music Volume (PlayerPrefs) |
| **GameplayUI** | MonoBehaviour + CanvasGroup | HUD khi choi: buttons (Fire/Play, Reset, Undo, Pause, Hint), timer, score/moves, level text |
| **PopupPause** | UIPanel | Resume, Home, Settings |
| **WinPanel** | UIPanel | Ket qua (stars, score, time), Next/Retry/Home |
| **FailPanel** | UIPanel | Thong bao thua, Retry/Home |

### 2. UIPanel Base Class

```csharp
// UIPanel.cs - Base cho tat ca panels (tru GameplayUI)
public class UIPanel : MonoBehaviour
{
    [SerializeField] protected string _panelName;
    protected CanvasGroup _canvasGroup;

    public virtual void Show()
    {
        gameObject.SetActive(true);
        // DOTween: scale 0.5->1 + fade 0->1, Ease.OutBack, 0.4s
        // .SetUpdate(true) <- QUAN TRONG: hoat dong khi TimeScale=0
	// .SetId(this)
    }

    public virtual void Hide()
    {
        if (!gameObject.activeSelf) return; // GUARD chong double-hide
        // DOTween: scale 1->0.5 + fade 1->0, Ease.InBack, 0.25s
        // OnComplete: gameObject.SetActive(false)
        // .SetUpdate(true)
    }

}
```

### 3. BUG-PROOF RULES (Rut ra tu kinh nghiem thuc te)

| Rule | Ly do | Hau qua neu vi pham |
|------|-------|---------------------|
| **GameplayUI dung CanvasGroup** de show/hide (KHONG SetActive) | GameplayUI subscribe events trong Awake(). Neu SetActive(false) -> OnDisable unsubscribe -> khong bao gio nhan event show lai | GameplayUI bien mat vinh vien |
| **GameplayUI subscribe trong Awake(), unsubscribe trong OnDestroy()** | OnEnable/OnDisable thay doi khi SetActive | Events mat khi panel an |
| **WinPanel subscribe result event trong Awake()** (KHONG phai OnEnable) | Event publish TRUOC khi panel SetActive(true) -> OnEnable chua chay -> khong nhan data | WinPanel hien thi data sai (0, 0, 0) |
| **WinPanel cache event data, hien thi trong Show()** | Data den truoc UI | Data bi mat |
| **Buttons trong Win/Fail KHONG goi Hide()** | Button Hide() kill animation -> State.Exit() goi Hide() lan 2 -> conflict | Panel khong dong, buttons disable |
| **UIPanel.Hide() co guard `if (!activeSelf) return`** | Chong goi Hide() tren panel da an | Null reference / tween errors |
| **UIManager.ForceHideAllPanels()** kill tweens + SetActive(false) NGAY LAP TUC | Animation hide conflict voi state transition moi | Panel cu van hien thi chong len |
| **Goi ForceHideAllPanels() TRUOC moi transition lon** (Win->Load, Fail->Load, GoHome) | Dam bao sach truoc khi load | Ghost panels |

### 4. GameplayUI (Dac biet - KHONG phai UIPanel)

```
GameplayUI extends MonoBehaviour (KHONG phai UIPanel)
- Co CanvasGroup component
- Subscribe tat ca events trong Awake()
- Unsubscribe trong OnDestroy()
- Show/Hide bang CanvasGroup.alpha = 1/0 (KHONG SetActive)
- React to GameStateChangedEvent:
  + MainMenu/Win/Fail -> alpha = 0
  + Setup/Loading -> alpha = 1, enable buttons
  + Simulating -> alpha = 1, disable action buttons
```

---

## III. GAME STATE FLOW

### 1. State Transitions

```
[MainMenu] ──Play──> [Loading] ──> [Setup/Playing] ──Action──> [Simulating/Processing]
                                         ^                              │
                                         │                    ┌─────────┴──────────┐
                                         │               Success              Failure/Miss
                                         │                    │                    │
                                       Reset              [Win]           Back to [Setup]
                                         │                    │            (timer continues)
                                         │              Next/Retry
                                         └────────────────────┘

[Setup] ──Pause──> [Paused] ──Resume──> [Setup]
                       │
                     Home ──> [MainMenu]

Timer/HP/etc expires ──> [Fail] ──Retry──> [Loading] ──> [Setup]
                            │
                          Home ──> [MainMenu]
```

### 2. LoadLevel Flow (CHINH XAC - da fix nhieu bug)

```
LoadLevel(index):
  1. StopAllActiveGameplay()        // Dung moi simulation
  2. StopTimer()                     // Dung timer
  3. ForceHideAllPanels()            // KILL tweens + SetActive(false) NGAY LAP TUC
  4. ClearLevel()                    // DOTween.KillAll(false) TRUOC, roi DestroyImmediate()
  5. ChangeState(Loading)
  6. BuildLevel(levelData)           // Tao objects moi
  7. ChangeState(Setup)
  8. StartTimer(levelData.TimeLimit)
```

**CRITICAL**:
- `DestroyImmediate()` cho game objects khi clear (KHONG dung `Destroy()` vi no delayed -> objects cu con ton tai khi build level moi -> DOTween conflict)
- `DOTween.KillAll(false)` TRUOC khi destroy objects (tranh tween reference null object)
- `ForceHideAllPanels()` TRUOC LoadLevel (tranh animation panels conflict)

---

## IV. ANIMATION & FX (THAT NHIEU)

### 1. DOTween Checklist

| Doi tuong | Animation | Chi tiet |
|-----------|-----------|----------|
| Game objects spawn | Scale appear | 0 -> 1, Ease.OutBack |
| Game objects despawn | Scale disappear | 1 -> 0, Ease.InBack |
| Level grid/board | Cascade appear | Staggered delay (0.03s per cell/item) |
| Player actions | Feedback | DOPunchScale, DOPunchRotation |
| UI Panel show | Scale + Fade | 0.5->1 + alpha 0->1, OutBack, **SetUpdate(true)** |
| UI Panel hide | Scale + Fade | 1->0.5 + alpha 1->0, InBack, **SetUpdate(true)** |
| Button click | Punch | DOPunchScale(0.1f), **SetUpdate(true)** |
| Win stars/rewards | Staggered appear | Scale 0->1 OutBack + DOPunchRotation, delay * index |
| Fail feedback | Shake | DOShakePosition or DOShakeRotation |
| Timer warning | Color pulse | DOColor red, loop when < threshold |
| Idle animations | Continuous | DOScale/DORotate loop PingPong |
| Score/number change | Punch | DOPunchScale when value changes |
| Transitions | Move | DOMove/DOAnchorPos for object movement |

**DOTween Rules**:
- `DOTween.Init(false, true, LogBehaviour.ErrorsOnly).SetCapacity(500, 100)`
- `.SetUpdate(true)` cho TAT CA UI animations (hoat dong khi Time.timeScale = 0)
- `.SetAutoKill(true)` cho hau het tweens
- `DOTween.KillAll(false)` trong ClearLevel() TRUOC khi destroy objects
- KHONG dung `DOTween.KillAll()` trong Update/LateUpdate

### 2. FX Spawn Points

Moi game object quan trong can co cac **Transform con** lam FX spawn points:
- Giup artist/designer dat ParticleSystem prefabs vao dung vi tri
- FXManager.SpawnFX(fxName, position, rotation) su dung Object Pool

**Moi prefab nen co it nhat**: FX_Center + 4 directional points (Top/Bottom/Left/Right)
**Special objects**: them FX points dac biet (vd: MuzzleFlash cho gun, HitPoint cho target)

### 3. Materials

Tao **MaterialFactory** editor tool:
- Dinh nghia tat ca materials voi color, emission, metallic, smoothness, transparency
- 1-click tao tat ca .mat files tai Assets/_Game/Art/Materials/
- 1-click gan vao tat ca prefabs
- Prefab creator tools su dung MaterialFactory.GetOrCreateMaterial(name) thay vi tao inline materials

---

## V. EDITOR TOOLS (1-CLICK SETUP)

### Bat buoc tao cac tools sau (Menu: [GameName] >)

| Tool | Chuc nang | Priority |
|------|-----------|----------|
| **Scene Setup Wizard** | 1-click tao TAT CA managers/camera/UI/canvas/EventSystem/lighting. Auto-assign references. Tao GameConfig asset | P0 |
| **Prefab Creator** | 1-click tao prefabs voi dung hierarchy, scripts, FX points, materials. Auto-assign vao Factory | P0 |
| **Material Factory** | Tao tat ca material assets, gan vao prefabs | P0 |
| **Level Editor** | Visual editor de thiet ke levels (grid/board paint, drag-drop) | P1 |
| **Level Batch Creator** | Tao nhieu levels da thiet ke san (10+ levels) | P1 |
| **Project Validator** | Kiem tra tat ca references, prefabs, levels, configs. Hien Pass/Warning/Error | P1 |

### Scene Setup Wizard tao gi:

1. **Managers**: GameManager, GameStateManager, LevelManager, [GameSpecificManagers], UIManager, InputManager, AudioManager, FXManager, [Timer/Score]
2. **Camera**: Main Camera + CameraController + AudioListener
3. **Physics**: Colliders/layers can thiet cho raycast (neu game can)
4. **UI Canvas**:
   - Canvas (ScreenSpace Overlay, CanvasScaler 1080x1920 ScaleWithScreenSize match=0.5)
   - EventSystem + StandaloneInputModule
   - UIManager
   - 6 panels: MainMenu, GameplayUI, WinPanel, FailPanel, PopupPause, PopupSettings
   - Moi panel co dung hierarchy, buttons, texts, CanvasGroup
5. **Lighting**: Directional Light
6. **Auto-assign**: Tat ca references (camera, layers, buttons, texts, panels -> UIManager)
7. **ScriptableObject**: GameConfig asset

### Prefab Creator quy trinh:

1. Tao GameObject voi dung script component
2. Tao VisualRoot child (target cho animations)
3. Tao placeholder 3D models (primitives) voi dung materials
4. Tao FX spawn points (FX_Center, FX_Top, ...)
5. Set references qua SerializedObject
6. Save as prefab asset
7. Auto-assign vao Factory

---

## VI. LEVEL SYSTEM

### LevelData (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "Level_01", menuName = "[GameName]/Level Data")]
public class LevelData : ScriptableObject
{
    [BoxGroup("Info")] public int LevelIndex;
    [BoxGroup("Info")] public string LevelName;

    [BoxGroup("Grid")] public int GridWidth, GridHeight;

    [BoxGroup("Rules")] public float TimeLimit = 60f;
    [BoxGroup("Rules")] public int ThreeStar, TwoStar, OneStar; // Score/move thresholds

    [BoxGroup("Placements"), TableList]
    public List<[ObjectPlacement]> Placements;

    // Game-specific data...
}
```

### 10 Levels bat buoc:
- **Level 1-3**: Tutorial, gioi thieu mechanic co ban
- **Level 4-5**: Gioi thieu mechanic moi
- **Level 6-7**: Ket hop mechanics
- **Level 8-9**: Tang do kho
- **Level 10**: Grand Finale - dung tat ca mechanics

### PlayerProgress (PlayerPrefs):
- CurrentLevel, HighScores per level, Stars per level
- SFX Volume, Music Volume
- Doc/ghi qua PlayerProgressData utility class

---

## VII. AUDIO SYSTEM

```csharp
AudioManager:
- SFX Library: List<(string Name, AudioClip Clip)>
- Music Library: List<(string Name, AudioClip Clip)>
- Volume control qua PlayerPrefs
- Subscribe PlaySFXEvent, PlayMusicEvent
- Object Pool cho AudioSources
```

**SFX names toi thieu**: ButtonClick, LevelComplete, LevelFail, [GameSpecificSounds]
**Music**: BackgroundMusic (loop)

---

## VIII. ODIN INSPECTOR

Dung Odin attributes cho Inspector dep va de dung:

| Attribute | Dung cho |
|-----------|----------|
| `[BoxGroup("name")]` | Nhom fields |
| `[FoldoutGroup("name")]` | Thu gon sections |
| `[TableList]` | List hien thi dang bang |
| `[Required]` | Bat buoc assign reference |
| `[ShowInInspector]` | Hien thi property/private field |
| `[EnumToggleButtons]` | Enum hien thi la buttons |
| `[PreviewField]` | Preview sprite/texture |
| `[ReadOnly]` | Khong cho sua trong Inspector |
| `[InfoBox("msg")]` | Hien thi huong dan |
| `[Button("name")]` | Tao button trong Inspector |
| `[ValidateInput]` | Validate data |
| `[PropertyOrder]` | Sap xep thu tu fields |

---

## IX. CHECKLIST TRUOC KHI HOAN THANH

### Code Quality
- [ ] Tat ca managers co namespace rieng
- [ ] CHI GameManager subscribe user-action events
- [ ] Workers KHONG subscribe events
- [ ] Tat ca config la ScriptableObject
- [ ] Odin attributes cho Inspector

### UI (Bug-prone area)
- [ ] GameplayUI dung CanvasGroup (KHONG SetActive)
- [ ] GameplayUI subscribe trong Awake(), unsubscribe trong OnDestroy()
- [ ] WinPanel subscribe result event trong Awake(), cache data
- [ ] Buttons trong Win/Fail KHONG goi Hide()
- [ ] UIPanel.Hide() co guard `if (!activeSelf) return`
- [ ] ForceHideAllPanels() truoc moi transition lon
- [ ] Panel animations dung .SetUpdate(true)

### Game Flow
- [ ] Play -> action -> success -> Win -> Next Level works
- [ ] Play -> action -> fail/miss -> co the thu lai (state quay ve Setup)
- [ ] Win -> Next -> level moi load dung (ForceHide + ClearLevel + BuildLevel)
- [ ] Win -> Retry -> cung level reset dung
- [ ] Timer/HP het -> Fail -> Retry hoat dong
- [ ] Fail -> Home -> MainMenu
- [ ] Pause -> Resume (Time.timeScale restore)
- [ ] Pause -> Home -> MainMenu
- [ ] Reset -> reload level hien tai

### Technical
- [ ] DestroyImmediate() khi clear level (KHONG Destroy())
- [ ] DOTween.KillAll(false) TRUOC destroy objects
- [ ] Object pools cho frequently spawned objects
- [ ] 10 levels data hoan chinh va choi duoc
- [ ] Editor tools tao va chay dung

### Output Files
- [ ] SETUP_GUIDE.txt - Huong dan chi tiet
- [ ] CONTEXT.md - File context cho session tiep theo
- [ ] Tat ca scripts theo folder structure
- [ ] Editor tools
- [ ] 10 LevelData assets

---

## X. LUU Y QUAN TRONG

1. **KHONG tao node_modules** hay bat ky file JavaScript nao trong Assets/
2. **KHONG tao file .docx** bang code - chi tao .txt hoac .md
3. **Game chay tren mobile** - Input.GetMouseButtonDown da map sang touch, Physics.Raycast hoat dong binh thuong
4. **UI dung UGUI** (UnityEngine.UI + TextMeshPro)
5. **Moi khi tao prefab** phai co materials thuc (khong inline), co FX spawn points, co dung scripts
6. **Test trong dau** tat ca flow truoc khi output - tranh cac bug patterns da liet ke

---

**DINH KEM**: [Dan file GDD cua game moi vao day]
