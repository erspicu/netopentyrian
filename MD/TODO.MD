# OpenTyrian C#/.NET 10 移植 TODO

最後更新: 2026-03-12
目前狀態: 第 0、1 階段完成，第 3、4 階段進行中

## 目標

將 `opentyrian-master` 移植為 `C# + .NET 10 + Windows Forms` 版本，並符合以下限制:

- 不使用第三方 library
- 視窗以 WinForms 建立
- 畫面輸出以 `Panel + Win32 GDI` 為主
- 鍵盤、滑鼠、手把、音效直接使用 .NET 內建功能或 Win32 API
- 將平台層與遊戲邏輯核心切開，方便日後移植其他平台
- 執行時使用 `tyrian21` 作為遊戲資料目錄
- 可參考或重用 `tool` 目錄內現有 Win32 / GDI / 輸入 / 音效工具程式

## 目前目錄

- 原始碼: `opentyrian-master`
- 遊戲資料: `tyrian21`
- 現有工具: `tool`
- 任務文件: `MD/TODO.MD`

## 目標架構

建議拆成以下專案:

1. `OpenTyrian.Core`
   純遊戲邏輯、資料格式、軟體 framebuffer、palette、sprite、font、關卡、碰撞、主流程。
2. `OpenTyrian.Platform`
   平台抽象介面，例如 `IVideoDevice`、`IAudioDevice`、`IInputDevice`、`IFileSystem`、`ITimer`。
3. `OpenTyrian.WinForms`
   WinForms 啟動專案，負責 Form、Panel、GDI、Win32 API、輸入綁定、音效設備初始化。
4. `OpenTyrian.Tools` 或沿用 `tool`
   放現有的 GDI、DirectInput/XInput、waveOut、縮放器等工具碼。

核心原則:

- `Core` 不直接依賴 WinForms
- `Core` 不直接依賴 GDI / Win32 / waveOut
- 平台相關邏輯集中在 WinForms / Platform 層

## 上游原始專案對應重點

重要入口與平台耦合模組:

- `src/opentyr.c`: 程式入口與主流程
- `src/video.c`: SDL 視窗、渲染、最終輸出
- `src/keyboard.c`: 鍵盤輸入
- `src/mouse.c`: 滑鼠輸入
- `src/joystick.c`: 手把輸入
- `src/sndmast.c`: 音效主控
- `src/lds_play.c`: 音樂播放
- `src/opl.c`: OPL/FM 音源模擬

可優先重用或參考的現有工具:

- `tool/NativeRendering.cs`
- `tool/InterfaceGraphic.cs`
- `tool/joystick.cs`
- `tool/WaveOutPlayer.cs`
- `tool/NativeAPIShare.cs`
- `tool/DirectInputHelper.cs`

## 移植策略

不要直接逐檔把 C 全部翻成 C#。

正確做法:

1. 先建立 .NET 解決方案與平台抽象
2. 先打通畫面輸出骨架
3. 再處理資料載入
4. 再移植 palette / sprite / font / background
5. 再移植輸入與音效
6. 最後打通主選單與遊戲流程

## 階段規劃

### 第 0 階段: 專案骨架

目標:

- 建立 `.NET 10` WinForms solution
- 建立 `Core / Platform / WinForms` 分層
- 建立最小可執行主程式

工作項目:

- 建 solution 與 csproj
- 設定 `net10.0-windows`
- 設定 unsafe / AllowUnsafeBlocks
- 建立基本命名空間與資料夾
- 導入 `tool` 內可重用程式

驗收標準:

- 專案可編譯
- WinForms 視窗可開啟

狀態: 已完成

### 第 1 階段: 視訊平台層與 GDI 輸出

目標:

- 以 Form + Panel 建立遊戲視窗
- 用 Win32 GDI 將軟體 framebuffer 輸出到 Panel

工作項目:

- 定義 `IVideoDevice`
- 建立 `WinFormsVideoDevice`
- 接上 `tool/NativeRendering.cs`
- 建立 320x200 或 320x240 軟體畫面緩衝
- 建立基本 frame pump / timer
- 視窗 resize 時維持正確顯示

驗收標準:

- Panel 可穩定刷新
- 可顯示測試圖、色塊、簡單動畫
- 不依賴 SDL 或第三方繪圖 library

