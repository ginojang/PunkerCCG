using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CCGKit;


public class Player : MonoBehaviour
{
    public bool isLocalPlayer;

    public bool isActivePlayer;

    public bool isHuman;


    protected GameState gameState = new GameState();
    protected PlayerInfo playerInfo = new PlayerInfo();
    protected PlayerInfo opponentInfo = new PlayerInfo();

    /// True if the game has started; false otherwise.
    protected bool gameStarted;

    /// Index of this player in the game.    
    protected int playerIndex;

    /// This game's turn duration (in seconds).
    protected int turnDuration;

    protected EffectSolver effectSolver;

    protected int currentTurn;

    protected int currentPlayerIndex;


    public int randomSeed;

    //
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

    public virtual void OnStartLocalPlayer()
    {
        // 게임에 대한 전체 Rule Base 세팅
        LoadPlayerStates();


        // 우선 첫번째 덱을 기본 덱으로 세팅 (임시)
        var defaultDeckIndex = 0;
        LoadDefaultDeck(defaultDeckIndex);

        // 서버 클래스 게임 시작 함수 수행 (임시)
        StartGame();

    }

    void LoadDefaultDeck(int defaultDeckIndex)
    {
        var decks = GameNetworkManager.Instance.playerDecks;
        var msgDefaultDeck = new List<int>();
        if (decks.Count > 0)
        {
            //var defaultDeckIndex = isHuman ? PlayerPrefs.GetInt("default_deck") : PlayerPrefs.GetInt("default_ai_deck");
            var defaultDeck = decks[defaultDeckIndex];
            for (var i = 0; i < defaultDeck.cards.Count; i++)
            {
                for (var j = 0; j < defaultDeck.cards[i].amount; j++)
                {
                    msgDefaultDeck.Add(defaultDeck.cards[i].id);
                }
            }

            GameManager.Instance.defaultDeck = defaultDeck;
        }
        else
        {
            var defaultDeck = GameManager.Instance.defaultDeck;
            for (var i = 0; i < defaultDeck.cards.Count; i++)
            {
                for (var j = 0; j < defaultDeck.cards[i].amount; j++)
                {
                    msgDefaultDeck.Add(defaultDeck.cards[i].id);
                }
            }
        }
    }

    void LoadPlayerStates()
    {
        var gameConfig = GameNetworkManager.Instance.config;
        foreach (var stat in gameConfig.playerStats)
        {
            var statCopy = new Stat();
            statCopy.statId = stat.id;
            statCopy.name = stat.name;
            statCopy.originalValue = stat.originalValue;
            statCopy.baseValue = stat.baseValue;
            statCopy.minValue = stat.minValue;
            statCopy.maxValue = stat.maxValue;
            playerInfo.stats[stat.id] = statCopy;
            playerInfo.namedStats[stat.name] = statCopy;
        }

        foreach (var stat in gameConfig.playerStats)
        {
            var statCopy = new Stat();
            statCopy.statId = stat.id;
            statCopy.name = stat.name;
            statCopy.originalValue = stat.originalValue;
            statCopy.baseValue = stat.baseValue;
            statCopy.minValue = stat.minValue;
            statCopy.maxValue = stat.maxValue;
            opponentInfo.stats[stat.id] = statCopy;
            opponentInfo.namedStats[stat.name] = statCopy;
        }

        foreach (var zone in gameConfig.gameZones)
        {
            var zoneCopy = new RuntimeZone();
            zoneCopy.zoneId = zone.id;
            zoneCopy.name = zone.name;
            if (zone.hasMaxSize)
            {
                zoneCopy.maxCards = zone.maxSize;
            }
            else
            {
                zoneCopy.maxCards = int.MaxValue;
            }
            playerInfo.zones[zone.id] = zoneCopy;
            playerInfo.namedZones[zone.name] = zoneCopy;
        }

        foreach (var zone in gameConfig.gameZones)
        {
            var zoneCopy = new RuntimeZone();
            zoneCopy.zoneId = zone.id;
            zoneCopy.name = zone.name;
            if (zone.hasMaxSize)
            {
                zoneCopy.maxCards = zone.maxSize;
            }
            else
            {
                zoneCopy.maxCards = int.MaxValue;
            }
            opponentInfo.zones[zone.id] = zoneCopy;
            opponentInfo.namedZones[zone.name] = zoneCopy;
        }
        gameState.players.Add(playerInfo);
        gameState.players.Add(opponentInfo);

        //
    }

