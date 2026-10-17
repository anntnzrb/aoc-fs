module Aoc.Tests.Y2017.Day25Tests

open Expecto
open FsCheck.FSharp
open Aoc.Solution
open Aoc.Y2017.Day25

let private rules name nextZero nextOne =
    $"""In state %O{name}:
  If the current value is 0:
    - Write the value 1.
    - Move one slot to the right.
    - Continue with state %O{nextZero}.
  If the current value is 1:
    - Write the value 0.
    - Move one slot to the left.
    - Continue with state %O{nextOne}.
"""

let private blueprint steps start definitions =
    $"Begin in state %O{start}.\nPerform a diagnostic checksum after %O{steps} steps.\n\n%O{definitions}"

[<Tests>]
let tests =
    testList
        "Y2017.Day25"
        [
            testCase "negative step count is rejected" (fun () ->
                // A negative count cannot describe a number of machine transitions.
                Expect.equal
                    (blueprint -1 "A" (rules "A" "A" "A") |> Primary.solve)
                    (Error "expected non-negative steps and unique state definitions")
                    "negative steps")
            testCase "duplicate state definitions are rejected" (fun () ->
                // Two definitions of A do not specify a unique transition table.
                let definition = rules "A" "A" "A"

                Expect.equal
                    (blueprint 1 "A" (definition + definition) |> Primary.solve)
                    (Error "expected non-negative steps and unique state definitions")
                    "duplicate states")
            testCase "every referenced state must be defined" (fun () ->
                // B is absent, whether referenced by the start, zero branch or one branch.
                for start, nextZero, nextOne in [ "B", "A", "A"; "A", "B", "A"; "A", "A", "B" ] do
                    Expect.equal
                        (blueprint 0 start (rules "A" nextZero nextOne) |> Primary.solve)
                        (Error "undefined machine state")
                        "undefined state even when no steps execute")
            testCase "zero steps leaves an all-zero tape" (fun () ->
                // The untouched tape has no ones, and this day emits only part 1.
                Expect.equal
                    (blueprint 0 "A" (rules "A" "A" "A")
                     |> Primary.solve
                     |> Result.map Answers.render)
                    (Ok "0\n")
                    "empty checksum")
            testCase "trailing text is rejected" (fun () ->
                // A complete blueprint cannot contain an extra token after its rules.
                Expect.isError
                    (blueprint 0 "A" (rules "A" "A" "A" + "extra") |> Primary.solve)
                    "the shared parser adapter preserves eof enforcement")
        ]



[<Tests>]
let cellZipperTests =
    let inputs =
        gen {
            let! steps = FsCheck.FSharp.Gen.choose (0, 2000)
            let! start = Gen.elements [ "A"; "B" ]
            let! actions = FsCheck.FSharp.Gen.arrayOfLength 12 (FsCheck.FSharp.Gen.choose (0, 3))

            let action index value =
                let offset = index * 6 + value * 3

                {
                    Input.Write = if actions[offset] % 2 = 0 then Input.Zero else Input.One
                    Input.Move =
                        if actions[offset + 1] % 2 = 0 then
                            Input.Left
                        else
                            Input.Right
                    Input.Next = Input.State(if actions[offset + 2] % 2 = 0 then "A" else "B")
                }

            let expected =
                {
                    Input.Start = Input.State start
                    Input.Steps = steps
                    Input.States =
                        [ "A"; "B" ]
                        |> List.mapi (fun index name ->
                            Input.State name,
                            {
                                Input.IfZero = action index 0
                                Input.IfOne = action index 1
                            })
                        |> Map.ofList
                }

            let definition index name =
                let branch value =
                    let offset = index * 6 + value * 3
                    let write = actions[offset] % 2
                    let move = if actions[offset + 1] % 2 = 0 then "left" else "right"
                    let next = if actions[offset + 2] % 2 = 0 then "A" else "B"
                    $"  If the current value is %O{value}:\n    - Write the value %O{write}.\n    - Move one slot to the %O{move}.\n    - Continue with state %O{next}.\n"

                $"In state %O{name}:\n" + branch 0 + branch 1

            return blueprint steps start (definition 0 "A" + definition 1 "B"), expected
        }

    testList
        "Y2017.Day25.cellzipper"
        [
            testProperty
                "parser preserves generated start steps and actions"
                (Prop.forAll (Arb.fromGen inputs) (fun (text, expected) -> Input.parse text = Ok expected))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 80
                }
                "cell zipper agrees on generated blueprints"
                (FsCheck.FSharp.Prop.forAll (FsCheck.FSharp.Arb.fromGen inputs) (fun (text, _) ->
                    CellZipper.solve text = Primary.solve text))
            testCase "both tape directions cross multiple words" (fun () ->
                for direction in [ "left"; "right" ] do
                    let definitions =
                        (rules "A" "A" "A")
                            .Replace("to the right", "to the " + direction)
                            .Replace("to the left", "to the " + direction)

                    let text = blueprint 257 "A" definitions

                    Expect.equal
                        (Primary.solve text |> Result.map Answers.render)
                        (Ok "257\n")
                        "every new cell receives one"

                    Expect.equal (CellZipper.solve text) (Primary.solve text) "word boundaries")
            testProperty "malformed blueprints return Error" ((+) "x" >> Primary.solve >> Result.isError)
        ]
