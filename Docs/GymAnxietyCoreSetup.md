# 健身焦慮核心系統：Hierarchy、Inspector 與驗收

已完整閱讀 `Unity2D_Gym_Anxiety_Core_Codex.cmd`，依目前 Unity 6000.0.43f1、Input System 1.13.1、UGUI／TextMeshPro、URP 17.0.4 實作。不安裝新套件、不更動渲染管線。

## 1. 本次保留與擴充

現有 `gym.unity` 已有 NPCOverthinkingSystem、五個生成點、石頭與心裡話 UI；玩家沿用 Player 與 GymStageBootstrap；E 鍵由 PlayerGymInteractor 處理；深蹲為真實的小遊戲，其他器材仍有原本的佔位流程。`jami.unity` 的 A／D 跑步機獨立保留，這次不替換或強行載入。

本次未直接修改任何 Scene、Prefab、玩家移動、動畫、Input 設定、URP 設定或深蹲判定。修改前備份：`Backups/GymAnxietyCore-20261122-165223/`，含目前 Scene 與將修改的原腳本；`original-assets-sha256.json` 記錄原有資產。

**程式與建立工具已完成；下方的 Editor 建立、掛載及 Game View 驗收，仍需要你操作，尚未自動替你儲存到 Scene。**

## 2. 最快開始

1. 等 Unity 匯入／編譯完成，停止 Play，開啟 `Assets/Scenes/gym.unity`。
2. 選 **Tools > Gym > Create Anxiety Core System**。
3. 只會新增 `GymAnxietyCore` 和獨立 Canvas。若已存在 Core，選單直接選取它；不重建 NPC 或改動原場景內容。
4. 選新根物件，在 GymGameBridge 的 Player 可拖入 `Circle` 上的 Player 元件；留空也會在 Play 時找現有 Player。
5. 檢查下列 Inspector 設定，按 Play。預設沿用場景 NPCSpawner2D 生成的 5 名路人，將最接近玩家的 2 名當作老手。
6. 靠近老手，觀察焦慮條、內心話與接近行為；停下後可按 F 交談、G 先不要，也可直接走開或按 E 使用器材。
7. 測試後停止 Play；確認要保留新增根物件時，再自行儲存 Scene。可先另存場景備份。

未找到既有 NPC 系統且 `Create Npc Support If Missing` 開啟時，只會在 Play 中建立獨立的測試 NPC 系統，不寫入 Scene 檔。既有 NPC 系統存在時不另建第二份。

## 3. Hierarchy 與腳本位置

```text
gym（原 Scene 保留）
├─ Circle                           原 Player；不掛新控制器
├─ 原器材、Camera、EventSystem       保留
├─ NPCOverthinkingSystem             原系統保留
│  ├─ WalkableArea、SpawnPoints、StoneTrigger...
│  └─ GymNPC_*                       Play 時生成
│     ├─ NPCWander2D                 原漫遊；老手接近時暫時讓出移動控制
│     ├─ GymVeteranNPC               選中的老手在 Play 時自動加上
│     └─ DetectionZone               原感知圈保留
└─ GymAnxietyCore                    新增、Scene 所屬，勿設 DontDestroyOnLoad
   ├─ GymGameBridge
   ├─ NPCDialogueController
   ├─ AnxietyEffectsController
   ├─ AnxietyHUD
   ├─ GymProgressHUD
   ├─ GymInnerThoughts
   └─ GymCoreCanvas                  GymCoreUI、Canvas、Scaler、Raycaster
      ├─ AnxietyVignette             AnxietyVignetteGraphic
      ├─ AnxietyHUD                  焦慮文字、Track/Fill
      ├─ ReduceMotion                Toggle
      ├─ ProgressHUD                 完成數、五項狀態
      ├─ VeteranChoice               F／G 提示與兩個 Button
      ├─ FriendlyDialogue            友善台詞、繼續訓練 Button
      ├─ VictoryPanel                通關、重新開始 Button
      └─ ThoughtBubbleCanvas         沿用 ThoughtBubbleUI 實作

DontDestroyOnLoad（Play 時）
└─ GymGameManager                    原有常駐管理器
   ├─ GymProgressManager             自動補上，五項進度唯一資料來源
   ├─ AnxietyManager                 自動補上，本回合焦慮資料
   └─ SquatMinigameUI                原本的小遊戲 UI（首次使用時建立）
```

