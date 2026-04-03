using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CCGKit;


public class Player : MonoBehaviour
{
    public bool isLocalPlayer;

    public bool isActivePlayer;

    public bool isHuman;


    protected PlayerInfo playerInfo = new PlayerInfo();
    protected PlayerInfo opponentInfo = new PlayerInfo();

    /// True if the game has started; false otherwise.
    protected bool gameStarted;

    /// Index of this player in the game.    
    protected int playerIndex;

    /// This game's turn duration (in seconds).
    protected int turnDuration;

    protected EffectSolver effectSolver;


    public PlayerInfo GetPlayerInfo()
    {
        return playerInfo;
    }

    public PlayerInfo GetOpponentInfo()
    {
        return opponentInfo;
    }


    protected virtual void Awake()
    {
    }

    protected virtual void Start()
    {
    }

    public virtual void OnStartGame(StartGameMessage msg)
    {

    }
    public virtual void OnEndGame(EndGameMessage msg)
    {

    }

    public virtual void OnStartTurn(StartTurnMessage msg)
    {

    }

    public virtual void OnEndTurn(EndTurnMessage msg)
    {
    }

    public virtual void StopTurn()
    {
    }


    public virtual void OnCardMoved(CardMovedMessage msg)
    {

    }

    public virtual void OnPlayerAttacked(PlayerAttackedMessage msg)
    {

    }

    public virtual void OnCreatureAttacked(CreatureAttackedMessage msg)
    {

    }

    public virtual void OnPlayerDrewCards(PlayerDrewCardsMessage msg)
    {

    }

    public virtual void OnOpponentDrewCards(OpponentDrewCardsMessage msg)
    {

    }


    //
    public void FightPlayer(int cardInstanceId)
    {
    }

    public void FightCreature(RuntimeCard attackingCard, RuntimeCard attackedCard)
    {
    }


    public void PlayCard(RuntimeCard card, List<int> targetInfo = null)
    {
        var libraryCard = GameManager.Instance.config.GetCard(card.cardId);
        PayResourceCosts(libraryCard.costs.ConvertAll(cost => cost as PayResourceCost));
        SendMoveCardMessage(card, targetInfo);
    }


    public void PayResourceCosts(List<PayResourceCost> costs)
    {
        costs.ForEach(cost => {
            if (cost != null)
            {
                playerInfo.stats[cost.statId].baseValue -= cost.value;
            }
        });
    }


    public void SendMoveCardMessage(RuntimeCard card, List<int> targetInfo = null)
    {
        // GINO CHECK
        /*
        var msg = new MoveCardMessage();
        msg.playerNetId = netIdentity;
        msg.cardInstanceId = card.instanceId;
        msg.originZoneId = playerInfo.namedZones["Hand"].zoneId;
        msg.destinationZoneId = playerInfo.namedZones["Board"].zoneId;
        if (targetInfo != null)
        {
            msg.targetInfo = targetInfo.ToArray();
        }
        NetworkClient.Send<MoveCardMessage>(msg);*/
    }


    ///  아래 네트웍 behavior

    /// <summary>Like Start(), but only called on server and host.</summary>
    public virtual void OnStartServer() { }

    /// <summary>Stop event, only called on server and host.</summary>
    public virtual void OnStopServer() { }

    /// <summary>Like Start(), but only called on client and host.</summary>
    public virtual void OnStartClient() { }

    /// <summary>Stop event, only called on client and host.</summary>
    public virtual void OnStopClient() { }

    /// <summary>Like Start(), but only called on client and host for the local player object.</summary>
    public virtual void OnStartLocalPlayer() { }

    /// <summary>Stop event, but only called on client and host for the local player object.</summary>
    public virtual void OnStopLocalPlayer() { }

    /// <summary>Like Start(), but only called for objects the client has authority over.</summary>
    public virtual void OnStartAuthority() { }

    /// <summary>Stop event, only called for objects the client has authority over.</summary>
    public virtual void OnStopAuthority() { }
}
