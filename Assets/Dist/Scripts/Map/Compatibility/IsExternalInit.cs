// Compatibility shim for init-only properties when the assembly is built outside Unity's netstandard profile.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
