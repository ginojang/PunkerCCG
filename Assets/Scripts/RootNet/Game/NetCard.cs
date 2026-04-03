using UnityEngine;


public struct NetKeyword
{
    public int keywordId;
    public int valueId;
}


public struct NetCard
{
    public int cardId;
    public int instanceId;
    public NetStat[] stats;
    public NetKeyword[] keywords;
}