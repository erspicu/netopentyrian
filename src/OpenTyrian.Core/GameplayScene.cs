namespace OpenTyrian.Core;

public sealed class GameplayScene : IScene
{
    private const float PlayfieldLeft = 12f;
    private const float PlayfieldTop = 22f;
    private const float PlayfieldRight = 308f;
    private const float PlayfieldBottom = 186f;

    private readonly EpisodeSessionState _sessionState;
    private readonly Random _random;
    private readonly List<ShotState> _shots;
    private readonly List<EnemyState> _enemies;
    private readonly int _missionLevelNumber;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private float _playerX;
    private float _playerY;
    private float _spawnTimer;
    private float _fireCooldown;
    private int _playerHitPoints;
    private int _destroyedEnemies;
    private int _earnedCash;
    private MissionPhase _phase;
    private bool _advancedToNextLevel;

    public GameplayScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
        _missionLevelNumber = Math.Max(1, sessionState.CurrentLevelNumber);
        _random = new Random((_missionLevelNumber * 997) + sessionState.Cash);
        _shots = new List<ShotState>();
        _enemies = new List<EnemyState>();
        _playerX = 154f;
        _playerY = 164f;
        _spawnTimer = 0.4f;
        _fireCooldown = 0f;
        _playerHitPoints = GetInitialHitPoints(sessionState);
        _phase = MissionPhase.Active;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        float frameSeconds = (float)Math.Min(0.05, Math.Max(0.0, deltaSeconds));

