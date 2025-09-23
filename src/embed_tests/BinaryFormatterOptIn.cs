using System;
using NUnit.Framework;

namespace Python.EmbeddingTest
{
    //.NET 7/8 disables BinaryFormatter at runtime for security reasons.
    //Some of our pythonnet tests (and pythonnet’s shutdown path via RuntimeData.Stash)
    //still use BinaryFormatter to round-trip internal state (e.g., MethodBase, runtime
    //caches). Without opting in, tests throw NotSupportedException on .NET 7/8.
    //We enable BinaryFormatter *for this test process only* by setting the official
    //AppContext switch once before any tests run. This is safe for tests and does not
    //affect production code. The switch is guarded by NET7_0_OR_GREATER so it’s a no-op
    //on older TFMs (e.g., net472).
    [SetUpFixture]
    public class BinaryFormatterOptIn
    {
        [OneTimeSetUp]
        public void Enable()
        {
#if NET7_0_OR_GREATER
        System.AppContext.SetSwitch(
            "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
#endif
        }
    }
}
