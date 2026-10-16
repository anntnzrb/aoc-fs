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
                let digits = weight.Substring(1, weight.Length - 2)

                match digits <> "" && digits |> Seq.forall System.Char.IsAsciiDigit, System.Int32.TryParse digits with
                | true, (true, value) ->
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
                |> Result.bind (fun (names, programs) ->
                    parseLine line
                    |> Result.bind (fun program ->
                        match Set.contains program.Name names with
                        | true -> Error "duplicate program name"
                        | false -> Ok(Set.add program.Name names, program :: programs))))
            (Ok(Set.empty, []))
        |> Result.map (snd >> List.rev)
