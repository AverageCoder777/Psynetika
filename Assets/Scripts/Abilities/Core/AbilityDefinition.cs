using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Psynetika/Ability", fileName = "Ability")]
public class AbilityDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public AbilitySlot slot = AbilitySlot.Regular;
    public float cooldown = 1f;
    public AnimationClip animClip;

    [Tooltip("Взять длительность стойки каста из длины animClip (castDuration тогда игнорируется)")]
    public bool matchDurationToClip = false;

    [Min(0.05f)]
    public float castDuration = 0.35f;

    [Range(0f, 1f)]
    public float castMomentNormalized = 0.5f;

    [SerializeReference, SubclassSelector]
    public List<AbilityNode> root = new();
}
