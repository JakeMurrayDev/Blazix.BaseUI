using Blazix.BaseUI.Playwright.Tests.Fixtures;

// Single Blazor server shared across all tests via assembly fixture
[assembly: AssemblyFixture(typeof(BlazorServerAssemblyFixture))]

// Each test class gets its own collection, enabling parallel execution between classes
// Tests within each class run sequentially (stable), classes run in parallel (fast)
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass)]