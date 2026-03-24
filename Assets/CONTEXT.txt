# BULLET ROUTE PUZZLE - Context & Reference

> File ghi lai toan bo context cua du an. Doc file nay truoc khi lam viec de tiet kiem token.
> Cap nhat lan cuoi: Session tao game tu GDD, fix nhieu vong bugs.

---

## 1. KIEN TRUC TONG QUAN

```
GameManager (Orchestrator)
    ├── GameStateManager (State Machine: 7 states)
    ├── LevelManager (Worker: load/build/clear levels)
    ├── BulletSimulator (Path calculation + DOTween sequences)
    ├── BulletManager (Object Pool)
    ├── GridManager (Grid data + cell backgrounds + coordinate conversion)
    ├── TileFactory (Prefab registry + instantiation)
    ├── InputManager (Raycast touch: tap=rotate, drag=swap)
    ├── LevelTimer (Countdown per level)
    ├── UIManager (Panel registry + show/hide)
    ├── FXManager (Particle pool by name)
    └── AudioManager (SFX + Music + volume via PlayerPrefs)
```

**Patterns**: Service Locator, EventBus (pub/sub), State Machine, Command (undo/redo), Factory, Object Pool

**Quy tac quan trong**:
- **GameManager la orchestrator DUY NHAT** - chi no moi subscribe PlayButtonPressedEvent, ResetButtonPressedEvent, LevelCompletedEvent, LevelFailedEvent, GoToMainMenuEvent, TimerExpiredEvent
- **LevelManager la worker** - KHONG subscribe bat ky event nao, chi expose public methods
- **UI panels KHONG goi Hide() trong button handlers** - de state transition tu hide qua WinState.Exit() / FailState.Exit()
- **WinPanel subscribe LevelCompletedEvent trong Awake()** (khong phai OnEnable) de nhan event ngay ca khi panel inactive
- **GameplayUI dung CanvasGroup** de show/hide (khong SetActive) vi no subscribe events trong Awake
- **DestroyImmediate()** cho tiles/backgrounds khi clear level (Destroy() delayed gay bug)
- **DOTween.KillAll(false)** trong ClearLevel() truoc khi destroy objects
- **ForceHideAllPanels()** goi truoc moi transition lớn (Win->Load, Fail->Load, GoToMainMenu)

---

## 2. GAME FLOW (State Transitions)

```
[MainMenu] ──Play──> [Loading] ──> [Setup] ──Fire──> [Simulating]
                                      ^                    │
                                      │              ┌─────┴──────┐
                                      │         All targets    Bullets miss
                                      │              │              │
                                    Reset        [Win]        Back to [Setup]
                                      │              │         (timer continues)
                                      │         Next/Retry
                                      │              │
                                      └──────────────┘

[Setup] ──Pause──> [Paused] ──Resume──> [Setup]
                       │
                     Home ──> [MainMenu]

Timer expires ──> [Fail] ──Retry──> [Loading] ──> [Setup]
                     │
                   Home ──> [MainMenu]
```

---

## 3. LUONG CHINH (TRACE)

### Fire -> Miss -> Re-fire
1. `GameplayUI.OnFireClicked()` → publish `PlayButtonPressedEvent`
2. `GameManager.OnPlayPressed()` → guard `state == Setup` → `ChangeState(Simulating)` → `_levelManager.FireBullets()`
3. `LevelManager.FireBullets()` → reset targets, return bullets, build turret list, delayed 0.4s → `_bulletSimulator.StartSimulation()`
4. `BulletSimulator.SimulatePath()` → bullets stop, `AnimateStop()` sets `_isActive = false` NGAY LAP TUC
5. `OnBulletStopped()` → check `allDone && targetsRemaining > 0` → delayed 0.3s → `ChangeState(Setup)`
6. Player co the Fire lai (timer van chay)

### Fire -> Win -> Next Level
1. All targets hit → `OnTargetHit()` → `targetsRemaining <= 0` → delayed 0.5s → publish `LevelCompletedEvent`
2. `WinPanel.OnLevelCompleted()` (subscribed in Awake) → cache data
3. `GameManager.OnLevelCompleted()` → save progress → `ChangeState(Win)` → WinState.Enter() → ShowPanelEvent("WinPanel")
4. `WinPanel.Show()` → display cached data, animate stars, SFX
5. User click Next → `GameManager.NextLevel()` → `LoadLevel(next)`
6. `LoadLevel()`: StopBullets → StopTimer → **ForceHideAllPanels()** → **ClearLevel()** (KillAll + DestroyImmediate) → ChangeState(Loading) → BuildLevel → ChangeState(Setup) → StartTimer

