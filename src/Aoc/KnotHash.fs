/// Circular reversal hashing shared by puzzle solvers; callers validate inputs before hashing.
module Aoc.KnotHash

/// Returns a fresh 256-mark permutation after the given nonnegative number of rounds.
/// Lengths must be in 0..256; position and skip carry across rounds, with circular indexing.
/// Zero rounds or no lengths leave marks in ascending order; invalid arguments are outside the contract.
let sparse rounds lengths =
    let marks = Array.init 256 id

    Seq.replicate rounds lengths
    |> Seq.concat
    |> Seq.fold
        (fun (position, skip) length ->
            for offset in 0 .. length / 2 - 1 do
                let left = (position + offset) % 256
                let right = (position + length - 1 - offset) % 256
                let saved = marks[left]
                marks[left] <- marks[right]
                marks[right] <- saved

            // Only the skip's residue affects circular positions, including across rounds.
            (position + length + skip) % 256, (skip + 1) % 256)
        (0, 0)
    |> ignore

    marks

/// Hashes ASCII text, including empty text, without trimming, into 16 bytes in block order.
/// Appends the standard suffix, runs 64 rounds and XORs consecutive 16-mark blocks.
/// The caller must supply ASCII text; non-ASCII text is outside the contract. Valid text cannot fail.
let dense (text: string) =
    let lengths = (text |> Seq.map int |> Seq.toList) @ [ 17; 31; 73; 47; 23 ]

    lengths
    |> sparse 64
    |> Array.chunkBySize 16
    |> Array.map (Array.fold (^^^) 0 >> byte)
