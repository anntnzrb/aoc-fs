/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day24.Input

/// Two non-negative pin counts; repeated components remain separate pieces.
type Component = Component of left: int * right: int

let private parseComponent (line: string) =
    match line.Split('/') with
    | [| left; right |] ->
        match System.Int32.TryParse left, System.Int32.TryParse right with
        | (true, a), (true, b) when a >= 0 && b >= 0 -> Ok(Component(a, b))
        | _ -> Error "invalid port pin count"
    | _ -> Error "expected two ports separated by /"

/// Parses the input into the puzzle domain model.
let parse (input: string) : Result<Component list, string> =
    match input.Trim() with
    | text when String.length text = 0 -> Error "empty component inventory"
    | text -> text.Replace("\r\n", "\n").Split('\n') |> Aoc.Parse.traverse parseComponent
