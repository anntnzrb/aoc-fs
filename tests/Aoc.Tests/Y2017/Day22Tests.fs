module Aoc.Tests.Y2017.Day22Tests

open Expecto
open FsCheck.FSharp
open Aoc.Y2017.Day22

let private nodes =
    [ Primary.Clean; Primary.Weakened; Primary.Infected; Primary.Flagged ]

let private nodeAt cell =
    match cell % 4 with
    | 0 -> Primary.Clean
    | 1 -> Primary.Weakened
    | 2 -> Primary.Infected
    | _ -> Primary.Flagged

[<Tests>]
let tests =
    testList
        "Y2017.Day22"
        [
            testCase "parser preserves rectangular row and cell order" (fun () ->
                // A 5-column, 3-row map has a unique centre; CRLF is only a line separator.
                let actual =
                    Input.parse "#....\r\n..#..\r\n....#\r\n"
                    |> Result.map (fun (Input.InfectionMap rows) ->
                        [
                            for y, row in List.indexed rows do
                                for x, node in List.indexed row do
                                    if node = Input.Infected then
                                        yield x, y
                        ])

                Expect.equal actual (Ok [ 0, 0; 2, 1; 4, 2 ]) "coordinates retain source order")
            testCase "parser rejects maps without a unique rectangular centre" (fun () ->
                // Empty, even-width, even-height and ragged maps violate the domain shape.
                for input in [ ""; "..\n"; ".\n.\n"; "...\n.\n...\n" ] do
                    Expect.equal (Input.parse input) (Error "expected an odd-sized rectangular map") "invalid shape")
            testCase "parser rejects unknown cells" (fun () ->
                Expect.equal (Input.parse "x\n") (Error "expected a clean or infected node") "invalid cell")
        ]



[<Tests>]
let tileTests =
    testList
        "Y2017.Day22.tiles"
        [
            testCase "all signed coordinates and states round trip without touching neighbours" (fun () ->
                for coordinate in -1000 .. 1000 do
                    for x, y in [ coordinate, -coordinate; coordinate, coordinate; -coordinate, coordinate ] do
                        Expect.equal ((x >>> 4) * 16 + (x &&& 15)) x "signed x split"
                        Expect.equal ((y >>> 4) * 16 + (y &&& 15)) y "signed y split"
                        let key = Primary.Tile.key x y
                        Expect.equal (int (key >>> 32)) (x >>> 4) "packed x"
                        let low = key &&& 0xffffffffL
                        let signedLow = if low >= 0x80000000L then low - 0x100000000L else low
                        Expect.equal signedLow (int64 (y >>> 4)) "packed y"

                        for state in nodes do
                            let tile = Primary.Tile.set x y state Primary.Tile.empty
                            Expect.equal (Primary.Tile.get x y tile) state "written state"
                            Expect.equal (Primary.Tile.get (x + 1) y tile) Primary.Clean "adjacent cell")
            testCase "every cell preserves every other cell and old versions" (fun () ->
                let filled =
                    [ 0..255 ]
                    |> List.fold
                        (fun tile cell -> Primary.Tile.set cell (cell >>> 4) (nodeAt cell) tile)
                        Primary.Tile.empty

                for cell in 0..255 do
                    for state in nodes do
                        let updated = Primary.Tile.set cell (cell >>> 4) state filled

                        for other in 0..255 do
                            Expect.equal
                                (Primary.Tile.get other (other >>> 4) updated)
                                (if cell = other then state else nodeAt other)
                                "independent cell"

                        Expect.equal (Primary.Tile.get cell (cell >>> 4) filled) (nodeAt cell) "persistent old tile")
            testPropertyWithConfig
                { FsCheckConfig.defaultConfig with
                    maxTest = 40
                    endSize = 8
                }
                "mutable agrees on small valid grids and bursts"
                (let inputs =
                    gen {
                        let! radius = FsCheck.FSharp.Gen.choose (0, 3)
                        let width = radius * 2 + 1

                        let! cells =
                            FsCheck.FSharp.Gen.arrayOfLength (width * width) (FsCheck.FSharp.Gen.elements [ '.'; '#' ])

                        let! bursts = FsCheck.FSharp.Gen.choose (0, 2000)

                        let text =
                            cells
                            |> Array.chunkBySize width
                            |> Array.map System.String
                            |> String.concat "\n"

                        return text, bursts
                    }

                 FsCheck.FSharp.Prop.forAll (FsCheck.FSharp.Arb.fromGen inputs) (fun (text, bursts) ->
                     Primary.solveWith bursts bursts text = Mutable.solveWith bursts bursts text))
            testProperty "unknown cells return Error" ((+) "x" >> Primary.solve >> Result.isError)
        ]
