/// Domain model and parser for a comma-separated hex-grid path.
module Aoc.Y2017.Day11.Input

/// A move to one of the six adjacent hexes.
type Direction =
    | North
    | Northeast
    | Southeast
    | South
    | Southwest
    | Northwest

let private direction =
    function
    | "n" -> Ok North
    | "ne" -> Ok Northeast
    | "se" -> Ok Southeast
    | "s" -> Ok South
    | "sw" -> Ok Southwest
    | "nw" -> Ok Northwest
    | token -> Error $"invalid direction: %A{token}"

/// Parses a non-empty path, rejecting unknown or empty direction tokens.
let parse (input: string) : Result<Direction list, string> =
    input.Trim().Split(',') |> Aoc.Parse.traverse direction
