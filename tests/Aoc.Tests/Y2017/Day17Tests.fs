module Aoc.Tests.Y2017.Day17Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day17

[<Tests>]
let tests =
    testList
        "Y2017.Day17"
        [
            testProperty
                "parser preserves nonnegative step counts"
                (Prop.forAll (Arb.fromGen (Gen.choose (0, System.Int32.MaxValue))) (fun steps ->
                    match Input.parse ($" \t{steps}\r\n") with
                    | Ok(Input.Steps parsed) -> parsed = steps
                    | Error _ -> false))
        ]
