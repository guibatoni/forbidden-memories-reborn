using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reborn.Presentation
{
    public sealed partial class ArenaHud
    {
        private RebornKeyBindings keyBindings;
        private RebornKeyAction? capturingKey;
        private bool keyboardSettingsOpen;
        private Vector2 keyboardSettingsScroll;
        private string keyboardStatus = "";

        public RebornKeyBindings KeyBindings => keyBindings;
        public bool KeyboardSettingsOpen => keyboardSettingsOpen;

        // Unity calls Awake after AddComponent finishes constructing ArenaHud.
        // Reading PlayerPrefs here is legal; reading it in a field initializer
        // throws UnityException and may leave other HUD fields uninitialized.
        private void Awake()
        {
            keyBindings = new RebornKeyBindings();
            keyBindings.LoadSaved();
        }

        public void ToggleKeyboardSettings()
        {
            keyboardSettingsOpen = !keyboardSettingsOpen;
            capturingKey = null;
            keyboardStatus = "";
            keyboardSettingsScroll = Vector2.zero;
        }

        public void CloseKeyboardSettings()
        {
            keyboardSettingsOpen = false;
            capturingKey = null;
        }

        /// <summary>
        /// Capture rebinding before all duel shortcuts, so assigning Space
        /// cannot pass a turn or accidentally open a second modal.
        /// </summary>
        public void CaptureKeyboardSetting(Keyboard keyboard)
        {
            if (!keyboardSettingsOpen || !capturingKey.HasValue ||
                keyboard == null) return;
            foreach (var control in keyboard.allKeys)
            {
                if (!control.wasPressedThisFrame) continue;
                if (control.keyCode == Key.Escape)
                {
                    capturingKey = null;
                    keyboardStatus = "Edicao da tecla cancelada.";
                    return;
                }
                if (keyBindings.TrySet(capturingKey.Value, control.keyCode,
                    out string message))
                    capturingKey = null;
                keyboardStatus = message;
                return;
            }
        }

        private void DrawKeyboardSettingsLauncher()
        {
            if (Screen.width < 420 || Screen.height < 400) return;
            if (GUI.Button(new Rect(Screen.width - 225, Screen.height - 48, 210, 33),
                "CONFIGURAR TECLADO", button))
                ToggleKeyboardSettings();
        }

        private void DrawKeyboardSettings()
        {
            Panel(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.005f,0.013f,0.030f,0.97f));
            float width = Mathf.Min(740f,Screen.width-26f);
            float height = Mathf.Min(648f,Screen.height-24f);
            float x = (Screen.width-width)*0.5f;
            float y = (Screen.height-height)*0.5f;
            Panel(new Rect(x,y,width,height),
                new Color(0.026f,0.043f,0.090f,1f));
            Panel(new Rect(x+14f,y+12f,width-28f,4f),
                new Color(0.91f,0.73f,0.39f));
            GUI.Label(new Rect(x+22f,y+25f,width-44f,37f),
                "CONFIGURACAO DO TECLADO",heading);
            GUI.Label(new Rect(x+22f,y+67f,width-44f,58f),
                "Clique em uma tecla para remapear. Teclas repetidas sao bloqueadas.\n" +
                "O mouse permanece disponivel; alteracoes ficam salvas neste computador.",small);

            float scrollHeight = Mathf.Max(115f,height-258f);
            float contentWidth = width-68f;
            float rowHeight = 43f;
            keyboardSettingsScroll = GUI.BeginScrollView(
                new Rect(x+22f,y+131f,width-42f,scrollHeight),
                keyboardSettingsScroll,
                new Rect(0,0,contentWidth,RebornKeyBindings.Actions.Count*rowHeight+12f));
            for (int i=0;i<RebornKeyBindings.Actions.Count;i++)
            {
                var action = RebornKeyBindings.Actions[i];
                float yy = 3f + i*rowHeight;
                if (i%2 == 0)
                    Panel(new Rect(0,yy,contentWidth,41f),
                        new Color(0.040f,0.066f,0.115f,1f));
                GUI.Label(new Rect(13f,yy+8f,contentWidth*0.64f,31f),
                    RebornKeyBindings.Label(action),body);
                bool capture = capturingKey == action;
                string label = capture ? "PRESSIONE UMA TECLA" :
                    keyBindings.Get(action).ToString();
                if (GUI.Button(new Rect(contentWidth*0.64f,yy+5f,
                    contentWidth*0.35f,32f),label,button))
                {
                    capturingKey = action;
                    keyboardStatus = "Pressione a nova tecla (ESC cancela).";
                }
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(x+22f,y+height-119f,width-44f,40f),
                string.IsNullOrEmpty(keyboardStatus) ?
                    "Na mao: CIMA marca fusao; BAIXO desmarca. W acessa o campo." :
                    keyboardStatus,small);
            float half=(width-59f)*0.5f;
            if (GUI.Button(new Rect(x+22f,y+height-66f,half,42f),
                "RESTAURAR PADRAO",button))
            {
                keyBindings.ResetDefaults();
                capturingKey = null;
                keyboardStatus = "Configuracoes de fabrica restauradas.";
            }
            if (GUI.Button(new Rect(x+37f+half,y+height-66f,half,42f),
                "SALVAR E FECHAR",button))
                CloseKeyboardSettings();
        }
    }
}
