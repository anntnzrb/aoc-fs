module Aoc.Tests.Y2017.Day14Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day14

let private keys =
    Gen.choose (1, 8)
    |> Gen.bind (fun length -> Gen.arrayOfLength length (Gen.elements [ 'a' .. 'z' ]))
    |> Gen.map System.String

[<Tests>]
let tests =
    testList
        "Y2017.Day14"
        [
            testProperty
                "parser preserves generated ASCII keys"
                (Prop.forAll (Arb.fromGen keys) (fun key ->
                    match Input.parse (" \t" + key + "\r\n") with
                    | Ok(Input.Key parsed) -> parsed = key
                    | Error _ -> false))
        ]
