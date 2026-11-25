@echo off
REM ================================================================
REM Unity 2D Gym Game - Core Systems Specification for Codex
REM This .cmd file is a TEXT REQUIREMENTS DOCUMENT, not an installer.
REM Do not double-click/run it. Ask Codex to READ it as UTF-8.
REM ================================================================
REM Read this file as a specification. The detailed requirements below
REM use lines beginning with REM so execution does not modify files.
REM ================================================================

REM [0. GAME VISION / 遊戲理念]
REM 遊戲想傳達：有夢想就勇敢踏出第一步，不要過度在意別人的眼光；
REM 一點一點累積努力就能接近理想中的自己。
REM 玩家在健身房挑戰五項器材，每項器材各有小遊戲；完成五項即可達成「傲人好身材」目標。
REM 一開始會因為資深健身者靠近而緊張、出現負面內心話；
REM 與對方對話後發現他們其實是友善的人，焦慮下降，操作變回正常。
REM 重要：不要把「不搭話」設計成無法破關；它只是較難，仍可完成。

REM [1. SCOPE / 這一階段要做的功能]
REM 請只做「核心系統」和可測試的 UI / 銜接接口：
REM 1) 玩家既有移動和器材互動沿用，必要時做最小限度修復。
REM 2) 五項器材的進度資料結構：未開始/進行中/已完成；UI 顯示 0/5 ~ 5/5。
REM 3) Anxiety / 焦慮值 (0~100) 與 HUD bar，調整速度、臨界值可在 Inspector 設定。
REM 4) 一名或多名「健身老手 NPC」在可控情境下主動靠近玩家。
REM 5) NPC 靠近時焦慮上升，並有冷卻時間的內心話字幕/對話泡泡。
REM 6) 允許玩家選擇是否對話：選擇談話則播放友善台詞並降低焦慮；
REM    選擇拒絕、忽略或離開，也可以繼續健身。
REM 7) 視覺干擾接口（如震動、四角暗角、魚眼），可依焦慮值計算強度，
REM    但本階段優先做到可測試且不依賴額外付費資源：震動+UI暗角；
REM    魚眼視 URP / Render Pipeline 再提供可選實作，若不支援請跳過並說明。
REM 8) 與後續五種小遊戲連動的共通接口（例如 OnAnxietyChanged、AnxietyNormalized、
REM    GetDifficultyMultiplier、IsMinigameActive），但不要重寫任何現有小遊戲。
REM 9) 五項完成時顯示通關 UI，提供重新開始的操作；無需新增美術素材。

REM [2. SAFETY FIRST / 保護現有成果]
REM 先盤點 Assets 中現有 Scene、Prefab、Scripts、GameManager、UI、玩家控制器、
REM 互動腳本與小遊戲。檢查 Git 狀態與當前 Console errors。
REM 先提出短的實作計畫和將修改/新增的檔案清單，再開始安全修改。
REM 不要刪除/覆蓋隊友合併的 Scene、Prefab、角色動畫、玩家移動、NPC 或現有小遊戲。
REM 不要把整個 SampleScene.unity 以字串方式重寫，也不要手動大量改 Unity YAML GUID/FileID。
REM 優先新增獨立腳本與 Prefab、使用 Inspector 接線；修改既有腳本前說明原因。
REM 如果有未提交變更，保留現狀，不要 Git reset/checkout --/clean/force push。
REM 不要自行引入外部 package。使用專案已採用的 Unity、Input System、TextMeshPro、
REM UI 和 Render Pipeline；若目前使用舊 Input Manager，也應兼容而不擅自切換專案設定。
REM 中文文字請用 UTF-8 存檔並確認 UI 字型能呈現中文。

