namespace OpenTyrian.Core;

public sealed partial class GameplayScene
{
    private IScene? UpdatePauseState(
        SceneResources resources,
        OpenTyrian.Platform.InputSnapshot input,
        bool cancelPressed,
        bool confirmPressed,
        bool upPressed,
        bool downPressed,
        bool pointerConfirmPressed)
    {
        int? hoveredIndex = input.PointerPresent
            ? HitTestPauseMenu(input.PointerX, input.PointerY)
            : null;

        if (hoveredIndex is int pointerIndex && pointerIndex != _pauseSelection)
        {
            SceneAudio.PlayCursor(resources);
            _pauseSelection = pointerIndex;
        }

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _phase = MissionPhase.Active;
            return null;
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _pauseSelection = _pauseSelection == 0 ? PauseMenuItems.Length - 1 : _pauseSelection - 1;
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _pauseSelection = (_pauseSelection + 1) % PauseMenuItems.Length;
        }

        if (!confirmPressed && !(pointerConfirmPressed && hoveredIndex is not null))
        {
            return null;
        }

        SceneAudio.PlayConfirm(resources);
        switch (_pauseSelection)
        {
            case 0:
                _phase = MissionPhase.Active;
                return null;

            case 1:
                return new GameplayScene(_sessionState, _returnToTitleOnExit);

            default:
                return _returnToTitleOnExit
                    ? new TitleMenuScene()
                    : CreateReturnMenuScene();
        }
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

