/// Pure scaffold rendering and compile-order regeneration from relative file lists.
module Aoc.Ops.Scaffold

open System
open System.Text.RegularExpressions

/// Substitutes the puzzle date placeholders in a template.
let render year day (template: string) =
    template.Replace("{{YEAR}}", string year).Replace("{{DAY}}", sprintf "%02d" day).Replace("{{DAYN}}", string day)

let private matching pattern paths =
    paths
    |> List.choose (fun path ->
        let matched = Regex.Match(path, pattern)
        if matched.Success then Some matched else None)

/// Extracts years in ascending order from scanned Y directories or file paths.
let years paths =
    matching @"\AY([0-9]{4,})(?:/|\z)" paths
    |> List.map (fun matched -> int matched.Groups[1].Value)
    |> List.distinct
    |> List.sort

let private days year paths =
    matching (sprintf @"\AY%d/Day([0-9]{2})/Primary\.fs\z" year) paths
    |> List.map (fun matched -> int matched.Groups[1].Value)
    |> List.distinct
    |> List.sort

let private explorations year day paths =
    matching (sprintf @"\AY%d/Day%02d/([A-Z][A-Za-z0-9]*)\.fs\z" year day) paths
    |> List.choose (fun matched ->
        match matched.Groups[1].Value with
        | "Input"
        | "Primary" -> None
        | name -> Some name)
    |> List.distinct
    |> List.sort

/// Rejects exploration names that would share a case-insensitive CLI key.
let uniqueExploration year day (name: string) paths =
    if
        explorations year day paths
        |> List.exists (fun existing -> existing.ToLowerInvariant() = name.ToLowerInvariant())
    then
        Error "exploration CLI name already exists"
    else
        Ok()

/// Substitutes an exploration's PascalCase module name and lowercase CLI variant.
let exploration year day (name: string) template =
    render year day template
    |> fun text -> text.Replace("{{EXPLORATION}}", name).Replace("{{VARIANT}}", name.ToLowerInvariant())

/// Renders primary and exploration entries, sorted by day then module name.
let yearRegistry (template: string) year paths =
    let entries =
        days year paths
        |> List.collect (fun day ->
            sprintf "        { Year = %d; Day = %d; Variant = Primary }, Day%02d.Primary.solve" year day day
            :: (explorations year day paths
                |> List.map (fun name ->
                    sprintf
                        "        { Year = %d; Day = %d; Variant = Exploration \"%s\" }, Day%02d.%s.solve"
                        year
                        day
                        (name.ToLowerInvariant())
                        day
                        name)))

    let body =
        match entries with
        | [] -> " Map.empty"
        | _ -> "\n    [\n" + String.concat "\n" entries + "\n    ]\n    |> Map.ofList"

    render year 0 template |> fun text -> text.Replace("{{ENTRIES}}", body)

/// Renders the root registry from all scanned years, sorted numerically.
let rootRegistry (template: string) paths =
    let entries =
        years paths |> List.map (sprintf "Y%d.Registry.solvers") |> String.concat "; "

    template.Replace("{{YEARS}}", entries)

/// Produces the library's Y compile block in F# dependency order.
let libraryEntries paths =
    [
        for year in years paths do
            for day in days year paths do
                let prefix = sprintf "Y%d/Day%02d/" year day
                yield prefix + "Input.fs"
                yield prefix + "Primary.fs"

                yield!
                    paths
                    |> List.filter (fun path ->
                        path.StartsWith(prefix, StringComparison.Ordinal)
                        && path.EndsWith(".fs", StringComparison.Ordinal)
                        && path <> prefix + "Input.fs"
                        && path <> prefix + "Primary.fs")
                    |> List.sort

            yield sprintf "Y%d/Registry.fs" year
    ]

/// Produces day-test compile entries in ascending year/day order.
let testEntries paths =
    matching @"\AY([0-9]{4,})/Day([0-9]{2})Tests\.fs\z" paths
    |> List.sortBy (fun matched -> int matched.Groups[1].Value, int matched.Groups[2].Value)
    |> List.map (fun matched -> matched.Value)

/// Replaces Y Compile lines only, keeping all comments and unrelated XML verbatim.
/// Library entries precede Registry.fs; test entries follow the last non-Y Compile.
let project (library: bool) (entries: string list) (text: string) : Result<string, string> =
    let newline = if text.Contains "\r\n" then "\r\n" else "\n"
    let lines = text.Split(newline) |> Array.toList
    let compile = Regex(@"^(\s*)<Compile\s+Include=""([^""]+)""\s*/>\s*$")

    match
        lines
        |> List.tryFind (fun line -> line.Contains "<Compile" && not (compile.IsMatch line))
    with
    | Some line ->
        let name =
            if library then
                "src/Aoc/Aoc.fsproj"
            else
                "tests/Aoc.Tests/Aoc.Tests.fsproj"

        Error(sprintf "unsupported Compile line in %s: %s" name (line.Trim()))
    | None ->
        let clean =
            lines
            |> List.filter (fun line ->
                let m = compile.Match line in not (m.Success && m.Groups[2].Value.StartsWith('Y')))

        let anchor =
            clean
            |> List.mapi (fun index line -> index, compile.Match line)
            |> List.filter (fun (_, m) -> m.Success && (not library || m.Groups[2].Value = "Registry.fs"))
            |> (if library then List.tryHead else List.tryLast)

        match anchor with
        | None -> Error "project is missing its Compile anchor"
        | Some(index, matched) ->
            let at = if library then index else index + 1

            let generated =
                entries
                |> List.map (fun path -> sprintf "%s<Compile Include=\"%s\" />" matched.Groups[1].Value path)

            Ok(String.concat newline (List.take at clean @ generated @ List.skip at clean))
