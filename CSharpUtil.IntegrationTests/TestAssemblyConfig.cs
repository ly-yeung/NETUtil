using Xunit;

// These tests shell out to the dotnet CLI (pack, restore, build, run), so they must not overlap:
// concurrent builds would contend for the same obj/bin folders and NuGet cache.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
