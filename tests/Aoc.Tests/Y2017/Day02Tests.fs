module Aoc.Tests.Y2017.Day02Tests

open Expecto
open Aoc.Solution
open Aoc.Y2017.Day02
open FsCheck.FSharp

let private spreadsheet =
    let row =
        Gen.choose (1, 6)
        |> Gen.bind (fun size -> Gen.listOfLength size (Gen.choose (1, 1000)))

    Gen.choose (1, 6)
    |> Gen.bind (fun size -> Gen.listOfLength size row)
    |> Gen.map (fun rows ->
        rows |> List.map (List.map string >> String.concat "\t") |> String.concat "\n",
        rows |> List.map (List.map bigint))

[<Tests>]
let tests =
    testList
        "Y2017.Day02"
        [
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "parser preserves spreadsheet rows"
                (Prop.forAll (Arb.fromGen spreadsheet) (fun (text, rows) ->
                    Input.parse text
                    |> Result.map (fun sheet -> sheet.Rows |> List.map (fun (Input.Row(first, rest)) -> first :: rest))
                        =
                        Ok rows))
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                }
                "solver is total on small spreadsheets"
                (Prop.forAll (Arb.fromGen spreadsheet) (fun (text, _) ->
                    match Primary.solve text with
                    | Ok _
                    | Error _ -> true))
            testCase "division checks either order without counting equal pairs twice" (fun () ->
                // Differences are 6, 6 and 0; quotients are 4, 4 and 1, once per unordered pair.
                Expect.equal
                    (Primary.solve "2 8\n8 2\n7 7\n" |> Result.map Answers.render)
                    (Ok "12\n9\n")
                    "both directions and duplicate values")
            testCase "zero denominators and inexact pairs contribute nothing" (fun () ->
                // Differences are 0, 5, 5 and 1; the zero quotients and inexact 3/2 sum to zero.
                Expect.equal
                    (Primary.solve "0 0\n0 5\n5 0\n2 3\n" |> Result.map Answers.render)
                    (Ok "11\n0\n")
                    "division remains total")
            testCase "parsing keeps arbitrary precision with mixed whitespace" (fun () ->
                // The first difference is 10^20 and quotient 2; the second is 6 and quotient 4.
                Expect.equal
                    (Primary.solve "100000000000000000000\t200000000000000000000\r\n8 2\r\n"
                     |> Result.map Answers.render)
                    (Ok "100000000000000000006\n6\n")
                    "bigint and mixed whitespace")
            testCase "parsing reports the first bad token across rows" (fun () ->
                Expect.equal
                    (Input.parse "2 8\n3 bad worse\nlater\n")
                    (Error "not an integer: \"bad\"")
                    "source-order failure")
            testCase "parsing rejects interior empty rows" (fun () ->
                Expect.equal (Input.parse "2 8\n\n3 9\n") (Error "empty spreadsheet row") "nonempty rows")
        ]