不要把 GymGameBridge 掛到玩家或隊友的總根物件；使用獨立 `GymAnxietyCore`。不要另外建立第二份 GymGameManager、GymProgressManager 或 AnxietyManager。
`GymCoreModels.cs` 是純資料／規則，不需要掛到任何物件。

## 4. GymGameBridge Inspector

| 欄位 | 拖入／設定 |
|---|---|
| Player | 原玩家的 Player 元件；不是子 Collider |
| Ui | GymCoreCanvas 的 GymCoreUI；選單已接好，可留空找子物件 |
| Stations | 必須五個非空、唯一、大小寫一致的 ID，見下一節 |
| Anxiety Settings | 初始值、增減速率、臨界值與友善減少量 |
| Freeze Anxiety During Minigames | 預設開啟；進入小遊戲後保持當前焦慮值 |
| Create Npc Support If Missing | 預設開啟；只有沒有既有 NPC 系統時建立測試支援物件 |
| Veteran Count | 預設 2，控制自動選取的老手數量 |
| Veteran Candidates | 可留空，從本 Scene 的 NPCWander2D 中優先選距離近的；也可指定想成為老手的 NPC |
| Automatic Veteran Settings | 自動加上的老手之感知、停止距離、追逐時間與冷卻設定 |
| Automatic Veteran Speed | 預設 0.8 |
| Freeze Veterans During Minigames | 預設開啟，暫停老手接近與狀態計時 |
| Paused | 可用此開關測試暫停；也可呼叫 SetPaused(bool) |

NPC 由 Spawner 在 Play 時產生時，場景中尚沒有對應實例可拖；此時使用 Veteran Count 與 Automatic Veteran Settings 即可。若在 NPC Prefab 上手動掛 GymVeteranNPC，該 Prefab 的老手設定會保留，不以自動參數覆寫。

目前正在玩的回合若已有進度，切到另一個 Scene 不會用不同 ID 清空它。請在各 Scene 使用同一組五項 ID；名稱與設定在開局前配置，不要把 Play 中的常駐 Manager 修改當作永久儲存。

## 5. 五項器材與現有 E 鍵

預設資料：

| ID | 名稱 | 目前接入狀態 |
|---|---|---|
| Dumbbell | 啞鈴 | 原 E 鍵佔位畫面與完成按鈕 |
| Treadmill | 跑步機 | 原 E 鍵佔位畫面；獨立 jami 尚未接入 |
| Squat | 深蹲 | 原真實深蹲，綠區移動縮小、五次結算均保留 |
| Bike | 健身車 | 新增進度 ID；需要設定器材 Trigger，先沿用佔位畫面 |
| LegPress | 腿推機 | 新增進度 ID；需要設定器材 Trigger，先沿用佔位畫面 |

只進入器材：未開始 → 進行中；返回但未成功：保持進行中；真正完成後才變已完成。
重複完成同一 ID 不會多算。進度顯示 0/5 到 5/5；交談、焦慮或石頭事件都不是完成器材的前置條件。

若要讓後兩項也能按 E 測試，不必改動原健身器材美術：

1. 在 Hierarchy **新增**空物件 `BikeInteraction`，放到健身車旁；另新增 `LegPressInteraction` 放腿推機旁。
2. 各加 BoxCollider2D，勾 Is Trigger，大小按互動範圍調整。
3. 各掛原有 GymEquipmentInteractable。
4. Equipment Id 分別填 `Bike`、`LegPress`；Display Name 填健身車、腿推機，Description 可填佔位訓練說明。
5. 玩家沿用既有 Rigidbody2D／Collider2D；確認 Layer Collision Matrix 允許兩者 Trigger。
6. 這兩個新增 Trigger 會被原 PlayerGymInteractor 自動辨識，按 E 仍呼叫同一個 GymGameManager，不另外掛按鍵腳本。

不要把現有 Dumbbell／Treadmill／Squat 的 ID 改掉；也不要將未設定的裝飾物隨意填成重複的不同站點 ID。

## 6. 焦慮與共通接口

開局預設 20；範圍固定 0～100。任一合格老手正在接近／等待且在感應範圍內，每秒 +8；沒有來源每秒 -3。
多名 NPC 不疊加速率：只選最近一名作為來源。友善對話結束立即 -40，另有 4 秒安心緩衝，期間不再次上升。

| Anxiety Settings 欄位 | 預設 |
|---|---:|
| Initial Value | 20 |
| Increase Per Second | 8 |
| Recovery Per Second | 3 |
| Friendly Reduction | 40 |
| Reassurance Seconds | 4 |
| Tense Threshold | 30 |
| High Threshold | 60 |
| Maximum Difficulty Multiplier | 1.35 |

