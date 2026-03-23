# MASTER PROMPT - Bullet Route Puzzle (Unity)

> **Cach dung**: Copy toan bo noi dung file nay, paste vao conversation moi cung voi file GDD (DOCX).
> Prompt nay da bao gom tat ca bai hoc, bug fixes, va architecture decisions tu nhieu vong development.

---

## PROMPT BAT DAU

Toi la Unity developer. Toi co kha nang lam Prefabs, setup Inspector, ke doi tuong trong scene.
Project da co DOTween va Odin Inspector.

Hay tao cho toi game **Bullet Route Puzzle** dua tren GDD dinh kem.

---

## YEU CAU TONG THE

### 1. KIEN TRUC CODE

- **SOLID principles**, Design Patterns, code he thong de sua doi
- **Patterns bat buoc**: Service Locator, EventBus (pub/sub), State Machine, Command (undo), Factory, Object Pool
- **Orchestrator pattern**: CHI GameManager subscribe user-action events (Play, Reset, NextLevel, GoHome, TimerExpired, LevelCompleted, LevelFailed). Cac Manager khac (LevelManager, BulletManager...) la **workers** - CHI expose public methods, KHONG subscribe events
- **Toan bo config** bang ScriptableObject (GameConfig, LevelData)
- **Odin Inspector**: dung [ShowInInspector], [BoxGroup], [FoldoutGroup], [TableList], [Required], [EnumToggleButtons], [PreviewField] de Inspector dep va de dung

### 2. GAME STATES (State Machine)

```
MainMenu -> Loading -> Setup -> Simulating -> Win/Fail
                        ^                       |
                        |____Retry/Next_________|
Setup -> Paused -> Setup (resume)
```

States: MainMenu, Loading, Setup, Simulating, Win, Fail, Paused
- Moi state co Enter() va Exit()
- Enter() publish ShowPanelEvent, Exit() publish HidePanelEvent
- PausedState: Time.timeScale = 0 on Enter, = 1 on Exit

### 3. UI SCREENS (UGUI + DOTween)

| Man hinh | Mo ta | Nut / Thanh phan |
|----------|-------|-----------------|
| **UIMainMenu** | Man chinh khi mo game | Play button, Level text, Settings button. An Play -> vao InGame |
| **PopupSettings** | Mo tu MainMenu hoac Pause | 2 sliders: SFX Volume, Music Volume. Doc/ghi PlayerPrefs |
| **GameplayUI** | HUD khi choi | Fire, Reset, Undo, Hint, Pause buttons. Timer display, Move count, Star slots, Level text |
| **PopupPause** | Mo khi an Pause | Resume, Home, Settings buttons. Resume -> quay lai Setup |
| **WinPanel** | Khi thang | "Level Complete!" text, Time display, Move count, 3 Stars animated, Next/Retry/Home buttons |
| **FailPanel** | Khi thua (het gio) | "Time's Up!" text, Retry/Home buttons |

**CRITICAL UI RULES** (da fix nhieu bug):
- **GameplayUI** dung CanvasGroup de show/hide (KHONG SetActive) vi no subscribe events trong Awake(). Subscribe trong Awake(), Unsubscribe trong OnDestroy()
- **WinPanel** subscribe LevelCompletedEvent trong **Awake()** (KHONG phai OnEnable) de nhan event khi panel inactive. Cache data, hien thi trong Show()
- **Buttons trong panels KHONG goi Hide()**. De State.Exit() tu hide qua HidePanelEvent
- **UIPanel.Hide()** can guard: `if (!gameObject.activeSelf) return;`
- **UIManager.ForceHideAllPanels()**: Kill all tweens, set alpha=0, SetActive(false) NGAY LAP TUC (khong animate). Goi TRUOC moi transition lon (Win->Load, Fail->Load, GoHome)
- **Popups (Pause, Settings)** la ngoai le - co the goi Hide() truc tiep vi khong qua state

### 4. GAME FLOW TRACES (PHAI DUNG CHINH XAC)

**Fire -> Miss -> Re-fire**:
1. GameplayUI.OnFireClicked() -> publish PlayButtonPressedEvent
2. GameManager guard `state == Setup` -> ChangeState(Simulating) -> _levelManager.FireBullets()
3. BulletSimulator chay -> bullets miss -> set `_isActive = false` **NGAY LAP TUC** (khong doi animation xong)
4. allDone && targetsRemaining > 0 -> delayed 0.3s -> ChangeState(Setup)
5. Player co the Fire lai (timer van chay)

