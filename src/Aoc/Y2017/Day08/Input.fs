/// Domain model and parser for conditional register instructions.
module Aoc.Y2017.Day08.Input

/// The change requested by an instruction.
type Operation =
    | Increase
    | Decrease

/// The comparison guarding a register change.
type Comparison =
    | Greater
    | Less
    | GreaterOrEqual
    | LessOrEqual
    | Equal
    | NotEqual

/// A register change guarded by a comparison with another register.
type Instruction =
    {
        Target: string
        Operation: Operation
        Amount: int64
        Source: string
        Comparison: Comparison
        Threshold: int64
    }

let private instruction (line: string) =
    let register name =
        name |> String.forall System.Char.IsAsciiLetter

    let operation =
        function
        | "inc" -> Some Increase
        | "dec" -> Some Decrease
        | _ -> None

    let comparison =
        function
        | ">" -> Some Greater
        | "<" -> Some Less
        | ">=" -> Some GreaterOrEqual
        | "<=" -> Some LessOrEqual
        | "==" -> Some Equal
        | "!=" -> Some NotEqual
        | _ -> None

    match line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries) with
    | [| target; op; amount; "if"; source; cmp; threshold |] when register target && register source ->
        match operation op, System.Int64.TryParse amount, comparison cmp, System.Int64.TryParse threshold with
        | Some op, (true, amount), Some cmp, (true, threshold) ->
            Ok
                {
                    Target = target
                    Operation = op
                    Amount = amount
                    Source = source
                    Comparison = cmp
                    Threshold = threshold
                }
        | _ -> Error $"invalid instruction: {line}"
    | _ -> Error $"invalid instruction: {line}"

/// Parses a non-empty sequence of instructions in execution order.
let parse (input: string) : Result<Instruction list, string> =
    match input.Trim() with
    | "" -> Error "empty register program"
    | text ->
        text.Replace("\r\n", "\n").Split('\n')
        |> Array.fold
            (fun acc line ->
                acc
                |> Result.bind (fun instructions -> instruction line |> Result.map (fun next -> next :: instructions)))
            (Ok [])
        |> Result.map List.rev
