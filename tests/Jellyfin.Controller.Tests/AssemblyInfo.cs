using Xunit;

// Several test classes swap the static BaseItem.LibraryManager; running them in parallel races.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
