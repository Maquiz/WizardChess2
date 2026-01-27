using UnityEngine;

/// <summary>
/// ScriptableObject defining an element's metadata (name, color, icon).
/// Create assets via Assets > Create > WizardChess > Element Definition.
/// </summary>
[CreateAssetMenu(fileName = "NewElement", menuName = "WizardChess/Element Definition")]
public class ElementDefinition : ScriptableObject
{
    public string elementName;
    public int elementId;
    public Color elementColor = Color.white;
    public Sprite icon;
    [TextArea] public string description;
}
