using UnityEngine;

/// <summary>
/// ScriptableObject defining an ability's metadata (name, cooldown, description).
/// Create assets via Assets > Create > WizardChess > Ability Definition.
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "WizardChess/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    public string abilityName;
    public int cooldown;
    public bool isPassive;
    public int elementId;
    public int pieceType;
    [TextArea] public string description;
    public Sprite icon;

    /// <summary>
    /// The fully qualified class name of the ability implementation.
    /// e.g. "FirePawnPassive" or "FirePawnActive"
    /// </summary>
    public string implementationClassName;
}
