using Orpheus.Adapters.State;

namespace Orpheus.Core.Tests;

public sealed class LastSpeechTextStoreTests
{
    [Fact]
    public async Task Disabled_store_does_not_persist_last_original_text()
    {
        using var workspace = TestWorkspace.Create();
        var store = new FileLastSpeechTextStore(
            new FileLastSpeechTextStoreOptions(workspace.Root, Enabled: false));

        await store.StoreAsync("wise-master", "Turn right.");

        Assert.Null(await store.GetLastOriginalTextAsync("wise-master"));
        Assert.Empty(Directory.EnumerateFiles(workspace.Root));
    }

    [Fact]
    public async Task Store_returns_last_original_text_for_persona()
    {
        using var workspace = TestWorkspace.Create();
        var store = new FileLastSpeechTextStore(
            new FileLastSpeechTextStoreOptions(workspace.Root));

        await store.StoreAsync("wise-master", "Turn right.");

        Assert.Equal("Turn right.", await store.GetLastOriginalTextAsync("wise-master"));
    }

    [Fact]
    public async Task Store_overwrites_previous_original_text_for_persona()
    {
        using var workspace = TestWorkspace.Create();
        var store = new FileLastSpeechTextStore(
            new FileLastSpeechTextStoreOptions(workspace.Root));

        await store.StoreAsync("wise-master", "Turn right.");
        await store.StoreAsync("wise-master", "Continue straight.");

        Assert.Equal("Continue straight.", await store.GetLastOriginalTextAsync("wise-master"));
        Assert.Single(Directory.EnumerateFiles(workspace.Root));
    }

    private sealed class TestWorkspace : IDisposable
    {
        private TestWorkspace(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TestWorkspace Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "orpheus-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TestWorkspace(root);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
