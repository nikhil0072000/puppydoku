using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Plays the grid intro animation: a staggered diagonal wave of cells scaling in,
/// followed by per-puppy pop-ins for any pre-placed puppies.
/// </summary>
public class GridAnimator : MonoBehaviour
{
    [Header("Tile Animation Settings")]
    [Tooltip("Delay between successive tiles in the diagonal wave.")]
    [SerializeField] private float staggerDelay = 0.04f;
    [Tooltip("Time for a single tile to scale from 0 up to its prefab scale.")]
    [SerializeField] private float scaleDuration = 0.28f;
    [Tooltip("Ease for the 0 → prefab-scale tween. OutBack adds a subtle pop; OutQuad is purely smooth.")]
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Background Board")]
    [Tooltip("Optional board / background container that grows in before the tiles.")]
    [SerializeField] private Transform boardContainer;
    [SerializeField] private float boardScaleStart = 0.95f;
    [SerializeField] private float boardFadeDuration = 0.25f;

    [Header("Puppy Animation")]
    [SerializeField] private float puppyScaleDuration = 0.2f;
    [Tooltip("Peak scale multiplier before settling back to the puppy's prefab scale.")]
    [SerializeField] private float puppyOvershoot = 1.1f;
    [SerializeField] private float puppySettleDuration = 0.1f;

    /// <summary>
    /// Animates all supplied cells in the order given (caller is responsible for
    /// ordering them — pass <see cref="GridManager.GetCellsInDiagonalOrder"/> for
    /// a diagonal wave). Invokes <paramref name="onComplete"/> once the final
    /// tile has fully settled.
    /// </summary>
    public IEnumerator AnimateGrid(List<Cell> cells, Action onComplete)
    {
        AnimateBoard();

        // Pre-pass: hide every cell BEFORE the wave starts, otherwise cells
        // that come later in the order would flash at full scale until the
        // staggered loop reached them.
        Vector3[] finalScales = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
        {
            Cell cell = cells[i];
            if (cell == null) continue;

            finalScales[i] = cell.transform.localScale;   // prefab scale (e.g. 0.3)
            cell.transform.localScale = Vector3.zero;

            SpriteRenderer overlay = cell.ZoneOverlay;
            if (overlay != null)
            {
                Color c = overlay.color;
                c.a = 0f;
                overlay.color = c;
            }
        }

        WaitForSeconds stagger = new WaitForSeconds(staggerDelay);

        // Wave: kick off one tween per cell on a stagger. Linking each tween
        // to the cell GameObject means destroying the cell mid-intro (level
        // reload) auto-kills the tween, avoiding MissingReferenceException.
        for (int i = 0; i < cells.Count; i++)
        {
            Cell cell = cells[i];
            if (cell == null) continue;

            Transform t = cell.transform;
            t.DOScale(finalScales[i], scaleDuration).SetEase(scaleEase)
             .SetLink(cell.gameObject, LinkBehaviour.KillOnDestroy);

            SpriteRenderer overlay = cell.ZoneOverlay;
            if (overlay != null)
                overlay.DOFade(1f, scaleDuration)
                       .SetLink(cell.gameObject, LinkBehaviour.KillOnDestroy);

            yield return stagger;
        }

        // Wait for the final cell's tween to finish before signalling completion.
        yield return new WaitForSeconds(scaleDuration);

        onComplete?.Invoke();
    }

    /// <summary>
    /// Pop-in animation for a single puppy. Safe to call on pre-placed and
    /// player-placed puppies alike.
    /// </summary>
    public void AnimatePuppy(Transform puppyTransform)
    {
        if (puppyTransform == null) return;

        Vector3 finalScale = puppyTransform.localScale;   // prefab scale
        Vector3 peakScale = finalScale * puppyOvershoot;
        puppyTransform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(puppyTransform.gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(puppyTransform.DOScale(peakScale, puppyScaleDuration).SetEase(Ease.OutBack));
        seq.Append(puppyTransform.DOScale(finalScale, puppySettleDuration).SetEase(Ease.OutCubic));
    }

    private void AnimateBoard()
    {
        if (boardContainer == null) return;

        boardContainer.localScale = Vector3.one * boardScaleStart;
        boardContainer.gameObject.SetActive(true);
        boardContainer.DOScale(1f, boardFadeDuration).SetEase(Ease.OutCubic)
                      .SetLink(boardContainer.gameObject, LinkBehaviour.KillOnDestroy);

        if (boardContainer.TryGetComponent(out SpriteRenderer sr))
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
            sr.DOFade(1f, boardFadeDuration)
              .SetLink(boardContainer.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
