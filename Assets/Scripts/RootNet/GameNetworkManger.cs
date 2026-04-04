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

    //
    // 기존 GameState 부분 통합
    //public List<PlayerInfo> players = new List<PlayerInfo>();

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


    private void Start()
    {
        gameObject.GetComponent<DemoHumanPlayer>().OnStartLocalPlayer();

        gameObject.GetComponent<DemoHumanPlayer>().OnStartGame("Gino", "Jisu");

    }


}
