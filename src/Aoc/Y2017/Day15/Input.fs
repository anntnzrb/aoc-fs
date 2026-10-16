/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day15.Input

open FParsec

/// A generator's starting value, below the modulus.
type Seed = Seed of int64

/// Starting values for the two generators.
type Generators = { A: Seed; B: Seed }

/// The modulus every generator step reduces by.
let modulus = 2147483647L

/// Parses the two labelled starting values in generator order.
let parse (input: string) : Result<Generators, string> =
    let seed =
        pint64
        >>= fun value ->
            if value >= 0L && value < modulus then
                preturn (Seed value)
            else
                fail "starting value must be between zero and the modulus"

    let generators =
        pstring "Generator A starts with " >>. seed .>> newline
        .>>. (pstring "Generator B starts with " >>. seed)
        .>> optional newline
        .>> eof
        |>> fun (a, b) -> { A = a; B = b }

    Aoc.Parse.run generators input
