// Copyright (C) 2016-2023 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

namespace CCGKit
{
    [PlayerTarget]
    public class DrawCardsEffect : PlayerEffect
    {
        [IntField("Amount")]
        [Order(1)]
        public int numCards;

        public override void Resolve(PlayerInfo player)
        {
            // GINO_CHECK
            //state.effectSolver.DrawCards(player.netId, numCards);
        }
    }
}