REM [3. ANXIETY MODEL / 焦慮系統]
REM 建議建立 AnxietyManager（或符合專案命名風格），焦慮值限制 0~100。
REM 預設初始 20；老手進入感應半徑時逐秒提高（例如 +8/s），
REM 離開時逐秒緩慢恢復（例如 -3/s）；談話成功立即降低（例如 -40）。
REM 數字是初版測試參數，均需可由 Inspector 調整。
REM 避免多位 NPC 同時造成焦慮飆升：明確定義疊加上限或最近一位優先規則。
REM 事件可被暫停：在對話視窗、暫停選單、過關畫面時停止增加焦慮。
REM 小遊戲進行時 NPC 移動與焦慮如何處理應有可切換設定，預設凍結 NPC 追逐，
REM 保留當前焦慮值，讓小遊戲以該焦慮值讀取干擾強度。
REM UI 顯示焦慮值與色彩/標籤層級：平穩 0~29 / 緊張 30~59 / 高焦慮 60~100。
REM 焦慮不影響角色永久數值、不導致不可逆的失敗，也不使用健康診斷敘述。

REM [4. NPC APPROACH / 老手主動接近]
REM NPC 平常可遊走或站立；當玩家進入觸發範圍時，NPC 會主動走向玩家，
REM 在一段安全距離停止，面朝玩家並浮出可見的互動提示（例如按 F 交談 / G 忽略）。
REM 預設不要強制直接開始對話；強制的是 NPC 主動靠近，不是玩家必須答話。
REM NPC 不應推動玩家、卡住門口或追逐到不合理距離；應設定停止距離、
REM 追逐超時、冷卻、漫遊/接近/等待/離開等狀態。
REM 建議使用 2D Rigidbody2D.MovePosition 或專案已有的移動系統，
REM 只在 FixedUpdate 做物理移動；遵守牆壁/碰撞，勿穿透其他角色或器材。
REM 如果沒有尋路系統，實作簡單安全方案並在文件中清楚說明限制；
REM 不要自行安裝 NavMesh / 路徑套件。
REM 必須避免 NPC 與 Player 的 Tag/Layer/Collider 引用錯誤。

REM [5. INNER MONOLOGUE / 內心話]
REM NPC 接近且焦慮達到門檻時，在玩家頭頂泡泡或螢幕小字幕隨機顯示，
REM 保持約 2~4 秒，自動淡出；相同句子不要連續重複，事件有冷卻。
REM 初版句庫（可在 Inspector 編輯 / 之後本地化）：
REM 「他為什麼一直看我？」
REM 「我是不是要被嘲笑了？」
REM 「他是不是覺得我做得不標準？」
REM 「我會不會擋到別人？」
REM 「大家是不是都比我厲害？」
REM 不要在 UI 上同時堆疊大量句子；對話期間隱藏內心話。

REM [6. FRIENDLY DIALOGUE / 自由選擇交談]
REM NPC 停下後顯示兩種選擇：「交談」與「先不要」。
REM 可用鍵盤操作與 UI Button 點擊；遵守目前遊戲既有按鍵配置，避免與 E 健身衝突。
REM 玩家選交談：顯示隨機友善台詞，結束後焦慮下降。
REM 例句：「請問可以共用器材嗎？」、「你的健身服真好看！」、
REM 「你剛開始練嗎？不用急，慢慢來就好。」
REM 玩家選先不要：結束提示並設置重觸發冷卻，焦慮不被強制清空；
REM 可以繼續做五項器材，之後也允許玩家重新選擇交談。
REM 對話 UI 彈出時，停止玩家移動輸入（若專案設計允許），關閉後恢復；
REM 不破壞玩家控制器原有功能。

REM [7. SCREEN EFFECTS / 焦慮視覺效果]
REM 實作 AnxietyEffectsController：輸入 normalizedAnxiety 0~1。
REM 焦慮低時無干擾；中等時很輕微鏡頭抖動/四角暗角；高時強度增大。
REM 震動必須使用小幅度、可配置、可關閉，且鏡頭回復到原本位置；
REM 優先與 Camera Follow / Cinemachine 相容，避免直接蓋掉相機追蹤。
REM 暗角可以先採簡單 UI Overlay 漸層素材或專案既有後製；
REM 沒有素材時用基本可測試方案，不要匯入不明圖檔。
REM 魚眼 distortion 僅作 optional hook：依現有 URP/PP 支援狀況實作，
REM 不要擅自更改渲染管線。提供「減少動態效果」選項可關閉震動/魚眼。
REM 小遊戲可自行訂閱效果事件而各自實作不同干擾：本次只做共通接口。

