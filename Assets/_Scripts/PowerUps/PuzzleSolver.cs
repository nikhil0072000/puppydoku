using System.Collections.Generic;
using UnityEngine;

namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// Runtime backtracking solver for the puppy puzzle. The level data carries no
    /// stored solution (the game only validates constraints), so the Puppy-reveal
    /// power-up uses this to find a guaranteed-correct cell: it computes a full
    /// solution that EXTENDS the puppies already on the board, then the caller
    /// places one of the not-yet-placed solution cells.
    ///
    /// The puzzle goal is exactly one puppy per colour index, obeying the same
    /// row / column / diagonal-touch / colour rules as <see cref="GameRules"/>.
    /// This is a one-shot call on a button press (not a hot path), so clarity wins
    /// over micro-optimisation. The MRV ordering keeps it fast on real boards.
    /// </summary>
    public static class PuzzleSolver
    {
        /// <summary>
        /// Finds a complete valid assignment (one cell per colour) that includes every
        /// position in <paramref name="alreadyPlaced"/>. Returns false if no such
        /// completion exists (e.g. the player painted the board into a dead end).
        /// </summary>
        public static bool TrySolve(int[,] zoneMap,
                                    int[] zoneToColorIndex,
                                    int totalColorCount,
                                    HashSet<Vector2Int> alreadyPlaced,
                                    out List<Vector2Int> solution)
        {
            solution = null;
            if (zoneMap == null || zoneToColorIndex == null) return false;

            int width = zoneMap.GetLength(0);
            int height = zoneMap.GetLength(1);

            // Candidate empty cells grouped by the colour index they would satisfy.
            var candidatesByColor = new Dictionary<int, List<Vector2Int>>();
            // Colours already satisfied by placed puppies are skipped entirely.
            var satisfiedColors = new HashSet<int>();

            foreach (Vector2Int p in alreadyPlaced)
                satisfiedColors.Add(ColorAt(p, zoneMap, zoneToColorIndex));

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (alreadyPlaced.Contains(pos)) continue;

                    int color = ColorAt(pos, zoneMap, zoneToColorIndex);
                    if (satisfiedColors.Contains(color)) continue;

                    if (!candidatesByColor.TryGetValue(color, out List<Vector2Int> list))
                    {
                        list = new List<Vector2Int>();
                        candidatesByColor[color] = list;
                    }
                    list.Add(pos);
                }
            }

            // Colours still needing a puppy, sorted by fewest candidates first (MRV).
            var colorsToFill = new List<int>(candidatesByColor.Keys);
            colorsToFill.Sort((a, b) => candidatesByColor[a].Count.CompareTo(candidatesByColor[b].Count));

            var working = new HashSet<Vector2Int>(alreadyPlaced);
            var chosen = new List<Vector2Int>();

            if (!Recurse(0, colorsToFill, candidatesByColor, working, chosen, zoneMap, zoneToColorIndex))
                return false;

            // Full solution = the puppies already down + the newly chosen ones.
            solution = new List<Vector2Int>(alreadyPlaced);
            solution.AddRange(chosen);
            return true;
        }

        private static bool Recurse(int index,
                                    List<int> colorsToFill,
                                    Dictionary<int, List<Vector2Int>> candidatesByColor,
                                    HashSet<Vector2Int> working,
                                    List<Vector2Int> chosen,
                                    int[,] zoneMap,
                                    int[] zoneToColorIndex)
        {
            if (index >= colorsToFill.Count) return true;

            int color = colorsToFill[index];
            foreach (Vector2Int candidate in candidatesByColor[color])
            {
                // Quiet check — the verbose overload would spam the console thousands of times.
                if (!GameRules.IsPlacementValid(candidate, working, zoneMap, zoneToColorIndex, verbose: false))
                    continue;

                working.Add(candidate);
                chosen.Add(candidate);

                if (Recurse(index + 1, colorsToFill, candidatesByColor, working, chosen, zoneMap, zoneToColorIndex))
                    return true;

                working.Remove(candidate);
                chosen.RemoveAt(chosen.Count - 1);
            }

            return false;
        }

        private static int ColorAt(Vector2Int p, int[,] zoneMap, int[] zoneToColorIndex)
        {
            int zone = zoneMap[p.x, p.y];
            return zoneToColorIndex[zone];
        }
    }
}
