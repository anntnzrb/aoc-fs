module Aoc.Tests.KnotHashTests

open Expecto
open FsCheck.FSharp

[<Tests>]
let tests =
    testList
        "KnotHash"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 50
                    endSize = 16
                }
                "sparse preserves the marks for bounded lengths and rounds"
                (let inputs = Gen.zip (Gen.choose (0, 3)) (Gen.listOf (Gen.choose (0, 256)))

                 Prop.forAll (Arb.fromGen inputs) (fun (rounds, lengths) ->
                     Aoc.KnotHash.sparse rounds lengths |> Array.sort = [| 0..255 |]))

            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 50
                    endSize = 12
                }
                "dense returns sixteen bytes for ASCII text"
                (let text =
                    Gen.choose (0, 127)
                    |> Gen.map char
                    |> Gen.listOf
                    |> Gen.map (Array.ofList >> System.String)

                 Prop.forAll (Arb.fromGen text) (fun value -> Aoc.KnotHash.dense value |> Array.length = 16))

            for text, expected in
                [
                    "", "a2582a3a0e66e6e86e3812dcb672a272"
                    "AoC 2017", "33efeb34ea91902bb2f59c9920caa6cd"
                    "1,2,3", "3efbe78a8d82f29979031a4aa0b16a9d"
                    "1,2,4", "63960835bcdc130f0b66d7ff4f6a5a8e"
                ] do
                testCase $"dense hash of %A{text}" (fun () ->
                    let actual =
                        text
                        |> Aoc.KnotHash.dense
                        |> Array.map (fun value ->
                            value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture))
                        |> String.concat ""

                    Expect.equal actual expected "Statement example")
        ]
