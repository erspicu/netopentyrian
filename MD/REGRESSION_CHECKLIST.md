# netopentyrian 回歸檢查清單

最後更新: 2026-03-12

## 啟動與資料

- 啟動 `build/OpenTyrian.WinForms.exe`
- 確認 title 背景正常顯示，沒有缺資料錯誤
- 確認狀態列顯示 palette / audio backend 資訊

## 主選單流程

- `Title -> Main Menu` 可用鍵盤 `Enter`、滑鼠左鍵進入
- `Main Menu` 可用鍵盤、滑鼠、手把切換 `1P Full Game / 1P Arcade / 2P Arcade`
- `Esc` 可由 `Main Menu` 返回 `Title`

## Episode / Full Game

- `Episode Select` 可切換 episode 並進入 `FullGameMenuScene`
- `Full Game` hub 可進入 `Data Cubes`、`Ship Specs`、`Upgrade Ship`、`Options`、`Next Level`
- `Quit` confirmation 可返回 episode select

## Options / Save

- `Keyboard Setup` 可 rebind 六個核心鍵並 reset defaults
- `Joystick Setup` 可顯示 `XInput + DirectInput` 狀態並做最小 rebind
- `Load Game` 可讀取 `tyrian.sav` slot 並重建 session
- `Save Game` 可編輯 14 字元 ASCII slot name 並寫回 `tyrian.sav`

## Gameplay

- `Next Level` 可進入 `GameplayScene`
- 玩家可移動、持續射擊，rear weapon / sidekick 會產生額外 fire pattern
- 敵人會生成、移動、發射 projectile，並與玩家/子彈碰撞
- HUD 會顯示 armor/shield bar 與 mission 內暫存 cash reward
- `Esc` 會開啟 pause menu，可 `Resume / Retry Level / Return to Menu`
- 過關後會更新 `cash / current level / save level`

## Audio

- title/menu/gameplay/shop 會切換不同 loop BGM
- cursor / confirm / cancel cue 會和 BGM 一起混音輸出
- 關閉程式時不應卡死在 audio shutdown

## 邊界

- `OpenTyrian.Core` 不直接引用 WinForms / GDI / Win32 API
- platform/device 細節集中於 `OpenTyrian.Platform` 與 `OpenTyrian.WinForms`
- network 維持停用
- 手把 ini 持久化延後，等專案之後需要時再追加