HUD 分級：平穩 0～29、緊張 30～59、高焦慮 60～100。焦慮不改 Player 的永久速度或屬性，沒有焦慮致死／不可逆失敗。
對話、暫停與通關時停止變動；小遊戲預設也停止變動，仍可讀當前值。若取消 Freeze Anxiety During Minigames，則按當時附近 NPC／恢復規則繼續更新。

未來小遊戲可接：

```csharp
using FinalProject.Gym.Core;

GymGameBridge core = GymGameBridge.Instance;
float anxiety01 = core != null ? core.AnxietyNormalized : 0f;
float difficulty = core != null ? core.GetDifficultyMultiplier() : 1f;
bool minigameActive = core != null && core.IsMinigameActive;

// 使用時訂閱，停用時用同一個方法解除訂閱：
// core.Anxiety.OnAnxietyChanged += HandleAnxiety;
// core.Anxiety.OnAnxietyChanged -= HandleAnxiety;
// 回呼參數為 0～100；需要 0～1 時讀 AnxietyNormalized。
```

本次不把 difficulty 直接乘進深蹲指針、跑步機或玩家移動。共通接口可用；各小遊戲專屬難度需後續明確接入，避免重寫原玩法。

## 7. 老手 NPC 與碰撞

狀態依序是漫遊 → 接近 → 等待 →（可選交談）→ 離開 → 冷卻。

| Automatic Veteran Settings／GymVeteranNPC.Settings | 預設 |
|---|---:|
| Detection Radius | 4 |
| Stop Distance | 1.25 |
| Chase Limit | 6 |
| Home Leash | 6 |
| Approach Timeout | 8 秒 |
| Waiting Timeout | 8 秒 |
| Return Timeout | 8 秒 |
| Cooldown | 15 秒 |

感知使用與玩家的 2D 距離，不需新增會阻擋玩家的碰撞圈。接近與返回只在 FixedUpdate 使用 MovePosition；過程暫時停用原 NPCWander2D，避免兩支腳本同時移動 Rigidbody2D。正常漫遊仍使用原腳本。
老手使用原 NPC 的 Walkable Area 和 Obstacle Layers；核心啟用期間會額外避開玩家、NPC 身體與器材互動區。感知 Trigger 與石頭 Trigger 不當作實體障礙。

原 NPC Prefab 應有：Kinematic Rigidbody2D、Gravity Scale 0、Freeze Rotation Z、Simulated 勾選；根 CircleCollider2D 為 Trigger；DetectionZone 是獨立子 Trigger。玩家可穿過 NPC，因此不會被卡住。

目前不做路徑搜尋。隔牆、器材擋路時，老手可能走不到玩家，會安全停住並在超時後離開／冷卻，不穿牆。返回若被堵住而超時，後續從目前安全位置重新漫遊，避免永遠卡在舊活動圈外。
沒有 Collider 的牆圖片無法阻擋移動，請沿用實體牆 Collider；如需補牆，可新增獨立牆碰撞物件，不必覆寫隊友美術。
若手動指定固定 Area Center，請讓追逐活動範圍與該中心的 Wander Radius 相容；預設不使用固定中心。

## 8. 交談、忽略與內心話

- 老手停下後，右側出現交談／先不要按鈕，鍵盤預設 F／G，不使用 E。
- 提示期間玩家仍可移動、離開、按 E 開始健身；忽略或等待超時不扣進度，也不強制清空焦慮。
- 選交談後，暫時停用原 Player 元件並阻擋新的 E 鍵進入。顯示友善台詞後按 F、Esc 或「繼續訓練」結束；恢復原 Player.enabled 狀態。
- 正式對話時清除內心話並凍結焦慮；結束友善對話減少焦慮，老手離開。這次不做道具、蛋白粉或永久增益。
- NPCDialogueController 可調 Talk Key、Ignore Key 和 Friendly Lines；E 保留給器材。

GymInnerThoughts 使用既有 ThoughtBubbleUI 與 OverthinkingSession 規則：焦慮門檻 30、全域冷卻 6 秒、同一 NPC 冷卻 10 秒，文案可在 Inspector 編輯。一次只顯示一條，不連續重複同一句。
此核心的焦慮內心話不需先碰石頭；核心啟用時只抑制舊系統的心裡話通道，保留石頭與漫遊。停用核心會解除自己持有的抑制，不會覆寫其他系統設定。
NPC 離開有效距離、正式對話、小遊戲、暫停或通關時清除字幕。

