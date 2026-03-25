# BULLET ROUTE PUZZLE - Project Context
> Cap nhat: final audit session. Doc file nay truoc khi lam viec.

---

## 1. PROJECT STATS

- **65 C# scripts** (56 runtime + 9 editor)
- **14 prefabs** (9 tiles + turret + target + bullet + 2 UI)
- **31 ScriptableObjects** (1 GameConfig + 30 LevelData)
- **24 materials** (14 tiles + 2 turret + 2 target + 3 bullet + 2 environment + 1 duplicate deleted)
- **Dependencies**: DOTween, TextMeshPro, Odin Inspector

---

## 2. ARCHITECTURE

```
GameManager (Orchestrator - DUY NHAT subscribe user events)
  ├── GameStateManager (7 states: MainMenu/Loading/Setup/Simulating/Win/Fail/Paused)
  ├── LevelManager (Worker: load/build/clear - NO event subscriptions)
  ├── BulletSimulator (Path calc + DOTween sequences)
  ├── BulletManager (Object Pool)
  ├── GridManager (Grid data + cell backgrounds)
  ├── TileFactory (Prefab registry)
  ├── InputManager (Raycast: tap=rotate, drag=swap)
  ├── LevelTimer (Countdown)
  ├── UIManager (Panel registry + ForceHideAll)
  ├── FXManager (Particle pool)
  └── AudioManager (SFX + Music + PlayerPrefs)
```

**Patterns**: ServiceLocator, EventBus, StateMachine, Command, Factory, ObjectPool

---

## 3. CRITICAL RULES (Bug-proven)

| Rule | Why |
|------|-----|
| GameManager is ONLY orchestrator subscribing user events | Double-handler bug |
| LevelManager exposes public methods only, NO subscriptions | Same |
| WinPanel subscribes LevelCompletedEvent in Awake() not OnEnable() | Event fires before panel activates |
| WinPanel caches event data, displays in Show() | Data arrives before UI |
| GameplayUI uses CanvasGroup show/hide, NOT SetActive | SetActive kills subscriptions |
| GameplayUI subscribes in Awake(), unsubscribes in OnDestroy() | Persists across visibility |
| Win/Fail buttons do NOT call Hide() | State.Exit() handles hiding |
| UIPanel.Hide() guards `if (!activeSelf) return` | Prevents double-hide |
| ForceHideAllPanels() before every major transition | Kills animations immediately |
| DestroyImmediate() when clearing level, NOT Destroy() | Destroy is delayed = ghost objects |
| DOTween.KillAll(false) BEFORE DestroyImmediate | Prevents null tween targets |
| BulletController.IsActive = false IMMEDIATELY (not in OnComplete) | allDone check timing |
| All UI panel animations use .SetUpdate(true) | Works during TimeScale=0 |
| Bullet miss -> return to Setup state (timer continues, can re-fire) | Not auto-fail |

---

## 4. FILE STRUCTURE (65 files)

```
Scripts/
├── Core/           GameManager, EventBus, Enums, GameEvents, ServiceLocator, ObjectPool, MonoSingleton
├── GameState/      GameStateManager (+ 7 state classes inline)
├── Level/          LevelManager, LevelData
├── Bullet/         BulletSimulator, BulletController, BulletManager
├── Grid/           GridManager, GridCell, GridSnapshot
├── Tile/           TileBase, ITile, Straight/Corner/Cross/Block/Splitter/Bomb/Absorb/Portal/Mirror + TileFactory
├── Turret/         TurretController
├── Target/         TargetController
├── Command/        ICommand, CommandManager, RotateTileCommand, SwapTilesCommand
├── Input/          InputManager
├── UI/             UIPanel, UIManager, UIMainMenu, GameplayUI, WinPanel, FailPanel, PopupPause, PopupSettings, LevelNodeUI, TutorialManager
├── Timer/          LevelTimer
├── Data/           GameConfig, PlayerProgressData
├── Audio/          AudioManager
├── CameraSystem/   CameraController
├── FX/             FXManager, FXSpawnPoint, FXAutoDestroy
├── Animation/      DOTweenAnimator
├── SafeArea.cs     (namespace: BulletRoute.UI)
└── Editor/         SceneSetupWizard, TilePrefabCreator, MaterialFactory, BulletPrefabCreator,
                    LevelEditorWindow, LevelBatchCreator, Level30BatchCreator,
                    LevelNodePrefabCreator, MainMenuPrefabCreator, ProjectValidator
```

---

## 5. GAME FLOW

```
[MainMenu] --Play--> [Loading] --> [Setup] --Fire--> [Simulating]
                                      ^                    |
                                      |            +-------+--------+
                                      |       All targets      Miss/stop
                                      |            |               |
                                    Reset        [Win]       Back to [Setup]
                                      |            |         (timer continues)
                                      |       Next/Retry
                                      +------------+

[Setup] --Pause--> [Paused] --Resume--> [Setup]
                       |
                     Home --> [MainMenu]

Timer expires --> [Fail] --Retry--> [Loading] --> [Setup]
                     |
                   Home --> [MainMenu]
```

### LoadLevel Flow:
```
1. StopAllActiveGameplay()
2. StopTimer()
3. ForceHideAllPanels()          // KILL tweens + SetActive(false) immediately
4. ClearLevel()                  // DOTween.KillAll(false) then DestroyImmediate()
5. ChangeState(Loading)
6. BuildLevel(levelData)
7. ChangeState(Setup)
8. StartTimer(levelData.TimeLimit)
```

---

## 6. EVENT CATALOG

