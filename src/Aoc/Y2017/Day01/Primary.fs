// approach: pair each digit with the digit a fixed step ahead on the circle (1 for part 1, half the length for part 2) and sum the digits that equal their partner
// cost: O(n) time, O(n) memory, one pass over the parsed digits per part
// trade: an indexed array makes the circular wrap a modulo instead of an appended copy
module Aoc.Y2017.Day01.Primary

open Aoc.Solution
open Aoc.Y2017.Day01.Input

let private matchSum step (digits: int array) =
    let n = digits.Length

    digits
    |> Array.mapi (fun i d -> d, digits[(i + step) % n])
    |> Array.sumBy (fun (d, partner) -> if d = partner then d else 0)

let solve: Solver =
    fun input ->
        input
        |> Input.parse
        |> Result.map (fun captcha ->
            let digits = captcha.Digits |> List.toArray
            Answers.both (matchSum 1 digits |> string) (matchSum (digits.Length / 2) digits |> string))
