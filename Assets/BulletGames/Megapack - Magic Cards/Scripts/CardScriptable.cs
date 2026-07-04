using System.Collections.Generic;
using UnityEngine;
using CardUtilities;

/// <summary>
/// The "CardScriptable" class allows us to create a scriptable object to edit a new card style
/// </summary>
[CreateAssetMenu(fileName = "NewStyle", menuName = "Custom Card/NewStyle", order = 1)]
public class CardScriptable : ScriptableObject {

    [Header("Card Style")]
    [Tooltip("Identifier for this style")]
    public StyleCardLabel label;

    [Header("UI Image")]
    [Tooltip("List of all elements that have the 'Image' component within our letter")]
    public List<UICardImage> UIElementsImage;

    [Header("UI Text")]
    [Tooltip("List of all elements that have the 'TextMeshPro' component within our letter")]
    public List<UICardText> UIElementsText;

}

