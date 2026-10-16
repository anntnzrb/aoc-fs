/// Domain model and parser for the binary tape machine blueprint.
module Aoc.Y2017.Day25.Input

open FParsec

type Bit =
    | Zero
    | One

type Direction =
    | Left
    | Right

type State = State of string

type Action =
    {
        Write: Bit
        Move: Direction
        Next: State
    }

type Rules = { Zero: Action; One: Action }

type Blueprint =
    {
        Start: State
        Steps: int
        States: Map<State, Rules>
    }

/// Parses complete state rules and rejects missing state definitions.
let parse (input: string) : Result<Blueprint, string> =
    let token text = pstring text .>> spaces
    let state = many1Satisfy System.Char.IsAsciiLetterUpper |>> State
    let named prefix suffix = token prefix >>. state .>> token suffix
    let bit = (pchar '0' >>% Zero) <|> (pchar '1' >>% One)
    let direction = (token "left" >>% Left) <|> (token "right" >>% Right)

    let action value =
        token $"If the current value is {value}:"
        >>. pipe3
            (token "- Write the value" >>. bit .>> token ".")
            (token "- Move one slot to the" >>. direction .>> token ".")
            (named "- Continue with state" ".")
            (fun write move next ->
                {
                    Write = write
                    Move = move
                    Next = next
                })

    let rules =
        pipe3 (named "In state" ":") (action 0) (action 1) (fun name zero one -> name, { Zero = zero; One = one })

    let blueprint =
        spaces
        >>. pipe3
            (named "Begin in state" ".")
            (token "Perform a diagnostic checksum after" >>. pint32
             .>> spaces
             .>> token "steps.")
            (many1 rules)
            (fun start steps states -> start, steps, states)
        .>> eof

    match run blueprint input with
    | Failure(message, _, _) -> Result.Error message
    | Success((start, steps, definitions), _, _) ->
        let states = Map.ofList definitions

        let referenced =
            start
            :: [
                for _, rules in definitions do
                    yield rules.Zero.Next
                    yield rules.One.Next
            ]

        if steps < 0 || states.Count <> definitions.Length then
            Result.Error "expected non-negative steps and unique state definitions"
        elif referenced |> List.exists (fun name -> not (Map.containsKey name states)) then
            Result.Error "undefined machine state"
        else
            Result.Ok
                {
                    Start = start
                    Steps = steps
                    States = states
                }
