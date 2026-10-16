/// Domain model and parser for firewall layers.
module Aoc.Y2017.Day13.Input

open FParsec

/// A layer's non-negative distance into the firewall.
type Depth = Depth of value: uint32

/// The positive number of positions scanned in a layer.
type Range = Range of value: uint32

/// A scanner at a particular firewall depth.
type Layer = { Depth: Depth; Range: Range }

/// Parses non-empty depth/range declarations, rejecting zero ranges.
let parse (input: string) : Result<Layer list, string> =
    let range =
        puint32
        >>= fun value ->
            if value > 0u then
                preturn (Range value)
            else
                fail "range must be positive"

    let layer =
        (puint32 |>> Depth) .>> pstring ": " .>>. range
        |>> fun (depth, range) -> { Depth = depth; Range = range }

    Aoc.Parse.run (sepEndBy1 layer newline .>> eof) input
