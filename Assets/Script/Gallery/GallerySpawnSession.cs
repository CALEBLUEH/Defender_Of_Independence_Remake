using UnityEngine;

public enum GallerySpawnKind
{
    Initial,
    VideoReturn
}

public static class GallerySpawnSession
{
    private static GallerySpawnKind nextSpawn = GallerySpawnKind.Initial;

    public static void RequestInitialSpawn() => nextSpawn = GallerySpawnKind.Initial;

    public static void RequestVideoSpawn() => nextSpawn = GallerySpawnKind.VideoReturn;

    public static GallerySpawnKind Consume()
    {
        GallerySpawnKind result = nextSpawn;
        nextSpawn = GallerySpawnKind.Initial;
        return result;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => nextSpawn = GallerySpawnKind.Initial;
}
