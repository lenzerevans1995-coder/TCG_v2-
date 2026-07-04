using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardUtilities;

/// <summary>
/// UICardController is the main class that has the logic of changing the styles of the cards in the UI
/// </summary>
public class UICardController : MonoBehaviour
{
    #region Variables
    //Style for the current card
    public CardScriptable styleData;

    [Header("UI")]
    [Header("Back")]
    [Tooltip("Object that has all the subObjects for the back of the card")]
    public GameObject backObject;
    [Header("Front")]
    [Tooltip("Object that has all the subObjects for the front of the card")]
    public GameObject frontObject;

    [Header("Image")]
    [Tooltip("List to associate a label with an 'Image' type element of the menu UI")]
    public List<ImageCardData> imagesList;

    [Header("Text")]
    [Tooltip("List to associate a label with an 'TextMeshPro' type element of the menu UI")]
    public List<TextCardData> textMeshList;

    private Dictionary<LabelImageCards, Image> imageMap;
    private Dictionary<LabelTextCards, TextMeshProUGUI> textMap;

    #endregion

    private void Awake()
    {
        InitDictionary();
    }

    void Start()
    {

    }

    /// <summary>
    /// The two dictionaries are initialized, one for the images and one for the texts in the card
    /// </summary>
    private void InitDictionary()
    {
        imageMap = new Dictionary<LabelImageCards, Image>();

        //The number of elements in the LabelImageCards enum
        int lengthImage = System.Enum.GetValues(typeof(LabelImageCards)).Length;

        for (int i = 0; i <= lengthImage; i++)
        {  
            LabelImageCards label = (LabelImageCards) i;
            imageMap[label] = GetImage(label);
        }

        textMap = new Dictionary<LabelTextCards, TextMeshProUGUI>();

        //The number of elements in the LabelTextCards enum
        int lengthText = System.Enum.GetValues(typeof(LabelTextCards)).Length;

        for (int i = 0; i <= lengthText; i++)
        {  
            LabelTextCards label = (LabelTextCards)i;
            textMap[label] = GetText(label);
        }
    }

    /// <summary>
    /// Gets the 'Image' component associated with a Label
    /// </summary>
    /// <param name="label">Tag of the image being searched for</param>
    /// <returns>Returns the Image that has that label associated with it in our list of UI elements.</returns>
    private Image GetImage(LabelImageCards label)
    {
        return imagesList.Find((x) => x.label == label).image;
    }

    /// <summary>
    /// Gets the 'TextMeshPro' component associated with a Label
    /// </summary>
    /// <param name="label">Tag of the text being searched for</param>
    /// <returns>Returns the TextMeshPro that has that label associated with it in our list of UI elements.</returns>
    private TextMeshProUGUI GetText(LabelTextCards label)
    {
        return textMeshList.Find((x) => x.label == label).textMesh;
    }


    /// <summary>
    /// Function to update the style of the card
    /// </summary>
    public void UpdateStyle()
    {
        if(imageMap == null || textMap == null)
        {
            InitDictionary();
        }

        //All UI elements are hidden, this is done because some styles may be more complex than others
        //and may not require all the UI elements associated with the prefab.
        foreach (ImageCardData img in imagesList)
        {
            img.image.gameObject.SetActive(false);
        }

        foreach (TextCardData txt in textMeshList)
        {
            txt.textMesh.gameObject.SetActive(false);
        }

        foreach (UICardImage element in styleData.UIElementsImage)
        {
            ChangeSprite(element);
        } 

        foreach (UICardText element in styleData.UIElementsText)
        {
            ChangeText(element);
        }
    }

    /// <summary>
    /// Function that updates the Card Image component with the data obtained from the current style
    /// </summary>
    public void ChangeSprite(UICardImage element)
    {
        if (imageMap.TryGetValue(element.label, out Image targetImage))
        {
            targetImage.sprite = element.sprite;
            targetImage.color = element.color;
            targetImage.rectTransform.SetLocalPositionAndRotation(
                element.position, 
                Quaternion.Euler(element.rotation.x,element.rotation.y,element.rotation.z)
            );
            targetImage.rectTransform.sizeDelta = element.size;
            targetImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Function that updates the Card TextMeshPro component with the data obtained from the current style
    /// </summary>
    public void ChangeText(UICardText element)
    {
        if (textMap.TryGetValue(element.label, out TextMeshProUGUI targetText))
        {
            targetText.text = element.text;
            targetText.color = element.color;
            targetText.rectTransform.SetLocalPositionAndRotation(element.position, Quaternion.identity);
            targetText.fontSize = element.size;
            targetText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Function that can show or hide a specific subpart of the card
    /// </summary>
    public void ShowFace(FaceCard newFace)
    {
        backObject.SetActive(false);
        frontObject.SetActive(false);

        switch (newFace)
        {
            case FaceCard.Back:
                backObject.SetActive(true);
                break;
            case FaceCard.Front:
                frontObject.SetActive(true);
                break;
        }
    }

}
