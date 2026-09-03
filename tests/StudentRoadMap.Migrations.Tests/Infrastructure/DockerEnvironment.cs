using System.Net.Sockets;

namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>
/// Docker daemon'i mavjudligini ANIQLAYDI va sababini saqlaydi.
///
/// ⚠️ Loyihaning qat'iy talabi (`docs/12` §5.1): Docker bo'lmagan muhitda bu testlar
/// **JIMGINA O'TIB KETMASLIGI** shart — jimgina "yashil" bo'lish eng yomon variant, chunki
/// bo'shliq yopilgandek ko'rinadi, aslida yopilmagan. Shu sabab ikki xatti-harakat bor:
///   • odatiy holatda — test ANIQ SABAB bilan `Skip` bo'ladi (natijalar jadvalida ko'rinadi);
///   • `SRM_REQUIRE_DOCKER=1` bo'lsa — `Skip` YO'Q, test ishga tushadi va fixture
///     tushunarli istisno bilan YIQILADI. CI shu rejimda ishlaydi (`.github/workflows/ci.yml`
///     `migrations` job) — u yerda Docker har doim bor, ya'ni "skip" holati sukut bilan
///     o'tib ketolmaydi.
/// </summary>
internal static class DockerEnvironment
{
    /// <summary>Docker majburiy — `Skip` o'rniga yiqilish (CI uchun).</summary>
    public const string RequireEnvironmentVariable = "SRM_REQUIRE_DOCKER";

    private static readonly Lazy<string?> LazyUnavailableReason =
        new(Probe, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Docker topilmasa — o'zbekcha sabab; topilsa — `null`.</summary>
    public static string? UnavailableReason => LazyUnavailableReason.Value;

    public static bool IsAvailable => UnavailableReason is null;

    /// <summary>`SRM_REQUIRE_DOCKER` yoqilganmi (CI): Docker yo'q bo'lsa skip emas, yiqilish.</summary>
    public static bool IsRequired
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable(RequireEnvironmentVariable);
            return !string.IsNullOrWhiteSpace(raw)
                && (raw.Equals("1", StringComparison.Ordinal)
                    || raw.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Fixture ishga tushishidan oldin chaqiriladi — Docker yo'q bo'lsa TUSHUNARLI istisno
    /// tashlaydi (Testcontainers'ning ichki xatosi o'rniga).
    /// </summary>
    public static void EnsureAvailable()
    {
        if (IsAvailable)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Migratsiya sinovlari uchun Docker SHART, lekin topilmadi: {UnavailableReason}\n"
            + "Bu testlar haqiqiy PostgreSQL 16 konteynerida migratsiya yo'lini tekshiradi "
            + "(`docs/12` §5.1) — ularsiz migratsiyalar faqat jonli bazada sinaladi.\n"
            + $"({RequireEnvironmentVariable} yoqilgan — shu sabab test 'skip' emas, 'fail' bo'ldi.)");
    }

    private static string? Probe()
    {
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            if (dockerHost.StartsWith("unix://", StringComparison.OrdinalIgnoreCase))
            {
                var path = dockerHost["unix://".Length..];
                return CanConnect(path)
                    ? null
                    : $"DOCKER_HOST='{dockerHost}' ko'rsatilgan, lekin soket ulanmadi ({path}).";
            }

            // tcp:// yoki npipe:// — soket sinovi qo'llanmaydi, ishonamiz (xato bo'lsa
            // fixture konteyner ko'targanda aniq xato beradi, jimgina o'tib ketmaydi).
            return null;
        }

        foreach (var candidate in CandidateSockets())
        {
            if (CanConnect(candidate))
            {
                return null;
            }
        }

        if (OperatingSystem.IsWindows())
        {
            return File.Exists(@"\\.\pipe\docker_engine")
                ? null
                : "Docker named pipe topilmadi (\\\\.\\pipe\\docker_engine) — Docker Desktop ishga tushirilganmi?";
        }

        return "Docker soketi topilmadi (tekshirildi: "
            + string.Join(", ", CandidateSockets())
            + "). Docker Desktop/Colima ishga tushirilganmi?";
    }

    private static IEnumerable<string> CandidateSockets()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        yield return "/var/run/docker.sock";
        yield return Path.Combine(home, ".docker", "run", "docker.sock");      // Docker Desktop (macOS)
        yield return Path.Combine(home, ".colima", "default", "docker.sock");  // Colima
        yield return Path.Combine(home, ".rd", "docker.sock");                 // Rancher Desktop

        var xdgRuntimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (!string.IsNullOrWhiteSpace(xdgRuntimeDir))
        {
            yield return Path.Combine(xdgRuntimeDir, "docker.sock");           // rootless Linux
        }
    }

    private static bool CanConnect(string socketPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint(socketPath));
            return socket.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
