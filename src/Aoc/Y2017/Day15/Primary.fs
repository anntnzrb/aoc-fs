// approach: independently advance each generator to its next acceptable value, then compare low 16 bits
// cost: O(g) time for g total generated values across both parts, O(1) memory, one input pass
// trade: capture-free tail recursion keeps constant-space loops without per-value closures
module Aoc.Y2017.Day15.Primary

open Aoc.Solution
open Aoc.Y2017.Day15.Input

[<TailCall>]
let rec private generate factor multiple previous =
    let value = previous * factor % modulus

    if value % multiple = 0L then
        value
    else
        generate factor multiple value

[<TailCall>]
let rec private countMatches remaining multipleA multipleB a b count =
    if remaining = 0 then
        count
    else
        let nextA = generate 16807L multipleA a
        let nextB = generate 48271L multipleB b
        let matched = if (nextA &&& 65535L) = (nextB &&& 65535L) then 1 else 0
        countMatches (remaining - 1) multipleA multipleB nextA nextB (count + matched)

let private matches pairs multipleA multipleB (Seed startA) (Seed startB) =
    countMatches pairs multipleA multipleB startA startB 0

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun generators ->
            Answers.both
                (matches 40000000 1L 1L generators.A generators.B |> string)
                (matches 5000000 4L 8L generators.A generators.B |> string))
