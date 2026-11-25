# NPC 漫遊、石頭驚嘆號與心裡話

依 `Unity2D_NPC_Overthinking_Codex.cmd` 實作。Unity 6000.0.43f1；專案目前啟用 Both 輸入模式，已有 Input System、UGUI、TextMeshPro。本功能不讀取任何按鍵。

## 原有內容與本次範圍

原有 `gym`、`jami`、`square`、隊友的 `New Scene`、TMP 範例 Scene 與既有 Prefab 均保留；目前 `gym` 沒有 NPC、石頭、Tilemap 或實體牆 Collider。玩家 `Circle` 的 Player 腳本未變動；既有 GymStageBootstrap 在 Play 時加上 Player Tag、Kinematic Rigidbody2D、CircleCollider2D。E 鍵仍由 PlayerGymInteractor 處理。

本次僅新增腳本、Editor 工具、測試與本說明，沒有修改任何現有資產。備份與原有檔案 SHA-256：`Backups/NPCOverthinking-20261119-153856/`。

**仍需要在 Editor 建立一次系統根物件。程式檔案已就緒，但這個場景設定尚未替你執行或儲存。**

## 最快建立方式

1. 等待 Unity 匯入／編譯完成，停止 Play。
2. 開啟 `Assets/Scenes/gym.unity`，選 `Tools > Gym > Create NPC Overthinking System`。
3. 工具只新增 `NPCOverthinkingSystem`，不修改原物件；若已找到一個管理器，會直接選取它，避免重複建立。可用 Undo 撤回這次新增。
4. 檢查新根物件的位置、`WalkableArea` 的範圍、五個 SpawnPoint 與 `StoneTrigger` 的位置。根物件 Scale 建議保持 `(1,1,1)`。
5. 按 Play，會生成預設 5 名 NPC。玩家先靠近石頭，再離開並進入 NPC 感知圈，才會顯示心裡話。
6. 測試完成後，若要保留此系統，停止 Play 再儲存場景。只有你執行這一步才會把新增物件存入 Scene。

預設階層：

```text
NPCOverthinkingSystem                 OverthinkingManager、NPCSpawner2D
├─ WalkableArea                       BoxCollider2D，Is Trigger，12 × 7
├─ SpawnPoints
│  ├─ SpawnPoint_1 ... SpawnPoint_5     可移動的空物件
├─ ThoughtBubbleCanvas                Screen Space Overlay、Scaler、CanvasGroup、ThoughtBubbleUI
│  └─ ThoughtPanel                    Image，不接收 Raycast
│     ├─ Title                        TextMeshProUGUI
│     └─ Message                      TextMeshProUGUI
├─ StoneTrigger                       CircleCollider2D Trigger、StoneSurpriseTrigger2D
│  ├─ Visual                          SpriteRenderer
│  └─ ExclamationCanvas               World Space Canvas
│     └─ Exclamation                  TextMeshProUGUI，文字 !
└─ GymNPC_1 ... GymNPC_5               只在 Play 時生成
   ├─ Visual                          SpriteRenderer
   └─ DetectionZone                   CircleCollider2D Trigger、NPCThoughtTrigger2D
```

預設石頭約在世界座標 `(2.6, -0.7, 0)`，可自行移動新物件。沒有指定美術時，NPC 和石頭在 Play 時使用彩色／灰色方形替身；不產生圖片檔，也不覆寫原素材。Editor 中替身可能沒有 Sprite，但可選取 Collider 查看位置；Play 後會顯示。可為新的 Visual 指定自己的 Sprite。

## NPCSpawner2D Inspector

選取 `NPCOverthinkingSystem`：

| 欄位 | 預設／設定方式 |
|---|---|
| Manager | 同一根物件上的 OverthinkingManager |
| Npc Count | 5，建議 4～6；下次進入 Play 時生效 |
| Npc Prefab | 可留空使用內建替身；或拖入自訂 NPC Prefab |
| Spawn Points | 預設 5 個空物件 Transform，移動它們即可調整出生位置 |
| Walkable Area | 拖入表示可走地面的 Collider2D；預設是新建的 WalkableArea |
| Obstacle Layers | 勾選實體牆、不可穿越障礙物所在 Layer；預設除 Ignore Raycast 外皆檢查 |
| Minimum Spawn Spacing | 1，NPC 出生點之間的最小距離 |
| Wander Radius | 1.6，各 NPC 相對出生點的活動半徑 |
| Move Speed | 0.65，NPC 走動速度 |
| Perception Radius | 1.5，各 NPC 子物件感知圈半徑 |
| Create Stone If Missing | 開啟。只有系統內沒有 StoneTrigger 時才嘗試另建一個 |

指定出生點無效時，會在 Walkable Area 內嘗試其他安全位置。位置若碰牆、超出可走區或過於接近其他出生點，會跳過；找不到足夠位置時只生成能安全放下的數量並警告一次，不強行把 NPC 放進牆裡。

Spawn Points 是出生位置，不是巡邏路徑。NPC 會在自己的活動半徑內隨機取目標、走動與停留，不會修改或移動 Spawn Points。

## 可走區域與牆壁設定

