/// Domain model and parser for the disk's hash key.
module Aoc.Y2017.Day14.Input

/// The ASCII key from which row hash inputs are formed.
[<Struct>]
type Key = Key of value: string

/// Parses one nonempty ASCII key, removing surrounding whitespace and rejecting embedded line breaks.
let parse (input: string) : Result<Key, string> =
    match input.Trim() with
    | text when String.length text = 0 -> Error "empty disk key"
    | key ->
        match key |> Seq.forall (fun c -> System.Char.IsAscii c && c <> '\r' && c <> '\n') with
        | true -> Ok(Key key)
        | false -> Error "disk key must be a single ASCII line"
