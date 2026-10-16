module Aoc.Tests.Y2017.Day10Tests

open Expecto
open Aoc.Y2017.Day10

[<Tests>]
let tests =
    testList
        "Y2017.Day10"
        [
            testCase "non-numeric text is not a length list" (fun () ->
                // Letters cannot represent any comma-separated length in 0..256.
                Expect.isError (Primary.solve "not-a-length-list") "require numeric lengths")
        ]