### Timer Expires -> Fail -> Retry
1. `LevelTimer.Update()` → timeRemaining <= 0 → publish `TimerExpiredEvent`
2. `GameManager.OnTimerExpired()` → publish `LevelFailedEvent`
3. `GameManager.OnLevelFailed()` → StopAllBullets → `ChangeState(Fail)` → FailState.Enter() → ShowPanelEvent("FailPanel")
4. User click Retry → `GameManager.LoadLevel(currentIndex)` → same as step 6 above

### Reset
1. `GameplayUI.OnResetClicked()` → publish `ResetButtonPressedEvent`
2. `GameManager.OnResetPressed()` → `LoadLevel(currentIndex)` → full reload

---

## 4. FILE STRUCTURE

```
Assets/_Game/Scripts/
├── Core/           GameManager, EventBus, Enums, GameEvents, ServiceLocator, ObjectPool, MonoSingleton
├── GameState/      GameStateManager (+ 7 state classes inline)
├── Level/          LevelManager, LevelData
├── Bullet/         BulletSimulator, BulletController, BulletManager
├── Grid/           GridManager, GridCell
├── Tile/           TileBase, ITile, StraightTile, CornerTile, CrossTile, BlockTile,
│                   SplitterTile, BombTile, AbsorbTile, PortalTile, MirrorTile, TileFactory
├── Turret/         TurretController (extends TileBase)
├── Target/         TargetController (extends TileBase)
├── Command/        ICommand, CommandManager, RotateTileCommand, SwapTilesCommand
├── Input/          InputManager
├── UI/             UIPanel, UIManager, GameplayUI, UIMainMenu, WinPanel, FailPanel,
│                   PopupSettings, PopupPause
├── Timer/          LevelTimer
├── Data/           PlayerProgressData, GameConfig
├── Audio/          AudioManager
├── CameraSystem/   CameraController
├── FX/             FXManager, FXSpawnPoint, FXAutoDestroy
├── Animation/      DOTweenAnimator
└── Editor/         SceneSetupWizard, TilePrefabCreator, MaterialFactory,
                    BulletPrefabCreator, LevelEditorWindow, LevelBatchCreator, ProjectValidator
```

---

## 5. EVENT CATALOG

| Event | Fields | Published By | Subscribed By |
|-------|--------|-------------|---------------|
| PlayButtonPressedEvent | - | GameplayUI | GameManager |
| ResetButtonPressedEvent | - | GameplayUI | GameManager |
| GoToMainMenuEvent | - | WinPanel, FailPanel, PopupPause | GameManager |
| LevelCompletedEvent | LevelIndex, Stars, MoveCount, TimeRemaining, TimeLimit | BulletSimulator | GameManager, WinPanel (Awake) |
| LevelFailedEvent | LevelIndex | GameManager (from TimerExpired) | GameManager |
| TimerExpiredEvent | LevelIndex | LevelTimer | GameManager |
| TimerTickEvent | TimeRemaining, TimeLimit | LevelTimer | GameplayUI |
| TimerStartedEvent | TimeLimit | LevelTimer | - |
| GameStateChangedEvent | PreviousState, NewState | GameStateManager | InputManager, GameplayUI, LevelTimer |
| LevelStartedEvent | LevelIndex | LevelManager | GameplayUI |
| ShowPanelEvent | PanelName | States (Enter) | UIManager |
| HidePanelEvent | PanelName | States (Exit) | UIManager |
| PlaySFXEvent | ClipName | UI panels | AudioManager |
| PlayMusicEvent | TrackName | GameManager | AudioManager |
| FXRequestEvent | FXName, Position, Rotation | BulletController, Tiles | FXManager |
| CameraShakeEvent | Intensity, Duration | FailPanel | CameraController |
| HintRequestedEvent | - | GameplayUI | (chua implement) |
| BulletFiredEvent | StartPosition, GridPos | BulletSimulator | - |
| BulletStoppedEvent | LastPos, Reason | BulletSimulator | - |
| BulletHitTargetEvent | TargetPos, TargetIndex | BulletSimulator | - |

