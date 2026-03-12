namespace OpenTyrian.Core;

public static class ItemPriceCalculator
{
    public const int MaxWeaponPower = 11;

    public static bool IsWeaponCategory(ItemCategoryKind kind)
    {
        return kind == ItemCategoryKind.FrontWeapon || kind == ItemCategoryKind.RearWeapon;
    }

    public static int ClampWeaponPower(int itemId, int power)
    {
        if (itemId == 0)
        {
            return 0;
        }

        if (power < 1)
        {
            return 1;
        }

        if (power > MaxWeaponPower)
        {
            return MaxWeaponPower;
        }

        return power;
    }

    public static int GetBaseCost(ItemCategoryKind kind, int itemId, ItemCatalog? catalog)
    {
        return catalog?.GetCost(kind, itemId) ?? 0;
    }

    public static int GetItemValue(ItemCategoryKind kind, int itemId, int weaponPower, ItemCatalog? catalog)
    {
        int baseCost = GetBaseCost(kind, itemId, catalog);
        if (baseCost <= 0 || itemId == 0)
        {
            return 0;
        }

        if (!IsWeaponCategory(kind))
        {
            return baseCost;
        }

        int clampedPower = ClampWeaponPower(itemId, weaponPower);
        int totalValue = baseCost;
        for (int i = 1; i < clampedPower; i++)
        {
            totalValue += GetWeaponPowerStepCost(baseCost, i);
        }

        return totalValue;
    }

    public static int GetWeaponUpgradeCost(ItemCategoryKind kind, int itemId, int currentPower, ItemCatalog? catalog)
    {
        if (!IsWeaponCategory(kind) || itemId == 0)
        {
            return 0;
        }

        int clampedPower = ClampWeaponPower(itemId, currentPower);
        if (clampedPower >= MaxWeaponPower)
        {
            return 0;
        }

        int baseCost = GetBaseCost(kind, itemId, catalog);
        return GetWeaponPowerStepCost(baseCost, clampedPower);
    }

    public static int GetWeaponDowngradeValue(ItemCategoryKind kind, int itemId, int currentPower, ItemCatalog? catalog)
    {
        if (!IsWeaponCategory(kind) || itemId == 0)
        {
            return 0;
        }

        int clampedPower = ClampWeaponPower(itemId, currentPower);
        if (clampedPower <= 1)
        {
            return 0;
        }

        int baseCost = GetBaseCost(kind, itemId, catalog);
        return GetWeaponPowerStepCost(baseCost, clampedPower - 1);
    }

    private static int GetWeaponPowerStepCost(int baseCost, int powerStep)
    {
        int scale = 0;
        for (int i = powerStep; i > 0; i--)
        {
            scale += i;
        }

        return baseCost * scale;
    }
}
