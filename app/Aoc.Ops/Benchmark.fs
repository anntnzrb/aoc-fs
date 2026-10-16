/// Filesystem and child-process interpreter for bench and record; never emits puzzle data.
module Aoc.Ops.Benchmark

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Runtime.InteropServices
open System.Threading
open Aoc.Solution
open Aoc.Ops.Bench

let private require =
    function
    | Ok value -> value
    | Error message -> failwith message

let private childProcess root executable arguments input =
    let start =
        ProcessStartInfo(
            executable,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root
        )

    start.Environment["LC_ALL"] <- "C"
    arguments |> List.iter (fun argument -> start.ArgumentList.Add argument)
    use child = new Process(StartInfo = start)
    let watch = Stopwatch.StartNew()

    if not (child.Start()) then
        failwithf "could not start %s" executable

    try
        let output = child.StandardOutput.ReadToEndAsync()
        let errors = child.StandardError.ReadToEndAsync()
        use timeout = new CancellationTokenSource(TimeSpan.FromMinutes 30.)
        let write = child.StandardInput.WriteAsync(input: string)
        write.GetAwaiter().GetResult()
        child.StandardInput.Close()
        child.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult()
        watch.Stop()

        child.ExitCode,
        output.GetAwaiter().GetResult(),
        errors.GetAwaiter().GetResult(),
        decimal watch.Elapsed.TotalMilliseconds * 1000M
    finally
        if child.Id > 0 && not child.HasExited then
            child.Kill(true)
            child.WaitForExit()

let private command root executable arguments =
    let code, output, _, _ = childProcess root executable arguments ""

    if code <> 0 then
        failwithf "%s failed (exit %d); diagnostics withheld" executable code

    output.Trim()

let private gitState root =
    let result args =
        let code, output, _, _ = childProcess root "git" args ""
        code, output

    let branch = result [ "branch"; "--show-current" ]
    let status = result [ "status"; "--porcelain"; "--untracked-files=no" ]
    branch, status

let private validateCanonical root =
    let branch, status = gitState root
    Bench.canonical branch status |> require

let private core () =
    if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
        File.ReadLines "/proc/self/status"
        |> Seq.tryFind (fun line -> line.StartsWith("Cpus_allowed_list:", StringComparison.Ordinal))
        |> Option.bind (fun line -> line.Split(':') |> Array.tryItem 1)
        |> Option.bind (fun value -> value.Trim().Split([| ','; '-' |]) |> Array.tryHead)
    else
        None

let private variants year day =
    Aoc.Registry.solvers
    |> Map.toList
    |> List.choose (fun (key, _) ->
        if key.Year = year && key.Day = day then
            match key.Variant with
            | Primary -> Some "primary"
            | Exploration name -> Some name
        else
            None)
    |> List.sortBy (fun name -> name <> "primary", name)

let private metadata root : Bench.Metadata =
    let cpu =
        if File.Exists "/proc/cpuinfo" then
            File.ReadLines "/proc/cpuinfo"
            |> Seq.tryFind (fun line -> line.StartsWith("model name", StringComparison.Ordinal))
            |> Option.map (fun line -> line.Substring(line.IndexOf(':') + 1).Trim())
            |> Option.defaultValue "unavailable"
        else
            command root "sysctl" [ "-n"; "machdep.cpu.brand_string" ]

    {
        Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
        Commit = command root "git" [ "rev-parse"; "HEAD" ]
        Host = Environment.MachineName
        Os =
            RuntimeInformation.OSDescription
            + " "
            + string RuntimeInformation.OSArchitecture
        Cpu = cpu
        Compiler =
            "SDK "
            + command root "dotnet" [ "--version" ]
            + "; runtime "
            + RuntimeInformation.FrameworkDescription
        Standard = "F# 10"
    }

let private renderSummary =
    function
    | None -> "unavailable"
    | Some(summary: Bench.Summary) ->
        sprintf
            "%s [%s, %s]"
            (Bench.number (Some summary.Median))
            (Bench.number (Some summary.Minimum))
            (Bench.number (Some summary.Maximum))

