module Aoc.Ops.Program

open System
open System.IO
open System.Net.Http
open System.Diagnostics
open System.Collections.Generic

let private root () =
    let rec walk (directory: DirectoryInfo) =
        if File.Exists(Path.Combine(directory.FullName, "global.json")) then
            directory.FullName
        else
            match directory.Parent with
            | null -> failwith "cannot find repository root: no global.json"
            | parent -> walk parent

    walk (DirectoryInfo(Directory.GetCurrentDirectory()))

let private require =
    function
    | Ok value -> value
    | Error message -> failwith message

let private atomicWrite (path: string) (text: string) =
    let parent = Path.GetDirectoryName path |> Option.ofObj |> Option.defaultValue "."
    Directory.CreateDirectory parent |> ignore<DirectoryInfo>
    let temporary = path + ".tmp"

    try
        File.WriteAllText(temporary, text)
        File.Move(temporary, path, true)
    finally
        if File.Exists temporary then
            File.Delete temporary

let private session root =
    let path = Path.Combine(root, ".env")

    if not (File.Exists path) then
        failwith "AOC_SESSION not set in .env"

    File.ReadAllText path |> Session.parse |> require

/// The User-Agent sent to adventofcode.com: a bare product token, deliberately anonymous.
let userAgent = "aoc-fs"

/// Formats an optional Retry-After value without reflecting untrusted header text.
let retryAfter (now: DateTimeOffset) (header: string option) =
    let delay =
        header
        |> Option.bind (fun value ->
            match
                Int64.TryParse(value, Globalization.NumberStyles.None, Globalization.CultureInfo.InvariantCulture)
            with
            | true, seconds -> Some(decimal seconds)
            | _ ->
                match
                    DateTimeOffset.TryParseExact(
                        value,
                        "r",
                        Globalization.CultureInfo.InvariantCulture,
                        Globalization.DateTimeStyles.AssumeUniversal
                    )
                with
                | true, date -> Some(max 0M (Decimal.Ceiling(decimal (date - now).TotalSeconds)))
                | _ -> None)

    match delay with
    | Some seconds -> sprintf "rate limited; retry after %Ms" seconds
    | None -> "rate limited; retry delay unavailable"

/// Sends one request, returning fixed diagnostics on transport failure and HTTP errors.
let request (baseAddress: Uri) token method (path: string) form =
    try
        if Session.parse ("AOC_SESSION=" + token) <> Ok token then
            failwith "invalid session cookie"

        use client = new HttpClient()
        use message = new HttpRequestMessage(method, Uri(baseAddress, path))
        message.Headers.Add("Cookie", "session=" + token)
        message.Headers.Add("User-Agent", userAgent)

        match form with
        | Some values -> message.Content <- new FormUrlEncodedContent(values: KeyValuePair<string, string> list)
        | None -> ()

        use response = client.Send message

        match int response.StatusCode with
        | 200 -> Ok(response.Content.ReadAsStringAsync().GetAwaiter().GetResult())
        | 429 ->
            let header =
                match response.Headers.TryGetValues "Retry-After" with
                | true, values -> values |> Option.ofObj |> Option.bind Seq.tryHead
                | _ -> None

            Error(retryAfter DateTimeOffset.UtcNow header)
        | status -> Error(sprintf "HTTP %d" status)
    with _ ->
        Error "HTTP request failed"

/// Fetches and atomically caches only an HTTP 200 response.
let fetch baseAddress token path cache =
    request baseAddress token HttpMethod.Get path None
    |> Result.map (fun text -> atomicWrite cache text)

/// Submits once and returns the public exit code and sanitized diagnostic.
let submit baseAddress token year day part answer =
    let form = [ KeyValuePair("level", string part); KeyValuePair("answer", answer) ]

    match request baseAddress token HttpMethod.Post (sprintf "%d/day/%d/answer" year day) (Some form) with
    | Error message -> 4, message
    | Ok html ->
        match Statement.submission html with
        | Error _ -> 3, "no submission article found"
        | Ok text -> Submission.classify text, text.Replace(token, "[redacted]")

let private baseAddress = Uri "https://adventofcode.com/"

let private scan root =
    let files =
        Directory.GetFiles(root, "*.fs", SearchOption.AllDirectories)
        |> Array.map (fun path -> Path.GetRelativePath(root, path).Replace('\\', '/'))
        |> Array.toList

    let years =
        Directory.GetDirectories(root, "Y*")
        |> Array.map Path.GetFileName
        |> Array.choose Option.ofObj
        |> Array.toList

    files @ years

