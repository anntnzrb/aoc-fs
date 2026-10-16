module Aoc.Tests.Y2017.Day05Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day05

[<Tests>]
let tests =
    testList
        "Y2017.Day05"
        [
            testProperty
                "mutable agrees on bounded offset grammar"
                (Prop.forAll
                    (Gen.choose (-3, 6)
                     |> Gen.nonEmptyListOf
                     |> Gen.resize 15
                     |> Gen.map (List.map string >> String.concat "\n")
                     |> Arb.fromGen)
                    (fun text -> Mutable.solve text = Primary.solve text))
            testProperty "integer extremes remain total" (fun (value: int) ->
                Primary.solve (string value) = Mutable.solve (string value))
            testProperty "malformed offset text returns Error" ((+) "x" >> Primary.solve >> Result.isError)
        ]
