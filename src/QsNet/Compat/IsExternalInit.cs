#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    /// <summary>
    ///     Polyfill for init-only setters on netstandard2.0.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
#endif