/// Domain model and parser for this puzzle.
module Aoc.Y2017.Day09.Input

open FParsec

/// A nested group or garbage containing only characters that were not canceled.
type Item =
    | Group of Item list
    | Garbage of string

/// Parses one complete group, removing canceled garbage characters.
let parse (input: string) : Result<Item, string> =
    let item, itemRef = createParserForwardedToRef<Item, unit>()
    let canceled = pchar '!' >>. anyChar >>% None
    let literal = noneOf "!>" |>> Some

    let garbage =
        between (pchar '<') (pchar '>') (many (canceled <|> literal))
        |>> (List.choose id >> List.toArray >> System.String >> Garbage)

    let group = between (pchar '{') (pchar '}') (sepBy item (pchar ',')) |>> Group
    itemRef.Value <- group <|> garbage

    match run (group .>> spaces .>> eof) input with
    | Success(value, _, _) -> Result.Ok value
    | Failure(message, _, _) -> Result.Error message
