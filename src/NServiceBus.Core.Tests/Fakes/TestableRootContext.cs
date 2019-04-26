namespace NServiceBus.Core.Tests.Fakes
{
    class TestableRootContext : RootContext
    {
        public TestableRootContext() : base(null, null, null, null)
        {
        }
    }
}