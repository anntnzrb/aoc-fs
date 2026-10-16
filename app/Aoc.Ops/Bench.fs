/// Pure benchmark decisions, statistics and canonical CSV serialization.
module Aoc.Ops.Bench

open System
open System.Globalization

/// Observations retain their range; an empty sample has no summary.
type Summary =
    {
        Median: decimal
        Minimum: decimal
        Maximum: decimal
    }

/// Summarizes a nonempty sample, averaging the middle pair for even counts.
let summarize values =
    let sorted = List.sort values
    let length = List.length sorted

    match List.tryHead sorted, List.tryLast sorted with
    | Some minimum, Some maximum ->
        let median =
            if length % 2 = 1 then
                List.tryItem (length / 2) sorted
            else
                match List.tryItem (length / 2 - 1) sorted, List.tryItem (length / 2) sorted with
                | Some left, Some right -> Some(left / 2M + right / 2M)
                | _ -> None

        median
        |> Option.map (fun value ->
            {
                Median = value
                Minimum = minimum
                Maximum = maximum
            })
    | _ -> None

/// Subtracts each paired empty baseline before summarizing; negative noise is retained.
let work pairs =
    pairs |> List.map (fun (full, empty) -> full - empty) |> summarize

/// Refuses failed children and disagreement without exposing answers or child diagnostics.
let agreement name expected (exitCode, output) =
    if exitCode <> 0 then
        Error(sprintf "%s process failed (exit %d); diagnostics withheld" name exitCode)
    elif output <> expected then
        Error(sprintf "%s disagrees with primary; answers withheld" name)
    else
        Ok()

/// Canonical records require successful git checks, the main branch and no tracked changes.
let canonical (branchCode, branch: string) (statusCode, status: string) =
    if branchCode <> 0 || statusCode <> 0 then
        Error "record requires successful git checks"
    elif branch.Trim() <> "main" then
        Error "record requires main"
    elif status.Trim() <> "" then
        Error "record requires a clean tracked tree"
    else
        Ok()

/// Quotes CSV text, including embedded quotes, delimiters and newlines.
let quote (text: string) =
    "\"" + text.Replace("\"", "\"\"") + "\""

/// Formats a metric without locale-dependent decimal separators; unavailable fields stay empty.
let number value =
    value
    |> Option.map (fun (n: decimal) -> n.ToString(CultureInfo.InvariantCulture))
    |> Option.defaultValue ""

/// Canonical column order, shared by every year and host.
let csvHeader =
    "date,commit,host,os,cpu,compiler,std,year,day,solution,runs,work_med,work_min,work_max,instr_med,wall_min_us,wall_med_us,cpu_med_us,rss_kib"

/// Host and revision metadata captured once per benchmark invocation.
type Metadata =
    {
        Date: string
        Commit: string
        Host: string
        Os: string
        Cpu: string
        Compiler: string
        Standard: string
    }

/// One variant's whole-process measurements; CPU includes user and system time.
type Measurement =
    {
        Solution: string
        Runs: int
        Work: Summary option
        Instructions: Summary option
        Wall: Summary
        Cpu: Summary
        Rss: decimal
    }

/// Serializes one canonical row with every text field quoted and every metric numeric.
let csvRow metadata year day measurement =
    let middle summary =
        summary |> Option.map (fun s -> s.Median) |> number

    [
        quote metadata.Date
        quote metadata.Commit
        quote metadata.Host
        quote metadata.Os
        quote metadata.Cpu
        quote metadata.Compiler
        quote metadata.Standard
        string year
        string day
        quote measurement.Solution
        string measurement.Runs
        middle measurement.Work
        measurement.Work |> Option.map (fun s -> s.Minimum) |> number
        measurement.Work |> Option.map (fun s -> s.Maximum) |> number
        middle measurement.Instructions
        number (Some measurement.Wall.Minimum)
        number (Some measurement.Wall.Median)
        number (Some measurement.Cpu.Median)
        number (Some measurement.Rss)
    ]
    |> String.concat ","

/// Parses perf's counters, including hybrid PMU aliases; inactive PMUs contribute no count.
let instructions (text: string) =
    let counters =
        text.Split('\n')
        |> Array.choose (fun line ->
            match line.Split(',') |> Array.toList with
            | value :: _unit :: event :: _ when
                event.Trim() = "instructions:u"
                || event.Trim().EndsWith("/instructions/u", StringComparison.Ordinal)
                ->
                match Decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture) with
                | true, count when count >= 0M -> Some count
                | _ -> None
            | _ -> None)

    if Array.isEmpty counters then
        None
    else
        Some(Array.sum counters)

/// Parses GNU time's user seconds, system seconds and maximum RSS in KiB.
let resources (text: string) =
    let parse (value: string) =
        match Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture) with
        | true, n when n >= 0M -> Some n
        | _ -> None

    match text.Trim().Split(',') |> Array.toList |> List.map parse with
    | [ Some user; Some system; Some rss ] -> Ok((user + system) * 1000000M, rss)
    | _ -> Error "invalid GNU time resource observation"
