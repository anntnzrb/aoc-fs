/// Domain model and parser for the tower's program descriptions.
module Aoc.Y2017.Day07.Input

open FParsec
open FsToolkit.ErrorHandling

/// A program's own weight and the programs directly supported by its disc.
type Program =
    {
        Name: string
        Weight: int
        Children: string list
    }

let private tower =
    let name = many1Satisfy (fun c -> c >= 'a' && c <= 'z')

    let weight =
        many1Satisfy System.Char.IsAsciiDigit
        >>= fun digits ->
            match System.Int32.TryParse digits with
            | true, value -> preturn value
            | _ -> fail "invalid program weight"

    let program =
        pipe3
            (name .>> pstring " (")
            (weight .>> pchar ')')
            (opt (pstring " -> " >>. sepBy1 name (pstring ", ")))
            (fun name weight children ->
                {
                    Name = name
                    Weight = weight
                    Children = children |> Option.defaultValue []
                })

    sepBy1 program newline .>> eof

/// Parses non-empty descriptions with unique program names, rejecting malformed lines.
let parse (input: string) : Result<Program list, string> =
    match input.Trim() with
    | text when String.length text = 0 -> Result.Error "empty tower"
    | text ->
        let text = text.Replace("\r\n", "\n")

        result {
            do! Result.requireFalse "invalid line separator" (text.Contains '\r')
            let! programs = Aoc.Parse.run tower text

            if programs |> List.map _.Name |> Set.ofList |> Set.count = programs.Length then
                return programs
            else
                return! Result.Error "duplicate program name"
        }
