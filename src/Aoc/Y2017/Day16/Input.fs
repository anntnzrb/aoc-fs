/// Dance moves for the sixteen programs named a through p.
module Aoc.Y2017.Day16.Input

open FParsec

/// A rotation, a swap of positions, or a swap of program names.
type Move =
    | Spin of int
    | Exchange of int * int
    | Partner of char * char

/// Parses a non-empty comma-separated dance, validating positions and names.
let parse (input: string) : Result<Move list, string> =
    let number =
        pint32
        >>= fun n ->
            if n >= 0 then
                preturn n
            else
                fail "expected a nonnegative number"

    let position =
        number
        >>= fun n ->
            if n < 16 then
                preturn n
            else
                fail "expected a position in 0..15"

    let name = satisfy (fun c -> c >= 'a' && c <= 'p')

    let move =
        choice
            [
                pchar 's' >>. number |>> Spin
                pchar 'x'
                >>. pipe2 (position .>> pchar '/') position (fun a b -> Exchange(a, b))
                pchar 'p' >>. pipe2 (name .>> pchar '/') name (fun a b -> Partner(a, b))
            ]

    input.Trim() |> Aoc.Parse.run (sepBy1 move (pchar ',') .>> eof)
