using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pkmds.WorkerPrototype;

[SupportedOSPlatform("browser")]
public static partial class WorkerMethods
{
    [JSExport]
    public static void InitializeCrypto()
    {
        var js = new Pkmds.Web.Services.JsService(new PrototypeJsRuntime());
        RuntimeCryptographyProvider.Aes = new Pkmds.Web.Services.BlazorAesProvider(js);
        RuntimeCryptographyProvider.Md5 = new Pkmds.Web.Services.BlazorMd5Provider(js);
    }

    [JSExport]
    public static string CheckCrypto()
    {
        var checks = CryptoBridgeChecks.Run();
        // Real Gen 7 signing uses AES-ECB; BDSP checksum generation uses MD5.
        // Exercise those PKHeX call sites as well as independent known answers.
        _ = new SAV7SM().Write();
        var bdsp = new SAV8BS();
        var exported = bdsp.Write();
        if (!new SAV8BS(exported).ChecksumsValid)
        {
            throw new CryptographicException("Synthetic BDSP checksum did not verify.");
        }
        checks["gen7-signing"] = "passed";
        checks["bdsp-checksum"] = "passed";
        return JsonSerializer.Serialize(checks, PrototypeJsonContext.Default.DictionaryStringString);
    }

    [JSExport]
    public static string CreateRequest(bool emptySet)
    {
        var save = new SAV7SM
        {
            OT = "WORKER",
            ID32 = 123456,
            Version = GameVersion.SN,
            Language = (int)LanguageID.English,
            Country = 49,
            Region = 7,
            ConsoleRegion = 1,
        };
        // As in ApplicationBenchmarkFixtures: the pinned PKHeX package leaves
        // the blank Gen 7 footer unset. Initialize only this synthetic fixture.
        "FEEB"u8.CopyTo(save.Data[^0x1F0..]);
        // Send an owned in-memory snapshot, not a save export. Gen 7 export
        // signs data with AES, which needs the app's browser crypto bridge.
        // No save export is part of this generation-only experiment.
        var request = new GenerationRequest(save.Data.ToArray(),
            emptySet ? "" : "Pikachu\nLevel: 100\nHardy Nature\n- Thunderbolt", false);
        return JsonSerializer.Serialize(request, PrototypeJsonContext.Default.GenerationRequest);
    }

    [JSExport]
    public static async Task<string> Generate(string requestJson)
    {
        var request = ReadRequest(requestJson);
        // The worker owns a snapshot. SaveUtil and the engine never receive the
        // request's buffer, much less a live editor/save object from the UI.
        var save = SaveUtil.GetSaveFile(request.SaveBytes.ToArray())
                   ?? throw new ArgumentException("Unrecognized save snapshot.");
        if (save is not SAV7SM
            {
                OT: "WORKER", ID32: 123456, Version: GameVersion.SN,
                Country: 49, Region: 7, ConsoleRegion: 1, Language: (int)LanguageID.English
            })
        {
            throw new InvalidOperationException("Synthetic trainer context did not round-trip.");
        }
        var original = save.Data.ToArray();
        ParseSettings.InitFromSaveFileData(save);
        GameInfo.Strings = GameInfo.GetStrings("en");
        var engine = new LegalizationService(new PrototypeAppState { IsHaXEnabled = request.IsHaXEnabled });
        var outcome = await engine.GenerateFromSetAsync(new ShowdownSet(request.Set), save);
        if (!save.Data.SequenceEqual(original))
        {
            throw new InvalidOperationException("Generation mutated its save snapshot.");
        }
        var bytes = new byte[outcome.Pokemon.SIZE_STORED];
        outcome.Pokemon.WriteDecryptedDataStored(bytes);
        var reply = new GenerationReply(bytes, outcome.Status, outcome.FailureReason,
            new LegalityAnalysis(outcome.Pokemon).Valid);
        return JsonSerializer.Serialize(reply, PrototypeJsonContext.Default.GenerationReply);
    }