狀態: 已完成

### 第 2 階段: 平台抽象介面定稿

目標:

- 將輸入、音效、檔案、計時從核心切離

工作項目:

- 定義 `IAudioDevice`
- 定義 `IInputDevice`
- 定義 `IAssetLocator` 或 `IFileSystem`
- 定義 `ITimer` / `IGameHost`
- 定義核心對平台的依賴注入方式

驗收標準:

- `Core` 專案不直接引用 WinForms / GDI / Win32 API

狀態: 進行中

### 第 3 階段: 資料路徑與檔案格式基礎

目標:

- 讓 C# 版能正確找到 `tyrian21`
- 先打通基本資料讀取能力

工作項目:

- 實作資料根目錄定位
- 移植 `file.c/.h` 相關讀檔工具
- 建立 little-endian 讀取工具
- 先支援 `palette.dat`
- 盤點並逐步支援 `PIC / PCX / SHP / SND / MUS / LVL`

驗收標準:

- 啟動時能正確找到 `tyrian21`
- 至少能讀出 `palette.dat` 與一個圖像資源

狀態: 進行中

### 第 4 階段: 軟體渲染核心

目標:

- 先重建 OpenTyrian 的 8-bit 畫面核心，再轉成 32-bit 輸出到 GDI

工作項目:

- 建立 indexed framebuffer
- 移植 `palette.c`
- 移植 `vga256d.c`
- 移植 `sprite.c`
- 移植 `font.c` / `fonthand.c`
- 移植 `backgrnd.c`
- 建立 8-bit -> 32-bit 轉換

驗收標準:

- 可顯示標題圖、背景、字型、游標
- 顏色與 palette 效果基本正確

狀態: 進行中

### 第 5 階段: 輸入層

目標:

- 不透過 SDL，改由 WinForms + Win32 API 處理輸入

工作項目:

- 鍵盤輸入抽象化
- 滑鼠位置 / 按鍵 / 顯示控制
- 將 WinForms KeyCode 對應到核心輸入碼
- 參考 `tool/joystick.cs` 處理 DirectInput / XInput
- 將輸入整理成 snapshot 或 event queue

驗收標準:

- 主選單可由鍵盤操作
- 滑鼠可操作選單
- 手把可映射方向與按鈕

狀態: 未開始

### 第 6 階段: 音效平台層

目標:

- 不透過 SDL 音效，改用 WinMM / waveOut

工作項目:

- 定義 `IAudioDevice`
- 整理 `tool/WaveOutPlayer.cs`
- 建立 PCM buffer 提交流程
- 移植音效混音邏輯
- 後續再移植 `opl.c` / `lds_play.c`

驗收標準:

- 可播放選單音效
- 可穩定輸出 PCM，不爆音、不頻繁卡死

狀態: 未開始

### 第 7 階段: 主選單垂直切片

目標:

- 完成從啟動到主選單可操作的第一個完整版本

工作項目:

- 移植 `opentyr.c` 啟動主流程
- 移植 `mainint.c`
- 移植 `menus.c`
- 移植 `game_menu.c`
- 打通畫面、輸入、音效、資料載入

驗收標準:

- 可啟動
- 可進入主選單
- 可操作選單
- 可正常退出

狀態: 未開始

### 第 8 階段: 實際遊戲邏輯

目標:

- 進入可玩狀態

工作項目:

- 移植 `episodes.c`
- 移植 `player.c`
- 移植 `shots.c`
- 移植 `tyrian2.c`
- 移植碰撞、敵人、關卡流程
- 移植存檔與設定

驗收標準:

- 可進入關卡
- 可移動、射擊
- 敵人與碰撞正常
- 可進出選單與遊戲流程

狀態: 未開始

### 第 9 階段: 音樂與進階功能

目標:

- 補齊高難度功能，接近原版體驗

工作項目:

- 移植 `opl.c`
- 移植 `lds_play.c`
- 移植 `nortsong.c`
- 視需要處理 `jukebox`
- 後期再評估 `network.c`

驗收標準:

- 背景音樂可正常播放
- 選單與關卡音樂切換正確

狀態: 未開始

### 第 10 階段: 重構與平台切割驗證

目標:

- 確保後續可替換平台層，而不影響核心

工作項目:

- 清理 WinForms 依賴外滲
- 確保 `Core` 不含平台 API
- 補齊文件
- 建立回歸驗證清單

驗收標準:

- 平台層可獨立替換
- 核心與平台邊界清楚

狀態: 未開始

## 近期優先順序

目前先做以下 3 件事:

1. 建立 `.NET 10` solution 與專案分層
2. 建立 WinForms + Panel + GDI 畫面輸出骨架
3. 建立資料目錄定位，確認可讀 `tyrian21`

## 第一個可驗收里程碑

里程碑名稱: 啟動骨架

內容:

- WinForms 視窗可開啟
- Panel 可用 GDI 顯示 framebuffer
- 可從 `tyrian21` 找到資料
- 可讀 `palette.dat`
- 可顯示測試畫面或一張遊戲資源圖
- 鍵盤可關閉程式

## TODO 更新規則

之後每完成一個階段，就更新本檔:

- 修改 `最後更新`
- 更新各階段 `狀態`
- 在下方追加一筆進度紀錄

## 進度紀錄

### 2026-03-12

- 已完成整體移植規劃
- 已確認工作區包含 `opentyrian-master`、`tyrian21`、`tool`
- 已確認上游 SDL 對應的主要平台耦合模組
- 已建立 `OpenTyrianDotNet.sln`
- 已建立 `OpenTyrian.Core`、`OpenTyrian.Platform`、`OpenTyrian.WinForms`
- 已建立最小 `GameHost` 與 `IAssetLocator` 骨架
- 已建立 WinForms `MainForm` 與遊戲 Panel
- 已建立 `tyrian21` 資料目錄定位，會檢查 `palette.dat`
- 已將專案統一為 `net10.0-windows`
- 已開啟 `AllowUnsafeBlocks`，供後續導入 GDI / 指標工具碼
- 已確認可使用 Visual Studio `MSBuild.exe` 成功編譯 solution
- 已新增 `build.ps1`，作為目前建置入口
- 發現此環境下 `dotnet build` / `dotnet restore` 對 WinForms 專案有 SDK workload resolver 問題，暫時改用 `MSBuild.exe`
- 已新增 `IVideoDevice` 與 `FrameBuffer` 骨架
- 已建立 `GdiVideoDevice`，使用 Win32 `StretchDIBits` 將 32-bit framebuffer 輸出到 WinForms `Panel`
- 已建立 `GameHost.Tick()` 測試畫面路徑，會產生動態測試圖
- 已建立 WinForms frame timer，打通 `GameHost -> FrameBuffer -> GDI -> Panel`
- 已確認目前 solution 可由 Visual Studio `MSBuild.exe` 成功編譯
- 已新增 `PaletteLoader`、`PaletteBank`、`PaletteColor`
- 已依照上游 `palette.c` 規則，將 `palette.dat` 的 6-bit VGA 色值展成 8-bit 色彩
- 目前測試畫面已改為使用 `palette.dat` 第一組 palette，而非硬編碼 RGB
- `MainForm` 狀態列目前會顯示 palette 載入結果與 palette 數量
- 已新增 `TyrianDataDirectoryResolver`，資料目錄定位改為比照上游 `file.c` 的搜尋概念
- 已將 `IAssetLocator` 升級為真正的檔案存取介面，可做存在檢查、取完整路徑、開啟讀取串流
- 已新增 `TyrianDataStream`，提供 little-endian 的安全讀取 API
- 已新增 `PicArchive` / `PicImage`，可解析 `tyrian.pic` 索引並解碼 320x200 RLE 圖片
- `GameHost` 目前會優先載入 `tyrian.pic` 第 1 張圖，若成功則直接顯示真實遊戲圖像
- 已將核心畫面模型改成 `IndexedFrameBuffer (8-bit)` + `ArgbFrameBuffer (32-bit)` 兩段式
- 已新增 `PaletteRenderer`，依 palette 將 indexed framebuffer 轉為 32-bit 輸出
- 目前核心的畫面流程已更接近上游 `rgb_palette[*src]` 的設計，後續可直接接 sprite / font / background
- 已新增 `Vga256` 基礎繪圖模組，包含像素點、十字點、矩形外框、區塊填色、shade、bright
- `GameHost` 的 fallback 畫面已改為直接使用 `Vga256` primitive 作圖，開始接近上游 `vga256d.c` 的使用方式
- 已新增 `PcxLoader` / `PcxImage`，可解析 320x200 8-bit PCX 與尾端 VGA palette
- 目前已打通 `tshp2.pcx` 的讀取路徑；若沒有可用的 `tyrian.pic`，可直接用 PCX 顯示 indexed 圖像
- 已新增 `Sprite2Loader` / `Sprite2Sheet` / `Sprite2Blitter`
- 已打通 `newsh1.shp` 的 offset table 解析與單張 sprite blit，並將前三張 sprite 疊到目前畫面左上角做驗證
- 已補上 `Sprite2` 的 `clip / blend / darken / filter / filter+clip` 變體
- 目前測試畫面已直接用不同 sprite blit 模式做可視驗證，開始接近上游 `sprite.c` 的常用路徑
- 已新增主 `SpriteTable` 載入路徑，可從 `tyrian.shp` 載入前三套字型 shape table
- 已新增 `TyrianFontRenderer`，目前可用上游 `font_ascii` 映射繪製 normal / small / tiny 三套字型
- 已接上最小文字測試路徑，測試畫面會直接疊出 OpenTyrian 字型與陰影文字，為後續主選單 UI 鋪底
- 已補上字型對齊、blend、drop shadow、full shadow 變體，讓 C# 端介面更接近上游 `font.c` 的常用呼叫方式
- 已將 `tyrian.shp` 主 shape tables 型別化，開始直接載入 `OPTION_SHAPES` / `FACE_SHAPES`
- 目前測試畫面已可疊出 message box、option icon、face sprite 等真實介面素材，作為主選單 UI 資源路徑的第一個驗證
- 已將目前的標題/測試畫面拆成獨立 `TitleScene` 結構，`GameHost` 改為只管理資源載入、scene 執行與 palette 轉換
- 已新增最小鍵盤輸入抽象 `IInputSource` / `InputSnapshot`，並由 WinForms 事件更新 snapshot
- `TitleScene` 目前已可用 `Enter` 進入 prototype menu、方向鍵切換選項、`Escape` 返回 title，作為後續主選單輸入骨架
- 已將 prototype menu 抽成 `MenuDefinition` / `MenuItemDefinition` / `MenuState`，scene 不再硬編碼選單控制流與說明文字
- 已將 `TitleScene` 與 `MainMenuScene` 分離，scene 切換改由 `Update()` 回傳下一個 scene，`GameHost` 不再管理 title/menu 內部狀態
- `MainMenuScene` 的選項與順序已開始對齊上游 `menus.c` 的 `gameplaySelect()`，目前使用 1P Full Game / 1P Arcade / 2P Arcade / Network disabled
- 已新增 `EpisodeSelectScene` 骨架，`1 Player Full Game` 現在會由主選單實際切進 episode 選擇流程
- 已新增 `EpisodeCatalogLoader`，目前會依 `tyrian1.lvl` 到 `tyrian4.lvl` 的實際存在狀態建立 episode 清單，對齊上游 `JE_scanForEpisodes()` 的可用性判斷
- 已新增 `TyrianHelpTextLoader`，episode 名稱現在優先從 `tyrian.hdt` 讀取上游加密 Pascal string，而不是使用內建常數
- 已將 `gameplay_name` 與 `mainMenuHelp` 一併納入 `tyrian.hdt` 載入路徑，`MainMenuScene` 現在優先使用真實遊戲文字生成標題、選項與 help 文案
- 已新增 `EpisodeStartInfo`，episode 選擇結果現在會攜帶 `tyrianX.lvl` / `levelsX.dat` / `cubetxtX.dat` 等初始化 metadata，對齊上游 `JE_initEpisode()` 所需資訊
- 已用 `EpisodeSessionState` + `EpisodeSessionScene` 取代單純 stub 畫面，現在會保存目前 episode number、level number 與對應資料檔路徑
- `EpisodeSessionState` 已補進 `initial episode number`、`bonusLevel`、`jumpBackToEpisode1`、`gameHasRepeated` 等欄位，開始對齊上游 episode/session 核心狀態
- 已新增 `LevelIndexLoader`，目前會解析 `.lvl` header 取得關卡數與 offset table，並把 `JE_analyzeLevel()` 對應資訊帶進 `EpisodeSessionState`
- 已新增 `EpisodeScriptLoader`，目前會對 `levelsX.dat` 建立最小 metadata，包含檔案存在、大小與前段 section marker 掃描結果
- 已新增 `CubeTextLoader`，目前會對 `cubetxtX.dat` 建立最小 metadata，讓 episode session 對三條主要 episode 資源路徑都具備基本掌握
- `EpisodeScriptLoader` 已擴成完整 section 掃描，現在會把 `levelsX.dat` 中 `*` 開頭的主 section 切出來，記錄 label、字串序號與檔案位移
- 已新增 `MainLevelEntry` 映射，`CurrentLevelNumber` 現在會直接對應到 `levelsX.dat` 中第 N 個 `*` section，開始貼近上游 `mainLevel` 的使用方式
- 已新增最小 section command model，現在會從當前 `main level` section 掃出 `]J`、`]2`、`]s`、`]b` 等控制指令，為後續執行流程做準備
- 已讓 `EpisodeSessionState` 可套用第一個 `]J` section jump，`Enter` 目前可觸發 `CurrentLevelNumber` / `CurrentMainLevelEntry` 更新，形成第一個可執行的 script 控制命令
- 已補上 `]2` 條件跳轉的最小執行路徑，暫時用 `IsArcadeLikeMode` placeholder 作條件判斷，讓 script command 開始具備分支能力
- 已把 `]s` / `]b` 轉成 session side effects，現在會記錄 `saveLevel` 與 `lastLevelSaveRequested`，作為後續真正存檔流程的前置狀態
- 已新增 `EpisodeCommandInterpreter`，把當前 section 的已知控制命令執行從 scene 拆出來，形成可重複呼叫的最小 interpreter
- 已補上 `]i`、`]B`、`]S` 的最小 interpreter 路徑，現在會記錄 item-shop song index、fade-black 請求與 network text sync 請求
- 已在 interpreter 執行前清除 transient flags，讓 `FadeBlackRequested` / `NetworkTextSyncRequested` 具備單次事件語意，不會持續黏在 session state 上
- 已把 `]I` 納入 block command parsing，loader 現在會連帶讀取後續 9 行 payload，interpreter 會先把 block 行數保存在 session state
- `]I` 的 9 行 payload 現在已會解析成 item availability 數字列，session state 不再只保存行數，而會保存結構化 row 資料
- `]I` 的 item availability 已進一步整理成接近上游 `itemAvail[9][10]` / `itemAvailMax[9]` 的資料形狀，session state 現在會保存各列最大值與首批 item id 摘要
- 已把上游 `itemAvailMap[7]` 轉成 `ShopCategory` 模型，現在 session state 可直接拿到商店分類與對應 item id 清單，為後續 upgrade/shop menu 對齊 `game_menu.c` 鋪路
- 已新增 `UpgradeMenuScene` 原型，`EpisodeSessionScene` 現在可用 `Down` 進入第一個可導航的商店/升級選單，並用 `Up/Down` 切分類、`Left/Right` 切 item slot
- `UpgradeMenuScene` 已往 `JE_genItemMenu()` 靠攏，現在會依分類生成 submenu 樣式的 item list、保留每個分類的目前 slot、支援 `Done` 列，並顯示 prepared item 狀態
- 已新增最小 `PlayerLoadoutState`，`UpgradeMenuScene` 的確認動作現在會真的寫回 session 內的裝備狀態，並在 episode session 畫面顯示 ship/front/rear 摘要
- 已將商店分類語意校正到上游 `playeritem_map()` / `itemAvailMap()`，現在 loadout 會完整追蹤 `ship/front/rear/shield/generator/left/right sidekick` 七類裝備
- `UpgradeMenuScene` 進入時已會依當前 loadout 對齊各分類預設 slot，行為開始接近上游 `JE_genItemMenu()` 會預設選到目前裝備項目的方式
- `UpgradeMenuScene` 已改成兩層互動：先選分類、再進 submenu 選 item，`Esc` 可從 submenu 返回分類層，整體流程更接近上游 `MENU_UPGRADES -> MENU_UPGRADE_SUB`
- `UpgradeMenuScene` 的 submenu 已支援可捲動視窗，分類項目超過可視列數時，清單會跟著目前 slot 移動，並顯示上下提示