---

## 6. TILE ROUTING REFERENCE

| Type | Routing | Rotatable | Fixed |
|------|---------|-----------|-------|
| Straight | Up-Down (rot=0) / Left-Right (rot=1) | Yes | No |
| Corner | rot=0: Up-Right, rot=1: Right-Down, rot=2: Down-Left, rot=3: Left-Up | Yes | No |
| Cross | Pass all directions | No | No |
| Block | Stops bullet | No | Yes |
| Mirror / | Up-Right, Down-Left (ExtraData=0) | Yes | No |
| Mirror \ | Up-Left, Down-Right (ExtraData=1) | Yes | No |
| Splitter | Entry -> 2 perpendicular exits | Yes | No |
| Portal | Teleport to paired (same ExtraData ID), keep direction | No | Yes |
| Bomb | Pass through + destroy adjacent blocks | No | No |
| Absorb | Swallows bullet | No | No |

**Rotation math**:
- `ReverseRotation(dir) = (dir - rotState + 4) % 4`
- `ApplyRotation(dir) = (dir + rotState) % 4`
- Corner only handles localEntry == Up or Right

---

## 7. UI PANELS

| Panel | Extends | Subscribe Pattern | Show/Hide |
|-------|---------|------------------|-----------|
| UIMainMenu | UIPanel | OnEnable/OnDisable (buttons only) | Via MainMenuState Enter/Exit |
| GameplayUI | MonoBehaviour | **Awake** (events persist) | CanvasGroup alpha (NOT SetActive) |
| WinPanel | UIPanel | **Awake** (LevelCompletedEvent), OnEnable (buttons) | Via WinState + ForceHide |
| FailPanel | UIPanel | OnEnable (buttons only) | Via FailState + ForceHide |
| PopupSettings | UIPanel | OnEnable/OnDisable | Manual Show/Hide |
| PopupPause | UIPanel | OnEnable/OnDisable | Manual Hide, resume via ChangeState |

**CRITICAL**: Panels KHONG goi `Hide()` trong button click handlers. State transitions tu hide.
**EXCEPTION**: PopupPause.Resume va PopupPause.Home goi `Hide()` vi PausedState.Exit() KHONG hide popup.

---

## 8. BUGS DA FIX (HISTORY)

### Double Handler Bug
- **Van de**: GameManager VA LevelManager deu subscribe ResetButtonPressedEvent/PlayButtonPressedEvent
- **Fix**: LevelManager KHONG subscribe events. Chi expose public methods.

### BulletController.IsActive Bug
- **Van de**: `AnimateStop()` set `_isActive = false` trong OnComplete (sau animation) → `OnBulletStopped` check isActive qua som → allDone = false → khong return Setup
- **Fix**: Set `_isActive = false` NGAY LAP TUC khi AnimateStop/AnimateHitTarget duoc goi

### Double Hide Bug
- **Van de**: Panel button goi `Hide()` → kill animation → state transition goi `Hide()` lan 2 → kill animation hide → SetActive(false) khong bao gio chay
- **Fix**: Buttons KHONG goi Hide(). State Exit() tu hide. Them guard `if (!gameObject.activeSelf) return` trong UIPanel.Hide()

### WinPanel Data Bug
- **Van de**: LevelCompletedEvent publish TRUOC WinPanel.SetActive(true) → OnEnable subscribe SAU event → khong nhan data
- **Fix**: Subscribe trong Awake(), cache data, hien thi trong Show()

### GameplayUI Disappear Bug
- **Van de**: Subscribe events trong OnEnable, SetActive(false) khi MainMenu → OnDisable unsubscribe → khong bao gio nhan GameStateChangedEvent de show lai
- **Fix**: Subscribe trong Awake(), unsubscribe trong OnDestroy(). Dung CanvasGroup de show/hide thay SetActive

### Destroy Delayed Bug (Level khong hien sau Next)
- **Van de**: `Destroy()` la delayed (cuoi frame). Tiles cu chua bi destroy khi BuildLevel() chay → DOTween conflict
- **Fix**: `DestroyImmediate()` cho tiles va backgrounds. `DOTween.KillAll(false)` trong ClearLevel()

