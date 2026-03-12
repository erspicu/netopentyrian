# OpenTyrian .NET 剩餘問題 TODO

最後更新: 2026-03-13  
目前狀態: 以這份文件作為後續真實進度基線  
總完成度(估算): 約 76%

## 百分比摘要

- 整體完成度(估算): `76%`
- 啟動與 title 主樹: `72%`
- New Game 子樹: `54%`
- Title 其他分支: `52%`
- Title Setup 與 Jukebox: `35%`
- 音樂與音效時機: `52%`
- Full Game / In-game 主流程: `45%`
- Full Game hub / item screen parity: `40%`
- 視覺與版面: `55%`
- 資料與存檔對齊: `60%`
- Input / Demo / Regression: `45%`

## 更新規則

- 每完成一個可建置 checkpoint，就同步更新上面這份百分比摘要
- 若某一組只是做出最小切片，不算完成，只調整到反映實際狀態的估算值
- 之後以「整體至少達到可接受的 `50%` 完善度」作為每輪最基本的品質底線，再往更高對齊原版

## 說明

舊版 `[TODO.MD](/C:/ai_project/netopentyrian/MD/TODO.MD)` 曾經把移植狀態估到 `100%`，但實際與原版 `opentyrian-master` 比對後，仍有明顯入口流程、title 分支、音樂時機、遊戲主迴圈與 UI 流程差距。

這份文件只列「真正還沒完成」或「雖然有切片、但尚未對齊原版流程」的項目，之後一律依這份清單往下做。

## 目前已確認的大缺口

1. 啟動流程不對齊
   - 原版是 `intro_logos() -> titleScreen()`
   - 現況是直接進自製 title shell
2. 啟動音樂時機不對
   - 原版 `Epic / Eclipse` 前導不先播 title song
   - 現況是程式一啟動就進 title 類型 BGM
3. title menu 樹不完整
   - 缺 `High Scores`
   - 缺真正 `Instructions`
   - 缺 title-level `Setup`
   - 缺 `Demo`
   - `Load Game` 掛在錯的位置
4. `New Game` 樹不完整
   - 缺 `difficultySelect()`
   - `gameplaySelect()` 目前掛在錯的位置
5. `Jukebox` 尚未完成為原版可用分支
6. `JE_main()` 只做了最小 gameplay slice，不是原版完整主流程

## 依序處理清單

### 第 1 組: 啟動與 title 主樹

狀態: 進行中，已達約 72%

1. 補 `intro_logos()`
   - `pic 10` -> `pic 12`
   - 支援按鍵跳過
   - 前導階段維持靜音
2. 補真正的 `titleScreen()`
   - 背景改成 `pic 4`
   - 項目改成 `Start New Game / Load Game / High Scores / Instructions / Setup / Demo / Quit`
3. 把現在的 `MainMenuScene` 正名成 `New Game -> gameplaySelect()` 的位置
4. `Quit` 從 title menu 真的可離開程式

驗收標準:
- 啟動後先看到兩段 logo，再進 title menu
- title song 在 title menu 才開始
- `Esc` 或 `Quit` 可正確離開程式

### 第 2 組: New Game 子樹

狀態: 進行中，已達約 54%

1. `Start New Game` -> `gameplaySelect()`
2. `gameplaySelect()` -> `episodeSelect()`
3. `episodeSelect()` -> `difficultySelect()`
4. `difficultySelect()` 完成後才建立 session 並進 full game / arcade 流程
5. arcade / full-game 初始 cash、player count、difficulty 狀態補齊

驗收標準:
- `New Game` 流程順序與原版一致
- 存檔讀回的 difficulty 欄位可正確套回 session

### 第 3 組: Title 其他分支

狀態: 進行中，已達約 52%

1. `Load Game`
   - 從 title menu 直接可進
   - 取消時回到 title menu，不再回錯到 in-game options
2. `High Scores`
   - 正確讀取/排序顯示存檔內 high score 資料
3. `Instructions`
   - 先做最小多頁版
   - 後續再逼近原版 `JE_helpSystem()`
4. `Demo`
   - 先做可進入 attract/demo route
   - 後續再補真正 demo record/replay 邏輯

