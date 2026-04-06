// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections.Generic;

using UnityEngine;

namespace CCGKit
{
    /// <summary>
    /// The modifier of a stat.
    /// </summary>
    public class Modifier
    {
        /// <summary>
        /// The constant value to identify a permanent modifier.
        /// </summary>
        private const int PERMANENT = 0;

        /// <summary>
        /// The value of this modifier.
        /// </summary>
        public int value;

        /// <summary>
        /// The duration of this modifier.
        /// </summary>
        public int duration;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The value of the modifier.</param>
        /// <param name="duration">The duration of the modifier.</param>
        public Modifier(int value, int duration = PERMANENT)
        {
            this.value = value;
            this.duration = duration;
        }

        /// <summary>
        /// Returns true if this modifier is permanent and false otherwise.
        /// </summary>
        /// <returns>True if this modifier is permanent; false otherwise.</returns>
        public bool IsPermanent()
        {
            return duration == PERMANENT;
        }

        /*
         
        Modifier는 사실상 이 두 값이 전부다.
            value
            duration

        그리고 duration == 0이면 permanent로 취급한다.
         */

    }

    /// <summary>
    /// Stats are a fundamental concept in CCG Kit. They represent integer values that can change over
    /// the course of a game and are used in both players and cards. For example, a player could have
    /// life and mana stats and a creature card could have cost, attack and defense stats. Stats are
    /// transmitted over the network, which means you should only use them to represent values that can
    /// actually change over the course of a game in order to save bandwidth.
    /// </summary>

    /*
     Stat은 CCG Kit에서 매우 핵심적인 개념이다.
    게임이 진행되는 동안 변화하는 정수 값들을 표현하며, 플레이어와 카드 양쪽 모두에서 사용된다.
    예를 들어 플레이어는 생명력이나 마나 같은 스탯을 가질 수 있고, 생물 카드는 비용, 공격력, 방어력 같은 스탯을 가진다.
    이러한 스탯들은 네트워크를 통해 전송되기 때문에, 불필요한 트래픽을 줄이기 위해 실제 게임 진행 중에 변화하는 값들만 스탯으로 사용하는 것이 좋다.
     */


    public class Stat
    {
        public int statId;
        public string name;
        public int originalValue;

        public int minValue;
        public int maxValue;

        
        // //////////////////////////////////////////////////////////////////////////
        //
        public List<Modifier> modifiers = new List<Modifier>();
        public Action<int, int> onValueChanged;


        // //////////////////////////////////////////////////////////////////////////
        //
        private int _baseValue = int.MinValue;
        public int baseValue
        {
            get { return _baseValue == int.MinValue ? originalValue : _baseValue; }
            set
            {
                var oldValue = _baseValue;
                _baseValue = value;
                if (onValueChanged != null && oldValue != _baseValue)
                {
                    onValueChanged(oldValue, value);
                }
            }
        }


        public int effectiveValue
        {
            get
            {
                // Start with the base value.
                var value = baseValue;

                // Apply all the modifiers.
                foreach (var modifier in modifiers)
                {
                    value += modifier.value;
                }

                // Clamp to [minValue, maxValue] if needed.
                if (value < minValue)
                {
                    value = minValue;
                }
                else if (value > maxValue)
                {
                    value = maxValue;
                }

                // Return the effective value.
                return value;
            }
        }



        /// <summary>
        /// Adds a modifier to this stat.
        /// </summary>
        /// <param name="modifier">The modifier to add.</param>
        public void AddModifier(Modifier modifier)
        {
            var oldValue = effectiveValue;
            modifiers.Add(modifier);
            if (onValueChanged != null)
            {
                onValueChanged(oldValue, effectiveValue);
            }
        }

        /// <summary>
        /// This method is automatically called when the turn ends.
        /// </summary>
        public void OnEndTurn()
        {
            /*
             * 이건 변경 전 최종값 스냅샷이다.  중요한 포인트는 baseValue가 아니라 **effectiveValue**를 저장한다는 점이다.
                즉 작성자 의도는 분명하다:
                이 함수가 관심 있는 건 “기준값이 바뀌었나”가 아니라      최종 표시/판정 값이 바뀌었나이다
             */
            var oldValue = effectiveValue;


            /*
             이건 제거 대상 후보를 담을 임시 리스트다.
            modifiers를 순회하면서 바로 삭제하면 컬렉션 변경 문제가 생길 수 있어서    먼저 제거 대상을 모은 뒤  나중에 한 번에 삭제한다
             */
            var modifiersToRemove = new List<Modifier>(modifiers.Count);

            /*
             핵심.........
             */
            var temporaryModifiers = modifiers.FindAll(x => !x.IsPermanent());
            foreach (var modifier in temporaryModifiers)
            {
                modifier.duration -= 1;
                if (modifier.duration <= 0)
                {
                    modifiersToRemove.Add(modifier);
                }
            }

            foreach (var modifier in modifiersToRemove)
            {
                modifiers.Remove(modifier);
            }
            if (modifiersToRemove.Count > 0 && onValueChanged != null)
            {
                onValueChanged(oldValue, effectiveValue);
            }
        }
    }
}