**Fire -> Win -> Next Level**:
1. All targets hit -> delayed 0.5s -> publish LevelCompletedEvent
2. WinPanel.OnLevelCompleted() cache data
3. GameManager -> save progress -> ChangeState(Win)
4. User click Next -> GameManager.NextLevel() -> LoadLevel(next)
5. LoadLevel(): StopBullets -> StopTimer -> **ForceHideAllPanels()** -> **ClearLevel()** (DOTween.KillAll + DestroyImmediate) -> ChangeState(Loading) -> BuildLevel -> ChangeState(Setup) -> StartTimer

**Timer Expires -> Fail -> Retry**:
1. LevelTimer.Update() -> timeRemaining <= 0 -> publish TimerExpiredEvent
2. GameManager -> publish LevelFailedEvent -> StopAllBullets -> ChangeState(Fail)
3. User click Retry -> LoadLevel(currentIndex) -> same flow as above

**Reset**:
1. ResetButtonPressedEvent -> GameManager.LoadLevel(currentIndex) -> full reload

### 5. GRID VISUAL

- Moi o grid co **cell background rieng** (Quad/Plane) voi mau toi (`Color(0.15, 0.15, 0.2)`)
- Cac o **cach nhau** boi CellSpacing (0.1f default)
- TotalCellSize = CellSize + CellSpacing
- Grid appear animation: cascade scale 0->1 cho tung cell (staggered delay 0.03s)
- Tiles nam tren cell backgrounds, co the xoay (tap) va doi cho (drag)

### 6. LEVEL SYSTEM

- LevelData (ScriptableObject): GridWidth, GridHeight, TimeLimit, Tiles[], Turrets[], Targets[], Star thresholds
- Game dem gio (countdown timer). Moi man co TimeLimit khac nhau
- Stars tinh theo thoi gian con lai: 3 star >= ThreeStarTime, 2 star >= TwoStarTime, else 1 star
- **Tao 10 levels co the choi duoc**, do kho tang dan, gioi thieu mechanic moi moi level:
  - Lv1: Tutorial straight only
  - Lv2: Corner rotation
  - Lv3: Multiple corners (S-curve)
  - Lv4: Mirror redirect
  - Lv5: Splitter (2 targets)
  - Lv6: Portal teleport
  - Lv7: Corner + Mirror combo
  - Lv8: 2 turrets, 2 targets
  - Lv9: Portal + corners relay
  - Lv10: All tile types combined

### 7. ANIMATIONS (DOTween - THAT NHIEU)

| Doi tuong | Animation | Kieu |
|-----------|-----------|------|
| Grid cells | Cascade scale appear | DOScale 0->1 OutBack staggered |
| Tiles | Rotate on tap | DOLocalRotate Z-axis OutBack |
| Tiles | Swap drag | DOMove tween |
| Tiles | Lock shake | DOPunchRotation khi tap locked tile |
| Bullet | Move per tile | DOMove SpeedBased + ease |
| Bullet | Pulse glow | DOScale loop PingPong |
| Bullet | Hit target | DOScale 1->1.5->0 |
| Bullet | Stop | DOShakePosition |
| Turret | Fire recoil | DOLocalMove barrel back+forward |
| Target | Ring rotate | DOLocalRotate loop |
| Target | Hit scale | DOPunchScale |
| UI Panels | Show | DOScale 0.5->1 + DOFade 0->1 OutBack |
| UI Panels | Hide | DOScale 1->0.5 + DOFade 1->0 InBack |
| Stars (Win) | Appear | DOScale 0->1 OutBack staggered + DOPunchRotation |
| Timer | Warning flash | DOColor red pulse khi < 10s |
| Level text | Appear | DOPunchScale |
| Buttons | Click feedback | DOPunchScale |
| MainMenu title | Idle pulse | DOScale loop |

**DOTween rules**:
- Init voi 500 tweens, 100 sequences capacity
- SetUpdate(true) cho UI panels (hoat dong khi Time.timeScale = 0)
- KillAll(false) trong ClearLevel() TRUOC khi DestroyImmediate
- SetAutoKill(true) cho hau het tweens

### 8. FX SPAWN POINTS (THAT NHIEU NOI DAT FX)

Moi Tile prefab can co:
- FX_Center, FX_Top, FX_Bottom, FX_Left, FX_Right (5 diem)

