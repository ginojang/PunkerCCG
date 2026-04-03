public struct NetModifier
{
    public int value;
    public int duration;
}

public struct NetStat
{
    public int statId;
    public int baseValue;
    public int originalValue;
    public int minValue;
    public int maxValue;
    public NetModifier[] modifiers;
}