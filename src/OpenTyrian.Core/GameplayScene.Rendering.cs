namespace OpenTyrian.Core;

public sealed partial class GameplayScene
{
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
        RenderBar(surface, 8, 12, 60, 3, _armor, _maxArmor, 4);
        RenderBar(surface, 74, 12, 60, 3, _shield, _maxShield, 10);

        if (resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawText(
            surface,
            8,
            4,
            string.Format("Lv {0:00} kills {1}/{2}", _missionLevelNumber, _destroyedEnemies, GetRequiredKills()),
            FontKind.Tiny,
            FontAlignment.Left,
            15,
            0,
            shadow: true);
        resources.FontRenderer.DrawText(
            surface,
            140,
            4,
            string.Format("arm {0}/{1}  shd {2}/{3}", (int)Math.Ceiling(_armor), (int)Math.Ceiling(_maxArmor), (int)Math.Ceiling(_shield), (int)Math.Ceiling(_maxShield)),
            FontKind.Tiny,
            FontAlignment.Left,
            14,
            0,
            shadow: true);
        resources.FontRenderer.DrawText(
            surface,
            312,
            4,
            string.Format("cash +{0}", _earnedCash),
            FontKind.Tiny,
            FontAlignment.Right,
            14,
            0,
            shadow: true);
        resources.FontRenderer.DrawDark(
            surface,
            160,
            194,
            _phase == MissionPhase.Active
                ? "Arrows move  Confirm fires  Esc pauses"
                : "Enter or Esc returns to full-game menu",
            FontKind.Tiny,
            FontAlignment.Center,
            black: false);
    }

    private static void RenderBar(IndexedFrameBuffer surface, int x, int y, int width, int height, float current, float maximum, byte color)
    {
        Vga256.DrawRectangle(surface, x - 1, y - 1, x + width, y + height, 13);
        if (maximum <= 0f || current <= 0f)
        {
            return;
        }

        int filledWidth = (int)Math.Round((current / maximum) * width);
        if (filledWidth < 1)
        {
            filledWidth = 1;
        }
        else if (filledWidth > width)
        {
            filledWidth = width;
        }

        Vga256.FillRectangleWH(surface, x, y, filledWidth, height, color);
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

    private static void RenderProjectiles(IndexedFrameBuffer surface, List<ProjectileState> projectiles)
    {
        for (int i = 0; i < projectiles.Count; i++)
        {
            ProjectileState shot = projectiles[i];
            Vga256.FillRectangleWH(surface, (int)shot.X, (int)shot.Y, 2, 5, shot.Color);
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

        if (_phase == MissionPhase.Paused)
        {
            RenderPauseOverlay(surface, resources);
            return;
        }

        Vga256.FillRectangleWH(surface, 56, 74, 208, 40, 1);
        Vga256.DrawRectangle(surface, 56, 74, 263, 113, 15);

        string title = _phase == MissionPhase.Cleared ? "Mission Cleared" : "Mission Failed";
        string detail = _phase == MissionPhase.Cleared
            ? _advancedToNextLevel
                ? string.Format("Next level ready: {0:00}", _missionLevelNumber + 1)
                : "Episode end placeholder reached"
            : "Retry from pause or return to full-game menu";
        resources.FontRenderer.DrawShadowText(surface, 160, 82, title, FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        resources.FontRenderer.DrawText(surface, 160, 96, string.Format("earned cash: +{0}", _earnedCash), FontKind.Tiny, FontAlignment.Center, 14, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 104, detail, FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 112, "Enter or Esc returns to full-game menu", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private void RenderPauseOverlay(IndexedFrameBuffer surface, SceneResources resources)
    {
        Vga256.FillRectangleWH(surface, 80, 70, 160, 54, 1);
        Vga256.DrawRectangle(surface, 80, 70, 239, 123, 15);
        resources.FontRenderer!.DrawShadowText(surface, 160, 78, "Paused", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        for (int i = 0; i < PauseMenuItems.Length; i++)
        {
            int y = PauseMenuTop + 6 + (i * PauseMenuRowHeight);
            if (i == _pauseSelection)
            {
                resources.FontRenderer.DrawBlendText(surface, 160, y, PauseMenuItems[i], FontKind.Small, FontAlignment.Center, 15, 3);
            }
            else
            {
                resources.FontRenderer.DrawText(surface, 160, y, PauseMenuItems[i], FontKind.Small, FontAlignment.Center, 13, 0, shadow: true);
            }
        }

        resources.FontRenderer.DrawDark(surface, 160, 116, "Up/Down choose  Enter select  Esc resume", FontKind.Tiny, FontAlignment.Center, black: false);
    }
}
