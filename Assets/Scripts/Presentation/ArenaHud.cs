using Reborn.Core;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>HUD IMGUI de prototipo, sem dependencias de fontes externas.</summary>
    public sealed class ArenaHud : MonoBehaviour
    {
        private DuelSession session;
        private DuelMatch match;
        public DuelMatch Match => match;
        public bool DuelModeActive { get; private set; }
        public bool AttackMode { get; set; }
        public int SelectedAttacker { get; set; } = -1;
        public bool UseDefense { get; private set; }
        public bool UseSecondaryStar { get; private set; }
        public int FusionCount { get; set; }
        public bool OpponentBusy { get; private set; }
        private int opponentPaceIndex; // 0 detalhado, 1 normal, 2 rapido
        public float OpponentPace => opponentPaceIndex == 0 ? 1.4f :
            (opponentPaceIndex == 1 ? 0.9f : 0.45f);
        public string OpponentPaceLabel => opponentPaceIndex == 0 ? "Detalhado" :
            (opponentPaceIndex == 1 ? "Normal" : "Rapido");
        public void SetOpponentBusy(bool active) { OpponentBusy = active; }
        public event Action OpponentTurnRequested;
        public event Action<bool> ModeChanged;
        private ArenaFactory arena;
        private CampaignRuntime story;
        private readonly Dictionary<int, OpponentDefinition> opponents = new Dictionary<int, OpponentDefinition>();
        private string importStatus = "";
        private readonly List<string> recentActions = new List<string>(5);
        public void Announce(string action)
        {
            Message = action ?? "";
            if (string.IsNullOrEmpty(Message)) return;
            recentActions.Add(Message);
            while (recentActions.Count > 5) recentActions.RemoveAt(0);
        }
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

        public void InitializeMatch(DuelMatch model)
        {
            match = model;
        }

        public void SetDuelMode(bool active)
        {
            if (match == null) return;
            DuelModeActive = active;
            OpponentBusy = false;
            recentActions.Clear();
            SelectedHandIndex = -1;
            SelectedAttacker = -1;
            FusionCount = 0;
            AttackMode = false;
            Message = active ? "MODO DUELO 0.4: motor experimental, cartas ficticias." :
                "Sandbox visual 0.3 restaurado.";
            ModeChanged?.Invoke(active);
        }

        public void ResetMatch(DuelMatch model)
        {
            match = model;
            OpponentBusy = false;
            recentActions.Clear();
            SelectedHandIndex = -1;
            SelectedAttacker = -1;
            FusionCount = 0;
            AttackMode = false;
            ModeChanged?.Invoke(DuelModeActive);
            Message = "Duelo experimental reiniciado.";
        }

        public void TryDirectAttack()
        {
            if (match == null || !DuelModeActive || OpponentBusy || SelectedAttacker < 0) return;
            if (match.TryAttack(DuelSide.Player,SelectedAttacker,-1,out var reason))
            {
                Message = "Ataque direto declarado.";
                SelectedAttacker = -1;
            }
            else Message = reason;
        }

        public bool IsPointerOverHud(Vector2 pointer)
        {
            float x = pointer.x;
            float y = Screen.height - pointer.y;
            return showStory || (x < 264f && y < (DuelModeActive ? 543f : 315f)) ||
                (x > Screen.width - 220f && y < 638f);
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
            if (DuelModeActive && match != null) { DrawMatchHud(); return; }
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
            Panel(new Rect(right,12,196,508),new Color(0.015f,0.027f,0.06f,0.94f));
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
            GUI.Label(new Rect(right+13,425,175,32),"* Sandbox: sem motor real de duelos",small);
            if (GUI.Button(new Rect(right+12,468,170,38),"Duelo experimental 0.4",button))
                SetDuelMode(true);

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

        private void DrawMatchHud()
        {
            float right = Screen.width - 208f;
            bool finished = match.Phase == MatchPhase.Finished;
            Panel(new Rect(12,12,251,305),new Color(0.015f,0.027f,0.06f,0.94f));
            GUI.Label(new Rect(23,17,222,33),"REBORN | 0.4",heading);
            GUI.Label(new Rect(23,55,224,26),"Seus LP: " + match.Player.Life,body);
            GUI.Label(new Rect(23,81,224,26),"Oponente LP: " + match.Opponent.Life,body);
            GUI.Label(new Rect(23,107,224,25),"Deck: " + match.Player.DeckCount +
                "   Cemiterio: " + match.Player.Graveyard.Count,small);
            string phaseName = finished ? "Encerrado" :
                (match.Phase == MatchPhase.Battle ? "Batalha" : "Principal");
            GUI.Label(new Rect(23,130,224,25),"Turno: " + match.Turn +
                "   Fase: " + phaseName,small);
            GUI.Label(new Rect(23,156,230,24),"Selecao fusao: " + FusionCount + "/5",small);
            if (SelectedHandIndex >= 0 && SelectedHandIndex < match.Player.Hand.Count)
            {
                var card = match.Player.Hand[SelectedHandIndex];
                GUI.Label(new Rect(23,183,227,44),card.Name,body);
                GUI.Label(new Rect(23,226,227,24),"#"+card.Id+"  "+card.Kind,small);
                GUI.Label(new Rect(23,249,227,50),card.Description,small);
            }
            else GUI.Label(new Rect(23,186,223,100),
                finished ? "Partida finalizada. Use Novo duelo para recomecar." :
                "Clique numa carta para selecionar. Shift+clique adiciona materiais na ordem.",small);

            Panel(new Rect(12,330,251,205),new Color(0.015f,0.027f,0.06f,0.91f));
            GUI.Label(new Rect(23,339,225,25),"REGISTRO DO DUELO",body);
            int shown = Mathf.Min(4, recentActions.Count);
            for (int i = 0; i < shown; i++)
            {
                int sourceIndex = recentActions.Count - shown + i;
                GUI.Label(new Rect(23,371+i*39,227,39),
                    (i+1) + ". " + recentActions[sourceIndex],small);
            }
            Panel(new Rect(right,12,196,612),new Color(0.015f,0.027f,0.06f,0.94f));
            GUI.Label(new Rect(right+12,20,180,32),"DUELO 0.4",heading);
            if (finished)
            {
                GUI.Label(new Rect(right+13,54,175,30),"Duelo encerrado",small);
                GUI.Label(new Rect(right+12,91,175,39),
                    match.Winner == DuelSide.Player ? "VITORIA" : "DERROTA",heading);
                GUI.Label(new Rect(right+13,142,175,25),
                    "Seus LP: " + match.Player.Life,body);
                GUI.Label(new Rect(right+13,168,175,25),
                    "Oponente: " + match.Opponent.Life + " LP",small);
                if (GUI.Button(new Rect(right+12,214,170,39),"Novo duelo",button))
                    NewMatchRequested?.Invoke();
                if (GUI.Button(new Rect(right+12,263,170,39),"Voltar ao Sandbox",button))
                    SetDuelMode(false);
                if (GUI.Button(new Rect(right+12,312,170,36),
                    AnimationsEnabled ? "Animacoes: ON" : "Animacoes: OFF",button))
                    AnimationsEnabled = !AnimationsEnabled;
                GUI.Label(new Rect(right+13,374,175,100),
                    "Os comandos de combate ficam bloqueados apos o fim da partida.",small);
            }
            else if (OpponentBusy)
            {
                GUI.Label(new Rect(right+13,56,176,33),"Turno da IA em andamento",body);
                GUI.Label(new Rect(right+13,102,174,98),
                    "Acompanhe a invocacao, o atacante, o ataque e a passagem de turno, um por vez.",small);
                if (GUI.Button(new Rect(right+12,227,170,39),
                    "Ritmo IA: " + OpponentPaceLabel,button))
                    opponentPaceIndex = (opponentPaceIndex+1)%3;
                if (GUI.Button(new Rect(right+12,281,170,39),
                    AnimationsEnabled ? "Animacoes: ON" : "Animacoes: OFF",button))
                    AnimationsEnabled = !AnimationsEnabled;
                GUI.Label(new Rect(right+13,347,174,125),
                    "Comandos bloqueados durante a vez adversaria para evitar acoes simultaneas.",small);
            }
            else
            {
                GUI.Label(new Rect(right+13,52,175,28),
                    match.CurrentSide == DuelSide.Player ? "Seu turno" : "Turno da IA",small);
                if (GUI.Button(new Rect(right+12,91,170,34),"Iniciar combate",button))
                {
                    if (!match.TryBeginBattle(DuelSide.Player,out var error)) Message = error;
                    else Message = "Selecione seu monstro e depois um alvo.";
                }
                if (GUI.Button(new Rect(right+12,132,170,34),"Encerrar turno",button))
                {
                    if (!match.TryEndTurn(DuelSide.Player,out var error)) Message = error;
                    else
                    {
                        AttackMode = false; SelectedAttacker = -1;
                        if (match.Phase == MatchPhase.Finished)
                            Message = "Duelo encerrado.";
                        else
                        {
                            OpponentBusy = true;
                            Message = "Turno adversario: preparando jogada...";
                            OpponentTurnRequested?.Invoke();
                        }
                    }
                }
                if (GUI.Button(new Rect(right+12,173,170,34),AttackMode ? "Ataque: ON" : "Selecionar ataque",button))
                {
                    AttackMode = !AttackMode; SelectedAttacker = -1;
                }
                if (GUI.Button(new Rect(right+12,214,170,34),"Atacar diretamente",button))
                    TryDirectAttack();
                if (GUI.Button(new Rect(right+12,255,170,34),
                    UseDefense ? "Posicao: DEF" : "Posicao: ATK",button))
                    UseDefense = !UseDefense;
                if (GUI.Button(new Rect(right+12,296,170,34),
                    UseSecondaryStar ? "Guardian: segunda" : "Guardian: primeira",button))
                    UseSecondaryStar = !UseSecondaryStar;
                if (GUI.Button(new Rect(right+12,337,170,34),"Novo duelo",button))
                    NewMatchRequested?.Invoke();
                if (GUI.Button(new Rect(right+12,378,170,34),"Voltar ao Sandbox",button))
                    SetDuelMode(false);
                if (GUI.Button(new Rect(right+12,419,170,34),
                    AnimationsEnabled ? "Animacoes: ON" : "Animacoes: OFF",button))
                    AnimationsEnabled = !AnimationsEnabled;
                if (GUI.Button(new Rect(right+12,465,170,34),
                    "Ritmo IA: " + OpponentPaceLabel,button))
                    opponentPaceIndex = (opponentPaceIndex+1)%3;
                GUI.Label(new Rect(right+13,509,175,95),
                    "Motor experimental. Magia, armadilha, equip e ritual ainda sem efeitos.",small);
            }
            float width = Mathf.Min(820f,Screen.width-550f);
            if (width > 280f)
            {
                Panel(new Rect((Screen.width-width)/2f,Screen.height-62,width,50),
                    new Color(0.015f,0.027f,0.06f,0.88f));
                string banner = finished ? (match.Winner == DuelSide.Player ?
                    "Vitoria! Duelo encerrado. Inicie uma nova partida." :
                    "Derrota. Duelo encerrado. Inicie uma nova partida.") : Message;
                GUI.Label(new Rect((Screen.width-width)/2f+12,Screen.height-56,width-24,39),banner,status);
            }
        }

        public event Action NewMatchRequested;

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