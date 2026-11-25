# Unity 2D 健身房遊戲：NPC 自由走動＋石頭驚嘆號＋內耗心裡話
# 這是給 VS Code Codex 閱讀的「需求說明文件」，不是可執行腳本。請勿雙擊執行。
# 在 Mac 上仍可用 VS Code 開啟 .cmd 作為文字檔。

【任務背景】
這是正在開發中的 Unity 2D 健身房遊戲。玩家已能移動，場景已與隊友的 Scene 合併；專案另有健身器材互動（靠近後按 E）、各種小遊戲（如深蹲）。現在要新增 NPC 及玩家心理活動的演出。必須先理解專案現況，不可假定既有的腳本名稱、場景名稱、Input System、渲染方式、相機、Tilemap、Animator 等。

【最重要的安全原則】
1. 請先掃描目前 Assets 下 Scenes、Scripts、Prefabs、Canvas、玩家相關腳本及 Git 狀態，提出實作計畫，再做最小必要變更。
2. 不要重建、刪除或覆寫既有 Scene、玩家控制器、健身器材、隊友物件、功能腳本或 Prefab。特別小心此前發生過 SampleScene.unity YAML merge conflict / duplicate identifier。
3. 若需要修改 Scene，先備份；優先使用新增 Prefab 與獨立腳本、獨立 UI，並提供 Unity Editor 手動掛載及 Inspector 設定方式。絕不直接用文字拼接 / 大幅重寫 .unity YAML。
4. 先確認目前使用的是新 Input System、舊 Input Manager 或兩者；不要讓新增功能破壞角色移動與按 E 健身互動。新功能主要靠 2D Trigger 自動觸發，不與 E 互動搶按鍵。
5. 若缺乏必要資源或無法安全判斷引用，先寫出可編譯腳本與清楚配置步驟，不要憑空指定場景物件引用。

【核心體驗與演出流程】
A. 遊戲中生成多名健身房 NPC（建議預設 4–6 名，可於 Inspector 修改數量），平時會在各自的活動範圍內悠閒來回走動，偶爾停下；看起來像健身房其他會員。NPC 不要穿牆或走出可走區域，也不要卡住玩家；若專案有地圖碰撞/走道系統請沿用。第一版以可可靠運作的 2D 漫遊為主，不要求複雜尋路。
B. 場景中有一個「石頭」GameObject（如果原場景沒有，請提供建立 StoneTrigger 物件的方式，不要假設已有）；玩家第一次經過／靠近石頭的觸發區時，在石頭上方短暫彈出一個「！」驚嘆號（世界空間位置，應跟著石頭），並開啟玩家的「容易內耗」心理狀態。
C. 玩家已被石頭事件觸發後，下一次靠近任何 NPC（進入 NPC 周圍的感知 Trigger，預設半徑 Inspector 可調），畫面出現一個短暫的、第一人稱的「心裡話」小文字框，例如：
   - 「他是在看我嗎？」
   - 「我是不是很怪？」
   - 「他會不會覺得我是新手……」
   - 「是不是我動作做錯了？」
   - 「沒事，應該只是我想太多。」
  文案風格可帶一點幽默和社交焦慮，但不侮辱玩家，不要說 NPC 真的在批評或嘲笑玩家。顯示約 2–3 秒後淡出；可支援 Typewriter 打字效果，但先以易運作為優先。
D. 為避免走過好幾位 NPC 就狂洗訊息：全域冷卻（如 5–8 秒，Inspector 可調）、同一名 NPC 個別冷卻（如 10 秒）、重複文字避免連續出現；只在玩家進入感知範圍時觸發（OnTriggerEnter2D），不是每個 Update 重複觸發。
E. 石頭驚嘆號預設僅首次顯示，心裡話功能在該回合之後保持啟用；可 Inspector 選擇是否允許石頭重複觸發。可以提供一個 Debug 開關重置狀態方便測試。
F. 玩家走離 NPC、正在玩深蹲或其他健身小遊戲、正在與 NPC 正式對話、遊戲暫停時，不要疊加/干擾心裡話 UI。進入小遊戲時可清除當前內心話，回到主場景恢復正常判定。請根據專案已有管理器建立相容方案；沒有時提供簡潔、可擴充的開關 API。
G. 專案之後還會有「玩家自主選擇要不要和健身老手搭話、獲得加速或蛋白粉」的對話系統；這次只做路人漫遊＋心裡話，不要擅自完成對話、道具或增益系統，保留未來擴充介面即可。

