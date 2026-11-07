using System.Collections;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Makes player attacks readable and prevents reentrant clicks. DuelMatch
    /// remains the only authority for damage, destruction and victory.
    /// </summary>
    public sealed class PlayerAttackDirector : MonoBehaviour
    {
        private ArenaFactory arena;
        private ArenaHud hud;
        private Coroutine active;

        public void Initialize(ArenaFactory scene, ArenaHud interfaceHud)
        {
            arena = scene;
            hud = interfaceHud;
        }

        public void RequestAttack(DuelMatch match, int fromSlot, int targetSlot)
        {
            if (active != null || hud == null || match == null || !hud.DuelModeActive ||
                hud.InputLocked || hud.Match != match) return;
            if (!match.CanAttack(DuelSide.Player,fromSlot,targetSlot,out string reason))
            {
                hud.Message = reason;
                return;
            }
            hud.SetPlayerAttackBusy(true);
            active = StartCoroutine(Execute(match,fromSlot,targetSlot));
        }

        private IEnumerator Execute(DuelMatch match, int fromSlot, int targetSlot)
        {
            var monster = match.Player.Monster(fromSlot);
            hud.Announce("Jogador selecionou " + monster.Card.Name + " para atacar.");
            arena.HighlightAttacker(DuelSide.Player,fromSlot);
            yield return new WaitForSeconds(hud.AnimationsEnabled ? 0.42f : 0.08f);

            if (targetSlot >= 0 && match.Opponent.Monster(targetSlot) != null)
            {
                hud.Announce("Alvo: " + match.Opponent.Monster(targetSlot).Card.Name + ".");
                arena.HighlightAttacker(DuelSide.Opponent,targetSlot);
            }
            else hud.Announce("Ataque direto declarado!");
            if (hud.AnimationsEnabled)
            {
                arena.PlayMatchAttack(DuelSide.Player,fromSlot,targetSlot);
                yield return new WaitForSeconds(0.71f);
            }
            else yield return null;

            if (match == hud.Match && hud.DuelModeActive &&
                match.CanAttack(DuelSide.Player,fromSlot,targetSlot,out string reason))
            {
                int playerLife = match.Player.Life;
                int opponentLife = match.Opponent.Life;
                if (match.TryAttack(DuelSide.Player,fromSlot,targetSlot,out reason))
                {
                    int dealt = opponentLife - match.Opponent.Life;
                    int reflected = playerLife - match.Player.Life;
                    hud.Announce(dealt > 0 ? "Ataque resolvido: -" + dealt + " LP adversarios." :
                        reflected > 0 ? "Ataque resolvido: -" + reflected + " dos seus LP." :
                        "Ataque resolvido: sem dano nos pontos de vida.");
                }
                else hud.Announce("Ataque recusado: " + reason);
            }
            arena.HighlightAttacker(DuelSide.Player,-1);
            arena.HighlightAttacker(DuelSide.Opponent,-1);
            hud.SetPlayerAttackBusy(false);
            active = null;
        }

        public void Cancel()
        {
            if (active != null) StopCoroutine(active);
            active = null;
            if (arena != null)
            {
                arena.HighlightAttacker(DuelSide.Player,-1);
                arena.HighlightAttacker(DuelSide.Opponent,-1);
            }
            if (hud != null) hud.SetPlayerAttackBusy(false);
        }

        private void OnDisable() { Cancel(); }
    }
}