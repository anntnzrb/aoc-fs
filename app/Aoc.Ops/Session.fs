/// Parses local session configuration without IO or disclosure in errors.
module Aoc.Ops.Session

open System
open System.Text.RegularExpressions

/// Returns the first non-empty cookie-safe value, or a fixed error without its contents.
let parse (text: string) : Result<string, string> =
    text.Split('\n')
    |> Array.tryPick (fun line ->
        let line = line.Trim()

        if line = "" || line.StartsWith('#') then
            None
        else
            match line.Split('=', 2) with
            | [| key; value |] when key.Trim() = "AOC_SESSION" ->
                let value = value.Trim()

                let unquoted =
                    if
                        value.Length >= 2
                        && ((value.StartsWith('"') && value.EndsWith('"'))
                            || (value.StartsWith('\'') && value.EndsWith('\'')))
                    then
                        value.Substring(1, value.Length - 2)
                    else
                        value

                if String.IsNullOrWhiteSpace unquoted then
                    None
                else
                    Some unquoted
            | _ -> None)
    |> function
        | Some token when Regex.IsMatch(token, @"\A[\x21\x23-\x2B\x2D-\x3A\x3C-\x5B\x5D-\x7E]+\z") -> Ok token
        | Some _ -> Error "AOC_SESSION in .env is not a valid cookie value"
        | None -> Error "AOC_SESSION not set in .env"