| Event | Published By | Subscribed By |
|-------|-------------|---------------|
| PlayButtonPressedEvent | GameplayUI | GameManager |
| ResetButtonPressedEvent | GameplayUI | GameManager |
| GoToMainMenuEvent | Win/Fail/Pause panels | GameManager |
| LevelCompletedEvent | BulletSimulator | GameManager, WinPanel(Awake) |
| LevelFailedEvent | GameManager | GameManager |
| TimerExpiredEvent | LevelTimer | GameManager |
| TimerTickEvent | LevelTimer | GameplayUI |
| GameStateChangedEvent | GameStateManager | InputManager, GameplayUI, LevelTimer |
| LevelStartedEvent | LevelManager | GameplayUI |
| ShowPanelEvent/HidePanelEvent | State Enter/Exit | UIManager |
| PlaySFXEvent/PlayMusicEvent | UI panels | AudioManager |
| FXRequestEvent | Bullets, Tiles | FXManager |
| CameraShakeEvent | FailPanel | CameraController |

---

## 7. TILE ROUTING

| Type | Routing | Notes |
|------|---------|-------|
| Straight | Up-Down (rot=0), Left-Right (rot=1) | Rotatable |
| Corner | rot=0: Up-Right, rot=1: Right-Down, rot=2: Down-Left, rot=3: Left-Up | Rotatable |
| Cross | All directions pass through | Not rotatable |
| Block | Stops bullet | Fixed |
| Mirror / | Up-Right, Down-Left (isForwardSlash=true) | Rotatable |
| Mirror \ | Up-Left, Down-Right (isForwardSlash=false) | Rotatable |
| Splitter | Entry -> 2 perpendicular exits | Rotatable |
| Portal | Teleport to paired (same ExtraData ID) | Fixed |
| Bomb | Pass through + destroy adjacent blocks | Not fixed |
| Absorb | Swallows bullet | Not fixed |

---

## 8. UI PANELS

| Panel | Base | Subscribe Pattern | Show/Hide Method |
|-------|------|-------------------|-----------------|
| UIMainMenu | UIPanel | OnEnable/OnDisable | Via MainMenuState |
| GameplayUI | UIPanel* | **Awake/OnDestroy** | CanvasGroup alpha (NOT SetActive) |
| WinPanel | UIPanel | **Awake**(LevelCompletedEvent) + OnEnable(buttons) | Via WinState + ForceHide |
| FailPanel | UIPanel | OnEnable | Via FailState + ForceHide |
| PopupPause | UIPanel | OnEnable | Manual Hide + ChangeState |
| PopupSettings | UIPanel | OnEnable | Manual Show/Hide |

*GameplayUI extends UIPanel but overrides to use CanvasGroup

---

## 9. EDITOR TOOLS (Menu: BulletRoute >)

| Tool | What it does |
|------|-------------|
| Scene Setup Wizard | 1-click: 11 managers, camera, grid collider, UI canvas + 6 panels, auto-assign refs |
| Tile Prefab Creator | 1-click: 11 tile prefabs with hierarchy, FX points, materials, auto-assign TileFactory |
| Material Factory | 24 materials with emission/metallic, auto-assign to prefabs |
| Create Bullet Prefab | Bullet.prefab with trail, glow, FX points |
| Level Editor | Visual grid painter (Tile/Turret/Target/Eraser) |
| Create 10 Levels | 10 verified LevelData assets |
| Create 30 Levels | 30 levels 5x5 to 10x10 |
| Level Node Prefab Creator | UI level select node |
| Main Menu Prefab Creator | Full main menu UI prefab |
| Project Validator | Scan all systems: Pass/Warning/Error |

---

## 10. KNOWN ISSUES (Fixed)

| Bug | Root Cause | Fix Applied |
|-----|-----------|-------------|
| SafeArea.cs wrong namespace | Copy from old project | Changed to BulletRoute.UI |
| MirrorTile always same reflection | if(_isForwardSlash) commented out | Uncommented both branches |
| Duplicate material file | Accidental creation | Deleted Tile_Straight_Arrow 1.mat |
| GameStateType.Playing not registered | Enum exists but no state class | Low priority - not used in flow |
| Double handler (GM + LM both subscribe) | Both subscribe to same events | LM has NO subscriptions |
| WinPanel shows 0/0/0 | Event before OnEnable | Subscribe in Awake, cache data |
| GameplayUI disappears | SetActive kills events | CanvasGroup + Awake/OnDestroy |
| Level not loading after Next | Destroy() delayed | DestroyImmediate + KillAll |
| Panel animation conflicts | Hide tween not killed | ForceHideAllPanels |
| Can't re-fire after miss | State stuck in Simulating | Return to Setup after all bullets stop |
| Reset needs 2 clicks | Ghost state | Full LoadLevel clears everything |

---

## 11. FX & AUDIO NAMES

**FX**: BulletPass, BulletHitTarget, BulletStop, MuzzleFlash, TurretCharge, MirrorReflect, PortalIn, PortalOut, Explosion, BlockHit, TargetHit, Confetti, StarBurst, BulletSplit, BulletAbsorb

**SFX**: ButtonClick, TileRotate, TileSwap, TurretFire, TargetHit, LevelComplete, LevelFail, StarEarned

**Music**: GameplayMusic

---

## 12. SETUP WORKFLOW

1. Import DOTween + TextMeshPro + Odin Inspector
2. BulletRoute > Scene Setup Wizard > SETUP FULL SCENE
3. BulletRoute > Material Factory > TAO TAT CA MATERIALS
4. BulletRoute > Tile Prefab Creator > TAO TAT CA 11 TILE PREFABS
5. BulletRoute > Create Bullet Prefab
6. BulletRoute > Create 10 Levels (or 30 Levels)
7. BulletRoute > Project Validator > all green
8. Play!