        return _returnToTitleOnExit
            ? new TitleMenuScene()
            : CreateReturnMenuScene();
    }

    private IScene CreateReturnMenuScene()
    {
        return _sessionState.StartMode == GameStartMode.FullGame
            ? new FullGameMenuScene(_sessionState)
            : new ArcadeMenuScene(_sessionState);
    }

    private void EnsureCombatState(ItemCatalog? itemCatalog)
    {
        if (_combatInitialized)
        {
            return;
        }

        ItemCatalogEntry? shipEntry = itemCatalog?.GetEntry(ItemCategoryKind.Ship, _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Ship));
        ItemCatalogEntry? shieldEntry = itemCatalog?.GetEntry(ItemCategoryKind.Shield, _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Shield));
        ItemCatalogEntry? generatorEntry = itemCatalog?.GetEntry(ItemCategoryKind.Generator, _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Generator));

        _maxArmor = 8f + ((shipEntry?.SecondaryStat ?? 0) * 0.75f);
        if (_maxArmor < 5f)
        {
            _maxArmor = 5f;
        }

        _armor = _maxArmor;

        if (shieldEntry is not null)
        {
            _maxShield = 4f + (shieldEntry.PrimaryStat * 0.45f);
            _shield = _maxShield;
        }
        else
        {
            _maxShield = 0f;
            _shield = 0f;
        }

        _shieldRechargeRate = 0f;
        if (_maxShield > 0f)
        {
            _shieldRechargeRate = (shieldEntry?.SecondaryStat ?? 0) * 0.18f;
            _shieldRechargeRate += (generatorEntry?.SecondaryStat ?? 0) * 0.35f;
            if (_shieldRechargeRate < 0.35f)
            {
                _shieldRechargeRate = 0.35f;
            }
        }

        _combatInitialized = true;
    }

    private void RegenerateShield(float deltaSeconds)
    {
        if (_shield >= _maxShield || _shieldRechargeRate <= 0f)
        {
            return;
        }

        _shield += _shieldRechargeRate * deltaSeconds;
        if (_shield > _maxShield)
        {
            _shield = _maxShield;
        }
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

    private void UpdateWeapons(ItemCatalog? itemCatalog, OpenTyrian.Platform.InputSnapshot input, float deltaSeconds)
    {
        if (_fireCooldown > 0f)
        {
            _fireCooldown -= deltaSeconds;
        }

        if (_rearFireCooldown > 0f)
        {
            _rearFireCooldown -= deltaSeconds;
        }

        if (_supportFireCooldown > 0f)
        {
            _supportFireCooldown -= deltaSeconds;
        }

        if (!input.Confirm)
        {
            return;
        }

        if (_fireCooldown <= 0f)
        {
            SpawnFrontVolley(itemCatalog);
            _fireCooldown = GetFrontFireInterval(itemCatalog);
        }

        if (_rearFireCooldown <= 0f && SpawnRearVolley(itemCatalog))
        {
            _rearFireCooldown = GetRearFireInterval(itemCatalog);
        }

        if (_supportFireCooldown <= 0f && SpawnSupportVolley(itemCatalog))
        {
            _supportFireCooldown = GetSupportFireInterval(itemCatalog);
        }
    }

    private void SpawnFrontVolley(ItemCatalog? itemCatalog)
    {
        int frontWeaponId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.FrontWeapon);
        int frontPower = Math.Max(1, _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.FrontWeapon));
        ItemCatalogEntry? weaponEntry = itemCatalog?.GetEntry(ItemCategoryKind.FrontWeapon, frontWeaponId);
        int shotCount = frontPower >= 8 ? 3 : frontPower >= 4 ? 2 : 1;
        int damage = 1 + (frontPower / 3);
        float speed = 180f + (frontPower * 5f);

        int[] offsets = shotCount switch
        {
            3 => new[] { -6, 0, 6 },
            2 => new[] { -4, 4 },
            _ => new[] { 0 },
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            float velocityX = offsets[i] * 0.7f;
            SpawnPlayerProjectile(_playerX + 5f + offsets[i], _playerY - 2f, velocityX, -speed, damage, weaponEntry is null ? (byte)15 : (byte)14);
        }
    }

    private bool SpawnRearVolley(ItemCatalog? itemCatalog)
    {
        int rearWeaponId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.RearWeapon);
        if (rearWeaponId == 0)
        {
            return false;
        }

        int rearPower = Math.Max(1, _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.RearWeapon));
        int damage = 1 + (rearPower / 4);
        float speed = 132f + (rearPower * 4f);
        float baseX = _playerX + 6f;
        float baseY = _playerY + 3f;

        SpawnPlayerProjectile(baseX - 2f, baseY, -44f, -speed, damage, 10);
        SpawnPlayerProjectile(baseX + 2f, baseY, 44f, -speed, damage, 10);

        if (rearPower >= 6)
        {
            SpawnPlayerProjectile(baseX, baseY - 2f, 0f, -(speed + 10f), damage, 11);
        }

        return true;
    }

    private bool SpawnSupportVolley(ItemCatalog? itemCatalog)
    {
        int leftId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.SidekickLeft);
        int rightId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.SidekickRight);
        if (leftId == 0 && rightId == 0)
        {
            return false;
        }

        ProjectileVector targetVector = GetSupportTargetVector();
        if (leftId != 0)
        {
            ItemCatalogEntry? leftEntry = itemCatalog?.GetEntry(ItemCategoryKind.SidekickLeft, leftId);
            SpawnPlayerProjectile(_playerX - 2f, _playerY + 4f, targetVector.X * 158f, targetVector.Y * 158f, 1 + ((leftEntry?.PrimaryStat ?? 0) / 3), 12);
        }

        if (rightId != 0)
        {
            ItemCatalogEntry? rightEntry = itemCatalog?.GetEntry(ItemCategoryKind.SidekickRight, rightId);
            SpawnPlayerProjectile(_playerX + 14f, _playerY + 4f, targetVector.X * 158f, targetVector.Y * 158f, 1 + ((rightEntry?.PrimaryStat ?? 0) / 3), 12);
        }

        return true;
    }

    private ProjectileVector GetSupportTargetVector()
    {
        float targetX = _playerX + 6f;
        float targetY = PlayfieldTop;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyState enemy = _enemies[i];
            if (!enemy.Active)
            {
                continue;
            }

            float dx = enemy.X - _playerX;
            float dy = enemy.Y - _playerY;
            float distance = (dx * dx) + (dy * dy);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                targetX = enemy.X;
                targetY = enemy.Y;
            }
        }

        return NormalizeVector(targetX - (_playerX + 6f), targetY - (_playerY + 4f));
    }

    private void SpawnPlayerProjectile(float x, float y, float velocityX, float velocityY, int damage, byte color)
    {
        _playerProjectiles.Add(new ProjectileState
        {
            X = x,
            Y = y,
            VelocityX = velocityX,
            VelocityY = velocityY,
            Damage = damage,
            Color = color,
            Active = true,
        });
    }

    private void SpawnEnemyProjectile(float x, float y, float velocityX, float velocityY, int damage)
    {
        _enemyProjectiles.Add(new ProjectileState
        {
            X = x,
            Y = y,
            VelocityX = velocityX,
            VelocityY = velocityY,
            Damage = damage,
            Color = 4,
            Active = true,
        });
    }

    private void UpdateEnemies(float deltaSeconds)
    {
        _spawnTimer -= deltaSeconds;
        if (_spawnTimer <= 0f && _enemies.Count < 8 && _destroyedEnemies + _enemies.Count < GetRequiredKills() + 3)
        {
            SpawnEnemy();
            _spawnTimer = GetSpawnInterval();
        }

        for (int i = 0; i < _enemies.Count; i++)
        {
            EnemyState enemy = _enemies[i];
            enemy.X += enemy.Drift * deltaSeconds;
            enemy.Y += enemy.Speed * deltaSeconds;
            enemy.ShotCooldown -= deltaSeconds;

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
                ApplyDamage(1.5f);
            }
            else if (enemy.Y > PlayfieldTop + 8f && enemy.ShotCooldown <= 0f)
            {
                SpawnEnemyShot(enemy);
                enemy.ShotCooldown = 1.2f + (float)(_random.NextDouble() * 0.8f);
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
            ShotCooldown = 0.5f + (float)(_random.NextDouble() * 0.8f),
            Active = true,
        });
    }

    private void SpawnEnemyShot(EnemyState enemy)
    {
        ProjectileVector direction = NormalizeVector((_playerX + 6f) - (enemy.X + 4f), (_playerY + 4f) - (enemy.Y + 6f));
        float speed = 80f + (_missionLevelNumber * 6f);
        int damage = 1 + (_missionLevelNumber / 6);
        SpawnEnemyProjectile(enemy.X + 4f, enemy.Y + 6f, direction.X * speed, direction.Y * speed, damage);
    }

    private static void UpdateProjectiles(float deltaSeconds, List<ProjectileState> projectiles)
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            ProjectileState shot = projectiles[i];
            shot.X += shot.VelocityX * deltaSeconds;
            shot.Y += shot.VelocityY * deltaSeconds;
            if (shot.X < PlayfieldLeft - 8f ||
                shot.X > PlayfieldRight + 8f ||
                shot.Y < PlayfieldTop - 8f ||
                shot.Y > PlayfieldBottom + 8f)
            {
                shot.Active = false;
            }

            projectiles[i] = shot;
        }
    }

    private void ResolveCollisions(SceneResources resources)
    {
        for (int shotIndex = 0; shotIndex < _playerProjectiles.Count; shotIndex++)
        {
            ProjectileState shot = _playerProjectiles[shotIndex];
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
                enemy.HitPoints -= shot.Damage;
                if (enemy.HitPoints <= 0)
                {
                    enemy.Active = false;
                    _destroyedEnemies++;
                    _earnedCash += 50 + (_missionLevelNumber * 10);
                    SceneAudio.PlayConfirm(resources);
                }

                _playerProjectiles[shotIndex] = shot;
                _enemies[enemyIndex] = enemy;
                break;
            }
        }

        for (int shotIndex = 0; shotIndex < _enemyProjectiles.Count; shotIndex++)
        {
            ProjectileState shot = _enemyProjectiles[shotIndex];
            if (!shot.Active || !Intersects(_playerX, _playerY, 12f, 11f, shot.X, shot.Y, 2f, 5f))
            {
                continue;
            }

            shot.Active = false;
            _enemyProjectiles[shotIndex] = shot;
            ApplyDamage(shot.Damage);
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
            ApplyDamage(2.5f);
        }
    }

    private void ApplyDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        if (_shield > 0f)
        {
            float shieldDamage = Math.Min(_shield, damage);
            _shield -= shieldDamage;
            damage -= shieldDamage;
        }

        if (damage > 0f)
        {
            _armor -= damage;
        }
    }

    private void CleanupInactiveEntities()
    {
        RemoveInactiveProjectiles(_playerProjectiles);
        RemoveInactiveProjectiles(_enemyProjectiles);

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (!_enemies[i].Active)
            {
                _enemies.RemoveAt(i);
            }
        }
    }

    private static void RemoveInactiveProjectiles(List<ProjectileState> projectiles)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            if (!projectiles[i].Active)
            {
                projectiles.RemoveAt(i);
            }
        }
    }

    private void CompleteMission(SceneResources resources)
    {
        _phase = MissionPhase.Cleared;
        _earnedCash += 500 + (_missionLevelNumber * 100);
        _sessionState.AddCash(_earnedCash);

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

    private float GetFrontFireInterval(ItemCatalog? itemCatalog)
    {
        int frontWeaponId = _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.FrontWeapon);
        int frontPower = _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.FrontWeapon);
        ItemCatalogEntry? weapon = itemCatalog?.GetEntry(ItemCategoryKind.FrontWeapon, frontWeaponId);
        ItemCatalogEntry? generator = itemCatalog?.GetEntry(ItemCategoryKind.Generator, _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Generator));

        float interval = 0.30f - (frontPower * 0.01f) - ((weapon?.PrimaryStat ?? 0) * 0.0005f) - ((generator?.PrimaryStat ?? 0) * 0.0025f);
        if (interval < 0.10f)
        {
            interval = 0.10f;
        }

        return interval;
    }

    private float GetRearFireInterval(ItemCatalog? itemCatalog)
    {
        int rearPower = _sessionState.PlayerLoadout.GetWeaponPower(ItemCategoryKind.RearWeapon);
        ItemCatalogEntry? generator = itemCatalog?.GetEntry(ItemCategoryKind.Generator, _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Generator));
        float interval = 0.46f - (rearPower * 0.014f) - ((generator?.PrimaryStat ?? 0) * 0.002f);
        return interval < 0.18f ? 0.18f : interval;
    }

    private float GetSupportFireInterval(ItemCatalog? itemCatalog)
    {
        int optionPower = 0;
        ItemCatalogEntry? left = itemCatalog?.GetEntry(ItemCategoryKind.SidekickLeft, _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.SidekickLeft));
        ItemCatalogEntry? right = itemCatalog?.GetEntry(ItemCategoryKind.SidekickRight, _sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.SidekickRight));
        optionPower += left?.PrimaryStat ?? 0;
        optionPower += right?.PrimaryStat ?? 0;

        float interval = 0.72f - (optionPower * 0.01f);
        return interval < 0.22f ? 0.22f : interval;
    }

    private float GetSpawnInterval()
    {
        float interval = 1.05f - (_missionLevelNumber * 0.05f);
        return interval < 0.45f ? 0.45f : interval;
    }

    private static ProjectileVector NormalizeVector(float x, float y)
    {
        float length = (float)Math.Sqrt((x * x) + (y * y));
        if (length <= 0.001f)
        {
            return new ProjectileVector { X = 0f, Y = -1f };
        }

        return new ProjectileVector
        {
            X = x / length,
            Y = y / length,
        };
    }

    private static bool Intersects(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
    {
        return ax < bx + bw &&
               ax + aw > bx &&
               ay < by + bh &&
               ay + ah > by;
    }

    private static int? HitTestPauseMenu(int x, int y)
    {
        if (x < PauseMenuLeft || x > PauseMenuRight)
        {
            return null;
        }

        for (int i = 0; i < PauseMenuItems.Length; i++)
        {
            int top = PauseMenuTop + 4 + (i * PauseMenuRowHeight);
            int bottom = top + PauseMenuRowHeight - 1;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }
}
