module Aoc.Tests.Y2017.Day19Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day19

let private diagrams =
    gen {
        let! padding = Gen.choose (0, 5)
        let! width = Gen.choose (1, 6)
        let! right = Gen.elements [ true; false ]

        let! stem =
            Gen.choose (0, 6)
            |> Gen.bind (fun length -> Gen.listOfLength length (Gen.elements ('|' :: [ 'A' .. 'Z' ])))

        let! endpoint = Gen.elements [ 'A' .. 'Z' ]
        let! newline = Gen.elements [ "\n"; "\r\n" ]
        let entrance = padding + (if right then 0 else width)
        let prefix = System.String(' ', entrance)

        let bottom =
            System.String(' ', padding)
            + (if right then "+" else string endpoint)
            + System.String('-', width - 1)
            + (if right then string endpoint else "+")

        let rows =
            (prefix + "|")
            :: (List.map (fun cell -> prefix + string cell) stem @ [ bottom ])

        return String.concat newline rows + newline, (rows, entrance)
    }

[<Tests>]
let tests =
    testList
        "Y2017.Day19"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                    endSize = 12
                }
                "parser preserves ragged rows and entrance columns"
                (Prop.forAll (Arb.fromGen diagrams) (fun (text, (rows, entrance)) ->
                    match Input.parse text with
                    | Ok diagram -> diagram.Rows = rows && diagram.Entrance = entrance
                    | Error _ -> false))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                    endSize = 12
                }
                "solver is total on generated terminating routes"
                (Prop.forAll (Arb.fromGen diagrams) (fun (text, _) ->
                    Primary.solve text |> ignore
                    true))
        ]
