module Aoc.Tests.Y2017.Day24Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day24

[<Tests>]
let tests =
    testList
        "Y2017.Day24"
        [
            testCase "bridge strength exceeds int32" (fun () ->
                // With M = Int32.MaxValue, 0/M then M/M has strength 3M and is the longest bridge.
                Expect.equal
                    (Primary.solve "0/2147483647\n2147483647/2147483647\n"
                     |> Result.map Answers.render)
                    (Ok "6442450941\n6442450941\n")
                    "both objectives retain the full strength")
        ]