let private format root paths =
    let start = ProcessStartInfo("dotnet", WorkingDirectory = root)

    for argument in [ "tool"; "run"; "fantomas" ] @ paths do
        start.ArgumentList.Add argument

    use child =
        Process.Start start
        |> Option.ofObj
        |> Option.defaultWith (fun () -> failwith "could not start Fantomas")

    child.WaitForExit()

    if child.ExitCode <> 0 then
        failwithf "Fantomas exited %d" child.ExitCode

[<TailCall>]
let rec private checkDirectory path =
    if File.Exists path then
        failwithf "file blocks directory: %s" path
    elif not (Directory.Exists path) then
        match Path.GetDirectoryName path |> Option.ofObj with
        | Some parent when parent <> path && parent.Length > 0 -> checkDirectory parent
        | _ -> ()

let private writeScaffold root writes fixture =
    // Check every destination, including ancestor collisions, before the first write.
    for path, _ in writes do
        let target = Path.Combine(root, path)

        if
            Directory.Exists target
            || File.Exists(target + ".tmp")
            || Directory.Exists(target + ".tmp")
        then
            failwithf "blocked scaffold path: %s" path

        Path.GetDirectoryName target |> Option.ofObj |> Option.iter checkDirectory

        if File.Exists target then
            use stream = File.Open(target, FileMode.Open, FileAccess.Write, FileShare.Read)
            ()

    checkDirectory fixture

    let originals =
        writes
        |> List.map (fun (path, _) ->
            let target = Path.Combine(root, path)

            target,
            if File.Exists target then
                Some(File.ReadAllBytes target)
            else
                None)

    let rec missingDirectories path =
        if Directory.Exists path then
            []
        else
            let parents =
                Path.GetDirectoryName path
                |> Option.ofObj
                |> Option.map missingDirectories
                |> Option.defaultValue []

            path :: parents

    let directories =
        fixture
        :: (originals
            |> List.choose (fun (path, _) -> Path.GetDirectoryName path |> Option.ofObj))
        |> List.collect missingDirectories
        |> List.distinct
        |> List.sortByDescending String.length

    try
        for path, text in writes do
            atomicWrite (Path.Combine(root, path)) text

        Directory.CreateDirectory fixture |> ignore<DirectoryInfo>

        writes
        |> List.choose (fun (path, _) ->
            if path.EndsWith(".fs", StringComparison.Ordinal) then
                Some path
            else
                None)
        |> format root
    with originalError ->
        let rollbackErrors = ResizeArray<exn>()

        let restore action =
            try
                action ()
            with error ->
                rollbackErrors.Add error

        for path, content in originals do
            restore (fun () ->
                match content with
                | Some bytes -> File.WriteAllBytes(path, bytes)
                | None ->
                    if File.Exists path then
                        File.Delete path)

        for directory in directories do
            restore (fun () ->
                if
                    Directory.Exists directory
                    && Directory.GetFileSystemEntries(directory).Length = 0
                then
                    Directory.Delete directory)

        if rollbackErrors.Count > 0 then
            raise (AggregateException("scaffold rollback failed", originalError :: List.ofSeq rollbackErrors))

        reraise ()