Bullet prefab can co:
- FX_Front, FX_Center, FX_Trail (3 diem)

Turret them:
- FX_MuzzleFlash (dau nong sung)

**FX names ma code goi** (FXManager spawns ParticleSystem tai vi tri):
BulletPass, BulletHitTarget, BulletStop, MuzzleFlash, TurretCharge, MirrorReflect, PortalIn, PortalOut, Explosion, BlockHit, TargetHit, Confetti, StarBurst, BulletSplit, BulletAbsorb

**SFX names ma code goi**:
ButtonClick, TileRotate, TileSwap, TurretFire, TargetHit, LevelComplete, LevelFail, StarEarned, GameplayMusic

### 9. EDITOR TOOLS (Menu: BulletRoute >)

Tao cac Editor tools de **1-click setup**, giam thoi gian setup thu cong:

| Tool | Chuc nang |
|------|-----------|
| **Scene Setup Wizard** | 1-click tao: 11 managers (GameManager, GridManager, TileFactory, LevelManager, BulletSimulator, BulletManager, GameStateManager, InputManager, FXManager, AudioManager, LevelTimer), Camera + CameraController, Grid Collider (auto-create "Grid" layer), UI Canvas (1080x1920 ScaleWithScreenSize) + EventSystem, 6 UI panels (MainMenu, Gameplay, Win, Fail, Pause, Settings), Auto-assign tat ca references, Tao GameConfig ScriptableObject |
| **Tile Prefab Creator** | 1-click tao 11 tile prefabs voi: dung script component, VisualRoot + placeholder 3D models, 5 FX spawn points, auto-assign TileFactory. Hoac tao tung cai rieng |
| **Material Factory** | Tao 23+ material assets (.mat) voi emission/metallic/transparency, tu dong gan vao tat ca prefabs. Materials luu tai Assets/_Game/Art/Materials/ |
| **Create Bullet Prefab** | Tao Bullet.prefab voi BulletController, sphere + glow + TrailRenderer, 3 FX points, auto-assign BulletManager |
| **Level Editor** | Visual grid - click de paint Tile/Turret/Target/Eraser. Chon type, rotation, locked. Portal pair validation. Fill empty cells. Gan vao LevelManager |
| **Level Batch Creator** | Tao 10 levels da thiet ke san voi data hoan chinh |
| **Project Validator** | Scan tat ca: Managers, Camera, Grid Collider, TileFactory prefabs, LevelManager levels, Bullet prefab, UI panels, FX entries, Audio entries. Hien Pass/Warning/Error, click de nhay den object |

### 10. TILE ROUTING LOGIC

| Type | Routing | Rotatable |
|------|---------|-----------|
| Straight | Up<->Down (rot=0), Left<->Right (rot=1) | Yes |
| Corner | rot=0: Up<->Right, rot=1: Right<->Down, rot=2: Down<->Left, rot=3: Left<->Up | Yes |
| Cross | Xuyen thang moi huong | No |
| Block | Chan dan (fixed) | No |
| Mirror / | Up<->Right, Down<->Left | Yes |
| Mirror \ | Up<->Left, Down<->Right | Yes |
| Splitter | Vao 1 huong -> ra 2 huong vuong goc (tach dan) | Yes |
| Portal | Teleport den portal cung ID, giu huong (fixed) | No |
| Bomb | Xuyen qua, pha Block ke ben | No |
| Absorb | Nuot dan (coi nhu miss) | No |

Rotation math:
```csharp
localEntry = (entryDir - rotState + 4) % 4  // Reverse rotation
exitDir = (localExit + rotState) % 4         // Apply rotation
```

### 11. KNOWN BUG PATTERNS (TRANH LAP LAI)

| Bug | Nguyen nhan | Cach tranh |
|-----|------------|------------|
| Double event handler | 2 managers cung subscribe 1 event | CHI GameManager subscribe user-action events |
| Panel khong dong | Button goi Hide() -> kill animation -> State Exit goi Hide() lan 2 | Buttons KHONG goi Hide(). Guard activeSelf trong Hide() |
| WinPanel data sai | Event publish truoc panel active -> OnEnable subscribe qua muon | Subscribe trong Awake(), cache data |
| GameplayUI bien mat | SetActive(false) -> OnDisable unsubscribe -> khong bao gio show lai | Dung CanvasGroup, subscribe trong Awake() |
| Level khong load sau Next | Destroy() delayed -> tiles cu con ton tai khi BuildLevel chay | DestroyImmediate() + DOTween.KillAll(false) |
| Panels animation conflict | Hide animation chua xong thi state moi da chay | ForceHideAllPanels() truoc transitions |
| Bullet khong detect done | IsActive = false trong OnComplete (sau animation) | Set IsActive = false NGAY LAP TUC |
| Re-fire khong hoat dong | State khong quay lai Setup sau khi miss | Check allDone + targets > 0 -> ChangeState(Setup) |

