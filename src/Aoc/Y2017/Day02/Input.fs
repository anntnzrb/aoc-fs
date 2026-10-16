/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day02.Input

/// A nonempty row of spreadsheet values.
type Row = Row of first: bigint * rest: bigint list

/// The spreadsheet's rows, in input order.
type Spreadsheet =
    {
        /// Each row contains at least one integer.
        Rows: Row list
    }

let private row (line: string) =
    line.Split([| ' '; '\t' |], System.StringSplitOptions.RemoveEmptyEntries)
    |> Aoc.Parse.traverse (fun token ->
        match
            bigint.TryParse(
                token,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture
            )
        with
        | true, value -> Ok value
        | false, _ -> Error $"not an integer: %A{token}")
    |> Result.bind (function
        | first :: rest -> Ok(Row(first, rest))
        | [] -> Error "empty spreadsheet row")

/// Parses space- or tab-separated integers; rejects empty rows and invalid tokens.
let parse (input: string) : Result<Spreadsheet, string> =
    match input.Trim() with
    | "" -> Error "empty spreadsheet"
    | text ->
        text.Replace("\r\n", "\n").Split('\n')
        |> Aoc.Parse.traverse row
        |> Result.map (fun rows -> { Rows = rows })
