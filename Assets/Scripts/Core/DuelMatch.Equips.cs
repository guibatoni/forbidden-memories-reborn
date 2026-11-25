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

        /// <summary>
        /// Activate a previously set equip: the existing support card vanishes
        /// when its bonus is applied. This does not use the turn's mandatory
        /// hand placement because the card entered the field in an earlier turn.
        /// </summary>
        public bool CanEquipFromSupport(DuelSide side, int supportSlot,
            int monsterSlot, out VerifiedEquipRule rule, out string reason)
        {
            rule = null;
            if (Phase == MatchPhase.Finished || CurrentSide != side)
                return Reject(out reason, "Nao e seu turno");
            var owner = Side(side);
            if (supportSlot < 0 || supportSlot >= ZoneCount ||
                owner.Support(supportSlot) == null)
                return Reject(out reason, "Equipamento setado nao encontrado");
            if (owner.Support(supportSlot).Kind != CardKind.Equip ||
                !owner.SupportIsFaceDown(supportSlot))
                return Reject(out reason, "Selecione um Equip virado para baixo");
            if (owner.SupportSetTurn(supportSlot) == Turn)
                return Reject(out reason, "Equip recem-setado aguarda o turno seguinte");
            if (monsterSlot < 0 || monsterSlot >= ZoneCount)
                return Reject(out reason, "Zona invalida");
            var monster = owner.Monster(monsterSlot);
            if (monster == null || monster.FaceDown)
                return Reject(out reason, "Alvo deve ser um monstro proprio revelado");
            if (!verifiedEquips.CanApply(owner.Support(supportSlot), monster.Card,
                out rule))
                return Reject(out reason, "Equip sem compatibilidade verificada para o alvo");
            if (!monster.CanApplyEquip(rule.AttackGain, rule.DefenseGain))
                return Reject(out reason, "Bonus fora do limite");
            reason = "";
            return true;
        }

        public bool TryEquipFromSupport(DuelSide side, int supportSlot,
            int monsterSlot, out string reason)
        {
            if (!CanEquipFromSupport(side, supportSlot, monsterSlot,
                out var rule, out reason)) return false;
            var owner = Side(side);
            var card = owner.Support(supportSlot);
            owner.SetSupport(supportSlot, null);
            owner.Monster(monsterSlot).ApplyEquip(rule.AttackGain, rule.DefenseGain);
            owner.Bury(card);
            Emit(MatchEventKind.Equipped, side, card.Id,
                FieldLane.Monster, monsterSlot, rule.AttackGain);
            Emit(MatchEventKind.Graveyard, side, card.Id,
                FieldLane.Support, supportSlot);
            SignalChanged();
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
