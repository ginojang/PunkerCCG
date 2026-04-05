// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

//using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = System.Random;

namespace CCGKit
{
    /// <summary>
    /// This class is responsible for resolving all the effects that take place in a game.
    /// There is an effect solver on the server side and another one on the client side. The
    /// goal of this duplicity is to allow for lag-free gameplay: the logic is always evaluated
    /// locally first so that clients do not need to wait for the server to present its results
    /// on the screen. The client's game state is still always synchronized with that of the
    /// server; it just happens to be executed locally first too.
    /// </summary>
    public class EffectSolver
    {
        /// <summary>
        /// The current state of the game.
        /// </summary>
        //public GameState gameState;

        /// <summary>
        /// The random number generator of the game.
        /// </summary>
        public Random rng;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameState">The state of the game.</param>
        /// <param name="rngSeed">The random number generator's seed.</param>
        public EffectSolver(int rngSeed)
        {
            //this.gameState = gameState;
            //this.gameState.config = GameManager.Instance.config;
            //this.gameState.effectSolver = this;
            rng = new Random(rngSeed);
        }

        /// <summary>
        /// This method is automatically called when the turn starts.
        /// </summary>
        public void OnTurnStarted()
        {
            foreach (var zone in GameNetworkManager.Instance.playerInfo.zones)
            {
                var zoneDefinition = GameNetworkManager.Instance.config.gameZones.Find(x => x.id == zone.Value.zoneId);
                if (zoneDefinition.type == ZoneType.Dynamic && zoneDefinition.opponentVisibility == ZoneOpponentVisibility.Visible)
                {
                    // 이 존은 상대에게 보여야 한다는 뜻이다.
                    // 이 조건 때문에 보통:     Board는 포함,  Hand는 제외 ,  Deck은 제외 가 될 가능성이 높다.
                    
                    // “턴 시작 트리거는 상대에게 공개된 동적 존의 카드들만 검사한다”
                    foreach (var card in zone.Value.cards)
                    {
                        // 이 카드가 가진 triggered ability 중에서 “플레이어 턴 시작 시 발동하는 트리거”만 찾아 실행해라
                        //
                        TriggerEffect<OnPlayerTurnStartedTrigger>(GameNetworkManager.Instance.playerInfo, card, x => { return true; });
                    }
                }
            }
        }

