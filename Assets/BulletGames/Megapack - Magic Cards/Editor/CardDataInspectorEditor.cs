using UnityEditor;
using UnityEngine;
using CardUtilities;

#if UNITY_EDITOR

/// <summary>
/// CardDataInspectorEditor is a class that runs within the editor, 
/// without having to run our game yet, and helps us to update style changes in our cards.
/// </summary>
[CustomEditor(typeof(CardData))]
public class CardDataInspectorEditor : Editor
{
    /// <summary>
    /// Customizing the UI in the inspector for the "CardData" script
    /// </summary>
    public override void OnInspectorGUI()
    {
        var enumScript = target as CardData;
        //The enum option is created to select the face of the card
        enumScript.currentFace = (FaceCard)EditorGUILayout.EnumPopup(enumScript.currentFace);

        //The button to update our style is created
        if (GUILayout.Button("Update Style"))
        {
            enumScript.SetStyleCard();
            enumScript.SetFaceCard(enumScript.currentFace);
        }
    }
}

#endif