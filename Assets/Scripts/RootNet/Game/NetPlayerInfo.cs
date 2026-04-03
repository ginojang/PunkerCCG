using UnityEngine;


public struct NetStaticCard
{
    public int cardId;
    public int instanceId;
}

public struct NetStaticZone
{
    public int zoneId;
    public NetStaticCard[] cards;
    public int numCards;
}

public struct NetDynamicZone
{
    public int zoneId;
    public NetCard[] cards;
    public int numCards;
}

public struct NetPlayerInfo
{
    public int id;
    public NetworkIdentity netId;
    public int currentCardInstanceId;
    public NetStat[] stats;
    public NetStaticZone[] staticZones;
    public NetDynamicZone[] dynamicZones;
}