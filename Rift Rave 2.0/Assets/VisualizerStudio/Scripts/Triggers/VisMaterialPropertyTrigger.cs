using System;
using UnityEngine;

/// <summary>
/// This trigger is used to set a float material
/// property as a reaction to changes in the music.
/// </summary>
[AddComponentMenu("Visualizer Studio/Triggers/Material Property Trigger")]
public class VisMaterialPropertyTrigger : VisBasePropertyTrigger 
{
	#region Defaults Static Class
	
	/// <summary>
    /// This internal class holds all of the defaults of the VisMaterialPropertyTrigger class. 
	/// </summary>
	public static new class Defaults
	{
        public const string targetProperty = "";
        public const bool applyToMaterialIndex = false;
        public const int materialIndex = 0;
	}
	
	#endregion
	
	#region Public Member Variables
	
	/// <summary>
	/// This is the target property to modify on the parent game object. 
	/// As an example of how this works:
	/// minControllerValue = 0.2
	/// maxControllerValue = 0.8
	/// minPropertyValue = 2.0
	/// maxPropertyValue = 4.0
	/// invertValue = false
	/// 
	/// controllerInputValue = 0.5 -- newPropertyValue = 3.0
	/// controllerInputVlaue = 0.35 -- newPropertyValue = 2.5
    /// </summary>
    //[HideInInspector()]
    public string targetProperty = Defaults.targetProperty;

    /// <summary>
    /// This indicates if this material property modifier should apply 
    /// to a material in a specific index, or the main material.
    /// </summary>
    //[HideInInspector()]
    public bool applyToMaterialIndex = false;

    /// <summary>
    /// This is the material index to apply this property change to.
    /// </summary>
    //[HideInInspector()]
    public int materialIndex = 0;

    #endregion

    #region Private Members

    /// <summary>
    /// This indicates if there was a special target property name entrered for this material property trigger.
    /// Valid values are ColorR, ColorRed, ColorG, ColorGreen, ColorB, ColorBlue, ColorA, ColorAlpha, and Color.
    /// </summary>
    private bool specialTargetProperty = false;

    #endregion
	
	#region Init/Deinit Functions
	
	/// <summary>
	/// This is callled when this commponent is reset. 
	/// </summary>
	public override void Reset()
	{
		base.Reset();

        targetProperty = Defaults.targetProperty;
        applyToMaterialIndex = Defaults.applyToMaterialIndex;
        materialIndex = Defaults.materialIndex;
	}
	
	/// <summary>
	/// This is called when this component is started. 
	/// </summary>
	public override void Start() 
	{
        base.Start();

        //check if this is using a special value
        string lowerCaseTargetProperty = targetProperty.ToLower();
        if (lowerCaseTargetProperty == "colorr" ||
            lowerCaseTargetProperty == "colorred" ||
            lowerCaseTargetProperty == "colorg" ||
            lowerCaseTargetProperty == "colorgreen" ||
            lowerCaseTargetProperty == "colorb" ||
            lowerCaseTargetProperty == "colorblue" ||
            lowerCaseTargetProperty == "colora" ||
            lowerCaseTargetProperty == "coloralpha" ||
            lowerCaseTargetProperty == "color")
            specialTargetProperty = true;
        else
            specialTargetProperty = false;
	}
	
	#endregion

    #region VisBasePropertyTrigger Implementation

    /// <summary>
    /// This function is used to call into sub classes of
    /// VisBasePropertyTrigger, in order for them to set their
    /// target property to the new value specified.
    /// </summary>
    /// <param name="propertyValue">The new value to set the property to.</param>
    public override void SetProperty(float propertyValue)
    {
        //create var to store target material
        Material targetMaterial = null;
        Renderer rendererComp = GetComponent<Renderer>();
                
        if (applyToMaterialIndex &&
            rendererComp != null &&
            materialIndex >= 0 &&
            materialIndex < rendererComp.materials.Length)
        {//get indexed material as normal material
            targetMaterial = rendererComp.materials[materialIndex];
        }
        else if (rendererComp != null &&
                 rendererComp.material != null)
        {//get main material as procedural material
            targetMaterial = rendererComp.material;
        }

        //make sure a target material was found
        if (targetMaterial != null)
        {//check if there is a special target property set
            if (specialTargetProperty)
            {
                //check for special float
                switch (targetProperty.ToLower())
                {
                    case "colorr":
                    case "colorred":
                        float clampedValue = Mathf.Clamp(propertyValue, 0.0f, 1.0f);
                        Color newColor = new Color(clampedValue,
                                                   targetMaterial.color.g,
                                                   targetMaterial.color.b,
                                                   targetMaterial.color.a);
                        targetMaterial.color = newColor;
                        break;
                    case "colorg":
                    case "colorgreen":
                        clampedValue = Mathf.Clamp(propertyValue, 0.0f, 1.0f);
                        newColor = new Color(targetMaterial.color.r,
                                             clampedValue,
                                             targetMaterial.color.b,
                                             targetMaterial.color.a);
                        targetMaterial.color = newColor;
                        break;
                    case "colorb":
                    case "colorblue":
                        clampedValue = Mathf.Clamp(propertyValue, 0.0f, 1.0f);
                        newColor = new Color(targetMaterial.color.r,
                                             targetMaterial.color.g,
                                             clampedValue,
                                             targetMaterial.color.a);
                        targetMaterial.color = newColor;
                        break;
                    case "colora":
                    case "coloralpha":
                        clampedValue = Mathf.Clamp(propertyValue, 0.0f, 1.0f);
                        newColor = new Color(targetMaterial.color.r,
                                             targetMaterial.color.g,
                                             targetMaterial.color.b,
                                             clampedValue);
                        targetMaterial.color = newColor;
                        break;
                    case "color":
                        clampedValue = Mathf.Clamp(propertyValue, 0.0f, 1.0f);
                        newColor = new Color(clampedValue,
                                             clampedValue,
                                             clampedValue,
                                             targetMaterial.color.a);
                        targetMaterial.color = newColor;
                        break;
                    default:
                        //set float
                        targetMaterial.SetFloat(targetProperty, propertyValue);
                        break;
                }
            }
            else
            {
                //set float
                targetMaterial.SetFloat(targetProperty, propertyValue);
            }
        }
    }

    #endregion
}

