# 深蹲小遊戲：安裝、Inspector 與驗收

已依 `Unity_Squat_Minigame_Codex.cmd` 實作。使用 Unity 6000.0.43f1、專案現有的 Input System、UGUI 與 TextMeshPro；不需要付費素材。

## 本次變更

- 新增 `Assets/script/Squat/SquatGameModel.cs`：獨立狀態機、指針反彈、區域判定、力竭恢復與次數。
- 新增 `SquatMinigameController.cs`：鍵盤輸入、開啟／返回、暫停確認、完成回呼與字型。
- 新增 `SquatMinigameView.cs`：UI 引用、色帶、文字、恢復條及按鈕。
- 新增 `SquatMinigameUIFactory.cs`：建立完整 Canvas 與 UI，不修改場景檔。
- 新增 `Assets/Editor/SquatMinigameMenu.cs`：Editor 內建立 UI 或另存新 Prefab 的選單。
- 修改 `GymGameManager.cs`：只將 `equipmentId == "Squat"` 分流到深蹲 UI，使用既有完成集合與玩家停用／恢復流程。
- 修改 `GymEquipmentInteractable.cs`：只在深蹲 UI 開啟時隱藏原本的器材標籤。
- 新增 `Tests/SquatGameModelTests.cs`、`Tests/run_squat_tests.py`。

本次沒有修改任何現有 `.unity`、`.prefab`、角色移動、跑步機腳本、輸入設定、Build Profiles 或既有 `.meta`。
`GymStageBootstrap.cs` 中先前接受 `gym`／`SampleScene` 的修復保留。

修改前備份：`Backups/SquatMinigame-20261119-142719/`。包括原有場景、Prefab、相關腳本與設定，共 92 個檔案；SHA-256 記錄在 `backup-manifest.json`。

## 1. 立即測試：不必手動建立 UI

1. 等 Unity 完成匯入、編譯，開啟 `Assets/Scenes/gym.unity`。
2. 按 Play，靠近中文名稱「深蹲」的方塊，確認出現「按 E 開始：深蹲訓練」。器材位置約為 `(4, 0, 0)`。
3. 按 E。管理器會自動建立 Canvas，在原場景上顯示「深蹲挑戰」。不載入新 Scene。
4. 空白鍵停住指針：綠色＝完美、黃色＝成功、紅色＝力竭。每次成功只加一次。
5. 力竭時必須另外按 8 次空白鍵。按住不會自動增加；造成力竭的那一下不算恢復次數。
6. 每次完成後顯示 0.7 秒回饋，再開始下一次。達到 5 次後顯示完成畫面，記錄 `Squat` 已完成。
7. 按完成畫面的「返回健身房」；玩家在原位置恢復移動，既有 HUD 顯示完成進度。

每次新一蹲開始時，綠區會隨機換位置，並比上一蹲縮小。預設五次寬度約為色帶的 **20%、16%、12.8%、10.2%、8.2%**。
紅色危險區保持原範圍，黃色區域隨綠區調整；畫面色帶與實際判定共用當次邊界。
同一蹲進入力竭、暫停或返回再繼續時，不會重抽綠區，也不會額外增加次數。

按 Esc 或畫面的返回按鈕會先詢問。確認畫面暫停指針與回饋計時；可繼續或返回。
未完成時返回不會記錄器材完成，但本次 Play 的深蹲次數、力竭恢復次數與狀態會保留；再按 E 可繼續。
完成後返回，再次進入會開始新一輪挑戰，原先已取得的器材完成紀錄不會取消。
本次 Play 結束後，局內次數不寫入存檔。

## 2. 在 Editor 中建立可調整的 UI

預設的自動建立路線已可玩。若希望在按 Play 前調整 Inspector，使用下列路線：

1. 停止 Play，開啟 `gym`。
2. 選 `Tools > Gym > Create Squat UI In Current Scene`。
3. 選單只新增一個 `SquatMinigameUI` 根物件，不改動其他物件。若已存在控制器，會選取它，不再新增第二份。
4. 根物件已掛載 Canvas、Canvas Scaler、Graphic Raycaster、Squat Minigame View、Squat Minigame Controller，UI 引用會自動填好。
5. 調整參數後可儲存場景。若希望保持場景檔完全不變，使用第 1 節的自動建立路線即可。
6. 進入 Play 時 Canvas 會先隱藏，按 E 才開啟；管理器會優先使用場景中的這份 UI。

