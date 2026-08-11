using Blazix.BaseUI.Parity.Tests.Fixtures;
using Blazix.BaseUI.Parity.Tests.Infrastructure;

// Single Blazor server, shared across all tests. It also serves the React bundle
// under /react, so both legs are captured from one origin.
[assembly: AssemblyFixture(typeof(ParityServerAssemblyFixture))]
[assembly: AssemblyFixture(typeof(ParityRunAccumulator))]

// Classes without an explicit [Collection] get their own, so classes run in
// parallel while the tests inside a class run sequentially.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass)]
