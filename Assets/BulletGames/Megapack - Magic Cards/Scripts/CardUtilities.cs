using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Collection of enums, structures and helper classes for handling card styles
/// </summary>
namespace CardUtilities
{
    #region Enums

    /// <summary>
    /// Name tags for our card styles
    /// </summary>
    [System.Serializable]
    public enum StyleCardLabel
    {
        Template, Style1, Style2, Style3, Style4, Style5
    }

    /// <summary>
    /// Label for subparts of the same card
    /// </summary>
    [System.Serializable]
    public enum FaceCard
    {
        Back, Front
    }

    /// <summary>
    /// Tags to identify UI objects that have 'Image' type components inside the card
    /// </summary>
    [System.Serializable]
    public enum LabelImageCards
    {
        //Back
        BackBackground, BackFrame, BackSymbol, BackSymbol2,

        //Front
        FrontBackground, FrontBackgroundText, FrontFrame, FrontIconValue1, FrontIconValue2, FrontIconValue3,
        SpriteImage, Mask, FrontDecorative, FrontRarity1, FrontRarity2, FrontRarity3, FrontRarity4,
        FrontRarity5
    }

    /// <summary>
    /// Labels to identify UI objects that have 'TextMeshPro' type components inside the card
    /// </summary>
    [System.Serializable]
    public enum LabelTextCards
    {
        TxtTitle, TxtDescription, TxtValue1, TxtValue2, TxtValue3
    }

    #endregion

    /// <summary>
    /// The UICardImage class contains the values ​​to be modified from an object with the 'Image' 
    /// component within the card UI from the styles panel
    /// </summary>
    [System.Serializable]
    public class UICardImage
    {
        [Tooltip("Tags to identify UI objects that have 'Image' type components inside the card")]
        public LabelImageCards label;
        [Space]
        [Tooltip("Sprite to be added to the UI")]
        public Sprite sprite;
        [Tooltip("Color variation to apply to the sprite")]
        public Color color;
        [Space]
        [Tooltip("Corresponds to the 'Pos X' and 'Pos Y' values of the 'RectTransform' component of the object")]
        public Vector2 position;
        [Tooltip("Corresponds to the Width and Height values of the 'RectTransform' component of the object")]
        public Vector2 size;
        [Tooltip("Corresponds to the Rotation values of the 'RectTransform' component of the object")]
        public Vector3 rotation;
    }

    /// <summary>
    /// The UICardText class contains the values ​​to be modified from an object with the 'TextMeshPro' 
    /// component within the card UI from the styles panel
    /// </summary>
    [System.Serializable]
    public class UICardText
    {
        [Tooltip("Labels to identify UI objects that have 'TextMeshPro' type components inside the card")]
        public LabelTextCards label;
        [Space]
        [TextArea(2, 5)]
        [Tooltip("Text to display")]
        public string text;
        [Tooltip("Variation in text color")]
        public Color color;
        [Space]
        [Tooltip("Corresponds to the 'Pos X' and 'Pos Y' values of the 'RectTransform' component of the object")]
        public Vector2 position;
        [Tooltip("Size of text to display")]
        public float size;
    }

    /// <summary>
    /// Structure that associates a label with a component of type 'Image'
    /// </summary>
    [System.Serializable]
    public struct ImageCardData
    {
        public LabelImageCards label;
        public Image image;
    }

    /// <summary>
    /// Structure that associates a label with a component of type 'TextMeshPro'
    /// </summary>
    [System.Serializable]
    public struct TextCardData
    {
        public LabelTextCards label;
        public TextMeshProUGUI textMesh;
    }
}

