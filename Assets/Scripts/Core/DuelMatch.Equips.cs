using System;

namespace Reborn.Core
{
    /// <summary>
    /// Original PS1 equip placement onto an already revealed own monster.
    /// The equip disappears from the field after use and the bonus persists
    /// until the monster leaves the field. No modern TCG equip zones.
    /// </summary>
    public sealed partial class DuelMatch
    {
        public bool HasVerifiedEquip(CardDefinition card) =>
            verifiedEquips.TryGet(card, out _);

        public bool CanEquipFromHand(DuelSide side, int handIndex, int monsterSlot,
            out VerifiedEquipRule rule, out string reason)
        {
            rule = null;
            if (!Active(side, MatchPhase.Main, out reason)) return false;
            if (hasPlayed)
                return Reject(out reason, "Uma carta ja foi baixada neste turno");
            var owner = Side(side);
            if (handIndex < 0 || handIndex >= owner.Hand.Count)
                return Reject(out reason, "Carta da mao invalida");
            var equip = owner.Hand[handIndex];
            if (equip.Kind != CardKind.Equip)
                return Reject(out reason, "Selecione uma carta de equipamento");
            if (monsterSlot < 0 || monsterSlot >= ZoneCount)
                return Reject(out reason, "Zona de monstro invalida");
            var target = owner.Monster(monsterSlot);
            if (target == null)
                return Reject(out reason, "Nao ha monstro para equipar");
            if (target.FaceDown)
                return Reject(out reason,
                    "Monstro esta oculto; espere ele ser revelado para receber Equip");
            if (!verifiedEquips.TryGet(equip, out rule))
                return Reject(out reason,
                    "Equipamento sem compatibilidade verificada no perfil Original");
            if (!rule.Accepts(target.Card))
                return Reject(out reason,
                    "Equipamento nao compativel com o monstro selecionado");
            if (!target.CanApplyEquip(rule.AttackGain, rule.DefenseGain))
                return Reject(out reason, "Bonus de equipamento fora do limite de atributos");
            reason = "";
            return true;
        }

        public bool TryEquipFromHand(DuelSide side, int handIndex, int monsterSlot,
            out string reason)
        {
            if (!CanEquipFromHand(side, handIndex, monsterSlot,
                out var rule, out reason)) return false;
            var owner = Side(side);
            var target = owner.Monster(monsterSlot);
            var original = owner.Take(handIndex);
            target.ApplyEquip(rule.AttackGain, rule.DefenseGain);
            owner.Bury(original);
            hasPlayed = true;
            Emit(MatchEventKind.Equipped, side, original.Id,
                FieldLane.Monster, monsterSlot, rule.AttackGain);
            Emit(MatchEventKind.Graveyard, side, original.Id,
                FieldLane.Support, -1);
            SignalChanged();
            reason = "";
            return true;
        }
    }
}