## 9. Canvas 與 Inspector 引用

選單已接好所有 UI；若手動重做，所有引用指向**新 Canvas**，不要把隊友既有 Canvas 直接拿來替換。

- Canvas：Screen Space - Overlay、Sort Order 280。
- CanvasScaler：Scale With Screen Size、1280×720、Match 0.5。
- GraphicRaycaster：保留；若場景已有 EventSystem 則沿用，不另建第二個。
- GymCoreUI.Chinese Font：拖入支援繁體中文的 TMP Font Asset。留空嘗試本機中文字型；跨平台發佈建議使用可合法散布的字型資產。
- 所有純文字、暗角、HUD 背景 Raycast Target 關閉；按鈕與 ReduceMotion Toggle 背景開啟。按鈕／Toggle 的 Navigation 為 None，不搶深蹲 Space Submit。

GymCoreUI 欄位對照：

| 欄位 | 拖入 |
|---|---|
| Anxiety Fill／Text | AnxietyHUD/Track/Fill 的 Image、AnxietyHUD/AnxietyText 的 TMP |
| Reduce Motion Toggle | ReduceMotion 的 Toggle |
| Progress Text／Station Status Text／Panel | ProgressHUD 下兩段 TMP、ProgressHUD 物件 |
| Choice Panel／Text | VeteranChoice 與 ChoiceText |
| Talk Button／Ignore Button | VeteranChoice 下兩個 Button |
| Dialogue Panel／Friendly Text／Close Button | FriendlyDialogue、FriendlyText、ContinueButton |
| Victory Panel／Restart Button | VictoryPanel、RestartButton |
| Thought UI | ThoughtBubbleCanvas 的 ThoughtBubbleUI |
| Vignette | AnxietyVignette 的 AnxietyVignetteGraphic |

按鈕事件在 Bind 時由腳本註冊，不需再手動加一次 On Click，避免雙重呼叫。
AnxietyHUD、GymProgressHUD、NPCDialogueController、GymInnerThoughts 都掛在新根物件；GymGameBridge 負責接線。

## 10. 視覺效果與減少動態

AnxietyEffectsController 可指定 Target Camera（預設 Camera.main）、Shake Enabled、Maximum Shake 0.025、Shake Frequency 12，以及 Reduce Motion。
低於緊張門檻無干擾，之後平滑增強。震動只在相機渲染前暫時偏移 view matrix，渲染後還原；不寫 Camera transform，也不改 Follow／Cinemachine 元件。停用核心或效果時會還原。
若有多台 Camera 或其他自訂 view matrix 效果，請明確指定相機並實際驗證效果順序。

AnxietyVignetteGraphic 以原生 UI 漸層網格繪製暗角，無外部圖片；預設 Edge Width 0.22、Maximum Alpha 0.32，可調低或設 0。暗角不攔截按鈕點擊。
右上「減少動態效果」會停用震動，並將可選魚眼接口輸出設 0；暗角本身仍可保留。

**本次沒有啟用魚眼，也沒有改 URP Volume／Profile。** `OptionalDistortionIntensityChanged` 與 `OptionalDistortionIntensity` 留給日後接既有後製。現有深蹲只受到共通暗角覆蓋，不更改綠區判定、速度或恢復次數。

## 11. 五項通關、重新開始與跨 Scene

完成第五項後，先讓目前的小遊戲結束／返回，再顯示通關 UI，避免蓋住深蹲的完成按鈕。
「重新開始」清空五項狀態、焦慮回到初始值、清除新舊內心話冷卻、重置老手狀態與深蹲局內次數，不重載或覆寫 Scene，不改玩家位置。

GymGameManager、GymProgressManager、AnxietyManager 同在既有 DontDestroyOnLoad 根物件上，跨 Scene 保留同一回合。Scene 所屬的 Core／UI／NPC 會正常卸載；各需要此功能的 Scene 最多放一個 Core。
Core 停用會恢復玩家、舊 HUD、舊 NPC 與抑制狀態；重複 Core 的 UI／元件會被停用，不刪除場景物件。
器材所屬 Scene 卸載時，管理器會收起舊小遊戲 UI、清除已失效的玩家／器材引用，但不清空器材完成資料。
正式 Scene 切換仍需將目標加入 Build Profiles；本次未修改清單，也沒有自動連到 jami。

## 12. Editor 專用測試入口

