/// Dance moves for the sixteen programs named a through p.
module Aoc.Y2017.Day16.Input

/// A rotation, a swap of positions, or a swap of program names.
type Move =
    | Spin of int
    | Exchange of int * int
    | Partner of char * char

/// Parses a non-empty comma-separated dance, validating positions and names.
let parse (input: string) : Result<Move list, string> =
    let number (text: string) =
        match System.Int32.TryParse text with
        | true, n when n >= 0 -> Some n
        | _ -> None

    let move (text: string) =
        match text |> Seq.toList with
        | 's' :: rest -> rest |> List.toArray |> System.String |> number |> Option.map Spin
        | 'x' :: rest ->
            match (rest |> List.toArray |> System.String).Split('/') with
            | [| a; b |] ->
                match number a, number b with
                | Some a, Some b when a < 16 && b < 16 -> Some(Exchange(a, b))
                | _ -> None
            | _ -> None
        | [ 'p'; a; '/'; b ] when a >= 'a' && a <= 'p' && b >= 'a' && b <= 'p' -> Some(Partner(a, b))
        | _ -> None

    input.Trim().Split(',')
    |> Array.fold
        (fun result text ->
            result
            |> Result.bind (fun moves ->
                match move text with
                | Some parsed -> Ok(parsed :: moves)
                | None -> Error $"invalid dance move: %A{text}"))
        (Ok [])
    |> Result.map List.rev
