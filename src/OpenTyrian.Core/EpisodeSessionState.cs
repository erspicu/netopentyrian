namespace OpenTyrian.Core;

public sealed class EpisodeSessionState
{
    private static readonly int[] FullGameInitialCashByEpisode = { 10000, 15000, 20000, 30000 };

    public EpisodeSessionState(EpisodeStartInfo startInfo, GameStartMode startMode)
    {
        StartInfo = startInfo;
        StartMode = startMode;
        InitialEpisodeNumber = startInfo.EpisodeNumber;
        CurrentEpisodeNumber = startInfo.EpisodeNumber;
        CurrentLevelNumber = 1;
        EpisodeFile = startInfo.EpisodeFile;
        CubeFile = startInfo.CubeFile;
        LevelFile = startInfo.LevelFile;
        BonusLevel = false;
        JumpBackToEpisode1 = false;
        GameHasRepeated = false;
        LevelCount = startInfo.LevelIndex?.LevelCount ?? 0;
        LevelOffsets = startInfo.LevelIndex?.LevelOffsets ?? new int[0];
        CurrentLevelOffset = LevelOffsets.Count > 0 ? LevelOffsets[0] : 0;
        EndOffset = startInfo.LevelIndex?.EndOffset ?? 0;
        ScriptExists = startInfo.ScriptInfo?.Exists ?? false;
        ScriptLength = startInfo.ScriptInfo?.Length ?? 0;
        ScriptPreviewStringCount = startInfo.ScriptInfo?.PreviewStringCount ?? 0;
        ScriptSectionMarkerCount = startInfo.ScriptInfo?.SectionMarkerCount ?? 0;
        ScriptSections = startInfo.ScriptInfo?.Sections ?? new EpisodeSectionInfo[0];
        MainLevelEntries = BuildMainLevelEntries(ScriptSections);
        CurrentMainLevelEntry = MainLevelEntries.Count > 0 ? MainLevelEntries[Math.Min(CurrentLevelNumber - 1, MainLevelEntries.Count - 1)] : null;
        CubeExists = startInfo.CubeInfo?.Exists ?? false;
        CubeLength = startInfo.CubeInfo?.Length ?? 0;
        CubePreviewStringCount = startInfo.CubeInfo?.PreviewStringCount ?? 0;
        CubeSectionMarkerCount = startInfo.CubeInfo?.SectionMarkerCount ?? 0;
        CubeEntries = startInfo.CubeInfo?.Entries ?? new CubeTextEntry[0];
        PlayerCount = startMode.GetPlayerCount();
        IsArcadeLikeMode = startMode.IsArcadeLike();
        InitialCash = GetInitialCash(startInfo.EpisodeNumber, startMode);
        Cash = InitialCash;
        SaveLevel = CurrentLevelNumber;
        LastLevelSaveRequested = false;
        ItemShopSongIndex = null;
        ItemAvailabilityBlockLineCount = 0;
        ItemAvailabilityRows = new IList<int>[0];
        ItemAvailabilityMaxPerRow = new int[0];
        ShopCategories = new ShopCategory[0];
        PlayerLoadout = new PlayerLoadoutState();
        FadeBlackRequested = false;
        NetworkTextSyncRequested = false;
    }

    public EpisodeStartInfo StartInfo { get; }

    public GameStartMode StartMode { get; }

    public int InitialCash { get; }

    public int InitialEpisodeNumber { get; }

    public int CurrentEpisodeNumber { get; private set; }

    public int CurrentLevelNumber { get; private set; }

    public string EpisodeFile { get; }

    public string CubeFile { get; }

    public string LevelFile { get; }

    public bool BonusLevel { get; }

    public bool JumpBackToEpisode1 { get; }

    public bool GameHasRepeated { get; }

    public int LevelCount { get; }

    public IList<int> LevelOffsets { get; }

    public int CurrentLevelOffset { get; private set; }

    public int EndOffset { get; }

    public bool ScriptExists { get; }

    public long ScriptLength { get; }

    public int ScriptPreviewStringCount { get; }

    public int ScriptSectionMarkerCount { get; }

    public IList<EpisodeSectionInfo> ScriptSections { get; }

    public IList<MainLevelEntry> MainLevelEntries { get; }

    public MainLevelEntry? CurrentMainLevelEntry { get; private set; }

    public bool CubeExists { get; }

    public long CubeLength { get; }

    public int CubePreviewStringCount { get; }

    public int CubeSectionMarkerCount { get; }

    public IList<CubeTextEntry> CubeEntries { get; }

    public int PlayerCount { get; }

    public bool IsArcadeLikeMode { get; private set; }

    public int Cash { get; private set; }

    public int SaveLevel { get; private set; }

    public bool LastLevelSaveRequested { get; private set; }

    public int? ItemShopSongIndex { get; private set; }

    public int ItemAvailabilityBlockLineCount { get; private set; }

    public IList<IList<int>> ItemAvailabilityRows { get; private set; }

    public IList<int> ItemAvailabilityMaxPerRow { get; private set; }

    public IList<ShopCategory> ShopCategories { get; private set; }

    public PlayerLoadoutState PlayerLoadout { get; }

    public bool FadeBlackRequested { get; private set; }

    public bool NetworkTextSyncRequested { get; private set; }