【建議架構（可依專案調整命名）】
- NPCWander2D.cs：NPC 在指定 2D 邊界或巡邏點內移動/隨機停頓、面向移動方向；支援 Animator 但無動畫也可用。若使用 Rigidbody2D，應避免在 Update 直接修改 transform 造成碰撞問題；在 FixedUpdate 使用 MovePosition（或採符合專案的既有移動方式）。
- NPCThoughtTrigger2D.cs：每名 NPC 的感知 Trigger，玩家接近時向管理器請求顯示心裡話；正確處理 Tag / Layer / 子 Collider。
- StoneSurpriseTrigger2D.cs：石頭附近感應玩家，短暫顯示世界空間「！」，開啟心理狀態。
- OverthinkingManager.cs：記住石頭事件是否發生、全域冷卻、NPC 冷卻、文字池、隨機文案、與小遊戲/對話狀態互斥。
- ThoughtBubbleUI.cs：使用專案現有 Canvas / TextMeshPro（若已安裝）顯示小內心話與淡入淡出；如果沒有 TMP，要提供替代方案或清楚說明安裝依賴。
可以合併/減少腳本數量，但要保持各部分責任清楚、Inspector 可設定。

【技術與 Inspector 需求】
- 玩家請沿用現有 Player Tag 或明確指出需要設定哪個 Tag，並使用 Collider2D；確保相關 2D 物件之間至少有一個 Rigidbody2D，且 Layer Collision Matrix 允許 Trigger 事件。
- NPC 身體碰撞和感知 Trigger 建議分離（例如 NPC 根物件 + 子物件 DetectionZone 的 CircleCollider2D / Is Trigger）；避免 NPC 被自己的感知 Collider 推開，避免和玩家移動互相干擾。
- NPC 若需 Prefab：請做一個可重複使用的 NPC Prefab 或詳述建立流程，生成點可在 Inspector 拖入空 GameObject / Transform；無有效生成點時提供可測試預設，不應出現在牆內。
- NPC 漫遊需要可配置：移動速度、停留秒數、巡邏半徑/活動區域、開始朝向、感知半徑；所有變數應有合理範圍與註解。
- 「！」用可替換的 TMP 或世界空間 Canvas 顯示在石頭上方（不是 Unity Console）；UI 要注意 Sorting Order、Canvas Render Mode、攝影機方向、遊戲畫面縮放。
- 心裡話建議固定在畫面下方或角色頭上附近的清楚位置，請依現有 UI 佈局選擇不遮擋健身互動提示的位置；至少支援文字內容、顯示時間與淡出。
- 善用 OnTriggerEnter2D、OnTriggerExit2D，並提供最少的 Debug.Log 訊息協助定位問題；不可在每幀大量輸出 Console。
- 不使用需要付費 Asset 的方案，不額外安裝不必要套件。程式需可在現有 Unity 版本編譯。

【預期測試案例／驗收標準】
1. Play 後多名 NPC 會各自慢慢走動、偶爾停下，沒有一直原地抖動或穿牆。
2. 尚未靠近石頭前，經過 NPC 不會觸發心裡話。
3. 玩家走過石頭觸發區，石頭上方顯示「！」然後消失。
4. 此後玩家第一次進入 NPC 範圍，畫面顯示一條心裡話並自動消失。
5. 玩家站在 NPC 範圍內不會每幀跳訊息；快速走過多名 NPC 不會洗版。
6. 角色正常移動，健身器材靠近按 E、深蹲與其他小遊戲正常，隊友 Scene 的內容沒有被覆蓋。
7. 移動範圍、NPC 數量、驚嘆號時間、心裡話內容、冷卻時間皆可以調整。
8. Console 無 NullReferenceException、MissingReferenceException、場景 YAML 冲突或重複 File ID。

【請 Codex 最終交付】
I. 列出新增／修改的具體檔案路徑，以及與現有系統的整合方式。
II. 提供完整 C# 腳本（可直接用於專案）與 Unity Inspector 逐步設定（建立 NPC、石頭 Trigger、玩家 Tag、Canvas 與文字元件、Prefab/生成點等）。
III. 說明如何連到「小遊戲進行中」狀態以暫停內心話觸發；若專案還沒有相關 API，給出可手動呼叫的方法。
IV. 用明確測試步驟驗證每項功能；若有場景設定需人工操作，請不要宣稱已自動完成。
V. 僅在完成備份/確認後才修改現有資產，不要任意改動角色移動、現有 Scene 或已有健身小遊戲。

【開始工作】
先回報你掃描到的 Unity 版本、現有 Scene/玩家/小遊戲入口、可能碰撞管理方式，以及你計畫新增的腳本與 GameObject 結構；然後在安全範圍內實作，最後詳細說明 Unity Editor 中需要如何操作與測試。
