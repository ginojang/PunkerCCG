using RootNet;


public struct RegisterPlayerMessage : NetworkMessage
{
    public NetworkIdentity netId;
    public string name;
    public bool isHuman;
    public int[] deck;
}

public struct StartGameMessage : NetworkMessage
{
    public NetworkIdentity recipientNetId;
    public int playerIndex;
    public int turnDuration;
    public string[] nicknames;
    public NetPlayerInfo player;
    public NetPlayerInfo opponent;
    public int rngSeed;
}

public struct StartTurnMessage : NetworkMessage
{
    public NetworkIdentity recipientNetId;
    public bool isRecipientTheActivePlayer;
    public int turn;
    public NetPlayerInfo player;
    public NetPlayerInfo opponent;
}

public struct PlayerGameStateMessage : NetworkMessage
{
    public NetPlayerInfo player;
}

public struct OpponentGameStateMessage : NetworkMessage
{
    public NetPlayerInfo[] opponents;
}

public struct MoveCardMessage : NetworkMessage
{
    public NetworkIdentity playerNetId;
    public int cardInstanceId;
    public int originZoneId;
    public int destinationZoneId;
    public int[] targetInfo;
}

public struct CardMovedMessage : NetworkMessage
{
    public NetworkIdentity playerNetId;
    public NetCard card;
    public int originZoneId;
    public int destinationZoneId;
    public int[] targetInfo;
}

public struct SummonCardMessage : NetworkMessage
{
    public int cardInstanceId;
}

public struct PlayedCardMessage : NetworkMessage
{
    public NetCard card;
}

public struct FightPlayerMessage : NetworkMessage
{
    public NetworkIdentity attackingPlayerNetId;
    public int cardInstanceId;
}

public struct FightCreatureMessage : NetworkMessage
{
    public NetworkIdentity attackingPlayerNetId;
    public int attackingCardInstanceId;
    public int attackedCardInstanceId;
}

public struct PlayerAttackedMessage : NetworkMessage
{
    public NetworkIdentity attackingPlayerNetId;
    public int attackingCardInstanceId;
}

public struct CreatureAttackedMessage : NetworkMessage
{
    public NetworkIdentity attackingPlayerNetId;
    public int attackingCardInstanceId;
    public int attackedCardInstanceId;
}

public struct EndGameMessage : NetworkMessage
{
    public NetworkIdentity winnerPlayerIndex;
}

public struct EndTurnMessage : NetworkMessage
{
    public NetworkIdentity recipientNetId;
    public bool isRecipientTheActivePlayer;
}

public struct StopTurnMessage : NetworkMessage
{
}

public struct MoveCardsMessage : NetworkMessage
{
    public NetworkIdentity recipientNetId;
    public string originZone;
    public string destinationZone;
    public int numCards;
}

public struct ActivateAbilityMessage : NetworkMessage
{
    public NetworkIdentity playerNetId;
    public int zoneId;
    public int cardInstanceId;
    public int abilityIndex;
}

public struct PlayerDrewCardsMessage : NetworkMessage
{
    public NetworkIdentity playerNetId;
    public NetCard[] cards;
}

public struct OpponentDrewCardsMessage : NetworkMessage
{
    public NetworkIdentity playerNetId;
    public int numCards;
}
