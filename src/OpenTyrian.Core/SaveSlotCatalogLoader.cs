using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public static class SaveSlotCatalogLoader
{
    public static SaveSlotCatalog Load(IUserFileStore userFileStore)
    {
        return SaveGameFileManager.Load(userFileStore).ToCatalog();
    }
}
