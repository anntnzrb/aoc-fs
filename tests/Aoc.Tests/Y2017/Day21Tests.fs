module Aoc.Tests.Y2017.Day21Tests

open Expecto
open FsCheck.FSharp
open Aoc.Solution
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

let private off size =
    List.replicate size (String.replicate size ".")

let private patterns size =
    [
        for bits in 0 .. (1 <<< (size * size)) - 1 ->
            [
                for y in 0 .. size - 1 ->
                    [
                        for x in 0 .. size - 1 -> if bits &&& (1 <<< (y * size + x)) = 0 then "." else "#"
                    ]
                    |> String.concat ""
            ]
    ]

// Coordinates are transformed independently of the solver's bit-key symmetry index.
let private transform coordinates (rows: string list) =
    let pixels = List.toArray rows

    [
        for y in 0 .. pixels.Length - 1 ->
            [
                for x in 0 .. pixels.Length - 1 ->
                    let sourceY, sourceX = coordinates pixels.Length y x
                    string (pixels[sourceY][sourceX])
            ]
            |> String.concat ""
    ]

[<Tests>]
let tests =
    testList
        "Y2017.Day21"
        [
            testCase "complete all-off rulebook enhances through both parts" (fun () ->
                let rules =
                    [
                        for size in [ 2; 3 ] do
                            for source in patterns size ->
                                {
                                    Input.Source = source
                                    Input.Target = off (size + 1)
                                }
                    ]
                // Every replacement is all-off, so both the fifth and eighteenth images contain zero lit pixels.
                Expect.equal (Primary.solve (render rules)) (Ok(Answers.both "0" "0")) "all rules are available")
            testCase "asymmetric rotated and reflected rules match a coordinate reference" (fun () ->
                let start = [ ".#."; "..#"; "###" ]

                for coordinates, inverse in
                    [
                        (fun size y x -> size - 1 - x, y), (fun size y x -> x, size - 1 - y)
                        (fun size y x -> y, size - 1 - x), (fun size y x -> y, size - 1 - x)
                    ] do
                    let source = transform coordinates start
                    Expect.notEqual source start "the lookup must transform an asymmetric pattern"
                    Expect.equal (transform inverse source) start "reference transformation matches the starting image"

                    let rules =
                        [
                            {
                                Input.Source = source
                                Input.Target = off 4
                            }
                            {
                                Input.Source = off 2
                                Input.Target = off 3
                            }
                            {
                                Input.Source = off 3
                                Input.Target = off 4
                            }
                        ]
                    // The transformed start matches once; every subsequent block remains all-off.
                    Expect.equal (Primary.solve (render rules)) (Ok(Answers.both "0" "0")) "symmetry lookup agrees")
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
