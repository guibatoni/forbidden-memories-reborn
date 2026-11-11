using System;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Explicit PS1 Equip confirmation. Preview and cancel have no side effect;
    /// the callback commits the single mandatory placement after confirmation.
    /// </summary>
    public sealed partial class ArenaHud
    {
        private CardDefinition selectedEquip;
        private CardDefinition equipTarget;
        private int equipGainAttack;
        private int equipGainDefense;
        private Func<bool> equipCommit;

        public bool EquipChoiceOpen => equipCommit != null;

        public void BeginEquipChoice(CardDefinition equip,
            MonsterOnField monster, VerifiedEquipRule rule, Func<bool> commit)
        {
            if (equip == null || monster == null || rule == null ||
                commit == null || PlacementWizardOpen || EquipChoiceOpen ||
                !DuelModeActive || match == null ||
                match.Phase != MatchPhase.Main || match.PlayedThisTurn ||
                match.CurrentSide != DuelSide.Player)
                return;
            selectedEquip = equip;
            equipTarget = monster.Card;
            equipGainAttack = rule.AttackGain;
            equipGainDefense = rule.DefenseGain;
            equipCommit = commit;
        }

        public void CancelEquipChoice()
        {
            equipCommit = null;
            selectedEquip = null;
            equipTarget = null;
            equipGainAttack = 0;
            equipGainDefense = 0;
        }

        public void ConfirmEquipChoice()
        {
            if (!EquipChoiceOpen) return;
            var commit = equipCommit;
            CancelEquipChoice();
            if (commit != null && !commit())
                Announce("Equipamento recusado: estado da jogada mudou.");
        }

        private void DrawEquipChoice()
        {
            if (!EquipChoiceOpen || selectedEquip == null || equipTarget == null) return;
            Panel(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.008f, 0.015f, 0.034f, 0.95f));
            float w = Mathf.Min(685f, Screen.width - 30f);
            float h = Mathf.Min(337f, Screen.height - 25f);
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            Panel(new Rect(x, y, w, h), new Color(0.028f, 0.050f, 0.092f, 1f));
            Panel(new Rect(x + 14f, y + 12f, w - 28f, 4f),
                new Color(0.92f, 0.76f, 0.36f, 1f));
            GUI.Label(new Rect(x + 23f, y + 28f, w - 46f, 43f),
                "EQUIPAR MONSTRO", heading);
            GUI.Label(new Rect(x + 23f, y + 83f, w - 46f, 38f),
                selectedEquip.Name + "  >  " + equipTarget.Name, body);
            GUI.Label(new Rect(x + 23f, y + 130f, w - 46f, 55f),
                "BONUS PERMANENTE: ATK +" + equipGainAttack +
                "   |   DEF +" + equipGainDefense, body);
            GUI.Label(new Rect(x + 23f, y + 195f, w - 46f, 53f),
                "No PS1, o Equip vai ao cemiterio e o bonus fica no monstro.\n" +
                "Confirmar consome a unica carta que pode baixar neste turno.",small);
            float buttonW = (w - 63f) * 0.5f;
            if (GUI.Button(new Rect(x + 23f, y + h - 60f, buttonW, 39f),
                "CONFIRMAR EQUIP", button))
            {
                ConfirmEquipChoice();
            }
            if (GUI.Button(new Rect(x + 40f + buttonW, y + h - 60f, buttonW, 39f),
                "CANCELAR SEM GASTAR CARTA", button))
            {
                CancelEquipChoice();
                Announce("Equipamento cancelado. Nenhuma carta foi consumida.");
            }
        }
    }
}
