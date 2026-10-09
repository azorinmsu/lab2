using System.Runtime.CompilerServices;

namespace AccountService.Tests;

internal static class DockerApiVersion
{
    // Docker Engine 20.10 speaks API 1.41. Testcontainers defaults to 1.44.
    [ModuleInitializer]
    internal static void UseCompatibleApi()
    {
        const string variable = "DOCKER_API_VERSION";
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
            Environment.SetEnvironmentVariable(variable, "1.41");
    }
}