1. Play 後在 Hierarchy 展開 DontDestroyOnLoad，選 GymGameManager。
2. 找到自動附加的 GymProgressManager。
3. Inspector 會顯示「模擬完成：啞鈴／跑步機／深蹲／健身車／腿推機」按鈕。
4. 小遊戲開啟時按鈕停用；各按一次，測試 0/5～5/5、去重計數、通關及重新開始。

測試按鈕只存在 `Assets/Editor/GymCoreMenu.cs`，不會出現在正式遊戲畫面或 Player Build。
正式小遊戲成功時呼叫 `core.Progress.CompleteStation(id)`，進入時 `StartStation(id)`；不要每幀呼叫完成。

## 13. 驗收清單

1. **原功能**：移動、D／T／S 的 E 鍵提示、原小遊戲及深蹲五次／綠區變化正常。
2. **接近**：老手先漫遊，進入感知範圍後接近，距離約 1.25 停住；不推人、不穿實體牆或器材。堵住路線會超時。
3. **焦慮**：初始 20，附近 +8/s，離開 -3/s；兩名老手同時靠近也不疊成 +16/s。
4. **內心話**：門檻後顯示一條並淡出，冷卻內不洗版；離開／開小遊戲／對話時清掉。
5. **拒絕**：按 G 或走開，NPC 離開冷卻，玩家仍能按 E 訓練；不靠交談也可完成全部五項。
6. **友善**：按 F，Player 暫停、E 不開新遊戲；結束後 -40 並恢復輸入，重複點關閉不會重複扣焦慮。
7. **效果**：高焦慮時暗角／輕震動可見，減少動態關掉震動；關閉效果／Core 後相機沒有殘留偏移。
8. **小遊戲**：預設 NPC 追逐與焦慮凍結，保留當前焦慮讀取接口；返回正常恢復。
9. **進度**：進入未完成為進行中，重複成功只算一次；第五項結束後才出通關 UI。
10. **重開**：0/5、初始焦慮，深蹲重新從 0/5 起，不殘留前回合局內次數。
11. **跨 Scene／停用**：舊 UI 關閉、事件解除訂閱；不留玩家 disabled、不留相機偏移，不產生重複資料管理器。
12. **Console**：檢查缺引用、無效 ID、NPC 路線設定等警告；不應有編譯錯誤或 NullReferenceException／MissingReferenceException。

自動測試：

```sh
python3 Tests/run_gym_core_tests.py
python3 Tests/run_squat_tests.py
python3 Tests/run_overthinking_tests.py
```

純邏輯測試涵蓋焦慮增減／上下限／暫停／緩衝／難度上限、五項狀態／去重／重置、NPC 接近／停下／交談／忽略／追逐超時／冷卻，以及不交談仍可過關。
遊戲與 Editor 腳本使用本專案 Unity 附帶編譯器驗證。這些不能取代 Game View 的碰撞、按鍵、UI、相機效果、Scene 切換與字型測試。
目前系統 Editor.log 指向另一個專案 `0917`，未將其錯誤當作本專案 Console；請打開 FinalProject 後實際驗收。

## 14. 檔案與修改原因

新增 `Assets/script/GymCore/`：GymCoreModels、GymProgressManager、AnxietyManager、GymVeteranNPC、NPCDialogueController、GymInnerThoughts、AnxietyHUD、GymProgressHUD、GymCoreUI、GymCoreFactory、AnxietyEffectsController、AnxietyVignetteGraphic、GymGameBridge。
另新增 `Assets/Editor/GymCoreMenu.cs`、`Tests/GymCoreModelTests.cs`、`Tests/run_gym_core_tests.py`、本說明與新腳本 `.meta`。

原腳本只擴充四支：

- GymGameManager：以五項進度取代三項 HashSet，加入對話輸入閘門／外部 HUD、開局重置與 Scene 卸載清理，保留原小遊戲入口。
- SquatMinigameController：只新增回合重置／Scene 關閉接口，不改狀態機或判定。
- NPCWander2D：提供活動框／Layer 讀取、核心老手可選的角色／器材避讓，以及返回超時後安全恢復漫遊；沒有核心時維持原規則。
- OverthinkingManager：加入有擁有者的抑制接口，避免兩套內心話疊加，解除時不覆寫其他系統的開關。

仍待後續：Bike／LegPress 的專屬真實小遊戲、jami 跑步機與主健身流程的成功／返回銜接、各遊戲讀取焦慮後的獨特難度、可選魚眼。這次只完成共通核心與可測試接口，不偽稱已完成五種新玩法。
