using Reborn.Core;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>HUD IMGUI de prototipo, sem dependencias de fontes externas.</summary>
    public sealed partial class ArenaHud : MonoBehaviour
    {
        private DuelSession session;
        private DuelMatch match;
        public DuelMatch Match => match;
        public bool DuelModeActive { get; private set; }
        public bool AttackMode { get; set; }
        public int SelectedAttacker { get; set; } = -1;
        public int PreviewTargetSlot { get; set; } = -1;
        public bool UseDefense { get; private set; }
        public bool UseSecondaryStar { get; private set; }
        public int FusionCount { get; set; }
        public bool OpponentBusy { get; private set; }
        public bool PlayerAttackBusy { get; private set; }
        private bool missingOriginalContent;
        private bool demoAcknowledged;
        public bool ContentImportRequired => missingOriginalContent && !demoAcknowledged;
        public bool InputLocked => OpponentBusy || PlayerAttackBusy || ContentImportRequired || PlacementWizardOpen || EquipChoiceOpen || StarterSelectionRequired;

        /// <summary>No arbitrary placeholder gameplay before explicit demo opt-in.</summary>
        public void ConfigureContentAvailability(bool originalLoaded)
        {
            missingOriginalContent = !originalLoaded;
            demoAcknowledged = false;
        }
        public event Action<int, int> PlayerAttackRequested;
        public void SetPlayerAttackBusy(bool active) { PlayerAttackBusy = active; }
        private int opponentPaceIndex; // 0 detalhado, 1 normal, 2 rapido
        public float OpponentPace => opponentPaceIndex == 0 ? 1.4f :
            (opponentPaceIndex == 1 ? 0.9f : 0.45f);
        public string OpponentPaceLabel => opponentPaceIndex == 0 ? "Detalhado" :
            (opponentPaceIndex == 1 ? "Normal" : "Rapido");
        public void SetOpponentBusy(bool active) { OpponentBusy = active; }
        public event Action NextOpponentRequested;
        public string OpponentSource { get; private set; } = "IA de teste";
        public bool HasOpponentSelection { get; private set; }
        public void SetOpponentSource(string name, bool hasAlternatives)
        {
            OpponentSource = string.IsNullOrWhiteSpace(name) ? "IA de teste" : name;
            HasOpponentSelection = hasAlternatives;
        }
        public string AiInsight { get; private set; } = "IA adaptativa";
        public void SetAiInsight(string strategy, int completedDuels)
        {
            AiInsight = "IA: " + strategy + " | " + completedDuels + " duelos";
        }
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
        public CardDefinition HoveredCard { get; set; }
        public CardDefinition FieldFusionPreview { get; set; }
        public bool CardPreviewEnabled { get; private set; } = true;
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
            CancelPlacementChoices();
            CancelEquipChoice();
            DuelModeActive = active;
            OpponentBusy = false;
            PlayerAttackBusy = false;
            recentActions.Clear();
            CloseCardInspection();
            FieldFusionPreview = null;
            SelectedHandIndex = -1;
            HoveredCard = null;
            SelectedAttacker = -1;
            PreviewTargetSlot = -1;
            SelectedFieldMonster = -1;
            SelectedSupportSlot = -1;
            FusionCount = 0;
            AttackMode = false;
            Message = active ? "MODO DUELO 0.15: controles classicos, motor experimental." :
                "Sandbox visual 0.3 restaurado.";
            ModeChanged?.Invoke(active);
        }

        public void ResetMatch(DuelMatch model)
        {
            CancelPlacementChoices();
            CancelEquipChoice();
            match = model;
            OpponentBusy = false;
            PlayerAttackBusy = false;
            recentActions.Clear();
            CloseCardInspection();
            FieldFusionPreview = null;
            SelectedHandIndex = -1;
            HoveredCard = null;
            SelectedAttacker = -1;
            PreviewTargetSlot = -1;
            SelectedFieldMonster = -1;
            SelectedSupportSlot = -1;
            FusionCount = 0;
            AttackMode = false;
            ModeChanged?.Invoke(DuelModeActive);
            Message = "Duelo experimental reiniciado.";
        }

        public void RequestPlayerAttack(int fromSlot, int targetSlot)
        {
            if (match == null || !DuelModeActive || InputLocked) return;
            if (!match.CanAttack(DuelSide.Player, fromSlot, targetSlot, out var reason))
            {
                Message = reason;
                return;
            }
            SelectedAttacker = -1;
            PreviewTargetSlot = -1;
            PlayerAttackRequested?.Invoke(fromSlot, targetSlot);
        }

        public void TryDirectAttack()
        {
            if (SelectedAttacker >= 0) RequestPlayerAttack(SelectedAttacker, -1);
            else Message = "Selecione um monstro atacante antes do ataque direto.";
        }

        public bool IsPointerOverHud(Vector2 pointer)
        {
            float x = pointer.x;
            float y = Screen.height - pointer.y;
            return ContentImportRequired || StarterSelectionRequired || PlacementWizardOpen || EquipChoiceOpen || CardInspectionOpen || showStory || (x < 264f && y < (DuelModeActive ? 543f : 315f)) ||
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
            if (ContentImportRequired)
            {
                DrawMissingOriginalContent();
                return;
            }
            if (StarterSelectionRequired)
            {
                DrawStarterDeckChoice();
                return;
            }
            if (EquipChoiceOpen)
            {
                DrawEquipChoice();
                return;
            }
            if (PlacementWizardOpen)
            {
                DrawPlacementChoices();
                return;
            }
            if (CardInspectionOpen)
            {
                DrawPinnedCardInspection();
                return;
            }
            if (DuelModeActive && match != null)
            {
                DrawMatchHud();
                DrawCardInspection();
                return;
            }
            float right = Screen.width - 208f;
            Panel(new Rect(12,12,251,279),new Color(0.015f,0.027f,0.06f,0.94f));
            GUI.Label(new Rect(23,17,220,32),"REBORN  |  0.6",heading);
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
            if (GUI.Button(new Rect(right+12,468,170,38),"Duelo experimental 0.6",button))
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
                    "Clique em carta > clique em zona\nArraste carta ate a zona\nBotao direito: inspecionar carta\nRoda do mouse: zoom\nTecla R: camera inicial\nESC: cancelar selecao",small);
            }
            DrawCardInspection();
            if (showStory && story != null) DrawStory();
        }

        /// <summary>
        /// Do not silently label fictional p01-p08 cards as the retail catalog.
        /// The game remains playable in an explicitly acknowledged demo mode.
        /// </summary>
        private void DrawMissingOriginalContent()
        {
            Panel(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.009f, 0.015f, 0.035f, 0.985f));
            float w = Mathf.Min(700f, Screen.width - 28f);
            float h = Mathf.Min(450f, Screen.height - 22f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Panel(new Rect(x,y,w,h),new Color(0.035f, 0.053f, 0.105f, 0.99f));
            Panel(new Rect(x+15f,y+14f,w-30f,5f),new Color(0.90f,0.68f,0.31f));
            GUI.Label(new Rect(x+25f,y+32f,w-50f,55f),
                "CATALOGO ORIGINAL AINDA NAO CARREGADO",heading);
            var info = "O Reborn encontrou apenas as cartas ficticias de demonstracao.\n" +
                "Nenhuma carta p01-p08 pertence ao catalogo original do PS1.\n\n" +
                "1. Pare o modo Play no Unity.\n" +
                "2. Na pasta do projeto, execute no PowerShell:\n" +
                "   .\\tools\\IMPORTAR_DO_DUELTRACE.ps1 -ArtesLocais\n" +
                "3. Aguarde a validacao das 722 cartas e reinicie o Play.\n\n" +
                "O Unity tambem tenta ler automaticamente um unico perfil\n" +
                "Original validado do DUELTRACE instalado neste Windows.";
            GUI.Label(new Rect(x+25f,y+92f,w-50f,250f),info,body);
            GUI.Label(new Rect(x+25f,y+h-94f,w-50f,48f),
                "Diagnostico: " + importStatus,small);
            if (GUI.Button(new Rect(x+25f,y+h-45f,w-50f,33f),
                "Continuar apenas no modo DEMONSTRACAO",button))
            {
                demoAcknowledged = true;
                Announce("DEMONSTRACAO: cartas ficticias p01-p08. Importe o catalogo Original.");
            }
        }

        private void DrawMatchHud()
        {
            float right = Screen.width - 208f;
            bool finished = match.Phase == MatchPhase.Finished;
            Panel(new Rect(12,12,251,305),new Color(0.015f,0.027f,0.06f,0.94f));
            GUI.Label(new Rect(23,17,222,33),"REBORN | 0.21",heading);
            GUI.Label(new Rect(23,55,224,26),"Seus LP: " + match.Player.Life,body);
            GUI.Label(new Rect(23,81,224,26),"Oponente LP: " + match.Opponent.Life,body);
            GUI.Label(new Rect(23,107,224,25),"Deck: " + match.Player.DeckCount +
                "   Cemiterio: " + match.Player.Graveyard.Count,small);
            string phaseName = finished ? "Encerrado" :
                (match.Phase == MatchPhase.Battle ? "Batalha" : "Principal");
            GUI.Label(new Rect(23,130,224,25),"Turno: " + match.Turn +
                "   Fase: " + phaseName,small);
            GUI.Label(new Rect(23,294,224,27),
                "Terreno: " + match.CurrentTerrainName, small);
            GUI.Label(new Rect(23,156,230,24),"Materiais da mao: " + FusionCount + "/5",small);
            if (FieldFusionPreview != null && match.Phase == MatchPhase.Main)
            {
                GUI.Label(new Rect(23,183,227,44),
                    (SelectedHandIndex >= 0 &&
                     SelectedHandIndex < match.Player.Hand.Count &&
                     match.Player.Hand[SelectedHandIndex].Kind == CardKind.Ritual ?
                     "RITUAL: " : "FUSAO: ") + FieldFusionPreview.Name,body);
                GUI.Label(new Rect(23,226,227,24),
                    "ATK " + FieldFusionPreview.Attack +
                    "  DEF " + FieldFusionPreview.Defense,small);
                GUI.Label(new Rect(23,249,227,50),
                    "Guardian: " + FieldFusionPreview.StarA +
                    " / " + FieldFusionPreview.StarB,small);
            }
            else if (SelectedHandIndex >= 0 && SelectedHandIndex < match.Player.Hand.Count)
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
            bool wantsForecast = AttackMode && SelectedAttacker >= 0 &&
                !InputLocked && !finished && match.Phase == MatchPhase.Battle;
            if (wantsForecast)
            {
                GUI.Label(new Rect(23,339,225,25),"PREVIA DE COMBATE",body);
                int previewTarget = PreviewTargetSlot >= 0 ? PreviewTargetSlot : -1;
                if (previewTarget < 0 && match.Opponent.MonsterCount > 0)
                {
                    GUI.Label(new Rect(23,372,225,132),
                        "Aponte para uma das cinco zonas de monstros adversarios. " +
                        "Os valores efetivos e o resultado previsto aparecerao aqui.",small);
                }
                else if (previewTarget >= 0 && match.Opponent.Monster(previewTarget) != null &&
                    match.Opponent.Monster(previewTarget).FaceDown)
                {
                    GUI.Label(new Rect(23,370,225,140),
                        "Carta adversaria virada para baixo. Os valores reais so " +
                        "serao revelados quando o ataque for resolvido.",small);
                }
                else if (match.TryPreviewAttack(DuelSide.Player,SelectedAttacker,
                    previewTarget,out var estimate,out var reason))
                {
                    GUI.Label(new Rect(23,370,225,155),
                        CombatExplanation.Describe(estimate),small);
                }
                else
                {
                    GUI.Label(new Rect(23,370,225,155),
                        "Ataque indisponivel: " + reason,small);
                }
            }
            else
            {
                GUI.Label(new Rect(23,339,225,25),"REGISTRO DO DUELO",body);
                int shown = Mathf.Min(4, recentActions.Count);
                for (int i = 0; i < shown; i++)
                {
                    int sourceIndex = recentActions.Count - shown + i;
                    GUI.Label(new Rect(23,371+i*39,227,39),
                        (i+1) + ". " + recentActions[sourceIndex],small);
                }
            }
            Panel(new Rect(right,12,196,612),new Color(0.015f,0.027f,0.06f,0.94f));
            GUI.Label(new Rect(right+12,20,180,32),"DUELO 0.17",heading);
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
                if (GUI.Button(new Rect(right+12,359,170,34),
                    CardPreviewEnabled ? "Cartas: detalhes ON" : "Cartas: detalhes OFF",button))
                    CardPreviewEnabled = !CardPreviewEnabled;
                GUI.Label(new Rect(right+13,409,175,85),
                    "Os comandos de combate ficam bloqueados apos o fim da partida.",small);
                GUI.Label(new Rect(right+13,475,175,23),AiInsight,small);
                GUI.Label(new Rect(right+13,500,175,39),"Oponente: " + OpponentSource,small);
                if (HasOpponentSelection &&
                    GUI.Button(new Rect(right+12,553,170,34),"Proximo oponente",button))
                    NextOpponentRequested?.Invoke();
            }
            else if (PlayerAttackBusy)
            {
                GUI.Label(new Rect(right+13,56,176,33),"Ataque em andamento",body);
                GUI.Label(new Rect(right+13,104,174,105),
                    "Acompanhe a trajetoria, o impacto e a resolucao do dano. " +
                    "As regras serao executadas apos o impacto.",small);
                GUI.Label(new Rect(right+13,250,174,95),
                    "Comandos temporariamente bloqueados para evitar ataques duplicados.",small);
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
                GUI.Label(new Rect(right+13,493,175,28),AiInsight,small);
            }
            else
            {
                bool player = match.CurrentSide == DuelSide.Player;
                GUI.Label(new Rect(right+13,52,175,27),
                    player ? (match.Phase == MatchPhase.Battle ?
                        "SUA BATALHA" : "SUA JOGADA") : "Turno da IA",small);
                GUI.Label(new Rect(right+13,77,175,19),AiInsight,small);
                GUI.Label(new Rect(right+12,101,177,23),
                    "JOGADA PASSO A PASSO",small);
                GUI.Label(new Rect(right+12,131,177,106),
                    "1. Selecione cartas da mao\n" +
                    "2. Escolha uma zona\n" +
                    "3. Guardian Star (tela)\n" +
                    "4. Face-up / Face-down (tela)\n" +
                    "5. ATK/DEF apos posicionar",small);
                GUI.enabled = player && !InputLocked;

                if (SelectedFieldMonster >= 0 && match.Player.Monster(SelectedFieldMonster) != null)
                {
                    var monster = match.Player.Monster(SelectedFieldMonster);
                    var next = monster.Position == BattlePosition.Attack ?
                        BattlePosition.Defense : BattlePosition.Attack;
                    if (!monster.FaceDown && monster.EquipCount > 0)
                        GUI.Label(new Rect(right+12,211,177,39),
                            "ATK " + monster.EffectiveAttack +
                            " / DEF " + monster.EffectiveDefense +
                            "  (+" + monster.EquipAttackBonus + " Equip)",small);
                    GUI.enabled = player && !InputLocked &&
                        match.CanChangePosition(DuelSide.Player,SelectedFieldMonster,next,out _);
                    if (GUI.Button(new Rect(right+12,250,170,36),
                        "Alterar para " + (next == BattlePosition.Attack ? "ATK" : "DEF"),button))
                        TryChangeSelectedMonster();
                    GUI.enabled = player && !InputLocked;
                }
                else if (SelectedSupportSlot >= 0)
                {
                    var chosenSupport = match.Player.Support(SelectedSupportSlot);
                    if (chosenSupport != null &&
                        chosenSupport.Kind == CardKind.Equip)
                    {
                        GUI.Label(new Rect(right+12,244,176,59),
                            "EQUIP setado: clique no seu monstro revelado para escolher o alvo.",small);
                    }
                    else
                    {
                        GUI.enabled = player && !InputLocked &&
                            match.CanActivateSupport(DuelSide.Player,
                                SelectedSupportSlot, out _);
                        if (GUI.Button(new Rect(right+12,250,170,36),
                            GUI.enabled ? "Ativar magia setada" : "Efeito indisponivel",button))
                            TryActivateSelectedSupport();
                        GUI.enabled = player && !InputLocked;
                    }
                }
                else GUI.Label(new Rect(right+13,247,174,37),
                    "Selecione um monstro no campo para mudar ATK/DEF",small);

                GUI.enabled = player && !InputLocked && match.PlayedThisTurn;
                if (GUI.Button(new Rect(right+12,296,170,39),
                    match.PlayedThisTurn ? "Passar turno [Espaco]" :
                    "Baixe uma carta primeiro",button))
                    TryPassPlayerTurn();
                GUI.enabled = true;
                GUI.Label(new Rect(right+12,342,177,53),
                    "Q/E: ATK/DEF  ESPACO: passar\nATACAR: monstro > alvo",small);
                if (GUI.Button(new Rect(right+12,404,170,33),"Novo duelo",button))
                    NewMatchRequested?.Invoke();
                if (GUI.Button(new Rect(right+12,446,170,33),"Voltar ao Sandbox",button))
                    SetDuelMode(false);
                if (GUI.Button(new Rect(right+12,488,170,30),
                    AnimationsEnabled ? "Animacoes: ON" : "Animacoes: OFF",button))
                    AnimationsEnabled = !AnimationsEnabled;
                if (GUI.Button(new Rect(right+12,529,170,30),
                    "Ritmo IA: " + OpponentPaceLabel,button))
                    opponentPaceIndex = (opponentPaceIndex+1)%3;
                if (HasOpponentSelection &&
                    GUI.Button(new Rect(right+12,568,170,30),"Proximo oponente",button))
                    NextOpponentRequested?.Invoke();
                else if (GUI.Button(new Rect(right+12,568,170,30),
                    CardPreviewEnabled ? "Detalhes: ON" : "Detalhes: OFF",button))
                    CardPreviewEnabled = !CardPreviewEnabled;
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

        /// <summary>
        /// A large readable 2D inspection overlay inside the already-reserved left HUD.
        /// It never reveals an opponent facedown card and never changes duel state.
        /// </summary>
        private void DrawCardInspection()
        {
            if (!CardPreviewEnabled) return;
            // Combat forecast takes precedence over enlarged card inspection.
            if (DuelModeActive && AttackMode && SelectedAttacker >= 0) return;
            CardDefinition card = FieldFusionPreview ?? HoveredCard;
            if (card == null && SelectedHandIndex >= 0)
            {
                if (DuelModeActive && match != null && SelectedHandIndex < match.Player.Hand.Count)
                    card = match.Player.Hand[SelectedHandIndex];
                else if (!DuelModeActive && session != null && SelectedHandIndex < session.Hand.Count)
                    card = session.Hand[SelectedHandIndex];
            }
            if (card == null) return;
            // The detail panel uses the existing left sidebar, not an additional
            // overlay over the five monster/support zones.
            float x = 12f, y = 12f, w = 251f;
            float h = Mathf.Min(530f, Screen.height - 88f);
            if (h < 310f) return;
            Panel(new Rect(x,y,w,h),new Color(0.025f,0.037f,0.075f,0.98f));
            Panel(new Rect(x+8,y+8,w-16,4),new Color(0.74f,0.57f,0.30f,1f));
            var nameStyle = Style(16,Color.white,FontStyle.Bold);
            GUI.Label(new Rect(x+13,y+17,w-26,44),card.Name,nameStyle);
            GUI.Label(new Rect(x+13,y+58,w-26,23),"ID: " + card.Id + "  |  " +
                CardFaceBuilder.KindLabel(card.Kind),small);
            float artworkHeight = Mathf.Min(225f, h * 0.43f);
            Panel(new Rect(x+12,y+85,w-24,artworkHeight+6f),
                new Color(0.71f,0.54f,0.29f,1f));
            GUI.DrawTexture(new Rect(x+15,y+88,w-30,artworkHeight),
                CardArtCatalog.Get(card),ScaleMode.StretchToFill);
            float ty = y+98f+artworkHeight;
            if (card.Kind == CardKind.Monster)
            {
                GUI.Label(new Rect(x+13,ty,w-26,27),
                    "ATK " + card.Attack + "   DEF " + card.Defense,body);
                ty += 27f;
                GUI.Label(new Rect(x+13,ty,w-26,24),
                    "Guardian: " + card.StarA + " / " + card.StarB,small);
                ty += 25f;
            }
            GUI.Label(new Rect(x+13,ty,w-26,20),"DESCRICAO",small);
            ty += 23f;
            GUI.Label(new Rect(x+13,ty,w-26,Mathf.Max(24f,y+h-ty-12f)),
                string.IsNullOrEmpty(card.Description) ?
                "Sem descricao importada." : card.Description,small);
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