        if (_phase != MissionPhase.Active)
        {
            _previousInput = input;
            return UpdateResultState(resources, cancelPressed, confirmPressed);
        }

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new FullGameMenuScene(_sessionState);
        }

        UpdatePlayerPosition(resources.ItemCatalog, input, frameSeconds);
        UpdateFire(resources.ItemCatalog, input, frameSeconds);
        UpdateEnemies(frameSeconds);
        UpdateShots(frameSeconds);
        ResolveCollisions(resources);
        CleanupInactiveEntities();

        if (_playerHitPoints <= 0)
        {
            _phase = MissionPhase.Failed;
            SceneAudio.PlayCancel(resources);
        }
        else if (_destroyedEnemies >= GetRequiredKills())
        {
            CompleteMission(resources);
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        RenderBackground(surface, timeSeconds);
        RenderHud(surface, resources);
        RenderPlayer(surface);
        RenderShots(surface);
        RenderEnemies(surface);
        RenderOverlay(surface, resources);
    }

    private IScene? UpdateResultState(SceneResources resources, bool cancelPressed, bool confirmPressed)
    {
        if (!cancelPressed && !confirmPressed)
        {
            return null;
        }

        if (_phase == MissionPhase.Cleared)
        {
            SceneAudio.PlayConfirm(resources);
        }
        else
        {
            SceneAudio.PlayCancel(resources);
        }

        return new FullGameMenuScene(_sessionState);
    }

    private void UpdatePlayerPosition(ItemCatalog? itemCatalog, OpenTyrian.Platform.InputSnapshot input, float deltaSeconds)
    {
        float speed = GetPlayerSpeed(itemCatalog) * deltaSeconds;
        if (input.Left)
        {
            _playerX -= speed;
        }

        if (input.Right)
        {
            _playerX += speed;
        }

        if (input.Up)
        {
            _playerY -= speed;
        }

        if (input.Down)
        {
            _playerY += speed;
        }

        if (_playerX < PlayfieldLeft)
        {
            _playerX = PlayfieldLeft;
        }
        else if (_playerX > PlayfieldRight - 12f)
        {
            _playerX = PlayfieldRight - 12f;
        }

        if (_playerY < PlayfieldTop)
        {
            _playerY = PlayfieldTop;
        }
        else if (_playerY > PlayfieldBottom - 11f)
        {
            _playerY = PlayfieldBottom - 11f;
        }
    }

    private void UpdateFire(ItemCatalog? itemCatalog, OpenTyrian.Platform.InputSnapshot input, float deltaSeconds)
    {
        if (_fireCooldown > 0f)
        {
            _fireCooldown -= deltaSeconds;
        }

        if (!input.Confirm || _fireCooldown > 0f)
        {
            return;
        }

        SpawnShots();
        _fireCooldown = GetFireInterval(itemCatalog);
    }

    private void SpawnShots()
    {
        int frontPower = _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.FrontWeapon);
        int shotCount = 1;
        if (frontPower >= 8)
        {
            shotCount = 3;
        }
        else if (frontPower >= 4)
        {
            shotCount = 2;
        }

        int[] offsets = shotCount switch
        {
            3 => new[] { -6, 0, 6 },
            2 => new[] { -4, 4 },
            _ => new[] { 0 },
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            _shots.Add(new ShotState
            {
                X = _playerX + 5f + offsets[i],
                Y = _playerY - 2f,
                Speed = 170f + (frontPower * 4f),
                Active = true,
            });
        }
    }

    private void UpdateEnemies(float deltaSeconds)
    {
        _spawnTimer -= deltaSeconds;
        if (_spawnTimer <= 0f && _enemies.Count < 8 && _destroyedEnemies + _enemies.Count < GetRequiredKills() + 2)
        {
            SpawnEnemy();
            _spawnTimer = GetSpawnInterval();
        }

        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyState enemy = _enemies[i];
            enemy.X += enemy.Drift * deltaSeconds;
            enemy.Y += enemy.Speed * deltaSeconds;

            if (enemy.X < PlayfieldLeft)
            {
                enemy.X = PlayfieldLeft;
                enemy.Drift = Math.Abs(enemy.Drift);
            }
            else if (enemy.X > PlayfieldRight - 10f)
            {
                enemy.X = PlayfieldRight - 10f;
                enemy.Drift = -Math.Abs(enemy.Drift);
            }

            if (enemy.Y > PlayfieldBottom - 8f)
            {
                enemy.Active = false;
                _playerHitPoints--;
            }

            _enemies[i] = enemy;
        }
    }

    private void SpawnEnemy()
    {
        _enemies.Add(new EnemyState
        {
            X = (float)(_random.NextDouble() * (PlayfieldRight - PlayfieldLeft - 12f)) + PlayfieldLeft,
            Y = PlayfieldTop - 10f,
            Speed = 28f + (_missionLevelNumber * 3f) + (float)(_random.NextDouble() * 14f),
            Drift = (float)(_random.NextDouble() * 36f) - 18f,
            HitPoints = Math.Max(1, 1 + (_missionLevelNumber / 4)),
            Active = true,
        });
    }

    private void UpdateShots(float deltaSeconds)
    {
        for (int i = 0; i < _shots.Count; i++)
        {
            ShotState shot = _shots[i];
            shot.Y -= shot.Speed * deltaSeconds;
            if (shot.Y < PlayfieldTop - 6f)
            {
                shot.Active = false;
            }

            _shots[i] = shot;
        }
    }

    private void ResolveCollisions(SceneResources resources)
    {
        for (int shotIndex = 0; shotIndex < _shots.Count; shotIndex++)
        {
            ShotState shot = _shots[shotIndex];
            if (!shot.Active)
            {
                continue;
            }

            for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                EnemyState enemy = _enemies[enemyIndex];
                if (!enemy.Active || !Intersects(shot.X, shot.Y, 2f, 5f, enemy.X, enemy.Y, 10f, 8f))
                {
                    continue;
                }

                shot.Active = false;
                enemy.HitPoints--;
                if (enemy.HitPoints <= 0)
                {
                    enemy.Active = false;
                    _destroyedEnemies++;
                    int reward = 50 + (_missionLevelNumber * 10);
                    _earnedCash += reward;
                    _sessionState.AddCash(reward);
                    SceneAudio.PlayConfirm(resources);
                }

                _shots[shotIndex] = shot;
                _enemies[enemyIndex] = enemy;
                break;
            }
        }

        for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
        {
            EnemyState enemy = _enemies[enemyIndex];
            if (!enemy.Active || !Intersects(_playerX, _playerY, 12f, 11f, enemy.X, enemy.Y, 10f, 8f))
            {
                continue;
            }

            enemy.Active = false;
            _enemies[enemyIndex] = enemy;
            _playerHitPoints--;
        }
    }

    private void CleanupInactiveEntities()
    {
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            if (!_shots[i].Active)
            {
                _shots.RemoveAt(i);
            }
        }

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (!_enemies[i].Active)
            {
                _enemies.RemoveAt(i);
            }
        }
    }

    private void CompleteMission(SceneResources resources)
    {
        _phase = MissionPhase.Cleared;
        int clearBonus = 500 + (_missionLevelNumber * 100);
        _earnedCash += clearBonus;
        _sessionState.AddCash(clearBonus);

        int nextLevelNumber = _missionLevelNumber;
        if (_missionLevelNumber < _sessionState.MainLevelEntries.Count)
        {
            nextLevelNumber = _missionLevelNumber + 1;
            _advancedToNextLevel = true;
        }

        _sessionState.SetCurrentMainLevel(nextLevelNumber);
        _sessionState.SetSaveLevel(nextLevelNumber);
        SceneAudio.PlayConfirm(resources);
    }

    private void RenderBackground(IndexedFrameBuffer surface, double timeSeconds)
    {
        Vga256.Clear(surface, 0);
        Vga256.FillRectangleWH(surface, 0, 0, surface.Width, 16, 1);
        Vga256.FillRectangleWH(surface, 0, 16, surface.Width, surface.Height - 16, 0);
        Vga256.DrawRectangle(surface, 8, 18, 311, 191, 13);

        int frame = (int)(timeSeconds * 60.0);
        for (int i = 0; i < 48; i++)
        {
            int speed = 1 + (i % 3);
            int x = 10 + ((i * 53) % 296);
            int y = 20 + (((i * 37) + (frame * speed)) % 168);
            byte color = speed == 3 ? (byte)15 : speed == 2 ? (byte)14 : (byte)8;
            Vga256.PutPixel(surface, x, y, color);
        }
    }

    private void RenderHud(IndexedFrameBuffer surface, SceneResources resources)
    {
        if (resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawText(
            surface,
            8,
            4,
            string.Format("Level {0:00}  kills {1}/{2}  hp {3}", _missionLevelNumber, _destroyedEnemies, GetRequiredKills(), Math.Max(0, _playerHitPoints)),
            FontKind.Tiny,
            FontAlignment.Left,
            15,
            0,
            shadow: true);
        resources.FontRenderer.DrawText(
            surface,
            312,
            4,
            string.Format("cash {0} (+{1})", _sessionState.Cash, _earnedCash),
            FontKind.Tiny,
            FontAlignment.Right,
            14,
            0,
            shadow: true);
        resources.FontRenderer.DrawDark(
            surface,
            160,
            194,
            "Arrows move  Enter/Confirm fires  Esc returns to full-game menu",
            FontKind.Tiny,
            FontAlignment.Center,
            black: false);
    }

    private void RenderPlayer(IndexedFrameBuffer surface)
    {
        int x = (int)_playerX;
        int y = (int)_playerY;
        Vga256.FillRectangleWH(surface, x + 4, y + 2, 4, 7, 11);
        Vga256.FillRectangleWH(surface, x, y + 6, 12, 3, 9);
        Vga256.FillRectangleWH(surface, x + 5, y, 2, 3, 15);
        Vga256.PutCrossPixel(surface, x + 6, y + 10, 10);
    }

    private void RenderShots(IndexedFrameBuffer surface)
    {
        for (int i = 0; i < _shots.Count; i++)
        {
            ShotState shot = _shots[i];
            Vga256.FillRectangleWH(surface, (int)shot.X, (int)shot.Y, 2, 5, 15);
        }
    }

    private void RenderEnemies(IndexedFrameBuffer surface)
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyState enemy = _enemies[i];
            int x = (int)enemy.X;
            int y = (int)enemy.Y;
            Vga256.FillRectangleWH(surface, x + 2, y, 6, 2, 12);
            Vga256.FillRectangleWH(surface, x, y + 2, 10, 5, 4);
            Vga256.PutPixel(surface, x + 2, y + 4, 15);
            Vga256.PutPixel(surface, x + 7, y + 4, 15);
        }
    }

    private void RenderOverlay(IndexedFrameBuffer surface, SceneResources resources)
    {
        if (_phase == MissionPhase.Active || resources.FontRenderer is null)
        {
            return;
        }

        Vga256.FillRectangleWH(surface, 56, 74, 208, 40, 1);
        Vga256.DrawRectangle(surface, 56, 74, 263, 113, 15);

        string title = _phase == MissionPhase.Cleared ? "Mission Cleared" : "Mission Failed";
        string detail = _phase == MissionPhase.Cleared
            ? _advancedToNextLevel
                ? string.Format("Next level ready: {0:00}", _missionLevelNumber + 1)
                : "Episode end placeholder reached"
            : "Return to full-game menu and try again";
        resources.FontRenderer.DrawShadowText(surface, 160, 82, title, FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        resources.FontRenderer.DrawText(surface, 160, 96, string.Format("earned cash: +{0}", _earnedCash), FontKind.Tiny, FontAlignment.Center, 14, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 104, detail, FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 112, "Enter or Esc returns to full-game menu", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private int GetRequiredKills()
    {
        return 6 + (_missionLevelNumber * 2);
    }

    private float GetPlayerSpeed(ItemCatalog? itemCatalog)
    {
        int shipId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Ship);
        ItemCatalogEntry? ship = itemCatalog?.GetEntry(ItemCategoryKind.Ship, shipId);
        return 110f + ((ship?.PrimaryStat ?? 0) * 2f);
    }

    private float GetFireInterval(ItemCatalog? itemCatalog)
    {
        int frontWeaponId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.FrontWeapon);
        int frontPower = _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.FrontWeapon);
        ItemCatalogEntry? weapon = itemCatalog?.GetEntry(ItemCategoryKind.FrontWeapon, frontWeaponId);
        float interval = 0.28f - (frontPower * 0.01f) - ((weapon?.SecondaryStat ?? 0) * 0.0015f);
        if (interval < 0.12f)
        {
            interval = 0.12f;
        }

        return interval;
    }

    private float GetSpawnInterval()
    {
        float interval = 1.05f - (_missionLevelNumber * 0.05f);
        return interval < 0.45f ? 0.45f : interval;
    }

    private static int GetInitialHitPoints(EpisodeSessionState sessionState)
    {
        int shieldId = sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Shield);
        return shieldId > 0 ? 4 : 3;
    }

    private static bool Intersects(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
    {
        return ax < bx + bw &&
               ax + aw > bx &&
               ay < by + bh &&
               ay + ah > by;
    }

    private enum MissionPhase
    {
        Active,
        Cleared,
        Failed,
    }

    private struct ShotState
    {
        public float X;
        public float Y;
        public float Speed;
        public bool Active;
    }

    private struct EnemyState
    {
        public float X;
        public float Y;
        public float Speed;
        public float Drift;
        public int HitPoints;
        public bool Active;
    }
}
