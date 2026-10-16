/// Pure source-documentation checks; filesystem discovery belongs to the command edge.
module Aoc.Ops.Doc

open System
open System.Text.RegularExpressions

/// Reports path:line diagnostics for solution headers and line-start #nowarn directives.
/// The directive check is deliberately lexical: even a line inside a multiline string
/// or block comment is rejected when its trimmed text starts with #nowarn.
let check (path: string) (text: string) =
    let path = path.Replace('\\', '/')
    let lines = text.Replace("\r\n", "\n").Split('\n')
    let report line reason = sprintf "%s:%d: %s" path line reason

    let headers =
        if
            Regex.IsMatch(path, @"(?:^|/)src/Aoc/Y[0-9]{4}/Day[0-9]{2}/[^/]+\.fs$")
            && not (path.EndsWith("/Input.fs", StringComparison.Ordinal))
        then
            [ "approach"; "cost"; "trade" ]
            |> List.mapi (fun index tag ->
                let prefix = "// " + tag + ":"

                match Array.tryItem index lines with
                | Some line when
                    line.StartsWith(prefix, StringComparison.Ordinal)
                    && not (String.IsNullOrWhiteSpace(line.Substring prefix.Length))
                    ->
                    None
                | _ -> Some(report (index + 1) (sprintf "expected %s with non-empty text before module" prefix)))
            |> List.choose id
        else
            []

    let directives =
        lines
        |> Array.mapi (fun index line ->
            if Regex.IsMatch(line, @"^\s*#nowarn\b") then
                Some(report (index + 1) "#nowarn is forbidden; document suppressions in Directory.Build.props")
            else
                None)
        |> Array.choose id
        |> Array.toList

    headers @ directives
