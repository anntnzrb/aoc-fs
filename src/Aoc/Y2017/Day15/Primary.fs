// approach: independently advance each generator to its next acceptable value, then compare low 16 bits
// cost: O(g) time for g total generated values across both parts, O(1) memory, one input pass
// trade: local mutable state keeps the recurrence direct without storing generated values
module Aoc.Y2017.Day15.Primary

open Aoc.Solution
open Aoc.Y2017.Day15.Input

let private matches pairs multipleA multipleB (Seed startA) (Seed startB) =
    let next factor multiple previous =
        let mutable value = previous * factor % 2147483647L

        while value % multiple <> 0L do
            value <- value * factor % 2147483647L

        value

    let mutable a = startA
    let mutable b = startB
    let mutable count = 0

    for _ in 1..pairs do
        a <- next 16807L multipleA a
        b <- next 48271L multipleB b

        if (a &&& 65535L) = (b &&& 65535L) then
            count <- count + 1

    count

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun generators ->
            Answers.both
                (matches 40000000 1L 1L generators.A generators.B |> string)
                (matches 5000000 4L 8L generators.A generators.B |> string))