    [JSExport]
    public static string Validate(string requestJson, string replyJson)
    {
        var request = ReadRequest(requestJson);
        var reply = JsonSerializer.Deserialize(replyJson, PrototypeJsonContext.Default.GenerationReply)
                    ?? throw new ArgumentException("Missing reply.");
        var save = SaveUtil.GetSaveFile(request.SaveBytes.ToArray())
                   ?? throw new ArgumentException("Unrecognized save snapshot.");
        ParseSettings.InitFromSaveFileData(save);
        var pokemon = EntityFormat.GetFromBytes(reply.PokemonBytes.ToArray(), save.Context)
                      ?? throw new InvalidOperationException("Result bytes did not rehydrate.");
        var roundTrip = new byte[pokemon.SIZE_STORED];
        pokemon.WriteDecryptedDataStored(roundTrip);
        if (!roundTrip.AsSpan().SequenceEqual(reply.PokemonBytes) ||
            new LegalityAnalysis(pokemon).Valid != reply.IsLegal || pokemon is not PK7)
        {
            throw new InvalidOperationException("Result bytes/type/legality changed in transit.");
        }
        var set = new ShowdownSet(request.Set);
        if (set.Species == 0)
        {
            if (reply.Status != LegalizationStatus.Failed || reply.FailureReason != "No species specified." || pokemon.Species != 0)
            {
                throw new InvalidOperationException("Empty-set failure semantics changed.");
            }
        }
        else if (reply.Status != LegalizationStatus.Success || !reply.IsLegal ||
                 pokemon.Species != set.Species || pokemon.CurrentLevel != set.Level ||
                 pokemon.Nature != set.Nature || pokemon.Move1 != set.Moves[0])
        {
            var analysis = new LegalityAnalysis(pokemon);
            var context = LegalityLocalizationContext.Create(analysis);
            var details = new List<string>();
            foreach (var result in analysis.Results)
            {
                if (!result.Valid) details.Add(context.Humanize(in result, verbose: false));
            }
            throw new InvalidOperationException($"Generation constraints failed: {reply.Status}: {reply.FailureReason}; {string.Join("; ", details)}");
        }
        // RNG-dependent PID/IV/EC/date values can differ across runtime instances.
        // Compare the requested semantics; never claim randomized bytes identical.
        return JsonSerializer.Serialize(new ResultSummary(reply.Status, reply.FailureReason,
                pokemon.Species, pokemon.CurrentLevel, pokemon.Nature, pokemon.Move1, reply.IsLegal),
            PrototypeJsonContext.Default.ResultSummary);
    }

    [JSExport]
    public static double BlockForMilliseconds(int milliseconds)
    {
        if (milliseconds is < 0 or > 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds));
        }
        // Bounded synthetic uncooperative work, not a PKHeX performance workload.
        // It proves whether cancellation/timeout actually terminates a busy worker.
        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < milliseconds) { }
        return timer.Elapsed.TotalMilliseconds;
    }

    [JSExport]
    public static string MemorySnapshot() => JsonSerializer.Serialize(
        new MemoryObservation(GC.GetTotalMemory(false), GC.GetTotalAllocatedBytes(),
            System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription),
        PrototypeJsonContext.Default.MemoryObservation);

    private static GenerationRequest ReadRequest(string json) =>
        JsonSerializer.Deserialize(json, PrototypeJsonContext.Default.GenerationRequest)
        ?? throw new ArgumentException("Missing request.");
}

internal sealed record GenerationRequest(byte[] SaveBytes, string Set, bool IsHaXEnabled);
internal sealed record GenerationReply(byte[] PokemonBytes, LegalizationStatus Status, string? FailureReason, bool IsLegal);
internal sealed record ResultSummary(LegalizationStatus Status, string? FailureReason, ushort Species, byte Level,
    Nature Nature, ushort Move, bool IsLegal);
internal sealed record MemoryObservation(long ManagedBytes, long AllocatedBytes, string Runtime);

[JsonSerializable(typeof(GenerationRequest))]
[JsonSerializable(typeof(GenerationReply))]
[JsonSerializable(typeof(ResultSummary))]
[JsonSerializable(typeof(MemoryObservation))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class PrototypeJsonContext : JsonSerializerContext;
