# netopentyrian

`netopentyrian` 是將 [OpenTyrian](https://github.com/opentyrian/opentyrian) 移植到 `C# / .NET Framework 4.0 / Windows Forms` 的實驗性專案。

目前目標不是直接包一層 SDL，而是把遊戲邏輯核心和平台層切開，讓後續除了 WinForms/Win32 之外，也能替換成其他平台實作。現階段優先使用 `.NET Framework 4.0` 可直接提供的 BCL，避免額外安裝套件。

## 專案目標

- 以 `Windows Forms` 建立視窗與宿主程式
- 以 `Panel + Win32 GDI` 輸出遊戲畫面
- 不使用第三方 library
- 不額外依賴 `System.Memory`、`System.Buffers` 等外掛套件
- 鍵盤、滑鼠、手把、音效盡量使用 .NET 內建或 Win32 API
- 將平台層與遊戲核心切割，方便日後移植
- 執行時使用 `tyrian21` 內的原始遊戲資料
- network 功能維持停用，不再納入移植範圍

## 目前目錄

- `opentyrian-master`
  OpenTyrian 上游原始碼
- `tyrian21`
  執行時使用的 Tyrian 2.1 資料檔
- `tool`
  現有 Win32 / GDI / 輸入 / 音效工具碼
- `src/OpenTyrian.Core`
  遊戲核心、資源格式、scene、session、script parsing
- `src/OpenTyrian.Platform`
  平台抽象介面
- `src/OpenTyrian.WinForms`
  WinForms 宿主與 GDI/輸入接線
- `MD/TODO.MD`
  分階段移植紀錄

## 目前進度

目前已完成或已打通的部分包含：

- `.NET Framework 4.0` solution 與分層骨架
- WinForms `Panel` + Win32 GDI framebuffer 輸出
- `palette.dat`、`tyrian.pic`、`PCX`、`Sprite2`、`tyrian.shp` 字型與主 shape table 載入
- title / main menu / episode select / episode session scene 骨架
- `tyrian.hdt` 文字載入
- `.lvl` header、`levelsX.dat` section 與部分 command parsing/interpreter
- `]I` item availability 解析
- upgrade/shop prototype 與最小 `PlayerLoadoutState`
- WinForms 滑鼠座標/按鍵已接入，main menu 與 episode select 可用滑鼠 hover/click
- `1P Full Game`、`1P Arcade`、`2P Arcade` 已會帶著對應模式進入 episode select / session
- shop 現在已追蹤 `cash / trade-in / affordability`，提交裝備會真正扣款或退款
- `cubetxtX.dat` 已結構化為 data cube entry，episode session 可進入 data cube viewer
- episode session 現在會在進場時自動執行當前 section command，遇到 `]I` 會直接轉進 upgrade/shop scene
- upgrade/shop scene 已支援滑鼠 hover/click 與右鍵返回，和目前 WinForms 輸入層對齊
- `]S` network text sync 仍會被解析，但執行時明確忽略，network 流程維持停用
- episode 選擇之後現在會進入 `FullGameMenuScene`，形成第一個比較接近 `MENU_FULL_GAME` 的 hub
- full-game hub 可進入 data cube、upgrade shop、next-level 選單與 session debug view，並支援滑鼠 hover/click
- `tyrian.hdt` 已額外接入 full-game menu 的 `menuInt[1]` 文字，減少這一段流程的硬編碼
- 已新增最小 `LevelSelectScene`，可瀏覽 parsed main-level section 並切換目前 level，返回 hub 後會自動執行新 section command
- full-game hub 的 `Ship Specs` 已啟用，會顯示目前 ship / shield / generator / weapon / sidekick 摘要
- `tyrian.hdt` 已開始額外載入 ship info 兩段文字，`ShipSpecsScene` 會優先顯示真實 ship 說明
- `ItemCatalog` 已擴充 ship / shield / generator / weapon 的基礎 stat metadata，供 ship specs 與後續 UI 顯示使用
- full-game hub 的 `Options` 已接成可進入的 menu scene，並開始使用 `tyrian.hdt` 的 options menu 文字
- `Quit` 現在不再直接跳回 episode select，而是先進入最小 confirmation scene
- 已建立 `IAudioDevice` 平台介面並接入 `GameHost`，silent backend 仍保留作 fallback scaffolding
- WinForms 端預設已切到 `WaveOutAudioDevice`，會直接開啟 WinMM/waveOut backend；目前尚未餵入實際遊戲 PCM，因此仍屬於 backend 建置階段
- 音效平台層已開始進場，但目前還沒接上真正的 menu/game PCM、混音與 OPL
- 已建立 scene -> host 的 audio cue 路徑，title / main menu / episode select / full-game hub / shop / data cube / options 等互動現在會送出最小 PCM 提示音
- 已新增 `IUserFileStore` 與 `tyrian.sav` 解碼路徑，`Options` 底下的 `Load Game / Save Game` 現在可進入 read-only slot browser
- load/save scene 目前只會瀏覽與顯示 slot metadata，不會真的載入或寫回存檔

目前專案仍屬於早期移植階段，重點是把資料格式、平台抽象與主流程骨架穩定下來。

## 建置方式

目前這個工作環境使用 Visual Studio 的 `MSBuild.exe` 建置最穩定。優先直接用根目錄的 `build.ps1`，它會自動偵測常見 Visual Studio / Build Tools 安裝位置，並先 restore 再 build：

```powershell
.\build.ps1 -Configuration Debug
```

若要手動指定路徑，也可以直接呼叫 `MSBuild.exe`：

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' OpenTyrianDotNet.sln /restore /t:Build /p:Configuration=Debug /p:RestoreIgnoreFailedSources=true /m:1 /v:minimal
```

此環境下 `dotnet build` 對 WinForms workload resolver 有問題，所以暫時不把它當主要入口。

## 執行需求

- Windows
- Visual Studio / MSBuild 可用
- 根目錄存在 `tyrian21` 並包含 Tyrian 2.1 資料

## 設計原則

- `OpenTyrian.Core` 不直接依賴 WinForms
- `OpenTyrian.Core` 不直接依賴 GDI / waveOut / Win32 視窗管理
- 平台耦合集中在 `OpenTyrian.WinForms` 與後續 platform backend
- 儘量保留 OpenTyrian 原本的 8-bit palette / software rendering 思路

## 後續方向

- 繼續把 `game_menu.c`、`mainint.c`、`episodes.c` 的主流程往 C# scene / state 模型對齊
- 補完整輸入層、音效層與更多 episode script command
- 將 full-game hub、upgrade/shop、next-level、ship specs、options 再往上游互動對齊
- network 對應流程維持停用，不再規劃 `network.c`
