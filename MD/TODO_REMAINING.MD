# OpenTyrian .NET 剩餘問題 TODO

最後更新: 2026-03-13  
目前狀態: 以這份文件作為後續真實進度基線  
總完成度(估算): 約 94%

## 百分比摘要

- 整體完成度(估算): `94%`
- 啟動與 title 主樹: `82%`
- New Game 子樹: `68%`
- Title 其他分支: `82%`
- Title Setup 與 Jukebox: `82%`
- 音樂與音效時機: `84%`
- Full Game / In-game 主流程: `55%`
- Full Game hub / item screen parity: `58%`
- 視覺與版面: `64%`
- 資料與存檔對齊: `60%`
- Input / Demo / Regression: `68%`

## 更新規則

- 每完成一個可建置 checkpoint，就同步更新上面這份百分比摘要
- 若某一組只是做出最小切片，不算完成，只調整到反映實際狀態的估算值
- 之後以「整體至少達到可接受的 `50%` 完善度」作為每輪最基本的品質底線，再往更高對齊原版

## 說明

舊版 `[TODO.MD](/C:/ai_project/netopentyrian/MD/TODO.MD)` 曾經把移植狀態估到 `100%`，但實際與原版 `opentyrian-master` 比對後，仍有明顯入口流程、title 分支、音樂時機、遊戲主迴圈與 UI 流程差距。

這份文件只列「真正還沒完成」或「雖然有切片、但尚未對齊原版流程」的項目，之後一律依這份清單往下做。

## 目前已確認的大缺口

1. 啟動/title 主樹已打通，但細節仍需校正
   - `intro_logos() -> titleScreen()` 已補上
   - logo / title 的淡入淡出與字/圖 timing 仍需持續比對原版
2. 啟動音樂時機已基本對齊，但仍需逐段驗證
   - 前導 logo 已維持靜音
   - title / setup / shop / gameplay 的切歌與淡出入仍要繼續比對
3. title menu 樹仍有未完全對齊部分
   - `Instructions` 已補成 topic/page 流程，但版面仍是近似實作
   - `Demo` 仍是最小 playable slice，不是原版 record/replay
   - `Load Game / Setup / High Scores` 雖已可從 title 正確進入，細節仍需持續校正
4. `New Game` 樹仍有未完全對齊部分
   - `difficultySelect()` 已補上
   - `arcade` 已拆出獨立 menu，不再直接掉進 full-game hub
   - 仍缺更接近原版的 arcade/item/menu 細節
5. `Jukebox` 已可切到真實 `music.mus` 曲目，但畫面與操作細節仍未完全對齊原版
6. `JE_main()` 只做了最小 gameplay slice，不是原版完整主流程

## 依序處理清單

### 第 1 組: 啟動與 title 主樹

狀態: 進行中，已達約 82%

1. `intro_logos()`
   - `pic 10` -> `pic 12` 已補上
   - 已支援按鍵跳過
   - 前導階段已維持靜音
2. `titleScreen()`
   - 背景 `pic 4` 與 7 個 title menu 項目已接上
   - `Quit` 已可從 title menu 正確離開程式
3. `MainMenuScene`
   - 已收回 `New Game -> gameplaySelect()` 的位置
4. 後續只剩 logo/title 的淡入淡出與細部 timing 校正

驗收標準:
- 啟動後先看到兩段 logo，再進 title menu
- title song 在 title menu 才開始
- `Esc` 或 `Quit` 可正確離開程式

### 第 2 組: New Game 子樹

狀態: 進行中，已達約 68%

1. `Start New Game` -> `gameplaySelect()`
2. `gameplaySelect()` -> `episodeSelect()`
3. `episodeSelect()` -> `difficultySelect()`
4. `difficultySelect()` 完成後才建立 session 並進 full game / arcade 流程
5. 已補 `difficulty -> full-game hub / arcade menu` 正確分流
6. arcade / full-game 初始 cash、player count、difficulty 狀態補齊

驗收標準:
- `New Game` 流程順序與原版一致
- 存檔讀回的 difficulty 欄位可正確套回 session

### 第 3 組: Title 其他分支

狀態: 進行中，已達約 82%

1. `Load Game`
   - 從 title menu 直接可進
   - 取消時回到 title menu，不再回錯到 in-game options
2. `High Scores`
   - 已改成 episode 分頁 + `One Player / Two Player` 雙區塊版面
   - 已直接讀 `tyrian.sav` 內前 18 筆 high score record，較接近原版資料來源
