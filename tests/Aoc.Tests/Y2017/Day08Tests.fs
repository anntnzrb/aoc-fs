module Aoc.Tests.Y2017.Day08Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day08

[<Tests>]
let tests =
    testList
        "Y2017.Day08"
        [
            testCase "final maximum can be negative" (fun () ->
                // The only register changes from zero to -5; its all-time maximum remains zero.
                Expect.equal
                    (Primary.solve "a dec 5 if a == 0" |> Result.map Answers.render)
                    (Ok "-5\n0\n")
                    "the final maximum has no zero floor")
            testCase "condition-only registers retain zero" (fun () ->
                // a becomes -5, but b is only read and remains zero, so both maxima are zero.
                Expect.equal
                    (Primary.solve "a dec 5 if b == 0" |> Result.map Answers.render)
                    (Ok "0\n0\n")
                    "condition-only registers count in the final maximum")
        ]
