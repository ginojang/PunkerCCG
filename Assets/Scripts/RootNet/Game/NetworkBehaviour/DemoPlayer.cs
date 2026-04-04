using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using CCGKit;

namespace CCGKit
{
	public class DemoPlayer : MonoBehaviour
	{
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
                    GameNetworkManager.Instance.playerInfo.stats[cost.statId].baseValue -= cost.value;
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

        public void FightPlayer(int cardInstanceId)
        {
        }

        public void FightCreature(RuntimeCard attackingCard, RuntimeCard attackedCard)
        {
        }

    }
}