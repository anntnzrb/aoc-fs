/// Validates submissions and classifies server verdict text without IO.
module Aoc.Ops.Submission

open System.Text.RegularExpressions

/// Accepts parts 1 and 2 and non-empty ASCII answer tokens only.
let validate (part: string) (answer: string) : Result<int * string, string> =
    match part with
    | "1"
    | "2" when Regex.IsMatch(answer, @"\A[0-9A-Za-z,-]+\z") -> Ok(int part, answer)
    | "1"
    | "2" -> Error "answer must be a non-empty token of ASCII letters, digits, commas and hyphens"
    | _ -> Error "part must be 1 or 2"

/// Returns 0 right, 1 wrong, 2 cooldown/already complete, or 3 unknown.
let classify (text: string) : int =
    if text.Contains "That's the right answer" then
        0
    elif text.Contains "That's not the right answer" then
        1
    elif
        text.Contains "You gave an answer too recently"
        || text.Contains "You don't seem to be solving the right level"
    then
        2
    else
        3