- `WalkableArea` 代表整塊允許活動的地面，必須在實際地圖內，建議使用獨立 BoxCollider2D／PolygonCollider2D 並勾 Is Trigger。
- 預設 12 × 7 只是目前空間的測試活動框，不代表程式已辨識地板或美術上的牆。請在 Scene View 對齊你的實際地圖。
- 沿用已有實體牆 Collider2D。若牆只有圖片，可新增獨立的 `NPCWallBounds` 空物件及 BoxCollider2D 描出牆面，Is Trigger 關閉；不必改動隊友的美術物件。
- 在 Obstacle Layers 包含這些牆的 Layer。Ignore Raycast 層預設不會擋住 NPC，需使用時自行勾選。
- NPC 在 FixedUpdate 使用 Rigidbody2D.MovePosition，移動前以 CircleCast 檢查完整移動路段，並檢查下一個位置。碰牆會停下再選方向，不要求 NavMesh。
- 身體使用不阻擋玩家的 Trigger，感知圈是另一個子 Trigger；不改 Physics2D 全域設定、不停用玩家碰撞。NPC／玩家之間可穿過，牆由 NPC 自己的移動檢查處理。
- 對沒有 Collider 的美術牆，程式無法可靠判定阻擋。請先完成牆 Collider 與活動框，再驗收不穿牆。

## 自訂 NPC／Prefab

可用 `Tools > Gym > Save New Wandering NPC Prefab` 建立一個新 Prefab。工具拒絕覆寫同名既有檔案。將新 Prefab 拖入 NPCSpawner2D 的 Npc Prefab。

也可手動建立：

1. 建立 `GymNPC` 根物件，Scale `(1,1,1)`。
2. 加 Rigidbody2D：Body Type Kinematic、Gravity Scale 0、Freeze Rotation Z、Simulated 勾選。
3. 加 CircleCollider2D，Radius 約 0.22，Is Trigger 勾選；不要再放會阻擋玩家的實體身體 Collider。
4. 加 `NPCWander2D`。
5. 建立 `Visual` 子物件，加 SpriteRenderer；拖入 NPC Sprite，Sorting Order 可設 5，低於目前玩家的 10。
6. 建立 `DetectionZone` 子物件，加 CircleCollider2D 並勾 Is Trigger，再掛 `NPCThoughtTrigger2D`。
7. 若 NPC 手動放在場景中，兩支腳本的 Manager 都拖入根系統的 OverthinkingManager；Wander 的 Walkable Area 指向地面活動框。
8. 若由 Spawner 產生，Manager、Walkable Area、速度、活動半徑與感知半徑會套用到新實例，不改原 Prefab 資產。

`NPCWander2D` 其他欄位：

- Minimum Pause／Maximum Pause：0.7／2.2 秒。
- Area Center：可留空使用出生位置，或指定一個活動中心 Transform。
- Initial Facing：預設向右，X 為負時起始 Sprite 翻向左。
- Character Renderer：拖入 Visual 的 SpriteRenderer。
- Animator：選填。若使用 Animator，Speed Parameter 指向 Float 參數（預設 `Speed`）；不存在時略過，不會逐幀警告。未提供動畫也能走動。

## OverthinkingManager Inspector 與心理狀態

| 欄位 | 設定方式 |
|---|---|
| Player | 可留空，在 Play 時找現有 Player；手動指定時拖玩家根 Transform，不要拖子 Collider |
| Thought UI | ThoughtBubbleCanvas 上的 ThoughtBubbleUI |
| Global Cooldown | 6 秒，跨 NPC 共用 |
| Same Npc Cooldown | 10 秒，同一名 NPC 的冷卻 |
| Thoughts | 至少放兩句不同的非空白心裡話；預設 5 句 |
| Dialogue Active | 正式對話時暫停心裡話，可由 API 控制 |
| Paused | 額外的暫停開關；Time.timeScale = 0 也會自動抑制 |
| Externally Suppressed | 留給過場或未來其他系統的開關 |
| Reset State On Next Update | Play 中勾一次即可重置石頭事件、文字與冷卻 |

同一句文字即使在文字池中出現兩次，也不會連續顯示。只有一句可用文字時，顯示過一次後會等待不同文案；建議至少保留兩句。
石頭事件預設只記錄本次場景／回合。未建立對話、道具、蛋白粉或加速系統。

## 石頭與世界空間驚嘆號

選取新建的 `StoneTrigger`：

- CircleCollider2D：Is Trigger 勾選，Radius 預設 0.7。
- StoneSurpriseTrigger2D.Manager：拖 OverthinkingManager。
- Exclamation Indicator：拖石頭子物件 `ExclamationCanvas`，不要拖入整顆石頭。
- Allow Repeat：預設關閉；開啟後，每次完全離開再進入可再次出現 !，但不重置心裡話冷卻。
- Exclamation Duration：1.5 秒。
- Pop Height：0.12。

