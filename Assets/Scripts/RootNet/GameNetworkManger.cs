using System.Collections.Generic;
using System.IO;
using FullSerializer;

using UnityEngine;
using UnityEngine.Assertions;

using CCGKit;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance;

    public bool IsSinglePlayer;

    //
    // 기존 GameManager 부분 통합.
    public GameConfiguration config = new GameConfiguration();

    public Deck defaultDeck;
    public List<Deck> playerDecks = new List<Deck>();

    public string playerName;
    public bool isPlayerLoggedIn;

    private fsSerializer serializer = new fsSerializer();


    //////////////////////////////////////////////////////////////////
    //
    // 기존 GameState 부분 통합
    public List<PlayerInfo> players = new List<PlayerInfo>();  // 링크용

    public PlayerInfo playerInfo = new PlayerInfo(); 
    public PlayerInfo opponentInfo = new PlayerInfo();

    public EffectSolver effectSolver;

    //
    // 기존 Player 부분 통합.  
    public bool isLocalPlayer;
    public bool isActivePlayer;
    public bool isHuman;

    public bool gameStarted;
    public int playerIndex;
    public int turnDuration;
    public int currentTurn;
    public int currentPlayerIndex;

    public int randomSeed;



    private void Awake()
    {
        Instance = this;
        turnDuration = 60;

        //
        config.LoadGameConfigurationAtRuntime();

        var decksPath = Application.persistentDataPath + "/decks.json";
        if (File.Exists(decksPath))
        {
            var file = new StreamReader(decksPath);
            var fileContents = file.ReadToEnd();
            var data = fsJsonParser.Parse(fileContents);
            object deserialized = null;
            serializer.TryDeserialize(data, typeof(List<Deck>), ref deserialized).AssertSuccessWithoutWarnings();
            file.Close();

            playerDecks = deserialized as List<Deck>;

            // 디폴트 덱은 게임 시작시 로비에서 전달해야 한다.  지금은 임시 0번 인덱스
            if (playerDecks.Count > 0)
            {
                defaultDeck = playerDecks[0];
            }
            else
            {
                // GINO TODO
                Assert.IsFalse(false);
            }
        }


        if (IsSinglePlayer == true)
        {
            BuildPlayerWithConfiguration(playerInfo);
            BuildPlayerWithConfiguration(opponentInfo);

            BuildPlayerMetadata();

            players.Add(playerInfo);
            players.Add(opponentInfo);

            BindEndGameConditions(playerInfo);
            BindEndGameConditions(opponentInfo);

            BuildDeck(playerInfo, defaultDeck);
            BuildDeck(opponentInfo, defaultDeck);  // 우선은 defaultDeck 사용
        }
        else
        {
            // GINO TODO
            Assert.IsFalse(false);
        }
    }

    void BuildPlayerWithConfiguration(PlayerInfo playerInfo)
    { 
        var gameConfig = config;
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
    }

    void BuildPlayerMetadata()
    {
        playerInfo.id = 0;
        playerInfo.nickname = string.IsNullOrWhiteSpace(playerName) ? "Gino" : playerName;
        playerInfo.isHuman = true;

        opponentInfo.id = 1;
        opponentInfo.nickname = "Jisu";
        opponentInfo.isHuman = false;
    }

    void BindEndGameConditions(PlayerInfo player)
    {
        foreach (var condition in config.properties.endGameConditions)
        {
            if (condition is PlayerStatEndGameCondition playerStatCondition)
            {
                var targetStat = player.stats[playerStatCondition.statId];
                targetStat.onValueChanged += (oldValue, newValue) =>
                {
                    if (playerStatCondition.IsTrue(player))
                    {
                        EndGame(player, playerStatCondition.type);
                    }
                };
            }
            else if (condition is CardsInZoneEndGameCondition cardsCondition)
            {
                var targetZone = player.zones[cardsCondition.zoneId];
                targetZone.onZoneChanged += value =>
                {
                    if (cardsCondition.IsTrue(player))
                    {
                        EndGame(player, cardsCondition.type);
                    }
                };
            }
        }
    }

    public void EndGame(PlayerInfo loser, EndGameType type)
    {
        if (!gameStarted)
            return;

        gameStarted = false;

        var winner = players.Find(x => x != loser);
        Debug.Log($"EndGame - loser: {loser.nickname}, winner: {winner?.nickname}, type: {type}");

        // TODO:
        // 1. currentTurn 정지
        // 2. UI 알림
        // 3. DemoHumanPlayer.OnEndGame(...) 연결
    }

    void BuildDeck(PlayerInfo player, Deck sourceDeck)
    {
        if (sourceDeck == null)
        {
            Debug.LogError("BuildDeck failed: sourceDeck is null.");
            return;
        }

        var deckZone = player.namedZones["Deck"];
        if (deckZone == null)
        {
            Debug.LogError("BuildDeck failed: Deck zone not found.");
            return;
        }

        // 혹시 재초기화될 수 있으니 한번 정리
        deckZone.cards.Clear();
        deckZone.numCards = 0;

        foreach (var deckEntry in sourceDeck.cards)
        {
            for (int i = 0; i < deckEntry.amount; i++)
            {
                var runtimeCard = new RuntimeCard();
                runtimeCard.cardId = deckEntry.id;
                runtimeCard.instanceId = player.currentCardInstanceId++;
                runtimeCard.ownerPlayer = player;

                var libraryCard = config.GetCard(deckEntry.id);
                if (libraryCard == null)
                {
                    Debug.LogError($"BuildDeck failed: library card not found. cardId={deckEntry.id}");
                    continue;
                }

                // Stat 복사
                foreach (var stat in libraryCard.stats)
                {
                    var statCopy = new Stat();
                    statCopy.statId = stat.statId;
                    statCopy.name = stat.name;
                    statCopy.originalValue = stat.originalValue;
                    statCopy.baseValue = stat.baseValue;
                    statCopy.minValue = stat.minValue;
                    statCopy.maxValue = stat.maxValue;

                    runtimeCard.stats[stat.statId] = statCopy;
                    runtimeCard.namedStats[stat.name] = statCopy;
                }

                // Keyword 복사
                foreach (var keyword in libraryCard.keywords)
                {
                    var keywordCopy = new RuntimeKeyword();
                    keywordCopy.keywordId = keyword.keywordId;
                    keywordCopy.valueId = keyword.valueId;
                    runtimeCard.keywords.Add(keywordCopy);
                }

                // 초기 세팅 단계이므로 이벤트 안 태우고 직접 추가
                deckZone.cards.Add(runtimeCard);
            }
        }

        // cards.Count와 numCards를 맞춤
        deckZone.numCards = deckZone.cards.Count;
    }


    private void Start()
    {
        gameObject.GetComponent<DemoHumanPlayer>().InitializePlayers();

        randomSeed = System.Environment.TickCount;
        effectSolver = new EffectSolver(randomSeed);

        //
        effectSolver.SetTriggers(playerInfo);
        foreach (var zone in playerInfo.zones)
        {
            foreach (var card in zone.Value.cards)
            {
                effectSolver.SetDestroyConditions(card);
                effectSolver.SetTriggers(card);
            }
        }

        //
        effectSolver.SetTriggers(opponentInfo);
        foreach (var zone in opponentInfo.zones)
        {
            foreach (var card in zone.Value.cards)
            {
                effectSolver.SetDestroyConditions(card);
                effectSolver.SetTriggers(card);
            }
        }

        if (IsSinglePlayer == true)
        {
            Invoke("StartGame", 1.0f);
        }
        else
        {
            // GINO TODO
            Assert.IsFalse(false);
        }
    }

    void StartGame()
    {
        gameObject.GetComponent<DemoHumanPlayer>().OnStartGame(playerInfo.nickname, opponentInfo.nickname);

        // 임시 테스트
        StartTurn_LocalPlayerOnly();
    }

    public void StartTurn_LocalPlayerOnly()
    {
        currentTurn += 1;
        currentPlayerIndex = 0;
        isActivePlayer = true;

        playerInfo.numTurn += 1;

        foreach (var action in config.properties.turnStartActions)
        {
            gameObject.GetComponent<DemoHumanPlayer>().ExecuteGameAction(action);
        }

        PerformTurnStartStateInitialization();

        effectSolver.OnTurnStarted();

        var msg = new StartTurnMessage();
        msg.isRecipientTheActivePlayer = true;
        msg.turn = currentTurn;

        gameObject.GetComponent<DemoHumanPlayer>().OnStartTurn(msg);
    }


    void PerformTurnStartStateInitialization()
    {
        // GINO TODO
    }

}