let private instructionSamples perf names primaryOutput input =
    let workSupport =
        if RuntimeInformation.IsOSPlatform OSPlatform.Linux then
            let code, output, errors, _ = perf "primary" ""

            match code, Bench.instructions errors with
            | 0, Some _ ->
                Bench.agreement "perf empty baseline" "" (code, output) |> require

                Ok
                    "Work counts user-space instructions across runtime/JIT and solver; paired startup subtraction retains noise."
            | _ ->
                Error
                    "Work unavailable: perf instructions:u is unsupported or denied on this host; no substitute metric."
        else
            Error "Work unavailable: perf instructions:u is Linux-only; no substitute metric."

    let available = Result.isOk workSupport

    let workLimitation =
        match workSupport with
        | Ok message
        | Error message -> message

    let workSamples =
        [ 1..5 ]
        |> List.fold
            (fun samples _ ->
                names
                |> List.fold
                    (fun samples name ->
                        if not available then
                            samples
                        else
                            let fullCode, fullOutput, fullErrors, _ = perf name input
                            Bench.agreement name primaryOutput (fullCode, fullOutput) |> require
                            let emptyCode, emptyOutput, emptyErrors, _ = perf name ""

                            Bench.agreement (name + " empty baseline") "" (emptyCode, emptyOutput)
                            |> require

                            match Bench.instructions fullErrors, Bench.instructions emptyErrors with
                            | Some full, Some empty ->
                                samples
                                |> Map.change name (fun old -> Some((full, empty) :: Option.defaultValue [] old))
                            | _ -> failwith "perf counter became unavailable during measurement")
                    samples)
            Map.empty

    workSamples, workLimitation

let private timedSamples root launch names primaryOutput input runs workSamples =
    let timingFile =
        Path.Combine(Path.GetTempPath(), "aoc-bench-" + Guid.NewGuid().ToString("N"))

    let timings =
        try
            [ 1..10 ]
            |> List.fold
                (fun samples round ->
                    names
                    |> List.fold
                        (fun samples name ->
                            if
                                round >
                                    (Map.tryFind name runs
                                     |> Option.defaultWith (fun () -> failwith "missing timing run count"))
                            then
                                samples
                            else
                                let executable, args = launch name

                                let code, output, _, wall =
                                    childProcess
                                        root
                                        "time"
                                        ([ "-o"; timingFile; "-f"; "%U,%S,%M"; "--"; executable ] @ args)
                                        input

                                Bench.agreement name primaryOutput (code, output) |> require
                                let cpu, rss = File.ReadAllText timingFile |> Bench.resources |> require

                                samples
                                |> Map.change name (fun old -> Some((wall, cpu, rss) :: Option.defaultValue [] old)))
                        samples)
                Map.empty
        finally
            if File.Exists timingFile then
                File.Delete timingFile

    let measurements: Bench.Measurement list =
        names
        |> List.map (fun name ->
            let observations =
                Map.tryFind name timings
                |> Option.defaultWith (fun () -> failwith "missing timing samples")

            let pairs = Map.tryFind name workSamples |> Option.defaultValue []

            {
                Solution = name
                Runs =
                    Map.tryFind name runs
                    |> Option.defaultWith (fun () -> failwith "missing timing run count")
                Work = Bench.work pairs
                Instructions = pairs |> List.map fst |> Bench.summarize
                Wall =
                    observations
                    |> List.map (fun (wall, _, _) -> wall)
                    |> Bench.summarize
                    |> Option.defaultWith (fun () -> failwith "no wall samples")
                Cpu =
                    observations
                    |> List.map (fun (_, cpu, _) -> cpu)
                    |> Bench.summarize
                    |> Option.defaultWith (fun () -> failwith "no CPU samples")
                Rss = observations |> List.fold (fun maximum (_, _, rss) -> max maximum rss) 0M
            })

    measurements

let private report year day (names: string list) (measurements: Bench.Measurement list) limitations =
    printfn "Benchmark %d/%02d: optimized publish; all %d variant(s) agree; answers withheld." year day names.Length

    Console.WriteLine
        "Statistics: median [minimum, maximum]; wall/CPU in us; work/instructions in user-space instructions."

    for measurement in measurements do
        printfn
            "%s: runs=%d; work=%s; instructions=%s; wall=%s; cpu=%s; rss_peak_kib=%s"
            measurement.Solution
            measurement.Runs
            (renderSummary measurement.Work)
            (renderSummary measurement.Instructions)
            (renderSummary (Some measurement.Wall))
            (renderSummary (Some measurement.Cpu))
            (Bench.number (Some measurement.Rss))

    limitations |> List.iter (fun limitation -> printfn "Limitation: %s" limitation)