驗收標準:
- title menu 七個選項都能進入對應功能
- 不再有「選項存在但實際跳錯地方」的情況

### 第 4 組: Title Setup 與 Jukebox

狀態: 進行中，已達約 35%

1. 建立 title-level `Setup` 分支
2. `Jukebox` 納入 Setup 必做項
3. `Jukebox` 最低標準
   - 可進入
   - 可切歌
   - 可退出回 Setup
4. `Jukebox` 後續加強
   - track label 對齊原版
   - 若後續補真正 `lds_play` / OPL，Jukebox 要能切到真曲目

驗收標準:
- `Setup -> Jukebox` 可操作
- 使用者已要求的 `Jukebox` 不再缺席

### 第 5 組: 音樂與音效時機

狀態: 進行中，已達約 52%

1. 修正 logo/title/menu 的 BGM 進場時機
2. scene 與 BGM 對應重新整理
3. title/menu/cancel/confirm cue 時機重新對齊
4. 真正原版音樂播放引擎仍屬後續工作
   - 目前合成式 BGM 只能先求流程對位
   - 若要聲音內容也對齊，最終還是要處理 `lds_play.c` / `opl.c`

驗收標準:
- logo 不提早播 title song
- title menu、setup、shop、gameplay 的音樂切換時機合理

### 第 6 組: Full Game / In-game 主流程

狀態: 進行中，已達約 55%

1. `JE_main()` 對應的主循環只做了最小切片，需逐步補齊
2. `JE_loadMap()` 對應流程仍未完成
3. 關卡中事件、敵人波次、掉落、level-end、回 item screen 仍不完整
4. arcade/full-game/game-loaded 三條路的收斂方式仍需補

驗收標準:
- 不只可進一個簡化戰鬥房，而是能沿著完整關卡流程前進

### 第 7 組: Full Game hub / item screen / menu parity

狀態: 進行中，已達約 60%

1. `FullGameMenuScene` 目前偏 debug hub，需要更接近原版 item screen
2. `Session State` 這種 debug 項目之後要退出主流程
3. `Ship Specs / Upgrade / Options / Next Level / Quit` 順序與返回路徑要更貼近原版
4. `Load Game / Save Game` 與 title/in-game options 的分工要再整理

驗收標準:
- full-game hub 不再像 debug launcher

### 第 8 組: 視覺與版面

狀態: 進行中，已達約 45%

1. title/menu 目前仍有 debug 文案與宿主資訊
2. WinForms status bar 應淡出或只在錯誤狀態顯示
3. 黑邊、縮放和 menu text 位置仍與原版有明顯差距
4. `320x240` 可接受，但 menu 與視窗構圖仍需調整

驗收標準:
- 首屏觀感不再一眼看出是 debug shell

### 第 9 組: 資料與存檔對齊

狀態: 待做

1. save/load 目前能運作，但 slot 欄位與 session 套用仍需更完整
2. difficulty / repeated-game / score 欄位要完整 round-trip
3. `High Scores` 顯示來源與排序規則要確認

驗收標準:
- 存檔寫回與讀回不丟重要欄位

### 第 10 組: Input / Demo / Regression

狀態: 待做

1. title/menu/demo 的 sendkey / keyboard / mouse 行為要重新驗證
2. 手把行為先維持目前 runtime 綁定
3. ini 持久化依既定決策繼續延後
4. 每完成一組，重新與原版做截圖比對

驗收標準:
- `temp/compare-versions` 可持續看出差距縮小

## 目前處理順序

接下來固定照這個順序：

1. 第 1 組 啟動與 title 主樹
2. 第 2 組 New Game 子樹
3. 第 3 組 Title 其他分支
4. 第 4 組 Title Setup 與 Jukebox
5. 第 5 組 音樂與音效時機
6. 第 6 組 Full Game / In-game 主流程
7. 第 7 組 Full Game hub / item screen parity
8. 第 8 組 視覺與版面
9. 第 9 組 資料與存檔對齊
10. 第 10 組 Input / Demo / Regression

## 備註

- `Jukebox` 已明確列為必做，不再當可選項
- network 仍維持停用，不回頭打開
- 手把 ini 持久化依既定決策，等主體流程完成後再處理