    void StartGame()
    {
        // 임시.
        currentPlayerIndex = 0;

        Debug.Log("Game has started.");

        currentTurn = 1;

        var players = gameState.players;


        // Create an array with all the player nicknames.
        var playerNicknames = new List<string>(players.Count);
        foreach (var player in players)
        {
            playerNicknames.Add(player.nickname);
        }

        // Set the current player and opponents.
        gameState.currentPlayer = players[currentPlayerIndex];
        gameState.currentOpponent = players.Find(x => x != gameState.currentPlayer);

        randomSeed = System.Environment.TickCount;
        effectSolver = new EffectSolver(gameState, randomSeed);

        foreach (var player in players)
        {
            effectSolver.SetTriggers(player);
            foreach (var zone in player.zones)
            {
                foreach (var card in zone.Value.cards)
                {
                    effectSolver.SetDestroyConditions(card);
                    effectSolver.SetTriggers(card);
                }
            }
        }

        // Execute the game start actions.
        foreach (var action in GameManager.Instance.config.properties.gameStartActions)
        {
            ExecuteGameAction(action);
        }

    }


    protected void ExecuteGameAction(GameAction action)
    {
        var targetPlayers = new List<PlayerInfo>();
        switch (action.target)
        {
            case GameActionTarget.CurrentPlayer:
                targetPlayers.Add(gameState.currentPlayer);
                break;

            case GameActionTarget.CurrentOpponent:
                targetPlayers.Add(gameState.currentOpponent);
                break;

            case GameActionTarget.AllPlayers:
                targetPlayers = gameState.players;
                break;
        }

        foreach (var player in targetPlayers)
        {
            action.Resolve(gameState, player);
        }
    }

    // 임시 함수.
    public StartGameMessage BuildStarGameMessage()
    {
        var players = gameState.players;

        // 
        var player = players[currentPlayerIndex];
        var msg = new StartGameMessage();

        // GINO CHECK..
        //msg.recipientNetId = 0; // player.netId;
        msg.playerIndex = currentPlayerIndex;
        msg.turnDuration = turnDuration;

        var playerNicknames = new List<string>(players.Count);
        playerNicknames.Add("Gino");
        playerNicknames.Add("Jisu");


        // GINO CHECK -  싱글 모드에서는 이미 player Info 가지고 있기 때문에  안보낸다.

        msg.nicknames = playerNicknames.ToArray();
        //
        //msg.player = GetPlayerNetworkState(player);
        //msg.opponent = GetOpponentNetworkState(players.Find(x => x != player));
        //msg.rngSeed = rngSeed;


        return msg;

    }


    public virtual void OnStartGame(StartGameMessage msg)
    {
        gameStarted = true;

        //effectSolver = new EffectSolver(gameState, msg.rngSeed);
        effectSolver = new EffectSolver(gameState, randomSeed);
        effectSolver.SetTriggers(playerInfo);
        effectSolver.SetTriggers(opponentInfo);

        // GINO CHECK..  네트웍 모드에서는 msg의 player_info 내용으로 갱신한다.

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
        var libraryCard = GameNetworkManager.Instance.config.GetCard(card.cardId);
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


    /// <summary>Stop event, but only called on client and host for the local player object.</summary>
    public virtual void OnStopLocalPlayer() { }

    /// <summary>Like Start(), but only called for objects the client has authority over.</summary>
    public virtual void OnStartAuthority() { }

    /// <summary>Stop event, only called for objects the client has authority over.</summary>
    public virtual void OnStopAuthority() { }
}
