@echo off
chcp 65001 >nul
REM ============================================================
REM Unity 2D 健身遊戲：交給 VS Code Codex AI 閱讀的需求說明檔
REM 這個 .cmd 檔案是「需求文件」，不需要執行。
REM 在 VS Code 中打開，請 Codex 讀取本檔並依照內容修改專案。
REM ============================================================
REM 
REM [給 Codex 的總指令]
REM 你是 Unity 2D / C# 遊戲開發助手。請先檢查目前工作區的 Unity 專案結構、
REM 已存在的場景、玩家移動腳本、Input System、Collider2D 與 Rigidbody2D 設定。
REM 以「最小改動、可以立刻測試」為原則實作下面的第一階段功能。
REM 不要覆蓋或重做已經能正常運作的角色移動，不要刪除既有場景或素材。
REM 如果無法直接看到 Unity Inspector，請在最後用繁體中文列出精確的手動設定步驟。
REM 
REM [遊戲完整構想 / 後續擴充方向]
REM 類型：Unity 2D 健身主題遊戲。
REM 玩家在健身房自由移動，操作不同健身器材，每種器材有不同玩法。
REM 例如啞鈴是依節奏按鍵的音樂節奏小遊戲（概念近似太鼓達人）。
REM 完成所有必要器材後，達成「好身材」的通關目標。
REM 過程中有健身老手 NPC 主動搭話，玩家可以接受或拒絕。
REM 接受後可能獲得速度加成、蛋白粉、跳過一項器材等幫助；
REM 拒絕也能完成遊戲，只是難度較高。
REM 本階段不要一次製作完整 NPC、道具及所有小遊戲；先建立可擴充的架構。
REM 
REM [目前已完成]
REM 1. 玩家角色移動已完成，請沿用現有程式碼。
REM 2. 主場景中已有 3 個方塊，暫時代表 3 種健身器材。
REM 
REM [本階段必做：走到方塊 -> 顯示提示 -> 按鍵 -> 進入小遊戲]
REM 1. 每個方塊都有獨立的健身器材識別與小遊戲目的地：
REM    方塊 A = 啞鈴 Dumbbell；方塊 B = 跑步機 Treadmill；
REM    方塊 C = 深蹲 Squat（名稱可在 Inspector 自訂）。
REM 2. 玩家進入方塊周圍的 Trigger Collider2D 範圍時，
REM    在畫面 UI 顯示「按 E 開始：啞鈴訓練」之類提示。
REM 3. 玩家離開偵測範圍時，提示自動隱藏。
REM 4. 只有玩家在偵測範圍內「按下 E 的當下」才切換小遊戲；
REM    不可只靠碰撞就自動進入，按住 E 也不可在回場景後連續觸發。
REM 5. 當兩個 Trigger 區域重疊時，不能因為離開其中一個就誤關提示；
REM    請選擇合理的互動目標處理方式（例如最近的器材）。
REM 6. 按 E 進入對應的小遊戲畫面/Scene；
REM    初期每個小遊戲可只是一個可執行的佔位畫面：
REM    顯示器材名稱、簡單說明、完成按鈕、返回主場景按鈕。
REM 7. 完成後紀錄該器材已完成，返回健身房主場景；
REM    同一器材完成狀態不可因場景切換而消失。
REM 8. 主場景顯示簡單進度，例如「完成器材 1 / 3」。
REM    三項都完成時顯示「恭喜達成好身材！」通關文字。
REM 9. 暫時不做真正的節奏遊戲與 NPC 對話，保留日後替換佔位玩法的空間。
REM 
REM [技術與相容性要求]
REM - 使用 Unity 2D 的 Collider2D / Rigidbody2D / OnTriggerEnter2D / OnTriggerExit2D。
REM - 確認玩家 Tag、碰撞層及至少一方 Rigidbody2D，並解說 Is Trigger 用在哪裡。
REM - 優先沿用專案既有的 Input System 實作；若採用新 Input System，
REM   不要直接混用 UnityEngine.Input.GetKeyDown 而導致 InvalidOperationException。
REM - Unity 與輸入系統版本先由專案檔確認，不可自行假設。
REM - UI 優先使用專案既有 Canvas / TextMeshPro；不要破壞原本 UI。
REM - 互動資料盡量可在 Inspector 設定，如器材 ID、顯示名稱、Scene 名稱。
REM - 需處理 Scene 未加入 Build Settings / Build Profiles 的情況並清楚提醒。
REM - 場景切換時避免重複產生 GameManager，避免 MissingReference / NullReference。
REM - 若採用 DontDestroyOnLoad，確保 Singleton 不重複，狀態可正確保持。
REM - 避免每幀 FindObjectOfType、無必要的 Update 掃描和硬寫場景索引。
REM 
REM [建議可調整的腳本架構]
REM - GymEquipmentInteractable.cs：掛在每個方塊上，儲存器材 ID、名稱、Scene。
REM - PlayerGymInteractor.cs：掛在玩家上，管理靠近、離開、按 E、提示 UI。
REM - GymGameManager.cs：追蹤器材完成狀態、進度與通關結果。
REM - GymMinigameController.cs：小遊戲佔位畫面、完成/返回流程。
REM - GymHUD.cs：主場景提示與完成進度顯示。
REM 命名或拆分可以微調，但避免不必要的複雜架構。
REM 
REM [你需要直接做的事]
REM A. 先讀專案並簡短報告已找到的 Player、Scene、Input、UI 設定。
REM B. 在可存取工作區時，建立/修改必要 C# 腳本；避免覆蓋既有玩家控制。
REM C. 若可以直接建立 Unity Scene / Prefab，請建立；否則給明確的編輯器操作步驟，
REM    包含每個腳本掛在哪個物件、每一個 Inspector 欄位拖入什麼。
REM D. 若需要建立 3 個 Scene，請提供一致的命名方案並列出 Build Settings 步驟。
REM E. 提供第一輪最小可運行測試步驟與常見錯誤排查。
REM F. 最後說明未來如何替換啞鈴場景為節奏玩法，並接上 NPC 加成，先不要實作。
REM 
REM [驗收標準]
REM - Play 時角色仍可正常移動。
REM - 靠近 A/B/C 各自顯示對應「按 E」提示。
REM - 離開器材提示消失，不會殘留。
REM - 按 E 進入正確的小遊戲畫面，能完成並回主場景。
REM - 三項完成狀態正確累積，主場景顯示 0/3 到 3/3。
REM - 任何一項未完成時不顯示通關；三項完成才通關。
REM 
REM [回答語言]
REM 請用繁體中文解釋，C# 程式碼及 Unity 元件名稱保留原英文。
REM ============================================================
exit /b 0