REM [8. FIVE-STATION PROGRESS / 五項進度]
REM 使用穩定唯一 ID 管理五個站點（Station01..Station05 或既有 ID），
REM 展示 0/5 完成數，避免重複進入同一項被累積兩次。
REM API 類似 StartStation(id) / CompleteStation(id) / IsCompleted(id)，
REM 以事件通知 HUD 與通關畫面。可以在 Inspector 設定站點清單名稱。
REM 不需要替五項器材創建全新小遊戲場景；既有項目保持原狀。
REM 只加開發測試入口（例如 Editor-only debug buttons）模擬完成一項，
REM 驗證 5/5 能顯示結局 UI。測試操作不得影響正式發行模式。

REM [9. EXPECTED FILES / 建議模組（依實際專案調整）]
REM AnxietyManager.cs, AnxietyHUD.cs, GymVeteranNPC.cs,
REM InnerThoughtUI.cs, NPCDialogueController.cs, AnxietyEffectsController.cs,
REM GymProgressManager.cs, GymProgressHUD.cs, GymGameBridge.cs。
REM 名稱僅為建議；如果已存在相同功能，優先重用而非重複產生。
REM 每個 script 專注單一功能，避免單一 God Object；處理 null reference、
REM 重複事件訂閱、OnEnable/OnDisable 解除訂閱、Scene 轉換等。
REM 考量 Unity 版本的 C# 語法；使用 serializable data、Inspector 參數、
REM 清楚的事件與 XML / 中文註解，不依賴全域 GameObject.Find 反覆搜尋。

REM [10. UNITY EDITOR SETUP / 請一定輸出詳細步驟]
REM 產生腳本後提供可逐步操作的中文指南：
REM - 檔案放在哪個 Assets/Scripts 子目錄
REM - Hierarchy 需要建立哪些 GameObject、Canvas、UI Text/Slider/Image
REM - 每支 Script 掛在哪個物件、Inspector 每個欄位拖什麼
REM - NPC Prefab、Rigidbody2D、Collider2D、Trigger、Layer 設定
REM - 玩家 Tag、碰撞矩陣、UI Canvas Render Mode、TMP 字型設定
REM - 和現有「按 E 健身」機制如何共存
REM - 不同場景時哪些 Manager 要保留，避免重複單例
REM - 如有必須人工做的素材/Inspector 設定，明確列出，不要假裝已完成

REM [11. TEST / 驗收條件]
REM 1. Play 後玩家正常移動，舊功能仍存在。
REM 2. NPC 先漫遊，再主動接近玩家、保持距離、顯示交談選擇。
REM 3. NPC 靠近焦慮條可見且逐漸增加，離開後逐漸下降。
REM 4. 出現內心話，沒有連續刷屏。
REM 5. 玩家可選「先不要」，依然能進入健身器材；不會被 NPC 卡住。
REM 6. 玩家選「交談」，出現友善話語，焦慮數值下降。
REM 7. 焦慮增加時干擾強度變化；若沒支援某種效果要清楚標註。
REM 8. 五項進度不重複計數，模擬 5/5 顯示通關。
REM 9. Console 不出現編譯錯誤、NullReferenceException、MissingReferenceException。
REM 10. 不覆蓋或破壞原有的 Scene / 玩家 / 健身小遊戲。

REM [12. DELIVERY / Codex 最後請回報]
REM A. 找到的現有架構、曾有的互動程式和本次修改風險。
REM B. 新增/修改檔案清單與每個功能的呼叫關係。
REM C. 完整中文 Inspector 設置流程，按順序列出。
REM D. 怎麼測試 NPC、焦慮、對話、視覺效果、五項進度。
REM E. 哪些功能仍是占位 / 需要後續各小遊戲接入。
REM F. 實際驗證了哪些（編譯/Play Test），哪些只能等待我在 Unity 手動確認。

REM END OF SPECIFICATION - DO NOT EXECUTE THIS CMD FILE.
