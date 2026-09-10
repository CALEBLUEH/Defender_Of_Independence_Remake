using UnityEngine;

/// <summary>
/// Carries the reason for entering the reusable credits scene across a scene load.
/// If no request was made, credits behave like a direct Gallery playback.
/// </summary>
public static class CreditPlaybackSession
{
    private static bool hasPendingRequest;
    private static bool showCompletionMessage;
    private static bool returnToVideoSpawn;
    private static string destinationScene = "Scene_Gallery";

    public static void RequestFromLevelCompletion(string gallerySceneName = "Scene_Gallery")
    {
        hasPendingRequest = true;
        showCompletionMessage = true;
        returnToVideoSpawn = false;
        destinationScene = SanitizeSceneName(gallerySceneName);
    }

    public static void RequestFromGallery(string gallerySceneName = "Scene_Gallery")
    {
        hasPendingRequest = true;
        showCompletionMessage = false;
        returnToVideoSpawn = true;
        destinationScene = SanitizeSceneName(gallerySceneName);
    }

    public static CreditPlaybackRequest Consume(string fallbackDestination = "Scene_Gallery")
    {
        CreditPlaybackRequest request = hasPendingRequest
            ? new CreditPlaybackRequest(showCompletionMessage, returnToVideoSpawn, destinationScene)
            : new CreditPlaybackRequest(false, false, SanitizeSceneName(fallbackDestination));

        hasPendingRequest = false;
        showCompletionMessage = false;
        returnToVideoSpawn = false;
        destinationScene = "Scene_Gallery";
        return request;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        hasPendingRequest = false;
        showCompletionMessage = false;
        returnToVideoSpawn = false;
        destinationScene = "Scene_Gallery";
    }

    private static string SanitizeSceneName(string sceneName) =>
        string.IsNullOrWhiteSpace(sceneName) ? "Scene_Gallery" : sceneName;
}

public readonly struct CreditPlaybackRequest
{
    public CreditPlaybackRequest(bool showCompletionMessage, bool returnToVideoSpawn, string destinationScene)
    {
        ShowCompletionMessage = showCompletionMessage;
        ReturnToVideoSpawn = returnToVideoSpawn;
        DestinationScene = destinationScene;
    }

    public bool ShowCompletionMessage { get; }
    public bool ReturnToVideoSpawn { get; }
    public string DestinationScene { get; }
}