/// Creates a primary or exploration, refusing collisions and rolling back failed writes or formatting.
let scaffold root year day exploration =
    let at path = Path.Combine(root, path)
    let dayPath = sprintf "src/Aoc/Y%d/Day%02d" year day
    let testPath = sprintf "tests/Aoc.Tests/Y%d/Day%02dTests.fs" year day

    let solutionPath = dayPath + "/" + Option.defaultValue "Primary" exploration + ".fs"

    match exploration with
    | None ->
        if
            Directory.Exists(at dayPath)
            || File.Exists(at dayPath)
            || File.Exists(at testPath)
            || Directory.Exists(at testPath)
        then
            failwith "refusing to overwrite an existing day or test path"
    | Some name ->
        if File.Exists(at solutionPath) || Directory.Exists(at solutionPath) then
            failwith "refusing to overwrite an existing exploration path"

        if Directory.Exists(at dayPath) then
            Directory.GetFiles(at dayPath, "*.fs")
            |> Array.map (fun path -> sprintf "Y%d/Day%02d/%s" year day (Path.GetFileName path))
            |> Array.toList
            |> Scaffold.uniqueExploration year day name
            |> require

        if
            [ dayPath + "/Primary.fs"; dayPath + "/Input.fs"; testPath ]
            |> List.forall (at >> File.Exists)
            |> not
        then
            failwith "exploration requires an existing primary, input and day tests"

    let template name =
        File.ReadAllText(at ("templates/" + name + ".fs.template"))

    let created =
        match exploration with
        | None ->
            [
                dayPath + "/Input.fs", Scaffold.render year day (template "Input")
                solutionPath, Scaffold.render year day (template "Primary")
                testPath, Scaffold.render year day (template "DayTests")
            ]
        | Some name ->
            [
                solutionPath, Scaffold.exploration year day name (template "Exploration")
                testPath,
                File.ReadAllText(at testPath)
                + "\n"
                + Scaffold.exploration year day name (template "ExplorationTests")
            ]

    let libraryFiles =
        scan (at "src/Aoc")
        @ (created
           |> List.map fst
           |> List.choose (fun path ->
               if path.StartsWith("src/Aoc/", StringComparison.Ordinal) then
                   Some(path.Substring(8))
               else
                   None))
        |> List.distinct

    let testFiles =
        scan (at "tests/Aoc.Tests") @ [ sprintf "Y%d/Day%02dTests.fs" year day ]
        |> List.distinct

    let libraryProject = "src/Aoc/Aoc.fsproj"
    let testProject = "tests/Aoc.Tests/Aoc.Tests.fsproj"

    let regenerated =
        [
            sprintf "src/Aoc/Y%d/Registry.fs" year, Scaffold.yearRegistry (template "Registry") year libraryFiles
            "src/Aoc/Registry.fs", Scaffold.rootRegistry (template "RootRegistry") libraryFiles
            libraryProject,
            File.ReadAllText(at libraryProject)
            |> Scaffold.project true (Scaffold.libraryEntries libraryFiles)
            |> require
            testProject,
            File.ReadAllText(at testProject)
            |> Scaffold.project false (Scaffold.testEntries testFiles)
            |> require
        ]

    let writes = created @ regenerated
    let fixture = at (sprintf "fixtures/%d/%02d" year day)
    writeScaffold root writes fixture

let private run root =
    function
    | Command.Doc ->
        let rec sources directory =
            seq {
                yield! Directory.EnumerateFiles(directory, "*.fs")
                yield! Directory.EnumerateFiles(directory, "*.fsi")
                yield! Directory.EnumerateFiles(directory, "*.fsx")

                for child in Directory.EnumerateDirectories directory do
                    if
                        not (
                            [ "bin"; "obj"; ".cache" ]
                            |> List.contains (Path.GetFileName child |> Option.ofObj |> Option.defaultValue "")
                        )
                    then
                        yield! sources child
            }

        let violations =
            [ "src"; "app"; "tests"; "analyzers" ]
            |> Seq.collect (fun directory -> sources (Path.Combine(root, directory)))
            |> Seq.sort
            |> Seq.collect (fun path -> Doc.check (Path.GetRelativePath(root, path)) (File.ReadAllText path))
            |> Seq.toList

        violations |> List.iter (fun violation -> printfn "%s" violation)
        if List.isEmpty violations then 0 else 1
    | Command.Fetch(year, day, force) ->
        let path = Path.Combine(root, "inputs", string year, sprintf "day%02d.txt" day)

        if force || not (File.Exists path) then
            fetch baseAddress (session root) (sprintf "%d/day/%d/input" year day) path
            |> require

        printfn "%s" path
        0
    | Command.Puzzle(year, day) ->
        let text =
            request baseAddress (session root) HttpMethod.Get (sprintf "%d/day/%d" year day) None
            |> require
            |> Statement.extract
            |> require

        let path = Path.Combine(root, "puzzles", string year, sprintf "%02d.txt" day)
        atomicWrite path text
        printfn "%s" path
        0
    | Command.Submit(year, day, part, answer) ->
        let code, text = submit baseAddress (session root) year day part answer
        Console.Out.Write text
        code
    | Command.Bench(year, day) -> Benchmark.run root year day false
    | Command.Record(year, day) -> Benchmark.run root year day true
    | Command.New(year, day) ->
        scaffold root year day None
        0
    | Command.NewExploration(year, day, name) ->
        scaffold root year day (Some name)
        0

[<EntryPoint>]
let main args =
    try
        let command = Command.parse args |> require
        run (root ()) command
    with error ->
        eprintfn "%s" error.Message
        4
