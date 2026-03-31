// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections.Generic;

namespace CCGKit
{
    /// <summary>
    /// The general properties of a game.
    /// </summary>
    public class GameProperties
    {
        /// <summary>
        /// Duration of a game turn (in seconds).
        /// </summary>
        public int turnDuration;            
        //턴 시간 제한, 실시간 턴 제한 시스템, 타이머 기반 턴 종료 가능성

        /// <summary>
        /// Minimum number of cards that need to be in a deck.
        /// </summary>
        public int minDeckSize;

        /// <summary>
        /// Maximum number of cards that can be in a deck.
        /// </summary>
        public int maxDeckSize;

        //덱 구성 규칙, 최소/최대 카드 수 제한.  덱 빌딩 룰 정의

        /// <summary>
        /// List of actions to perform when a game starts.
        /// </summary>
        public List<GameAction> gameStartActions = new List<GameAction>();

        /// <summary>
        /// List of actions to perform when a turn starts.
        /// </summary>
        public List<GameAction> turnStartActions = new List<GameAction>();

        /// <summary>
        /// List of actions to perform when a turn ends.
        /// </summary>
        public List<GameAction> turnEndActions = new List<GameAction>();

        /// <summary>
        /// List of end game conditions of this game.
        /// </summary>
        public List<EndGameCondition> endGameConditions = new List<EndGameCondition>();
    }
}


/*=====================================================================================================
 * 
{
    "turnDuration": 30,
    "minDeckSize": 25,
    "maxDeckSize": 35,
    "gameStartActions": [anffh
        {
            "zoneId": 0,
            "name": "Shuffle cards",
            "target": "AllPlayers",
            "$type": "CCGKit.ShuffleCardsAction"
        },
        {
            "originZoneId": 0,
            "destinationZoneId": 1,
            "numCards": 5,
            "name": "Move cards",
            "target": "AllPlayers",
            "$type": "CCGKit.MoveCardsAction"
        }
    ],
    "turnStartActions": [
        {
            "statId": 1,
            "value": {
                "$type": "CCGKit.TurnNumberValue"
            },
            "name": "Set player stat",
            "target": "CurrentPlayer",
            "$type": "CCGKit.SetPlayerStatAction"
        },
        {
            "originZoneId": 0,
            "destinationZoneId": 1,
            "numCards": 1,
            "name": "Move cards",
            "target": "CurrentPlayer",
            "$type": "CCGKit.MoveCardsAction"
        }
    ],
    "turnEndActions": [],
    "endGameConditions": [
        {
            "statId": 0,
            "op": "LessThanOrEqualTo",
            "value": 0,
            "type": "Loss",
            "$type": "CCGKit.PlayerStatEndGameCondition"
        },
        {
            "zoneId": 0,
            "op": "LessThanOrEqualTo",
            "value": 0,
            "type": "Loss",
            "$type": "CCGKit.CardsInZoneEndGameCondition"
        }
    ]
}

 
 * 
 =====================================================================================================*/

