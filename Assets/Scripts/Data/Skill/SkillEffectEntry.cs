using System;
using UnityEngine;

[Serializable]
public class SkillEffectEntry
{
    [SerializeField] private SkillEffectType _type;
    [SerializeField] private float _value;
    [SerializeField] private SkillValueType _valueType;
    [SerializeField] private float _duration;

    public SkillEffectType Type => _type;
    public float Value => _value;
    public SkillValueType ValueType => _valueType;
    public float Duration => _duration;
}
