module Aoc.Tests.Y2017.Day21Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day21

let private pattern size =
    Gen.elements [ '.'; '#' ]
    |> Gen.listOfLength size
    |> Gen.map (List.toArray >> System.String)
    |> Gen.listOfLength size

let private rule size =
    Gen.zip (pattern size) (pattern (size + 1))
    |> Gen.map (fun (source, target) ->
        {
            Input.Source = source
            Input.Target = target
        })

let private rules =
    gen {
        let! count = Gen.choose (1, 4)
        return! Gen.oneof [ rule 2; rule 3 ] |> Gen.listOfLength count
    }

let private render (rules: Input.Rule list) =
    rules
    |> List.map (fun rule ->
        let source = String.concat "/" rule.Source
        let target = String.concat "/" rule.Target
        $"%O{source} => %O{target}")
    |> String.concat "\n"

let private config =
    { FsCheckConfig.defaultConfig with
        maxTest = 30
        endSize = 4
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day21"
        [
            testPropertyWithConfig
                config
                "parser preserves square rule sizes and pixels"
                (Prop.forAll (Arb.fromGen rules) (fun expected -> Input.parse (render expected) = Ok expected))
            testPropertyWithConfig
                config
                "incomplete rulebooks return a result without throwing"
                (
                // With only 3-to-4 rules, enhancement stops at the next 2-by-2 lookup.
                Prop.forAll (Arb.fromGen (rule 3)) (fun generated ->
                    let initial =
                        { generated with
                            Source = [ ".#."; "..#"; "###" ]
                        }

                    Primary.solve (render [ initial ]) = Error "no enhancement rule matches a block"))
        ]
