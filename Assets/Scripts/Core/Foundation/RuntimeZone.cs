// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections.Generic;

namespace CCGKit
{
    /// <summary>
    /// This class represents a runtime instance of a zone.
    /// </summary>
    public class RuntimeZone
    {
        /// <summary>
        /// The identifier of this zone.
        //Deck,  Hand, Board.  Graveyard.  같은 걸 식별하는 키. namedZones["Hand"]의 숫자 버전

        /// </summary>
        public int zoneId;


        /// <summary>
        /// The instance identifier of this zone.
        /// </summary>
        //public int instanceId;

        /// <summary>
        /// The name of this zone.
        /// </summary>
        public string name;

        /// <summary>
        /// The cards of this zone.
        /// </summary>
        public List<RuntimeCard> cards = new List<RuntimeCard>();
        // 실제 카드 본체.


        /// <summary>
        /// The number of cards of this zone.
        /// </summary>
        protected int _numCards;

        /*
         * 여기가 핵심이다.

        보통은 cards.Count만 있으면 되는데, 여긴 따로 _numCards를 들고 있고 numCards 프로퍼티로 이벤트까지 쏜다.

        이게 왜 있냐면, 원본 구조는 존의 실제 카드 목록을 전부 모를 수도 있는 상황을 지원하려고 한 것 같다.
        대표 예가 상대 덱/상대 손패다.

        상대 손패는 몇 장인지는 알지만 어떤 카드인지는 모름
        상대 덱도 몇 장인지는 알지만 내부 카드는 모름
        반면 내 손패/보드/묘지는 실제 cards 리스트를 다 가짐


        즉 이 클래스는

        cards = 내가 실제 내용을 아는 카드들
        numCards = 이 존에 존재하는 총 장수
        를 분리해서 관리한다.


        이건 꽤 중요한 설계다.
        그래서 cards.Count와 numCards가 달라도 되는 상황이 생긴다.

            예:

            상대 손패: cards.Count == 0 일 수 있지만 numCards == 5
            내 손패: cards.Count == numCards

            이걸 이해 못하면 이 클래스가 이상하게 보인다.
         */

        /// <summary>
        /// The number of cards of this zone.
        /// </summary>
        public int numCards
        {
            get
            {
                return _numCards;
            }
            set
            {
                _numCards = value;
                if (onZoneChanged != null)
                {
                    onZoneChanged(_numCards);
                }
            }
        }

        /// <summary>
        /// The maximum number of cards of this zone.
        /// </summary>
        public int maxCards;


        /*
         이 존은 단순 리스트가 아니라 이벤트 허브다.


        onZoneChanged(int) = 장수 변화
        onCardAdded(RuntimeCard) = 카드 추가
        onCardRemoved(RuntimeCard) = 카드 제거
        onCardCreatedByEffectAdded(RuntimeCard) = 효과로 생성되어 추가됨

        이벤트 구분이 세분화돼 있다.
        원본 UI에서도 이걸 그대로 물고 있다.

        예를 들어:

        손패 카운트 UI는 onZoneChanged
        손패 카드 오브젝트 생성은 onCardAdded
        효과 생성 보드 소환은 onCardCreatedByEffectAdded
        이렇게 나뉘어 있다.

        즉 이 클래스는 자료구조라기보다 존 상태 변경 알림 버스에 가깝다. 
         */

        /// <summary>
        /// The callback that is called when this zone changes.
        /// </summary>
        public Action<int> onZoneChanged;

        /// <summary>
        /// The callback that is called when a card is added to this zone.
        /// </summary>
        public Action<RuntimeCard> onCardAdded;
        
        /// <summary>
        /// The callback that is called when a card is added to this zone as a result of a game effect.
        /// </summary>
        public Action<RuntimeCard> onCardCreatedByEffectAdded;

        /// <summary>
        /// The callback that is called when a card is removed from this zone.
        /// </summary>
        public Action<RuntimeCard> onCardRemoved;

        /// <summary>
        /// Adds a card to this zone.
        /// </summary>
        /// <param name="card">The card to add.</param>
        /// 
        /*

            존 제한보다 작고   이미 없는 카드면   추가한다    장수 1 증가
            zone changed 이벤트
            card added 이벤트

            여기서 포인트 몇 개:

            cards.Contains(card)

            이건 객체 참조 기준일 가능성이 높다.
            즉 동일 instanceId라도 다른 RuntimeCard 객체면 막지 못할 수 있다.
            이건 약간 위험하다. instanceId 기준 중복 체크가 더 자연스러웠을 수 있다.

            _numCards += 1

            setter를 안 쓰고 직접 증가시킨 뒤 onZoneChanged(numCards)를 호출한다.
            이건 내부적으로는 괜찮지만 스타일이 일관되진 않다.
            어떤 곳은 numCards = zone.numCards처럼 setter를 쓰고, 여긴 _numCards를 직접 건드린다.

            즉 numCards는 논리상 프로퍼티인데 실제론 반쯤 수동 관리 상태다.
         */
        public void AddCard(RuntimeCard card)
        {
            if (cards.Count < maxCards && !cards.Contains(card))
            {
                cards.Add(card);
                _numCards += 1;
                if (onZoneChanged != null)
                {
                    onZoneChanged(numCards);
                }
                if (onCardAdded != null)
                {
                    onCardAdded(card);
                }
            }
        }



        /// <summary>
        /// Adds a card to this zone as a result of a game effect.
        /// </summary>
        /// <param name="card">The card to add.</param>
        /// 
        /*
         * 즉 일반 추가 이벤트는 그대로 타고,
            추가로 “이건 효과로 생성된 카드다”라는 별도 신호를 준다.

            이건 왜 필요하냐면, 보드에 카드가 들어오는 방식이 둘로 나뉘기 때문이다.

            손에서 정상 플레이해서 들어옴
            토큰 생성/소환/복제 등 효과로 생김

            원본 UI는 이 둘을 다르게 처리한다.
            예를 들어 boardZone.onCardCreatedByEffectAdded += AddCreatureToPlayerBoard 같은 식으로 effect spawn 연출을 따로 태운다.

            즉 이건 꽤 실전적인 분기다.
         */
        public void AddCardCreatedByEffect(RuntimeCard card)
        {
            if (cards.Count < maxCards && !cards.Contains(card))
            {
                AddCard(card);
                if (onCardCreatedByEffectAdded != null)
                {
                    onCardCreatedByEffectAdded(card);
                }
            }
        }

        /// <summary>
        /// Removes a card from this zone.
        /// </summary>
        /// <param name="card">The card to remove.</param>
        public void RemoveCard(RuntimeCard card)
        {
            if (cards.Contains(card))
            {
                cards.Remove(card);
                _numCards -= 1;
                if (onZoneChanged != null)
                {
                    onZoneChanged(numCards);
                }
                if (onCardRemoved != null)
                {
                    onCardRemoved(card);
                }
            }
        }

        /// <summary>
        /// Removes a number of cards from this zone.
        /// </summary>
        /// <param name="amount">The number of cards to remove.</param>
        public void RemoveCards(int amount)
        {
            cards.RemoveRange(0, amount);
            _numCards -= amount;
            if (onZoneChanged != null)
            {
                onZoneChanged(numCards);
            }
        }
    }
}