另一種方式：選 `Tools > Gym > Save New Squat UI Prefab`，選一個新的 `.prefab` 路徑。
此工具拒絕覆寫已存在的檔案。將新 Prefab 拖入 `gym` 即可調整並使用；不要同時放入多份控制器。

## 3. Inspector：Squat Minigame Controller

選取 `SquatMinigameUI`，展開 `Squat Minigame Controller > Settings`：

| 欄位 | 預設 | 用途 |
|---|---:|---|
| Reps To Win | 5 | 達標次數 |
| Recovery Presses Required | 8 | 每次力竭需要的新按鍵次數 |
| Sweep Speed | 0.7 | 指針初始速度，單位為每秒幾個色帶寬度 |
| Sweep Acceleration | 0.15 | 每秒增加的速度；設 0 即等速 |
| Maximum Sweep Speed | 2 | 速度上限 |
| Feedback Duration | 0.7 | 每次成功後的回饋秒數 |
| Left Danger End | 0.15 | 左紅區右邊界，包含邊界 |
| Perfect Start | 0.4 | 與 Perfect End 的差值決定第一蹲綠區寬度；實際位置會隨機 |
| Perfect End | 0.6 | 初始寬度預設 0.6 − 0.4 = 0.2 |
| Perfect Width Multiplier Per Rep | 0.8 | 每一蹲的綠區寬度乘數；預設比上一蹲縮小 20% |
| Right Danger Start | 0.85 | 右紅區左邊界，包含邊界 |

其他欄位：

- `Timing Key = Space`：停止與恢復都使用此鍵，畫面提示會跟著變。
- `Return Key = Escape`：顯示／關閉返回確認；可設 None 關閉快捷鍵。不能與 Timing Key 相同。
- `Chinese Font`：可指定支援繁體中文的 TMP Font Asset。留空會嘗試使用本機 PingFang TC、Microsoft JhengHei、Noto Sans CJK 等字型，僅套用在深蹲 UI。
- `View`：同一根物件上的 `SquatMinigameView`，可留空由程式取得。

數值會限制在有效範圍。每一蹲重新設定綠區，保持在兩側紅區之間並保留黃色間隔；色帶寬度和判定採用同一份當次邊界，不要用縮放個別色塊的方式改判定範圍。
中途返回再繼續時沿用該局設定；新設定在下一輪新挑戰生效。

若中文字出現方框，請將支援繁體中文的字型製成 TMP Font Asset，拖入 `Chinese Font`。正式跨平台發佈時建議打包可合法散布的中文字型，不依賴執行裝置安裝字型。
這不需要更動隊友的 TMP 全域設定或其他 UI 字型。

## 4. UI 階層與手動引用

選單／執行時建立的階層如下，亦可依照此結構自行製作：

```text
SquatMinigameUI                 Canvas、Scaler、Raycaster、View、Controller
├─ Backdrop                    Image
├─ Panel                       Image
│  ├─ Title                    TextMeshProUGUI
│  ├─ RepCounter               TextMeshProUGUI
│  ├─ Instructions             TextMeshProUGUI
│  ├─ TimingBar                RectTransform
│  │  ├─ RedLeft               Image
│  │  ├─ YellowLeft            Image
│  │  ├─ Green                 Image
│  │  ├─ YellowRight           Image
│  │  ├─ RedRight              Image
│  │  └─ Pointer               Image、RectTransform
│  ├─ BarLegend                TextMeshProUGUI
│  ├─ Feedback                 TextMeshProUGUI
│  ├─ RecoveryPanel
│  │  ├─ RecoveryLabel         TextMeshProUGUI
│  │  └─ RecoveryTrack         Image
│  │     └─ RecoveryFill       Image
│  └─ ReturnButton             Button + Image，內有文字 Label
├─ CompletionPanel             Image
│  ├─ Heading                  TextMeshProUGUI
│  ├─ Summary                  TextMeshProUGUI
│  └─ ReturnButton             Button + Image
└─ ReturnConfirmPanel          Image
   ├─ Heading                  TextMeshProUGUI
   ├─ Summary                  TextMeshProUGUI
   ├─ ContinueButton           Button + Image
   └─ ConfirmReturnButton      Button + Image
```

Canvas 設定：Screen Space - Overlay，Sort Order 200。
Canvas Scaler：Scale With Screen Size，Reference Resolution 1280 × 720，Match 0.5。
保留 Graphic Raycaster。按鈕的 Navigation 設 None，避免空白鍵兼作按鈕 Submit。
文字的 Raycast Target 關閉；確認面板的背景 Image 保留 Raycast Target，擋住下方按鈕。