let private recordMeasurements root year day revision measurements limitations =
    if command root "git" [ "rev-parse"; "HEAD" ] <> revision then
        failwith "revision changed during benchmark"

    let host = metadata root
    let directory = Path.Combine(root, "bench")
    let csv = Path.Combine(directory, string year + ".csv")
    Directory.CreateDirectory directory |> ignore<DirectoryInfo>

    let prefix =
        if File.Exists csv then
            let existing = File.ReadAllText csv

            if
                not (existing.StartsWith(Bench.csvHeader + "\n", StringComparison.Ordinal))
                || not (existing.EndsWith("\n", StringComparison.Ordinal))
            then
                failwith "existing benchmark CSV has invalid header or unterminated row"

            ""
        else
            Bench.csvHeader + "\n"

    let hosts = Path.Combine(directory, "hosts")
    Directory.CreateDirectory hosts |> ignore<DirectoryInfo>
    let snapshot = Path.Combine(hosts, host.Host + ".md")

    if not (File.Exists snapshot) then
        File.WriteAllText(
            snapshot,
            String.concat
                "\n"
                ([
                    "# " + host.Host
                    ""
                    "OS: " + host.Os
                    "CPU: " + host.Cpu
                    "Compiler: " + host.Compiler
                    "Language: " + host.Standard
                    ""
                 ]
                 @ limitations)
            + "\n"
        )

    File.AppendAllText(
        csv,
        prefix
        + (measurements |> List.map (Bench.csvRow host year day) |> String.concat "\n")
        + "\n"
    )

    printfn "Recorded %d row(s) in bench/%d.csv." measurements.Length year


/// Verifies all registered variants, measures optimized processes and optionally appends canonical records.
let run root year day record =
    if record then
        validateCanonical root

    let revision = command root "git" [ "rev-parse"; "HEAD" ]
    let names = variants year day

    if not (List.contains "primary" names) then
        failwith "no registered primary for requested day"

    let inputPath = Path.Combine(root, "inputs", string year, sprintf "day%02d.txt" day)
    let input = File.ReadAllText inputPath

    if input.Length = 0 then
        failwith "cached input is empty"

    let published = Path.Combine(root, ".cache", "bench", "publish")

    command root "dotnet" [ "publish"; "app/Aoc.Cli"; "-c"; "Release"; "-o"; published; "--nologo" ]
    |> ignore<string>

    let pinned = core ()

    let launch name =
        let args =
            [ Path.Combine(published, "aoc.dll"); string year; string day ]
            @ (if name = "primary" then [] else [ name ])

        match pinned with
        | Some cpu -> "taskset", [ "-c"; cpu; "dotnet" ] @ args
        | None -> "dotnet", args

    let execute name text =
        let executable, args = launch name
        childProcess root executable args text

    let primaryCode, primaryOutput, _, primaryWall = execute "primary" input
    Bench.agreement "primary" primaryOutput (primaryCode, primaryOutput) |> require

    if primaryOutput.Length = 0 then
        failwith "primary returned no answers"

    let pilots =
        names
        |> List.map (fun name ->
            if name = "primary" then
                name, primaryWall
            else
                let code, output, _, wall = execute name input
                Bench.agreement name primaryOutput (code, output) |> require
                name, wall)
        |> Map.ofList
    // No timed phase begins until every registered variant agrees with the primary.
    let emptyCode, emptyOutput, _, _ = execute "primary" ""
    Bench.agreement "empty baseline" "" (emptyCode, emptyOutput) |> require

    let perf name text =
        let executable, args = launch name
        childProcess root "perf" ([ "stat"; "-x,"; "-e"; "instructions:u"; "--"; executable ] @ args) text

    let workSamples, workLimitation = instructionSamples perf names primaryOutput input

    let runs = pilots |> Map.map (fun _ wall -> if wall > 1000000M then 3 else 10)

    let measurements =
        timedSamples root launch names primaryOutput input runs workSamples

    let limitations =
        [
            workLimitation
            (match pinned with
             | Some cpu -> "Pinned to allowed CPU " + cpu + "."
             | None -> "CPU affinity unavailable; scheduler migration may affect comparisons.")
            "Wall microseconds include process launch, redirected IO, GNU time and .NET runtime/JIT startup; no baseline subtraction."
            "CPU microseconds come from GNU time user+system, rounded to 10000 us per component; small samples are coarse."
            "RSS KiB is GNU time maximum resident set per fresh process, peak across timing runs, including runtime."
            "Variants are interleaved per phase; work uses 5 full/empty pairs when available. Timing uses 10 runs, or 3 after a pilot exceeds 1 s."
            "Background system load and runtime/JIT nondeterminism remain; no generated-code mechanism or speedup is inferred."
        ]

    report year day names measurements limitations

    if record then
        validateCanonical root

        recordMeasurements root year day revision measurements limitations

    0
