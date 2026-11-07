using Reborn.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>HUD IMGUI de prototipo, sem dependencias de fontes externas.</summary>
    public sealed class ArenaHud : MonoBehaviour
    {
        private DuelSession session;
        private ArenaFactory arena;
        private CampaignRuntime story;
        private readonly Dictionary<int, OpponentDefinition> opponents = new Dictionary<int, OpponentDefinition>();
        private string importStatus = "";
        private bool showStory;
        private Vector2 storyScroll;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle button;
        private GUIStyle status;
        public int SelectedHandIndex { get; set; } = -1;
        public string Message { get; set; } = "Selecione uma carta e clique na zona desejada.";
        public bool ShowHelp { get; private set; } = true;
        public bool AnimationsEnabled { get; private set; } = true;
        public bool SandboxRemovalMode { get; private set; }

        public void Initialize(DuelSession model, ArenaFactory visual,
            GameContentBundle content, string statusText)
        {
            session = model; arena = visual;
            importStatus = statusText ?? "";
            if (content != null)
            {
                foreach (var opponent in content.Opponents) opponents[opponent.Id] = opponent;
                if (content.Campaign != null) story = new CampaignRuntime(content.Campaign);
            }
            Debug.Log("REBORN: " + importStatus);
        }

        public bool IsPointerOverHud(Vector2 pointer)
        {
            float x = pointer.x;
            float y = Screen.height - pointer.y;
            return showStory || (x < 264f && y < 305f) || (x > Screen.width - 220f && y < 476f);
        }

        private GUIStyle Style(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size, fontStyle = weight,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = color },
                wordWrap = true
            };
        }

        private void InitStyles()
        {
            if (heading != null) return;
            heading = Style(24,new Color(1,0.85f,0.52f),FontStyle.Bold);
            body = Style(15,Color.white);
            small = Style(13,new Color(0.76f,0.82f,0.94f));
            status = Style(14,new Color(0.35f,0.94f,1f));
            button = new GUIStyle(GUI.skin.button)
            {
                fontSize=15, fontStyle=FontStyle.Bold,
                normal={textColor=Color.white},
                hover={textColor=new Color(0.45f,0.91f,1f)}
            };
        }

        private void Panel(Rect area, Color tint)
        {
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OnGUI()
        {
            if (session == null) return;
            InitStyles();
            float right = Screen.width - 208f;
            Panel(new Rect(12,12,251,279),new Color(0.015f,0.027f,0.06f,0.94f));
            GUI.Label(new Rect(23,17,220,32),"REBORN  |  0.3",heading);
            GUI.Label(new Rect(24,54,226,28),"Player LP: " + session.PlayerLife,body);
            GUI.Label(new Rect(24,81,226,28),"Oponente LP: " + session.OpponentLife,body);
            GUI.Label(new Rect(24,109,226,26),"Deck: " + session.DeckCount + "    Turno: " + session.Turn,small);
            if (SelectedHandIndex >= 0 && SelectedHandIndex < session.Hand.Count)
            {
                var card = session.Hand[SelectedHandIndex];
                GUI.Label(new Rect(24,145,225,28),"#" + card.Id + " " + card.Name,body);
                GUI.Label(new Rect(24,172,225,38),card.Kind == CardKind.Monster
                    ? $"ATK {card.Attack}   DEF {card.Defense}" : card.Kind.ToString(),small);
                GUI.Label(new Rect(24,215,220,61),card.Description,small);
            }
            else
            {
                GUI.Label(new Rect(24,150,220,106),"Clique em uma das cartas na parte inferior da arena para visualizar seus atributos.",small);
            }
            Panel(new Rect(right,12,196,454),new Color(0.015f,0.027f,0.06f,0.94f));
            GUI.Label(new Rect(right+11,18,180,32),"CONTROLES",heading);
            if (GUI.Button(new Rect(right+12,66,170,38),"Repor mao",button))
            {
                var count = session.DrawUpToFive();
                Message = count == 0 ? "Mao cheia ou deck vazio." : $"Compradas: {count} carta(s).";
            }
            if (GUI.Button(new Rect(right+12,112,170,38),"Proximo turno*",button))
            {
                session.NextSandboxTurn();
                Message = "Turno de sandbox (sem regras de batalha).";
            }
            if (GUI.Button(new Rect(right+12,158,170,38),"Reiniciar arena",button))
            {
                SelectedHandIndex = -1;
                session.Reset();
                arena.Orbit.ResetView();
                Message = "Sandbox reiniciado.";
            }
            if (GUI.Button(new Rect(right+12,204,170,38),"Resetar camera",button))
                arena.Orbit.ResetView();
            if (GUI.Button(new Rect(right+12,250,170,38),"Historia",button))
            {
                if (story != null) showStory = !showStory;
                else Message = "Campanha nao importada: faltam eventos validados da ISO.";
            }
            if (GUI.Button(new Rect(right+12,296,170,38),ShowHelp ? "Ocultar ajuda" : "Exibir ajuda",button))
                ShowHelp = !ShowHelp;
            if (GUI.Button(new Rect(right+12,342,170,36),
                AnimationsEnabled ? "Animacoes: ON" : "Animacoes: OFF",button))
                AnimationsEnabled = !AnimationsEnabled;
            if (GUI.Button(new Rect(right+12,386,170,36),
                SandboxRemovalMode ? "Remover teste: ON" : "Remover teste: OFF",button))
                SandboxRemovalMode = !SandboxRemovalMode;
            GUI.Label(new Rect(right+13,426,175,34),"* Sandbox: sem motor real de duelos",small);

            var width = Mathf.Min(850, Screen.width - 550);
            if (width > 280)
            {
                Panel(new Rect((Screen.width-width)/2f,Screen.height-62,width,50),new Color(0.015f,0.027f,0.06f,0.88f));
                GUI.Label(new Rect((Screen.width-width)/2f+15,Screen.height-55,width-30,39),Message,status);
            }
            if (ShowHelp && Screen.width > 930)
            {
                Panel(new Rect(12,Screen.height-153,263,130),new Color(0.015f,0.027f,0.06f,0.87f));
                GUI.Label(new Rect(23,Screen.height-146,240,116),
                    "Clique em carta > clique em zona\nArraste carta ate a zona\nBotao direito: girar camera\nRoda do mouse: zoom\nTecla R: camera inicial\nESC: cancelar selecao",small);
            }
            if (showStory && story != null) DrawStory();
        }

        private void DrawStory()
        {
            var width = Mathf.Min(690f,Screen.width-80f);
            var height = Mathf.Min(425f,Screen.height-40f);
            float x = (Screen.width-width)/2f;
            float y = (Screen.height-height)/2f;
            Panel(new Rect(x,y,width,height),new Color(0.018f,0.026f,0.045f,0.98f));
            GUI.Label(new Rect(x+20,y+14,width-100,31),"HISTORIA  |  " + story.Current.Location,heading);
            if (GUI.Button(new Rect(x+width-55,y+12,38,32),"X",button)) { showStory = false; return; }
            var node = story.Current;
            var speaker = string.IsNullOrEmpty(node.Speaker) ? "Narrador" : node.Speaker;
            GUI.Label(new Rect(x+20,y+60,width-40,28),speaker,body);
            GUI.Label(new Rect(x+20,y+92,width-40,109),node.Text,body);
            if (story.WaitingForDuel)
            {
                string opponentName = opponents.TryGetValue(node.OpponentId,out var opponent)
                    ? opponent.Name : "ID " + node.OpponentId;
                GUI.Label(new Rect(x+20,y+210,width-45,70),
                    "Duelo obrigatorio contra: " + opponentName +
                    "\nAguardando o motor de duelos original. A historia nao avanca artificialmente.",small);
            }
            else
            {
                var choices = story.AvailableChoices();
                float clipHeight = Mathf.Max(60,height-277f);
                var rowHeight = 43f;
                storyScroll = GUI.BeginScrollView(new Rect(x+17,y+210,width-34,clipHeight),
                    storyScroll,new Rect(0,0,width-63,Mathf.Max(clipHeight,choices.Count*rowHeight)));
                for (int i=0;i<choices.Count;i++)
                {
                    if (GUI.Button(new Rect(0,i*rowHeight,width-64,38),choices[i].Text,button))
                    {
                        if (!story.TryChoose(i,out var error)) Message=error;
                        storyScroll = Vector2.zero;
                        break;
                    }
                }
                GUI.EndScrollView();
            }
            GUI.Label(new Rect(x+20,y+height-49,width-40,40),
                "Roteiro: dados externos do usuario. Duelo e progresso reais ainda pendentes.",small);
        }
    }
}
