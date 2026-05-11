using UnityEngine;


public class Cell : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer zoneOverlay; // colored zone background
    public SpriteRenderer catSprite;   // the cat
    public GameObject catIndicator;    // maybe a little paw print if no cat yet
    public bool hasCat = false;
    public bool isGiven = false;
    public Vector2Int position;
    public int zoneID;

    Color[] zoneColors; // assign palette

    public void Init(Vector2Int pos, int zone)
    {
        position = pos;
        zoneID = zone;
        if (zoneOverlay != null)
            zoneOverlay.color = zoneColors[zone % zoneColors.Length];
        catSprite.gameObject.SetActive(false);
        catIndicator.SetActive(false);
    }

    public void PlaceCat(bool cat, bool isGiven)
    {
        hasCat = true;
        this.isGiven = isGiven;
        catSprite.gameObject.SetActive(true);
        catSprite.color = isGiven ? Color.white : Color.green; // given vs player
        // Play pop animation
        GetComponent<Animator>()?.SetTrigger("PlaceCat");
    }

    public void RemoveCat()
    {
        hasCat = false;
        isGiven = false;
        catSprite.gameObject.SetActive(false);
    }

    public void ShowIncorrectPlacement()
    {
        // Red flash, shake
        StartCoroutine(IncorrectFeedback());
    }

    System.Collections.IEnumerator IncorrectFeedback()
    {
        catSprite.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        catSprite.color = Color.green;
    }

    public void ShowHint()
    {
        // Glow effect or small arrow
        catSprite.gameObject.SetActive(true);
        catSprite.color = new Color(1,1,0,0.5f); // semi-transparent yellow cat
        // Auto-place after delay? In real game, hint might place directly.
    }
}
