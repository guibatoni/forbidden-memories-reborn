using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Presentation
{
    /// <summary>
    /// Rebindable controls, persisted per local Windows player profile.
    /// All actions have distinct keys, so a saved mapping cannot silently
    /// execute two different duel commands on the same key press.
    /// </summary>
    public enum RebornKeyAction
    {
        PassTurn,
        AttackPosition,
        DefensePosition,
        Select,
        Cancel,
        InspectCard,
        OpponentBoard,
        NavigateLeft,
        NavigateRight,
        FusionMarkUp,
        FusionUnmarkDown
    }

    public sealed class RebornKeyBindings
    {
        private const string Prefix = "Reborn.Keyboard.v1.";
        private static readonly RebornKeyAction[] All =
        {
            RebornKeyAction.PassTurn,
            RebornKeyAction.AttackPosition,
            RebornKeyAction.DefensePosition,
            RebornKeyAction.Select,
            RebornKeyAction.Cancel,
            RebornKeyAction.InspectCard,
            RebornKeyAction.OpponentBoard,
            RebornKeyAction.NavigateLeft,
            RebornKeyAction.NavigateRight,
            RebornKeyAction.FusionMarkUp,
            RebornKeyAction.FusionUnmarkDown
        };

        private readonly Dictionary<RebornKeyAction, Key> keys =
            new Dictionary<RebornKeyAction, Key>();

        public static IReadOnlyList<RebornKeyAction> Actions => All;
        public RebornKeyBindings()
        {
            foreach (var action in All)
            {
                var fallback = Default(action);
                string stored = PlayerPrefs.GetString(Prefix + action, fallback.ToString());
                if (!Enum.TryParse(stored, true, out Key candidate) ||
                    candidate == Key.None || !Enum.IsDefined(typeof(Key), candidate) ||
                    keys.ContainsValue(candidate))
                    candidate = fallback;
                keys[action] = candidate;
            }
        }

        public static string Label(RebornKeyAction action)
        {
            switch (action)
            {
                case RebornKeyAction.PassTurn: return "Passar turno";
                case RebornKeyAction.AttackPosition: return "Virar para ATK";
                case RebornKeyAction.DefensePosition: return "Virar para DEF";
                case RebornKeyAction.Select: return "Selecionar / confirmar";
                case RebornKeyAction.Cancel: return "Cancelar selecao";
                case RebornKeyAction.InspectCard: return "Informacoes da carta";
                case RebornKeyAction.OpponentBoard: return "Mesa do oponente";
                case RebornKeyAction.NavigateLeft: return "Seletor esquerda";
                case RebornKeyAction.NavigateRight: return "Seletor direita";
                case RebornKeyAction.FusionMarkUp: return "Marcar fusao / mover acima";
                case RebornKeyAction.FusionUnmarkDown: return "Desmarcar / mover abaixo";
                default: return action.ToString();
            }
        }

        public static Key Default(RebornKeyAction action)
        {
            switch (action)
            {
                case RebornKeyAction.PassTurn: return Key.Space;
                case RebornKeyAction.AttackPosition: return Key.Q;
                case RebornKeyAction.DefensePosition: return Key.E;
                case RebornKeyAction.Select: return Key.Z;
                case RebornKeyAction.Cancel: return Key.X;
                case RebornKeyAction.InspectCard: return Key.S;
                case RebornKeyAction.OpponentBoard: return Key.W;
                case RebornKeyAction.NavigateLeft: return Key.LeftArrow;
                case RebornKeyAction.NavigateRight: return Key.RightArrow;
                case RebornKeyAction.FusionMarkUp: return Key.UpArrow;
                case RebornKeyAction.FusionUnmarkDown: return Key.DownArrow;
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public Key Get(RebornKeyAction action) => keys[action];

        public bool Pressed(Keyboard keyboard, RebornKeyAction action)
        {
            if (keyboard == null) return false;
            var control = keyboard[Get(action)];
            return control != null && control.wasPressedThisFrame;
        }

        public bool TrySet(RebornKeyAction action, Key key, out string reason)
        {
            if (!keys.ContainsKey(action) || key == Key.None ||
                !Enum.IsDefined(typeof(Key), key))
            {
                reason = "Escolha uma tecla valida.";
                return false;
            }
            foreach (var other in All)
            {
                if (other != action && keys[other] == key)
                {
                    reason = "Tecla ja utilizada em: " + Label(other) + ".";
                    return false;
                }
            }
            keys[action] = key;
            PlayerPrefs.SetString(Prefix + action, key.ToString());
            PlayerPrefs.Save();
            reason = "Comando atualizado: " + Label(action) + " = " + key + ".";
            return true;
        }

        public void ResetDefaults()
        {
            foreach (var action in All)
            {
                keys[action] = Default(action);
                PlayerPrefs.DeleteKey(Prefix + action);
            }
            PlayerPrefs.Save();
        }
    }
}