### 12. FOLDER STRUCTURE

```
Assets/_Game/
├── Art/
│   ├── Materials/          <- .mat files (generated by MaterialFactory)
│   │   ├── Tiles/
│   │   ├── Bullet/
│   │   └── Environment/
│   ├── Models/             <- 3D models (user import)
│   └── Textures/           <- Textures (user import)
├── Prefabs/
│   ├── Tiles/              <- 9 tile prefabs
│   ├── Turret/             <- Tile_Turret.prefab
│   ├── Target/             <- Tile_Target.prefab
│   ├── Bullet/             <- Bullet.prefab
│   ├── FX/                 <- ParticleSystem prefabs (user import)
│   └── UI/                 <- (optional UI prefabs)
├── Scenes/
│   └── GameScene.unity
├── ScriptableObjects/
│   ├── Configs/
│   │   └── GameConfig.asset
│   └── Levels/
│       ├── Level_01.asset ... Level_10.asset
└── Scripts/
    ├── Core/               <- GameManager, EventBus, Enums, GameEvents, ServiceLocator, ObjectPool
    ├── GameState/           <- GameStateManager + 7 state classes
    ├── Level/               <- LevelManager, LevelData
    ├── Bullet/              <- BulletSimulator, BulletController, BulletManager
    ├── Grid/                <- GridManager, GridCell
    ├── Tile/                <- TileBase, ITile, 9 tile scripts, TileFactory
    ├── Turret/              <- TurretController
    ├── Target/              <- TargetController
    ├── Command/             <- ICommand, CommandManager, RotateTileCommand, SwapTilesCommand
    ├── Input/               <- InputManager
    ├── UI/                  <- UIPanel, UIManager, GameplayUI, UIMainMenu, WinPanel, FailPanel, PopupSettings, PopupPause
    ├── Timer/               <- LevelTimer
    ├── Data/                <- GameConfig, PlayerProgressData
    ├── Audio/               <- AudioManager
    ├── CameraSystem/        <- CameraController
    ├── FX/                  <- FXManager, FXSpawnPoint, FXAutoDestroy
    ├── Animation/           <- DOTweenAnimator
    └── Editor/              <- 7 editor tools
```

### 13. OUTPUT YEU CAU

Sau khi code xong, tao:
1. **SETUP_GUIDE.txt** - Huong dan setup chi tiet tung buoc (8 buoc workflow voi Editor Tools)
2. **CONTEXT.md** - File context day du de doc lai nhanh (kien truc, events, bugs, flows)
3. Tat ca **57+ C# scripts** theo folder structure tren
4. **7 Editor tools** de 1-click setup
5. **10 LevelData assets** co the choi duoc

### 14. KIEM TRA CUOI CUNG

Truoc khi hoan thanh, tu verify:
- [ ] Tat ca state transitions dung (khong deadlock)
- [ ] Fire -> miss -> re-fire hoat dong (state quay ve Setup)
- [ ] Fire -> win -> Next -> level moi load dung (ForceHide + ClearLevel + BuildLevel)
- [ ] Fire -> win -> Retry -> cung level reset dung
- [ ] Timer het -> Fail -> Retry hoat dong
- [ ] Timer het -> Fail -> Home -> MainMenu
- [ ] Pause -> Resume hoat dong (Time.timeScale restore)
- [ ] Pause -> Home -> MainMenu
- [ ] Reset button -> reload level hien tai
- [ ] WinPanel hien thi dung data (time, moves, stars)
- [ ] GameplayUI khong mat sau khi vao MainMenu roi quay lai
- [ ] DestroyImmediate cho tiles khi clear
- [ ] DOTween.KillAll truoc destroy
- [ ] ForceHideAllPanels truoc moi transition lon
- [ ] BulletController.IsActive set NGAY LAP TUC
- [ ] 10 levels load dung, paths verify

---

**DINH KEM**: [Dan file GDD .docx vao day]
