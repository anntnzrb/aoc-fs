// approach: interpret part 1; replace a verified nested factor-search loop with trial division for part 2
// cost: O(n log(l+2)+t+(s+k) log(l+2)+k sqrt(b)) time: n instructions, l recognized loops, t/s literal steps, k accelerated loops bounded by b
// cost: O(n+l) memory, one parse pass and six buffered passes
// trade: local machine state stays literal except for a structurally checked loop; no input constants are embedded
module Aoc.Y2017.Day23.Primary

open Aoc.Solution
open Aoc.Y2017.Day23.Input

let private index =
    function
    | A -> 0
    | B -> 1
    | C -> 2
    | D -> 3
    | E -> 4
    | F -> 5
    | G -> 6
    | H -> 7

type private FactorLoop =
    {
        LeftFactor: int
        RightFactor: int
        Product: int
        Bound: int
        Flag: int
    }

let private factorLoops instructions =
    instructions
    |> List.windowed 15
    |> List.indexed
    |> List.fold
        (fun result (position, remaining) ->
            result
            |> Result.bind (fun loops ->
                match remaining with
                | Set(_, Literal 2L) :: Set(_, Literal 2L) :: _ as remaining ->
                    // Only this exact, alias-free loop tests all d*e pairs in [2,b); reject other candidate shapes.
                    match remaining with
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
                        let loop =
                            {
                                LeftFactor = index d
                                RightFactor = index e
                                Product = index g
                                Bound = index b
                                Flag = index f
                            }

                        Ok(Map.add position loop loops)
                    | _ -> Error "unsupported nested factor-loop structure"
                | _ -> Ok loops))
        (Ok Map.empty)
    |> Result.bind (fun loops ->
        // Full windows omit the tail, where a truncated candidate must still be rejected.
        let truncatedCandidate =
            instructions
            |> List.skip (max 0 (List.length instructions - 14))
            |> List.pairwise
            |> List.exists (function
                | Set(_, Literal 2L), Set(_, Literal 2L) -> true
                | _ -> false)

        if truncatedCandidate then
            Error "unsupported nested factor-loop structure"
        else
            Ok loops)

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
    registers[index A] <- initialA

    let value =
        function
        | Literal number -> number
        | Read register -> registers[index register]

    let mutable position = 0L
    let mutable multiplications = 0L
    let mutable error = None

    let update operation register source =
        let target = index register
        let result = operation (bigint registers[target]) (bigint (value source))

        if result < bigint System.Int64.MinValue || result > bigint System.Int64.MaxValue then
            error <- Some "register overflow"
        else
            registers[target] <- int64 result

    while position >= 0L && position < int64 program.Length && Option.isNone error do
        match Map.tryFind (int position) loops with
        | Some loop ->
            let number = registers[loop.Bound]
            // The literal loop multiplies factors up to number-1; accelerate only when every product fits.
            let squareFits =
                number >= 3L && number - 1L <= System.Int64.MaxValue / (number - 1L)

            if number < 3L then
                error <- Some "factor-loop bound must exceed two for termination"
            elif not squareFits then
                error <- Some "factor-loop products exceed the integer range"
            else
                if composite number then
                    registers[loop.Flag] <- 0L

                registers[loop.LeftFactor] <- number
                registers[loop.RightFactor] <- number
                registers[loop.Product] <- 0L
                position <- position + 15L
        | None ->
            let offset =
                match program[int position] with
                | Set(register, source) ->
                    registers[index register] <- value source
                    1L
                | Sub(register, source) ->
                    update (-) register source
                    1L
                | Mul(register, source) ->
                    update (*) register source
                    multiplications <- multiplications + 1L
                    1L
                | Jump(condition, distance) -> if value condition <> 0L then value distance else 1L

            position <- position + offset

    match error with
    | Some message -> Error message
    | None -> Ok(multiplications, registers[index H])

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
