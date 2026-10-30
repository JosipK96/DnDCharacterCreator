using System.Reflection;

namespace CharacterCreatorApp.Contracts.Common;

/// <summary>
/// The version an assembly was built as: the release tag without its "v" for a release image,
/// <c>0.0.0-dev</c> (plus the commit, where the build knows it) for anything else. The Api and
/// the client are published together with the same version, so each reads its own.
/// </summary>
public static class BuildVersion
{
    public const string Unversioned = "0.0.0-dev";

    public static string Of(Assembly assembly)
    {
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(version) ? Unversioned : version;
    }
}
