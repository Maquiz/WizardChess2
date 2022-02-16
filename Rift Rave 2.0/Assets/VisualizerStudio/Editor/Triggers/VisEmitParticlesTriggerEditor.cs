using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(VisEmitParticlesTrigger))]
public class VisEmitParticlesTriggerEditor : VisBaseTriggerEditor 
{
    public VisEmitParticlesTriggerEditor()
    {
        showAutomaticInspectorGUI = false;
    }

    protected override void CustomInspectorGUI()
    {
#if !(UNITY_2_6 || UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_3 || UNITY_3_4)
        VisEmitParticlesTrigger trigger = target as VisEmitParticlesTrigger;
        if (trigger != null)
        {
            ParticleSystem particleSystemComp = trigger.GetComponentInChildren<ParticleSystem>();

            base.CustomInspectorGUI();

            if (particleSystemComp != null)
            {
                trigger.emitCount = EditorGUILayout.IntField("  Emit Count", trigger.emitCount);
                trigger.emitCountVariance = EditorGUILayout.IntField("  Emit Count Variance", trigger.emitCountVariance);
            }

            if (particleSystemComp != null)
            {
                EditorGUILayout.HelpBox("Using Particle System on GameObject: " + particleSystemComp.name, MessageType.Info);
            }


#if !(UNITY_2017_1_OR_NEWER)
            ParticleEmitter particleEmitterComp = trigger.GetComponentInChildren<ParticleEmitter>();

            if (particleEmitterComp != null)
            {
                EditorGUILayout.HelpBox("Using Legacy Particle System on GameObject: " + particleEmitterComp.name, MessageType.Info);
            }

            if (particleSystemComp == null && particleEmitterComp == null)
            {
                EditorGUILayout.HelpBox("No supported Particle Component found! Please add a ParticleSystem (Shruiken) or ParticleEmitter (Legacy) Component to this Game Object.", MessageType.Error);
            }
#endif
        }
#else
        base.CustomInspectorGUI();
#endif
    }
}