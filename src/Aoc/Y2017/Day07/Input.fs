/// Domain model and parser for the tower's program descriptions.
module Aoc.Y2017.Day07.Input

/// A program's own weight and the programs directly supported by its disc.
type Program =
    {
        Name: string
        Weight: int
        Children: string list
    }

let private validName (name: string) =
    name <> "" && name |> Seq.forall (fun c -> c >= 'a' && c <= 'z')

let private parseLine (line: string) =
    let parseProgram description children =
        match description |> List.ofArray with
        | [ name; weight ] when validName name && children |> List.forall validName ->
            match weight.StartsWith('('), weight.EndsWith(')') with
            | true, true ->
                match System.Int32.TryParse(weight.Trim('(', ')')) with
                | true, value when value >= 0 ->
                    Ok
                        {
                            Name = name
                            Weight = value
                            Children = children
                        }
                | _ -> Error "invalid program weight"
            | _ -> Error "expected a parenthesized weight"
        | _ -> Error "invalid program description"

    match line.Split(" -> ", System.StringSplitOptions.None) |> Array.toList with
    | [ description ] -> parseProgram (description.Split(' ')) []
    | [ description; children ] ->
        parseProgram (description.Split(' ')) (children.Split(", ", System.StringSplitOptions.None) |> Array.toList)
    | _ -> Error "invalid child list"

/// Parses non-empty descriptions with unique program names, rejecting malformed lines.
let parse (input: string) : Result<Program list, string> =
    match input.Trim() with
    | "" -> Error "empty tower"
    | text ->
        text.Replace("\r\n", "\n").Split('\n')
        |> Array.fold
            (fun acc line ->
                acc
                |> Result.bind (fun programs ->
                    parseLine line
                    |> Result.bind (fun program ->
                        match programs |> List.exists (fun previous -> previous.Name = program.Name) with
                        | true -> Error "duplicate program name"
                        | false -> Ok(program :: programs))))
            (Ok [])
        |> Result.map List.rev
