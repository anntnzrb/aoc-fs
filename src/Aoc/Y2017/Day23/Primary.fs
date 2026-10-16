// approach: interpret part 1; replace a verified nested factor-search loop with trial division for part 2
// cost: O(n+t+s+k sqrt(b)) time: n instructions, t/s literal steps, k factor loops bounded by b; O(n) memory, one parse pass
// trade: local machine state stays literal except for a structurally checked loop; no input constants are embedded
module Aoc.Y2017.Day23.Primary

open Aoc.Solution
open Aoc.Y2017.Day23.Input

let private index (Register name) = int name - int 'a'

let private factorLoops instructions =
    instructions
    |> Seq.unfold (function
        | [] -> None
        | _ :: tail as remaining -> Some(remaining, tail))
    |> Seq.indexed
    |> Seq.fold
        (fun result (position, remaining) ->
            result
            |> Result.bind (fun loops ->
                match remaining with
                | Set(_, Literal 2L) :: Set(_, Literal 2L) :: _ as remaining ->
                    // Only this exact, alias-free loop tests all d*e pairs in [2,b); reject other candidate shapes.
                    match remaining |> List.truncate 15 with
                    | [ Set(d, Literal 2L)
                        Set(e, Literal 2L)
                        Set(g, Read d1)
                        Mul(g1, Read e1)
                        Sub(g2, Read b)
                        Jump(Read g3, Literal 2L)
                        Set(f, Literal 0L)
                        Sub(e2, Literal -1L)
                        Set(g4, Read e3)
                        Sub(g5, Read b1)
                        Jump(Read g6, Literal -8L)
                        Sub(d2, Literal -1L)
                        Set(g7, Read d3)
                        Sub(g8, Read b2)
                        Jump(Read g9, Literal -13L) ] when
                        [ d1; d2; d3 ] |> List.forall ((=) d)
                        && [ e1; e2; e3 ] |> List.forall ((=) e)
                        && [ g1; g2; g3; g4; g5; g6; g7; g8; g9 ] |> List.forall ((=) g)
                        && b1 = b
                        && b2 = b
                        && Set.count (Set.ofList [ d; e; g; b; f ]) = 5
                        ->
                        Ok(Map.add position (index d, index e, index g, index b, index f) loops)
                    | _ -> Error "unsupported nested factor-loop structure"
                | _ -> Ok loops))
        (Ok Map.empty)

let private composite number =
    Seq.unfold
        (fun divisor ->
            if divisor <= number / divisor then
                Some(divisor, divisor + 1L)
            else
                None)
        2L
    |> Seq.exists (fun divisor -> number % divisor = 0L)

let private execute initialA loops (program: Instruction array) =
    let registers = Array.zeroCreate<int64> 8
    registers[0] <- initialA

    let value =
        function
        | Literal number -> number
        | Read register -> registers[index register]

    let mutable position = 0L
    let mutable multiplications = 0L
    let mutable error = None

    while position >= 0L && position < int64 program.Length && Option.isNone error do
        match Map.tryFind (int position) loops with
        | Some(d, e, g, b, f) ->
            let number = registers[b]

            if number < 3L then
                error <- Some "factor-loop bound must exceed two for termination"
            elif number - 1L > System.Int64.MaxValue / (number - 1L) then
                error <- Some "factor-loop products exceed the integer range"
            else
                if composite number then
                    registers[f] <- 0L

                registers[d] <- number
                registers[e] <- number
                registers[g] <- 0L
                position <- position + 15L
        | None ->
            let mutable offset = 1L

            match program[int position] with
            | Set(register, source) -> registers[index register] <- value source
            | Sub(register, source) -> registers[index register] <- registers[index register] - value source
            | Mul(register, source) ->
                registers[index register] <- registers[index register] * value source
                multiplications <- multiplications + 1L
            | Jump(condition, distance) ->
                if value condition <> 0L then
                    offset <- value distance

            position <- position + offset

    match error with
    | Some message -> Error message
    | None -> Ok(multiplications, registers[7])

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.bind (fun instructions ->
            let program = List.toArray instructions

            execute 0L Map.empty program
            |> Result.bind (fun (part1, _) ->
                factorLoops instructions
                |> Result.bind (fun loops -> execute 1L loops program)
                |> Result.map (fun (_, part2) -> Answers.both (string part1) (string part2))))