        /// <summary>
        /// This method is automatically called when the turn ends.
        /// </summary>
        public void OnTurnEnded()
        {
            foreach (var zone in GameNetworkManager.Instance.playerInfo.zones)
            {
                var zoneDefinition = GameNetworkManager.Instance.config.gameZones.Find(x => x.id == zone.Value.zoneId);
                if (zoneDefinition.type == ZoneType.Dynamic && zoneDefinition.opponentVisibility == ZoneOpponentVisibility.Visible)
                {
                    foreach (var card in zone.Value.cards)
                    {
                        TriggerEffect<OnPlayerTurnEndedTrigger>(GameNetworkManager.Instance.playerInfo, card, x => { return true; });
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the combat between the specified card and its opponent player.
        /// </summary>
        /// <param name="attackingPlayerNetId">The network identifier of the attacking player.</param>
        /// <param name="attackingCardInstanceId">The instance identifier of the attacking card.</param>
        public void FightPlayer(NetworkIdentity attackingPlayerNetId, int attackingCardInstanceId)
        {
            var attackingPlayer = GameNetworkManager.Instance.players.Find(x => x.netId == attackingPlayerNetId);
            var attackedPlayer = GameNetworkManager.Instance.players.Find(x => x.netId != attackingPlayerNetId);
            if (attackingPlayer != null && attackedPlayer != null)
            {
                var board = attackingPlayer.namedZones["Board"];
                var card = board.cards.Find(x => x.instanceId == attackingCardInstanceId);
                if (card != null)
                {
                    attackedPlayer.namedStats["Life"].baseValue -= card.namedStats["Attack"].effectiveValue;
                }
            }
        }

        /// <summary>
        /// Resolves the combat between the specified creatures.
        /// </summary>
        /// <param name="attackingPlayerNetId">The network identifier of the attacking player.</param>
        /// <param name="attackingCreature">The attacking creature.</param>
        /// <param name="attackedCreature">The attacked creature.</param>
        public void FightCreature(NetworkIdentity attackingPlayerNetId, RuntimeCard attackingCreature, RuntimeCard attackedCreature)
        {
            var attackingPlayer = GameNetworkManager.Instance.players.Find(x => x.netId == attackingPlayerNetId);
            var attackedPlayer = GameNetworkManager.Instance.players.Find(x => x.netId != attackingPlayerNetId);
            if (attackingPlayer != null && attackedPlayer != null)
            {
                attackedCreature.namedStats["Life"].baseValue -= attackingCreature.namedStats["Attack"].effectiveValue;
                attackingCreature.namedStats["Life"].baseValue -= attackedCreature.namedStats["Attack"].effectiveValue;
            }
        }

        /// <summary>
        /// Moves the specified card from the specified origin zone to the specified destination zone.
        /// </summary>
        /// <param name="playerNetId">The network identifier of the card's owner player.</param>
        /// <param name="card">The card to move.</param>
        /// <param name="originZone">The origin zone.</param>
        /// <param name="destinationZone">The destination zone.</param>
        /// <param name="msgTargetInfo">The optional target information.</param>
        public void MoveCard(NetworkIdentity playerNetId, RuntimeCard card, string originZone, string destinationZone, int[] msgTargetInfo = null)
        {
            var player = GameNetworkManager.Instance.players.Find(x => x.netId == playerNetId);
            if (player != null)
            {
                List<int> targetInfo = null;
                if (msgTargetInfo != null)
                {
                    targetInfo = new List<int>(msgTargetInfo);
                }

                player.namedZones[originZone].RemoveCard(card);
                player.namedZones[destinationZone].AddCard(card);
                TriggerEffect<OnCardLeftZoneTrigger>(player, card, x => { return x.IsTrue(originZone); }, targetInfo);
                TriggerEffect<OnCardEnteredZoneTrigger>(player, card, x => { return x.IsTrue(destinationZone); }, targetInfo);

                var libraryCard = GameNetworkManager.Instance.config.GetCard(card.cardId);
                var cardType = GameNetworkManager.Instance.config.cardTypes.Find(x => x.id == libraryCard.cardTypeId);
                if (cardType.moveAfterTriggeringEffect)
                {
                    var finalDestinationZone = GameNetworkManager.Instance.config.gameZones.Find(x => x.id == cardType.zoneId);
                    // We do not use the MoveCards function here, because we do not want to trigger any effects
                    // (which would cause an infinite recursion).
                    player.namedZones[destinationZone].RemoveCard(card);
                    player.namedZones[finalDestinationZone.name].AddCard(card);
                }
            }
        }

        /// <summary>
        /// Draws the specified number of cards from the deck into the hand.
        /// </summary>
        /// <param name="playerNetId">The network identifier of the card's owner player.</param>
        /// <param name="numCards">The number of cards to draw.</param>
        /// <param name="targetInfo">The optional target information.</param>
        public void DrawCards(NetworkIdentity playerNetId, int numCards, List<int> targetInfo = null)
        {
            var player = GameNetworkManager.Instance.players.Find(x => x.netId == playerNetId);
            if (player != null)
            {
                var deck = player.namedZones["Deck"];
                if (deck.cards.Count > 0)
                {
                    var cards = deck.cards.GetRange(0, numCards);
                    deck.RemoveCards(numCards);
                    player.namedZones["Hand"].cards.AddRange(cards);

                    var serverGo = GameObject.Find("Server");
                    if (serverGo != null && serverGo.activeSelf)
                    {
                        // GINO CHECK
                        /*
                        var server = serverGo.GetComponent<Server>();
                        var msg = new PlayerDrewCardsMessage();
                        msg.playerNetId = player.netId;
                        var netCards = new List<NetCard>();
                        foreach (var card in cards)
                        {
                            var netCard = NetworkingUtils.GetNetCard(card);
                            netCards.Add(netCard);
                        }
                        msg.cards = netCards.ToArray();
                        server.SafeSendToClient(player, msg);

                        var opponent = gameState.players.Find(x => x != player);
                        var oppMsg = new OpponentDrewCardsMessage();
                        oppMsg.playerNetId = opponent.netId;
                        oppMsg.numCards = cards.Count;
                        server.SafeSendToClient(opponent, oppMsg);
                        */
                    }
                }
            }
        }



        // 패시브 (자동발동)
        /*
            이벤트 기반, 조건 만족 시 자동 실행, 플레이어 입력 없음

            예:

            “내 턴 시작 시 카드 1장 뽑기”
            “피해를 받으면 공격력 +1”
            “카드가 죽으면 상대에게 2 데미지”
         */

        /// <summary>
        /// Triggers the triggered effects of the specified card.
        /// </summary>
        /// <typeparam name="T">The type of the trigger.</typeparam>
        /// 즉 T 자리에 트리거 타입을 넣어서 재사용한다.
        /*
        /// 예를 들면:

                OnPlayerTurnStartedTrigger
                OnPlayerTurnEndedTrigger
                OnCardEnteredZoneTrigger
                OnPlayerStatIncreasedTrigger

                이런 식이다. 즉 이 함수는 모든 triggered effect를 한 함수로 처리하기 위한 공통 엔진이다. */
        /// 
        /// <param name="player">The owner player of the card that is triggering the effect.</param>
        /// <param name="card">The card that is triggering the effect.</param>
        /// <param name="predicate">The predicate that needs to be satisfied in order to trigger the effect.</param>
        /// <param name="targetInfo">The optional target information.</param>
        public void TriggerEffect<T>(PlayerInfo player, RuntimeCard card, Predicate<T> predicate, List<int> targetInfo = null) where T : Trigger
        {
            /*
             * 이건 런타임 카드(RuntimeCard)에서 원본 카드 정의(Card)를 다시 찾는 부분이다.

                왜 필요하냐면, 런타임 카드에는:

                현재 stats
                현재 keywords
                같은 상태는 있지만,

                어떤 ability들이 붙어 있는지는 원본 카드 정의를 봐야 하기 때문이다.
    
                즉:         RuntimeCard = 현재 상태
                            libraryCard = 능력 정의서    >>  이렇게 역할이 나뉜다.
             */
            var libraryCard = GameNetworkManager.Instance.config.GetCard(card.cardId);


            /*
             * 카드의 전체 ability 목록 중에서        TriggeredAbility만 필터링한다.

                    즉 activated ability는 여기서 안 본다.
                    이 함수는 이름 그대로 triggered effect 전용이다.

                    예를 들어 카드에 능력이 3개 있어도:

                    activated 1개
                    triggered 2개

                    라면 여기서는 triggered 2개만 본다.
             */
            var triggeredAbilities = libraryCard.abilities.FindAll(x => x is TriggeredAbility);
            foreach (var ability in triggeredAbilities)
            {
                /*
                 * 카드 한 장이 여러 triggered ability를 가질 수 있다는 뜻이다.

                        예:

                        “내 턴 시작 시 +1 공격력”
                        “체력이 감소하면 카드 1장 뽑기”

                        이 둘이 같은 카드에 붙어 있을 수도 있다.
                 */


                /*
                 * triggeredAbility.trigger를 지금 함수의 제네릭 타입 T로 캐스팅한다.

                        예를 들어 이 함수가:   TriggerEffect<OnPlayerTurnStartedTrigger>(...)

                로 불렸다면, OnPlayerTurnStartedTrigger인 trigger는 살아남고, 다른 타입 trigger는 null이 된다
                
                즉 이 줄이 사실상:     “지금 발생한 이벤트 타입과 이 카드 능력의 trigger 타입이 맞는가?” 를 검사하는 부분이다.
                 */
                var triggeredAbility = ability as TriggeredAbility;
                var trigger = triggeredAbility.trigger as T;
                if (trigger != null && predicate(trigger) == true)
                {

                    //  PlayerEffect 처리            
                    /*
                     이건 effect가 플레이어를 대상으로 하는 효과일 때다.

                    예:
                            플레이어 체력 회복, 마나 증가, 상대에게 피해, 플레이어 버프

                    흐름은:
                            이 effect가 현재 타겟을 가질 수 있는지 검사.  실제 플레이어 타겟 목록 계산. 각 타겟에 대해 Resolve() 실행

                    즉 이 부분은: PlayerEffect용 타겟 계산 + 실행 루프 다.
                     */
                    if (triggeredAbility.effect is PlayerEffect && AreTargetsAvailable(triggeredAbility.effect, card, triggeredAbility.target))
                    {
                        var targets = GetPlayerTargets(player, triggeredAbility.target, targetInfo);
                        foreach (var t in targets)
                        {
                            (triggeredAbility.effect as PlayerEffect).Resolve(t);
                        }
                    }


                    // CardEffect 처리
                    /*
                     이건 effect가 카드를 대상으로 하는 경우다.

                    예:
                        카드 공격력 증가, 카드 체력 감소, 특정 카드 파괴, 특정 타입 카드 강화
                        여기서는 GetCardTargets(...)를 쓴다.
                        중요한 포인트는 이 함수가 추가로:
                        gameZoneId, cardTypeId
                        까지 받아서 필터링한다는 점이다.

                    즉:
                    어느 존에서 찾을지 어떤 타입 카드만 대상으로 할지. 같은 룰이 들어간다.
                     */
                    else if (triggeredAbility.effect is CardEffect && AreTargetsAvailable(triggeredAbility.effect, card, triggeredAbility.target))
                    {
                        var cardEffect = triggeredAbility.effect as CardEffect;
                        var targets = GetCardTargets(player, card, triggeredAbility.target, cardEffect.gameZoneId, cardEffect.cardTypeId, targetInfo);
                        foreach (var t in targets)
                        {
                            (triggeredAbility.effect as CardEffect).Resolve(t);
                        }
                    }


                    // MoveCardEffect 처리
                    /*
                     이건 카드 이동 효과다.

                    예:
                        카드 한 장을 Hand로 되돌림, Board에서 Graveyard로 보냄, Deck에서 Hand로 가져옴

                        이것도 타겟은 카드니까 GetCardTargets()를 쓰지만, 의미는 단순 stat 변경이 아니라 zone 이동이다.

                        즉 CardEffect와 비슷해 보이지만, 실제 실행은 더 강한 효과다.
                     */
                    else if (triggeredAbility.effect is MoveCardEffect && AreTargetsAvailable(triggeredAbility.effect, card, triggeredAbility.target))
                    {
                        var moveCardEffect = triggeredAbility.effect as MoveCardEffect;
                        var targets = GetCardTargets(player, card, triggeredAbility.target, moveCardEffect.originGameZoneId, moveCardEffect.cardTypeId, targetInfo);
                        foreach (var t in targets)
                        {
                            (triggeredAbility.effect as MoveCardEffect).Resolve(t);
                        }
                    }
                }
            }
        }

        /*
         * 패시브>>.
         
        Activated Ability (수동 발동)

                플레이어가 선택해서 사용, 보통 비용(cost)이 있음, 타겟 선택 필요할 수도 있음

                예:
                “2 마나: 카드 1장 뽑기”
                “이 카드를 희생: 적 카드 파괴”
                “클릭 시 공격력 +2”
         */

        /// <summary>
        /// Activates the specified ability of the specified card.
        /// </summary>
        /// <param name="player">The owner player of the card that is activating the effect.</param>
        /// <param name="card">The card that is activating the effect.</param>
        /// <param name="abilityIndex">The index of the ability to activate.</param>
        /// <param name="targetInfo">The optional target information.</param>
        public void ActivateAbility(PlayerInfo player, RuntimeCard card, int abilityIndex, List<int> targetInfo = null)
        {
            /* 위 TriggerEffect<T> 함수와 같다.  단 ActivatedAbility 만 뽑는다 */
            var libraryCard = GameNetworkManager.Instance.config.GetCard(card.cardId);
            var activatedAbilities = libraryCard.abilities.FindAll(x => x is ActivatedAbility);

            /*
            이제 activated ability들 중에서, 지정한 인덱스 하나를 꺼낸다.  이건 꽤 중요한 의미가 있다.
            즉 카드가 activated ability를 여러 개 가질 수 있다는 뜻이다.

                예:
                    능력 0: “1마나: 공격력 +1”
                    능력 1: “3마나: 카드 1장 뽑기”

                    이런 경우 abilityIndex로 구분할 수 있다.

                주의점
                    여기엔 범위 체크가 없다. 즉 abilityIndex가 잘못 들어오면 바로 터질 수 있다. 지금은 내부 호출이 맞다고 가정한 구조다.
                             */
            var activatedAbility = activatedAbilities[abilityIndex] as ActivatedAbility;


            // PlayerEffect 처리
            /*
             이건 플레이어 대상 효과다.

            예:
                내 체력 회복, 상대 체력 감소, 마나 증가, 플레이어 버프/디버프

            흐름은:
                이 effect가 현재 유효한 타겟을 가질 수 있는지 확인, 실제 플레이어 타겟 목록 계산, 각 타겟에 대해 Resolve() 실행
                즉 수동 능력이라고 해도 타겟 검증을 먼저 한다.
                 */
            if (activatedAbility.effect is PlayerEffect && AreTargetsAvailable(activatedAbility.effect, card, activatedAbility.target))
            {
                var targets = GetPlayerTargets(player, activatedAbility.target, targetInfo);
                foreach (var t in targets)
                {
                    (activatedAbility.effect as PlayerEffect).Resolve(t);
                }
            }

            // CardEffect 처리
            /*
             이건 카드 대상 효과다.

                예:
                    카드 공격력 증가, 적 카드 체력 감소, 특정 카드 강화, 특정 조건 카드 선택
                    여기서는 GetCardTargets()를 써서:
                    어떤 존에서 어떤 타입 카드인지 targetInfo가 뭔지 를 보고 실제 카드 목록을 뽑는다.
                    즉 이 부분은:  카드 대상 액티브 능력 실행기  다.
             */
            else if (activatedAbility.effect is CardEffect && AreTargetsAvailable(activatedAbility.effect, card, activatedAbility.target))
            {
                var cardEffect = activatedAbility.effect as CardEffect;
                var targets = GetCardTargets(player, card, activatedAbility.target, cardEffect.gameZoneId, cardEffect.cardTypeId, targetInfo);
                foreach (var t in targets)
                {
                    (activatedAbility.effect as CardEffect).Resolve(t);
                }
            }

            // MoveCardEffect 처리
            /*
             이건 카드 이동 효과다.

            예:

                카드 한 장 손으로 되돌리기
                묘지로 보내기
                덱으로 넣기
                다른 존으로 이동

            여기도 타겟은 카드라 GetCardTargets()를 쓰지만, 실제 실행은 이동 계열 effect다.
            즉 CardEffect와 타겟 방식은 비슷하지만, 결과가 zone 이동이라는 점이 다르다.
             */
            else if (activatedAbility.effect is MoveCardEffect && AreTargetsAvailable(activatedAbility.effect, card, activatedAbility.target))
            {
                var moveCardEffect = activatedAbility.effect as MoveCardEffect;
                var targets = GetCardTargets(player, card, activatedAbility.target, moveCardEffect.originGameZoneId, moveCardEffect.cardTypeId, targetInfo);
                foreach (var t in targets)
                {
                    (activatedAbility.effect as MoveCardEffect).Resolve(t);
                }
            }
        }



        /// <summary>
        /// Sets the destroy conditions of the specified card.
        /// </summary>
        /// <param name="card">The card to set.</param>
        public void SetDestroyConditions(RuntimeCard card)
        {
            if (card == null)
            {
                Debug.LogError("SetDestroyConditions: card is null");
                return;
            }

            var cardType = card.cardType;
            
            if (cardType == null)
            {
                Debug.LogError($"SetDestroyConditions: cardType is null. cardId={card.cardId}");
                return;
            }

            if (cardType.destroyConditions == null)
            {
                Debug.LogWarning($"SetDestroyConditions: destroyConditions is null. cardType={cardType.name}");
                return;
            }


            Debug.Log($"SetDestroyConditions: cardId={card.cardId}, instanceId={card.instanceId}, cardType={cardType.name}, destroyConditionsCount={(cardType.destroyConditions != null ? cardType.destroyConditions.Count : -1)}");


            ///
            foreach (var condition in cardType.destroyConditions)
            {
                if (condition is StatDestroyCardCondition)
                {
                    var statCondition = condition as StatDestroyCardCondition;
                    card.stats[statCondition.statId].onValueChanged += (oldValue, newValue) =>
                    {
                        if (statCondition.IsTrue(card))
                        {
                            // GINO CHECK
                            //MoveCard(card.ownerPlayer.netId, card, "Board", "Graveyard");
                        }
                    };
                }
            }
        }

        /// <summary>
        /// Sets the triggers of the specified player.
        /// </summary>
        /// <param name="player">The player to set.</param>
        public void SetTriggers(PlayerInfo player)
        {
            foreach (var stat in player.stats)
            {
                stat.Value.onValueChanged += (oldValue, newValue) =>
                {
                    foreach (var zone in player.zones)
                    {
                        var zoneDefinition = GameNetworkManager.Instance.config.gameZones.Find(x => x.id == zone.Value.zoneId);
                        if (zoneDefinition.type == ZoneType.Dynamic && zoneDefinition.opponentVisibility == ZoneOpponentVisibility.Visible)
                        {
                            foreach (var card in zone.Value.cards)
                            {
                                TriggerEffect<OnPlayerStatIncreasedTrigger>(player, card, x => { return x.IsTrue(stat.Value, newValue, oldValue); });
                                TriggerEffect<OnPlayerStatDecreasedTrigger>(player, card, x => { return x.IsTrue(stat.Value, newValue, oldValue); });
                                TriggerEffect<OnPlayerStatReachedValueTrigger>(player, card, x => { return x.IsTrue(stat.Value, newValue, oldValue); });
                            }
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Sets the triggers of the specified card.
        /// </summary>
        /// <param name="card">The card to set.</param>
        public void SetTriggers(RuntimeCard card)
        {
            foreach (var stat in card.stats)
            {
                stat.Value.onValueChanged += (oldValue, newValue) =>
                {
                    TriggerEffect<OnCardStatIncreasedTrigger>(card.ownerPlayer, card, x => { return x.IsTrue(stat.Value, newValue, oldValue); });
                    TriggerEffect<OnCardStatDecreasedTrigger>(card.ownerPlayer, card, x => { return x.IsTrue(stat.Value, newValue, oldValue); });
                    TriggerEffect<OnCardStatReachedValueTrigger>(card.ownerPlayer, card, x => { return x.IsTrue(stat.Value, newValue, oldValue); });
                };
            }
        }

        /// <summary>
        /// Returns the actual player targets of the specified target type.
        /// </summary>
        /// <param name="player">The current player.</param>
        /// <param name="abilityTarget">The target.</param>
        /// <param name="targetInfo">The target information.</param>
        /// <returns>The actual player targets of the specified target type.</returns>
        public List<PlayerInfo> GetPlayerTargets(PlayerInfo player, Target abilityTarget, List<int> targetInfo)
        {
            var playerTargets = new List<PlayerInfo>();
            var target = abilityTarget.GetTarget();
            switch (target)
            {
                case EffectTarget.Player:
                    playerTargets.Add(player);
                    break;

                case EffectTarget.Opponent:
                    playerTargets.Add(GameNetworkManager.Instance.players.Find(x => x != player));
                    break;

                case EffectTarget.TargetPlayer:
                    if (targetInfo != null && targetInfo[0] == 0)
                    {
                        playerTargets.Add(player);
                    }
                    else
                    {
                        playerTargets.Add(GameNetworkManager.Instance.players.Find(x => x != player));
                    }
                    break;

                case EffectTarget.RandomPlayer:
                    {
                        playerTargets.AddRange(GameNetworkManager.Instance.players);
                        playerTargets = playerTargets.OrderBy(x => x.netId).ToList();
                        var randomPlayer = playerTargets[GetRandomNumber(playerTargets.Count)];
                        playerTargets.RemoveAll(x => x != randomPlayer);
                    }
                    break;

                case EffectTarget.AllPlayers:
                    playerTargets.AddRange(GameNetworkManager.Instance.players);
                    break;

                default:
                    break;
            }
            playerTargets.RemoveAll(x =>
            {
                var conditionsFullfilled = true;
                var playerTarget = abilityTarget as PlayerTargetBase;
                foreach (var condition in playerTarget.conditions)
                {
                    if (!condition.IsTrue(x))
                    {
                        conditionsFullfilled = false;
                        break;
                    }
                }
                return !conditionsFullfilled;
            });
            return playerTargets;
        }

        /// <summary>
        /// Returns the actual card targets of the specified target.
        /// </summary>
        /// <param name="player">The current player.</param>
        /// <param name="sourceCard">The current card.</param>
        /// <param name="abilityTarget">The target.</param>
        /// <param name="gameZoneId">The game zone identifier.</param>
        /// <param name="cardTypeId">The card type.</param>
        /// <param name="targetInfo">The target information.</param>
        /// <returns>The actual card targets of the specified target.</returns>
        public List<RuntimeCard> GetCardTargets(PlayerInfo player, RuntimeCard sourceCard, Target abilityTarget, int gameZoneId, int cardTypeId, List<int> targetInfo)
        {
            var cardTargets = new List<RuntimeCard>();
            var opponent = GameNetworkManager.Instance.players.Find(x => x != player);
            var target = abilityTarget.GetTarget();
            var effectZone = gameZoneId;
            var effectCardType = cardTypeId;
            var zoneId = (targetInfo != null && targetInfo.Count > 0) ? targetInfo[0] : effectZone;
            switch (target)
            {
                case EffectTarget.ThisCard:
                    cardTargets.Add(sourceCard);
                    break;

                case EffectTarget.PlayerCard:
                    {
                        var card = player.GetCard(targetInfo[1], zoneId);
                        cardTargets.Add(card);
                    }
                    break;

                case EffectTarget.OpponentCard:
                    {
                        var card = opponent.GetCard(targetInfo[1], zoneId);
                        cardTargets.Add(card);
                    }
                    break;

                case EffectTarget.TargetCard:
                    {
                        var card = player.GetCard(targetInfo[1], zoneId);
                        if (card == null)
                        {
                            card = opponent.GetCard(targetInfo[1], zoneId);
                        }
                        cardTargets.Add(card);
                    }
                    break;

                case EffectTarget.RandomPlayerCard:
                    {
                        cardTargets.AddRange(player.zones[zoneId].cards);
                        cardTargets.RemoveAll(x => x.cardType.id != effectCardType);
                        var card = cardTargets[GetRandomNumber(cardTargets.Count)];
                        cardTargets.RemoveAll(x => x != card);
                    }
                    break;

                case EffectTarget.RandomOpponentCard:
                    {
                        cardTargets.AddRange(opponent.zones[zoneId].cards);
                        cardTargets.RemoveAll(x => x.cardType.id != effectCardType);
                        var card = cardTargets[GetRandomNumber(cardTargets.Count)];
                        cardTargets.RemoveAll(x => x != card);
                    }
                    break;

                case EffectTarget.RandomCard:
                    {
                        cardTargets.AddRange(player.zones[zoneId].cards);
                        cardTargets.AddRange(opponent.zones[zoneId].cards);
                        cardTargets.RemoveAll(x => x.cardType.id != effectCardType);
                        var card = cardTargets[GetRandomNumber(cardTargets.Count)];
                        cardTargets.RemoveAll(x => x != card);
                    }
                    break;

                case EffectTarget.AllPlayerCards:
                    cardTargets.AddRange(player.zones[zoneId].cards);
                    cardTargets.RemoveAll(x => x.cardType.id != effectCardType);
                    break;

                case EffectTarget.AllOpponentCards:
                    cardTargets.AddRange(opponent.zones[zoneId].cards);
                    cardTargets.RemoveAll(x => x.cardType.id != effectCardType);
                    break;

                case EffectTarget.AllCards:
                    cardTargets.AddRange(player.zones[zoneId].cards);
                    cardTargets.AddRange(opponent.zones[zoneId].cards);
                    cardTargets.RemoveAll(x => x.cardType.id != effectCardType);
                    break;

                default:
                    break;
            }
            cardTargets.RemoveAll(x =>
            {
                var conditionsFullfilled = true;
                var cardTarget = abilityTarget as CardTargetBase;
                foreach (var condition in cardTarget.conditions)
                {
                    if (!condition.IsTrue(x))
                    {
                        conditionsFullfilled = false;
                        break;
                    }
                }
                return !conditionsFullfilled;
            });
            return cardTargets;
        }

        /// <summary>
        /// Returns true if there are any targets available for the specified effect and false otherwise.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="sourceCard">The card originating the effect.</param>
        /// <param name="target">The target.</param>
        /// <returns>True if there are any targets available for the specified effect; false otherwise.</returns>
        public bool AreTargetsAvailable(Effect effect, RuntimeCard sourceCard, Target target)
        {
            return effect.AreTargetsAvailable(sourceCard, target);
        }

        /// <summary>
        /// Returns a random number in the [0, max] range.
        /// </summary>
        /// <param name="max">The maximum value.</param>
        /// <returns>A random number in the [0, max] range.</returns>
        public int GetRandomNumber(int max)
        {
            return rng.Next(max);
        }

        /// <summary>
        /// Returns a random number in the [min, max] range.
        /// </summary>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>A random number in the [min, max] range.</returns>
        public int GetRandomNumber(int min, int max)
        {
            return rng.Next(min, max);
        }
    }
}