手動建立驚嘆號時，在石頭下建立 World Space Canvas，Rect 80 × 80、Local Scale `(0.01,0.01,0.01)`、Local Position `(0,0.85,0)`、Sorting Order 80。放滿版 TextMeshProUGUI，字串 `!`、字級 64、置中、黃色、Raycast Target 關閉。
預設使用半形 ! 作為驚嘆號圖示，可替換成自己的文字或圖像。指示器跟隨石頭，顯示期間會朝向 Camera.main。
玩家至少一邊要有 Rigidbody2D；目前 gym 的玩家會由既有初始化腳本加上，不需要改玩家控制器。

## 心裡話 Canvas

選取 `ThoughtBubbleCanvas`：

- Canvas：Screen Space - Overlay，Sort Order 80，低於深蹲的 200。
- Scaler：Scale With Screen Size，Reference 1280 × 720，Match 0.5。
- CanvasGroup：Alpha 由腳本控制，Interactable／Blocks Raycasts 都關閉。
- ThoughtBubbleUI.Message：拖 ThoughtPanel/Message 的 TMP 文字。
- Group：拖根 CanvasGroup。
- Display Duration：2.5 秒（包含淡入淡出）；Fade In 0.12、Fade Out 0.4。
- Chinese Font：可指定支援繁體中文的 TMP Font Asset；留空會嘗試本機系統中文字型。若顯示方框請指定字型，跨平台發佈建議打包可合法散布的中文字型。
- ThoughtPanel 預設 X Anchors 0.17～0.83、Y 0.19～0.33，位於 E 鍵提示上方。所有 Image／TMP 的 Raycast Target 關閉。

心裡話是第一人稱想法，不是 NPC 對話。離開產生該句的 NPC 感知圈時立即清除，否則約 2.5 秒後消失。

## 與小遊戲、對話及暫停整合

不用更改 GymGameManager。新管理器會直接讀取既有 `GymGameManager.Instance.IsMiniGameOpen`：

- 深蹲、啞鈴或跑步機佔位小遊戲開啟時，清除內心話、隱藏驚嘆號並暫停 NPC 漫遊。
- 玩家返回後恢復漫遊與正常感知；不排隊補播被阻擋的訊息。需重新離開／進入感知圈，才算新的接近事件。
- 不監聽 E、Space 或 Esc，不改玩家移動、不改深蹲狀態或器材完成進度。

未來正式對話或暫停可呼叫：

```csharp
using FinalProject.Gym.NPC;

// overthinking 為 Inspector 指向的 OverthinkingManager。
overthinking.SetDialogueActive(true);   // 開始對話
overthinking.SetDialogueActive(false);  // 結束對話
overthinking.SetPaused(true);           // 遊戲自己的暫停流程
overthinking.SetPaused(false);
overthinking.SetExternalSuppression(true);  // 過場等額外抑制
overthinking.SetExternalSuppression(false);
```

這些方法也可從 Inspector UnityEvent 綁定 Bool。

## 驗收

1. Play：有 5 個 NPC，各自在青色 Gizmo 活動圈內走走停停；不持續抖動。
2. 有實體牆時：檢查出生點不在牆內、NPC 到牆前停住；走出活動框的候選點被拒絕。
3. 石頭之前：接近 NPC 不出現文字。
4. 首次進入石頭 Trigger：石頭上方 ! 出現約 1.5 秒，之後消失；再進入預設不重播。
5. 石頭之後：進入 NPC 感知圈顯示一條心裡話；站著不會重複觸發。
6. 走離 NPC：當前心裡話立即消失。
7. 快速走過多名 NPC：6 秒全域冷卻內不洗訊息，同 NPC 至少 10 秒；連續兩句不同。
8. 測試玩家含兩個子 Collider：只顯示一次，離開其中一個不應提前清除，全部離開才清除。
9. 開深蹲與其他器材小遊戲：心裡話與 ! 不疊加，NPC 暫停；返回後原角色移動、E 鍵與完成進度正常。
10. 設 Dialogue Active、Paused 或 Time.timeScale = 0：清除心裡話；取消後重新進入感知圈可正常判定。
11. Play 中 Reset State：需離開石頭再進入，才能重新啟用；可另測 Allow Repeat。
12. 檢查 Console 的缺引用警告、被跳過的出生點警告、字型問題；不應有 NullReferenceException／MissingReferenceException。

自動測試：`python3 Tests/run_overthinking_tests.py`，可用第一個參數指定其他 Unity Contents 目錄。涵蓋石頭一次性、重播、全域／個別冷卻、拒絕不消耗冷卻、文案去重與重置。
此測試與編譯不能替代 Unity Game View 的實際碰撞、走動、UI 與字型驗收；這些仍需依上述步驟操作。

## 新增檔案

`Assets/script/NPC/`：OverthinkingSession、OverthinkingManager、ThoughtBubbleUI、NPCPhysics2D、NPCWander2D、NPCThoughtTrigger2D、StoneSurpriseTrigger2D、NPCSpawner2D、NPCOverthinkingFactory。

`Assets/Editor/NPCOverthinkingMenu.cs`、`Tests/OverthinkingSessionTests.cs`、`Tests/run_overthinking_tests.py`、本說明及新腳本的 `.meta`。未修改現有 Scene、Prefab、Player 或健身小遊戲腳本。