3. `Instructions`
   - 已補成 topic menu + 8 page help flow
   - 已讀取 `tyrian.hdt` 內 `helpTxt / miscText / topicName`
   - 仍需持續微調 page box 版面與字距
4. `Demo`
   - 已補 title menu `Demo` route 與 30 秒 idle demo 觸發
   - 已開始直接讀 `demo.1` 到 `demo.5` 的 header 與 key stream
   - demo 目前會以原版 demo 檔驅動最小 gameplay slice，但還不是原版完整關卡/replay

驗收標準:
- title menu 七個選項都能進入對應功能
- 不再有「選項存在但實際跳錯地方」的情況

### 第 4 組: Title Setup 與 Jukebox

狀態: 進行中，已達約 82%

1. 建立 title-level `Setup` 分支
2. `Jukebox` 納入 Setup 必做項
3. `Jukebox` 最低標準
   - 可進入
   - 可切歌
   - 可退出回 Setup
4. `Jukebox` 後續加強
   - 已改成較接近原版的自由操作模式，不再是 action list menu
   - 已補 `Esc/Q` 返回、方向鍵換曲、`S/R` stop/restart、`Space` 隱藏說明
   - 已補原版 VGA palette 與近似環狀星場，但畫面細節仍未完全對齊原版 starlib

驗收標準:
- `Setup -> Jukebox` 可操作
- 使用者已要求的 `Jukebox` 不再缺席

### 第 5 組: 音樂與音效時機

狀態: 進行中，已達約 84%

1. `music.mus -> lds_play -> OPL` 已接入目前 WinForms 版本
2. scene 與 BGM 對應重新整理
3. title/menu/cancel/confirm cue 時機重新對齊
4. 尚未完成的部分
   - level / event 專屬曲目切換仍未完整對齊
   - logo/title/setup/shop/gameplay 的進退場 timing 仍需逐段比對
   - `Jukebox` 已可對原曲做換曲 / stop / restart，不再只靠 placeholder loop

驗收標準:
- logo 不提早播 title song
- title menu、setup、shop、gameplay 已不再使用 placeholder synth
- level/event 曲切換與淡入淡出時機後續再補齊

### 第 6 組: Full Game / In-game 主流程

狀態: 進行中，已達約 55%

1. `JE_main()` 對應的主循環只做了最小切片，需逐步補齊
2. `JE_loadMap()` 對應流程仍未完成
3. 關卡中事件、敵人波次、掉落、level-end、回 item screen 仍不完整
4. arcade/full-game/game-loaded 三條路的收斂方式仍需補

驗收標準:
- 不只可進一個簡化戰鬥房，而是能沿著完整關卡流程前進

### 第 7 組: Full Game hub / item screen / menu parity

狀態: 進行中，已達約 58%

1. `FullGameMenuScene` 已移除 `Session State` 這類 debug 主選項，並改回較接近 item/menu 的畫面
2. `FullGameMenuScene` 仍需要更接近原版 item screen 細節與子選單佈局
3. `Ship Specs / Upgrade / Options / Next Level / Quit` 順序與返回路徑要更貼近原版
4. `Arcade` 已補獨立 `ArcadeMenuScene`，`Options` 也拆出 limited arcade variant
5. `Load Game / Save Game` 與 title/in-game options 的分工要再整理

驗收標準:
- full-game hub 不再像 debug launcher

### 第 8 組: 視覺與版面

狀態: 進行中，已達約 64%

1. title/menu 目前仍有 debug 文案與宿主資訊
2. WinForms status bar 應淡出或只在錯誤狀態顯示
3. 黑邊、縮放和 menu text 位置仍與原版有明顯差距
4. `320x240` 可接受，但 menu 與視窗構圖仍需調整
5. `Level Select / Ship Specs / Data Cubes / Upgrade / Quit` 等二級 scene 已改回 `pic 2` 背景，不再共用 debug title overlay

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

狀態: 進行中，已達約 68%

1. title/menu/demo 的 sendkey / keyboard / mouse 行為要重新驗證
2. 手把行為先維持目前 runtime 綁定
3. ini 持久化依既定決策繼續延後
4. 每完成一組，重新與原版做截圖比對
5. `compare_versions.ps1` 已再次刷新原版 / C# 入口畫面截圖到 `temp/compare-versions`
6. `compare_jukebox.ps1` 已改成逐段導航與分步截圖，但 C# 版自動導覽仍需持續校正 delay / key route
7. title demo 已開始直接讀原版 `demo.1` 到 `demo.5`，不再只是假 route

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
