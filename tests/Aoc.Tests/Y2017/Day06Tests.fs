module Aoc.Tests.Y2017.Day06Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day06

[<Tests>]
let tests =
    testList
        "Y2017.Day06"
        [
            testCase "redistribution preserves totals above Int32.MaxValue" (fun () ->
                // Independent bigint two-bank reference: if a >= b, next is
                // (a/2, b+(a+1)/2); otherwise (a+(b+1)/2, b/2).
                // Starting with both Int32.MaxValue, the first repeat is at
                // cycle 32, revisiting cycle 30 (period 2).
                Expect.equal
                    (Primary.solve "2147483647 2147483647" |> Result.map Answers.render)
                    (Ok "32\n2\n")
                    "large block totals retain their exact cycle")
        ]
