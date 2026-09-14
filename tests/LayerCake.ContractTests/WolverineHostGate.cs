using System.Reflection;
using JasperFx;
using Wolverine;

namespace LayerCake.ContractTests;

/// <summary>
/// Since slice 005 this process builds two kinds of Wolverine host, the after
/// twin and Tendr, from two collections running in parallel. Wolverine and
/// JasperFx each pin the application assembly process-wide to whichever host
/// is built first, and a later host then scans that assembly for handlers
/// and endpoints instead of its own (the after twin would try to map Tendr's
/// endpoints). So every Wolverine host is built through here: one at a time,
/// with both remembered assemblies set to that host's own.
/// </summary>
public static class WolverineHostGate
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<T> BuildAsync<T>(Assembly applicationAssembly, Func<Task<T>> build)
    {
        await Gate.WaitAsync();

        try
        {
            JasperFxOptions.RememberedApplicationAssembly = applicationAssembly;
            WolverineOptions.RememberedApplicationAssembly = applicationAssembly;

            return await build();
        }
        finally
        {
            Gate.Release();
        }
    }

    public static Task BuildAsync(Assembly applicationAssembly, Action build)
    {
        return BuildAsync(applicationAssembly, () =>
        {
            build();
            return Task.FromResult(true);
        });
    }
}
