using System;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// One target can receive an immediate compatible equip or a hidden Equip
    /// set in its matching support slot. Both are explicit transactions.
    /// </summary>
    public sealed partial class ArenaHud
    {
        private CardDefinition selectedEquip;
        private CardDefinition equipTarget;
        private int equipGainAttack;
        private int equipGainDefense;
        private Func<bool> equipCommit;
        private Func<bool> equipSetCommit;

        public bool EquipChoiceOpen => equipCommit != null || equipSetCommit != null;

        public void BeginEquipChoice(CardDefinition equip,
            MonsterOnField monster, VerifiedEquipRule rule, Func<bool> commit,
            Func<bool> setCommit = null, bool prepared = false)
        {
            if (equip == null || monster == null ||
                (commit == null && setCommit == null) ||
                PlacementWizardOpen || EquipChoiceOpen ||
                !DuelModeActive || match == null ||
                (!prepared && match.PlayedThisTurn) ||
                match.Phase == MatchPhase.Finished ||
                match.CurrentSide != DuelSide.Player)
                return;
            selectedEquip = equip;
            equipTarget = monster.Card;
            equipGainAttack = rule != null ? rule.AttackGain : 0;
            equipGainDefense = rule != null ? rule.DefenseGain : 0;
            equipCommit = commit;
            equipSetCommit = setCommit;
        }

        public void CancelEquipChoice()
        {
            equipCommit = null;
            equipSetCommit = null;
            selectedEquip = null;
            equipTarget = null;
            equipGainAttack = 0;
            equipGainDefense = 0;
        }

        private void CommitEquipChoice(Func<bool> commit)
        {
            if (!EquipChoiceOpen || commit == null) return;
            CancelEquipChoice();
            if (!commit()) Announce("Equipamento recusado; sua carta nao foi consumida.");
        }

        public void ConfirmEquipChoice()
        {
            CommitEquipChoice(equipCommit ?? equipSetCommit);
        }

        public void ConfirmEquipSetChoice()
        {
            CommitEquipChoice(equipSetCommit);
        }

        private void DrawEquipChoice()
        {
            if (!EquipChoiceOpen || selectedEquip == null || equipTarget == null) return;
            Panel(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.008f, 0.015f, 0.034f, 0.95f));
            float w = Mathf.Min(730f, Screen.width - 28f);
            float h = Mathf.Min(365f, Screen.height - 25f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Panel(new Rect(x,y,w,h), new Color(0.028f,0.050f,0.092f,1f));
            Panel(new Rect(x+14f,y+12f,w-28f,4f),
                new Color(0.92f,0.76f,0.36f));
            GUI.Label(new Rect(x+23f,y+28f,w-46f,43f),
                "EQUIP: APLICAR OU PREPARAR",heading);
            GUI.Label(new Rect(x+23f,y+82f,w-46f,38f),
                selectedEquip.Name + "  >  " + equipTarget.Name,body);
            GUI.Label(new Rect(x+23f,y+127f,w-46f,54f),
                equipCommit != null ?
                    "Compatibilidade confirmada. Bonus ATK +" + equipGainAttack +
                    " / DEF +" + equipGainDefense :
                    "Sem compatibilidade para aplicar agora. Pode setar Face Down.",
                body);
            GUI.Label(new Rect(x+23f,y+200f,w-46f,60f),
                "APLICAR: a carta Equip vai ao cemiterio e bonifica o monstro.\n"+
                "SETAR: carta fica oculta sobre o monstro, na zona de suporte "+
                "correspondente. Z confirma, E seta, X cancela.",small);

            int n = equipSetCommit == null ? 2 : 3;
            float gap = 11f;
            float width = (w-46f-(n-1)*gap)/n;
            if (equipCommit != null && GUI.Button(
                new Rect(x+23f,y+h-63f,width,40f),
                "EQUIPAR AGORA [Z]",button))
                ConfirmEquipChoice();
            else if (equipCommit == null &&
                GUI.Button(new Rect(x+23f,y+h-63f,width,40f),
                    "SOMENTE SET DISPONIVEL",button))
                ConfirmEquipSetChoice();
            if (equipSetCommit != null && GUI.Button(
                new Rect(x+23f+width+gap,y+h-63f,width,40f),
                "SETAR FACE DOWN [E]",button))
                ConfirmEquipSetChoice();
            if (GUI.Button(new Rect(x+w-23f-width,y+h-63f,width,40f),
                "CANCELAR [X]",button))
                CancelEquipChoice();
        }
    }
}
