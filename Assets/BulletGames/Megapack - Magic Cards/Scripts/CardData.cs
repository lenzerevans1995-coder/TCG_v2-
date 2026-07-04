using UnityEngine;
using CardUtilities;

/// <summary>
/// CardData is a class that we modified using the "CardDataInspectorEditor" script 
/// to give the user options in the inspector so they can update their styles quickly.
/// </summary>
[RequireComponent(typeof(UICardController))]
public class CardData : MonoBehaviour
{
    public StyleCardLabel style;
    public FaceCard currentFace;

    private void Start()
    {
        SetStyleCard();
        SetFaceCard(currentFace);
    }

    /// <summary>
    /// Call the "UpdateStyle" function to update the current style of the card.
    /// </summary>
    public void SetStyleCard()
    {
        GetComponent<UICardController>().UpdateStyle();
    }

    /// <summary>
    /// Call the "ShowFace" function to display a specific face of the card
    /// </summary>
    /// <param name="newFace">The new face of the card to be displayed in the UI</param>
    public void SetFaceCard(FaceCard newFace)
    {
        GetComponent<UICardController>().ShowFace(newFace);
    }
}