`Squat Minigame View` 引用對照：

| 欄位 | 拖入物件／元件 |
|---|---|
| Title、Rep Counter、Instructions、Feedback | Panel 中的同名 TMP 文字 |
| Recovery Label | Panel/RecoveryPanel/RecoveryLabel 的 TMP 文字 |
| Completion Summary | CompletionPanel/Summary 的 TMP 文字 |
| Pointer | Panel/TimingBar/Pointer 的 RectTransform |
| Timing Zones | 長度 5，依序 RedLeft、YellowLeft、Green、YellowRight、RedRight 的 Image |
| Recovery Fill | RecoveryTrack/RecoveryFill 的 Image |
| Recovery Panel、Completion Panel、Return Confirm Panel | 對應面板 GameObject |
| Return Button | Panel/ReturnButton 的 Button |
| Completion Return Button | CompletionPanel/ReturnButton 的 Button |
| Continue Button、Confirm Return Button | ReturnConfirmPanel 中對應 Button |

Pointer 的 X 位置以 TimingBar 的 0～1 錨點控制，不用 Collider。五個色塊的 X 錨點由 Settings 自動設定，Y 錨點為 0～1、Offsets 為 0。
Recovery Fill 使用 Simple Image；程式調整其 X 錨點表示進度，不需要 Slider 或額外 Sprite。
按鈕的 On Click 會在開啟時由控制器註冊，不需手動添加事件，也不要把完成按鈕另接成直接加分。
若採用上述名稱與結構，空引用會嘗試自動綁定；若改名，請依表手動填入。

## 5. 器材、管理器與場景設定

- `gym` 的中文「深蹲」方塊：保持 `GymEquipmentInteractable.equipmentId = Squat`，大小寫相同。
- 保持它的 BoxCollider2D 啟用並勾選 Is Trigger。現有場景已正確設定，不需重做。
- 玩家繼續使用現有 Player 腳本；`GymStageBootstrap` 會加入互動與 2D 物理元件。
- 不必將深蹲控制器掛到玩家或器材上。控制器掛在專用 UI 根物件。
- `GymGameManager` 若由初始化程式自動建立，不需另外新增一個。
- 如果已有手動放置的 GymGameManager，可把 UI 控制器拖入它的 `Squat Minigame` 欄位；未指定時會尋找場景控制器，再視需要自動建立。
- 不必新增 Scene，不必調整 Build Profiles；維持現有 `gym` 場景項目。`jami.unity` 與原本跑步機腳本保留。
- 啞鈴與跑步機的 E 鍵入口仍開啟原本的佔位小遊戲。這次沒有將 `jami` 接入，也沒有替換其他器材玩法。

## 6. 驗收步驟

1. **綠區**：在綠色停住，應顯示「完美！+1」，次數只加一次。
2. **黃區**：在兩側黃色停住，顯示「成功！+1」。
3. **紅區**：停在紅色，指針暫停、顯示力竭與 0/8；另外按 7 次仍未完成，第 8 次才加一次。
4. **按住**：紅區按住空白鍵，不應連續增加恢復次數；回饋期間連按也不會多算。
5. **邊界與難度**：0、0.15、0.85、1 仍歸紅區；當次綠區的左右邊界歸綠區，其餘中間位置歸黃區。完成一蹲後，確認下一蹲的綠區換位置且縮小。
6. **完成**：累積 5 次，完成面板出現，返回後 Squat 僅記錄一次；其餘器材完成進度保留。
7. **返回**：挑戰中按 Esc，指針停止；選繼續可接著玩，選返回後玩家可移動、位置不變；再 E 保留本次 Play 的進度。
8. **回歸**：測試啞鈴／跑步機原有 E 鍵畫面、完成與返回；單獨開啟 `jami` 檢查原有 A／D 小遊戲。
9. **解析度**：以 1280×720、1920×1080 與較窄視窗檢查色帶、文字、恢復條和按鈕。

純邏輯自動測試（本機已有 Python 與 Unity 時）：

```sh
python3 Tests/run_squat_tests.py
```

可在第一個參數指定其他 Unity 安裝的 Contents 目錄。測試不開啟 Editor、不寫入場景。
測試涵蓋分區邊界、紅區額外 8 次輸入、同幀防重複、回饋防重複、5 次結算、反彈、加速度與設定驗證，並以固定亂數種子檢查每蹲換位置、逐步縮小，以及恢復時不重抽。
編譯與純邏輯測試無法替代實際 Game View 的按鍵、UI、字型與返回流程驗收。