    private static IList<MainLevelEntry> BuildMainLevelEntries(IList<EpisodeSectionInfo> sections)
    {
        List<MainLevelEntry> entries = new(sections.Count);
        for (int i = 0; i < sections.Count; i++)
        {
            entries.Add(new MainLevelEntry
            {
                MainLevelNumber = i + 1,
                Section = sections[i],
            });
        }

        return entries;
    }

    public bool SetCurrentMainLevel(int mainLevelNumber)
    {
        if (mainLevelNumber < 1 || mainLevelNumber > MainLevelEntries.Count)
        {
            return false;
        }

        CurrentLevelNumber = mainLevelNumber;
        CurrentMainLevelEntry = MainLevelEntries[mainLevelNumber - 1];
        CurrentLevelOffset = LevelOffsets.Count >= mainLevelNumber ? LevelOffsets[mainLevelNumber - 1] : 0;
        return true;
    }

    public void ClearLastLevelSaveRequest()
    {
        LastLevelSaveRequested = false;
    }

    public void SetSaveLevel(int mainLevelNumber)
    {
        SaveLevel = mainLevelNumber;
    }

    public void RequestLastLevelSave()
    {
        LastLevelSaveRequested = true;
    }

    public void SetItemShopSongIndex(int songIndex)
    {
        ItemShopSongIndex = songIndex;
    }

    public void SetItemAvailabilityBlockLineCount(int lineCount)
    {
        ItemAvailabilityBlockLineCount = lineCount;
    }

    public void SetItemAvailability(ItemAvailabilityInfo? availability)
    {
        ItemAvailabilityRows = availability?.Rows ?? new IList<int>[0];
        ItemAvailabilityMaxPerRow = availability?.MaxPerRow ?? new int[0];
        ItemAvailabilityBlockLineCount = ItemAvailabilityMaxPerRow.Count;
        ShopCategories = availability?.ShopCategories ?? new ShopCategory[0];
        SeedLoadoutFromAvailability();
    }

    public void EquipShopItem(ItemCategoryKind kind, int itemId)
    {
        PlayerLoadout.Equip(kind, itemId);
    }

    public int GetTradeInValue(ItemCategoryKind kind, ItemCatalog? itemCatalog)
    {
        return PlayerLoadout.GetEquippedValue(kind, itemCatalog);
    }

    public int GetTransactionCostDelta(ItemCategoryKind kind, int itemId, int weaponPower, ItemCatalog? itemCatalog)
    {
        int currentValue = GetTradeInValue(kind, itemCatalog);
        int nextValue = ItemPriceCalculator.GetItemValue(kind, itemId, weaponPower, itemCatalog);
        return nextValue - currentValue;
    }

    public int GetCashAfterTransaction(ItemCategoryKind kind, int itemId, int weaponPower, ItemCatalog? itemCatalog)
    {
        return Cash - GetTransactionCostDelta(kind, itemId, weaponPower, itemCatalog);
    }

    public bool CanAffordTransaction(ItemCategoryKind kind, int itemId, int weaponPower, ItemCatalog? itemCatalog)
    {
        return GetCashAfterTransaction(kind, itemId, weaponPower, itemCatalog) >= 0;
    }

    public bool TryCommitShopSelection(ItemCategoryKind kind, int itemId, int weaponPower, ItemCatalog? itemCatalog)
    {
        int cashAfter = GetCashAfterTransaction(kind, itemId, weaponPower, itemCatalog);
        if (cashAfter < 0)
        {
            return false;
        }

        Cash = cashAfter;
        PlayerLoadout.Equip(kind, itemId);
        if (ItemPriceCalculator.IsWeaponCategory(kind))
        {
            PlayerLoadout.SetWeaponPower(kind, weaponPower);
        }

        return true;
    }

    public int GetTotalAssetValue(ItemCatalog? itemCatalog)
    {
        return PlayerLoadout.GetTotalValue(itemCatalog);
    }

    public int GetTotalScore(ItemCatalog? itemCatalog)
    {
        return Cash + GetTotalAssetValue(itemCatalog);
    }

    public void SetWeaponPower(ItemCategoryKind kind, int power)
    {
        PlayerLoadout.SetWeaponPower(kind, power);
    }

    private void SeedLoadoutFromAvailability()
    {
        foreach (ShopCategory category in ShopCategories)
        {
            if (category.ItemCount > 0 && PlayerLoadout.GetEquippedItemId(category.Kind) == 0)
            {
                PlayerLoadout.Equip(category.Kind, category.ItemIds[0]);
                if (ItemPriceCalculator.IsWeaponCategory(category.Kind) && category.ItemIds[0] != 0)
                {
                    PlayerLoadout.SetWeaponPower(category.Kind, 1);
                }
            }
        }
    }

    public void RequestFadeBlack()
    {
        FadeBlackRequested = true;
    }

    public void RequestNetworkTextSync()
    {
        NetworkTextSyncRequested = true;
    }

    public void ClearTransientCommandFlags()
    {
        FadeBlackRequested = false;
        NetworkTextSyncRequested = false;
    }

    private static int GetInitialCash(int episodeNumber, GameStartMode startMode)
    {
        if (startMode != GameStartMode.FullGame)
        {
            return 0;
        }

        if (episodeNumber < 1 || episodeNumber > FullGameInitialCashByEpisode.Length)
        {
            return 0;
        }

        return FullGameInitialCashByEpisode[episodeNumber - 1];
    }
}