### ForceHideAllPanels
- **Van de**: Animation hide cua panels conflict voi state transitions
- **Fix**: `UIManager.ForceHideAll()` - kill tweens, set alpha=0, SetActive(false) NGAY LAP TUC

---

## 9. EDITOR TOOLS (Menu: BulletRoute >)

| Tool | Chuc nang |
|------|-----------|
| Scene Setup Wizard | 1-click tao 11 managers, camera, grid collider, 6 UI panels, auto-assign refs |
| Tile Prefab Creator | Tao 11 tile prefabs voi placeholder 3D, FX points, auto-assign TileFactory |
| Material Factory | Tao 23 materials (emission/metallic), auto-assign vao prefabs |
| Create Bullet Prefab | Tao Bullet.prefab voi trail, glow, FX points |
| Level Editor | Visual grid painter (Tile/Turret/Target/Eraser modes) |
| Create 10 Levels | Tao 10 LevelData assets da verify paths |
| Project Validator | Scan Pass/Warning/Error cho tat ca systems |

---

## 10. GAMECONFIG (ScriptableObject)

| Field | Default | Muc dich |
|-------|---------|----------|
| CellSize | 1.2f | Kich thuoc moi o grid |
| CellSpacing | 0.1f | Khoang cach giua cac o |
| BulletSpeedPerTile | 0.5f | Thoi gian di chuyen 1 o |
| BulletSpawnDelay | 0.3f | Delay giua cac turret fire |
| MaxBulletSteps | 100 | Chong infinite loop |
| TileRotateDuration | 0.25f | Thoi gian xoay tile |
| TileSwapDuration | 0.3f | Thoi gian swap tile |
| GridAppearDelay | 0.03f | Cascade delay moi cell |
| AutoResetDelay | 1.5f | (khong con dung - FailPanel handle) |
| WinPanelDelay | 0.5f | Delay truoc khi show WinPanel |
| TimerWarningThreshold | 10f | Timer do khi < X giay |
| CellBackgroundColor | (0.15,0.15,0.2) | Mau nen o grid |

---

## 11. FX & AUDIO NAMES

**FX** (gan vao [FXManager] Inspector):
BulletPass, BulletHitTarget, BulletStop, MuzzleFlash, TurretCharge, MirrorReflect, PortalIn, PortalOut, Explosion, BlockHit, TargetHit, Confetti, StarBurst, BulletSplit, BulletAbsorb

**SFX** (gan vao [AudioManager] Inspector):
ButtonClick, TileRotate, TileSwap, TurretFire, TargetHit, LevelComplete, LevelFail, StarEarned

**Music**: GameplayMusic

---

## 12. 10 LEVELS

| Lv | Name | Grid | Time | T | X | Mechanic |
|----|------|------|------|---|---|----------|
| 1 | First Shot | 5x5 | 90s | 1 | 1 | Tutorial straight |
| 2 | Turn the Corner | 5x5 | 90s | 1 | 1 | 1 corner rotation |
| 3 | S-Curve | 5x5 | 80s | 1 | 1 | 2 corners |
| 4 | Mirror Mirror | 5x5 | 75s | 1 | 1 | Mirror redirect |
| 5 | Split Decision | 5x5 | 70s | 1 | 2 | Splitter |
| 6 | Portal Hop | 6x6 | 65s | 1 | 1 | Portal teleport |
| 7 | Mixed Signals | 6x6 | 60s | 1 | 1 | Corner+Mirror |
| 8 | Double Trouble | 6x6 | 55s | 2 | 2 | 2 turrets |
| 9 | Portal Relay | 7x7 | 50s | 1 | 1 | Portal+corners |
| 10 | Grand Finale | 7x7 | 45s | 1 | 1 | All types |

T=Turrets, X=Targets. Tao lai: `BulletRoute > Create 10 Levels`

---

## 13. SETUP WORKFLOW

1. Import DOTween + TextMeshPro
2. `BulletRoute > Scene Setup Wizard > SETUP FULL SCENE`
3. `BulletRoute > Material Factory > TAO TAT CA MATERIALS`
4. `BulletRoute > Tile Prefab Creator > TAO TAT CA 11 TILE PREFABS`
5. `BulletRoute > Create Bullet Prefab`
6. `BulletRoute > Create 10 Levels`
7. `BulletRoute > Project Validator` → all green
8. Play!
