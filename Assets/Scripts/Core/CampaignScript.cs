using System;
using System.Collections.Generic;

namespace Reborn.Core
{
    /// <summary>Oponentes identificados pelo ID da fonte validada; nao presumir ordem linear.</summary>
    public sealed class OpponentDefinition
    {
        public int Id { get; }
        public string Name { get; }
        public string DeckKey { get; }
        public OpponentDefinition(int id, string name, string deckKey = "")
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome de oponente obrigatorio");
            Id = id; Name = name; DeckKey = deckKey ?? "";
        }
    }

    /// <summary>Uma alternativa leva a um no diferente, com flags opcionais de historia.</summary>
    public sealed class CampaignChoice
    {
        public string Text { get; }
        public string NextNodeId { get; }
        public string RequiresFlag { get; }
        public string ForbidsFlag { get; }
        public string SetsFlag { get; }
        public CampaignChoice(string text, string nextNodeId, string requiresFlag = "",
            string forbidsFlag = "", string setsFlag = "")
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Texto da escolha obrigatorio");
            if (string.IsNullOrWhiteSpace(nextNodeId)) throw new ArgumentException("Destino obrigatorio");
            Text = text; NextNodeId = nextNodeId;
            RequiresFlag = requiresFlag ?? ""; ForbidsFlag = forbidsFlag ?? "";
            SetsFlag = setsFlag ?? "";
        }
    }

    /// <summary>
    /// O no pode ser dialogo (opponentId=0) ou duelo (opponentId>0).
    /// O duelo so avanca apos o motor real informar vitoria ou derrota.
    /// </summary>
    public sealed class CampaignNode
    {
        public string Id { get; }
        public string Location { get; }
        public string Speaker { get; }
        public string Text { get; }
        public int OpponentId { get; }
        public string WinNodeId { get; }
        public string LoseNodeId { get; }
        public IReadOnlyList<CampaignChoice> Choices { get; }
        public string WinSetsFlag { get; }
        public string LoseSetsFlag { get; }
        public bool IsDuel => OpponentId >= 0;

        public CampaignNode(string id, string location, string speaker, string text, int opponentId,
            string winNodeId, string loseNodeId, IReadOnlyList<CampaignChoice> choices,
            string winSetsFlag = "", string loseSetsFlag = "")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID de evento obrigatorio");
            // -1 indica dialogo. ID 0 e valido para um oponente original indexado desde zero.
            if (opponentId < -1) throw new ArgumentOutOfRangeException(nameof(opponentId));
            Id = id; Location = location ?? ""; Speaker = speaker ?? ""; Text = text ?? "";
            OpponentId = opponentId; WinNodeId = winNodeId ?? ""; LoseNodeId = loseNodeId ?? "";
            WinSetsFlag = winSetsFlag ?? ""; LoseSetsFlag = loseSetsFlag ?? "";
            Choices = choices ?? new CampaignChoice[0];
        }
    }

    /// <summary>Representa uma sequencia ramificada de eventos; nao contem conteudo do jogo original.</summary>
    public sealed class CampaignScript
    {
        private readonly Dictionary<string, CampaignNode> nodes = new Dictionary<string, CampaignNode>(StringComparer.Ordinal);
        public string StartNodeId { get; }
        public IReadOnlyDictionary<string, CampaignNode> Nodes => nodes;

        public CampaignScript(string startNodeId, IReadOnlyList<CampaignNode> events,
            IReadOnlyList<OpponentDefinition> opponents)
        {
            if (string.IsNullOrWhiteSpace(startNodeId)) throw new ArgumentException("Inicio obrigatorio");
            if (events == null || events.Count == 0) throw new ArgumentException("Nenhum evento de campanha");
            var knownOpponents = new HashSet<int>();
            if (opponents != null) foreach (var opponent in opponents)
                if (!knownOpponents.Add(opponent.Id)) throw new ArgumentException("Oponente duplicado: " + opponent.Id);
            foreach (var evt in events)
            {
                if (evt == null || nodes.ContainsKey(evt.Id)) throw new ArgumentException("Evento ausente ou duplicado");
                nodes.Add(evt.Id,evt);
            }
            StartNodeId = startNodeId;
            RequireNode(startNodeId);
            foreach (var evt in events)
            {
                if (evt.IsDuel)
                {
                    if (!knownOpponents.Contains(evt.OpponentId))
                        throw new ArgumentException($"Oponente {evt.OpponentId} inexistente no evento {evt.Id}");
                    if (evt.Choices.Count > 0) throw new ArgumentException("Evento de duelo nao admite escolhas: " + evt.Id);
                    if (string.IsNullOrEmpty(evt.WinNodeId) || string.IsNullOrEmpty(evt.LoseNodeId))
                        throw new ArgumentException("Duelo requer caminhos de vitoria e derrota: " + evt.Id);
                    RequireNode(evt.WinNodeId); RequireNode(evt.LoseNodeId);
                }
                else
                {
                    if (!string.IsNullOrEmpty(evt.WinNodeId) || !string.IsNullOrEmpty(evt.LoseNodeId))
                        throw new ArgumentException("Somente duelos possuem caminhos de resultado: " + evt.Id);
                    foreach (var choice in evt.Choices) RequireNode(choice.NextNodeId);
                }
            }
        }

        private void RequireNode(string id)
        {
            if (!nodes.ContainsKey(id)) throw new ArgumentException("Evento de destino inexistente: " + id);
        }

        public CampaignNode Find(string id) => nodes[id];
    }

    /// <summary>Controla escolhas e flags sem incluir IU, Unity, temporizadores ou duelos simulados.</summary>
    public sealed class CampaignRuntime
    {
        private readonly CampaignScript script;
        private readonly HashSet<string> flags = new HashSet<string>(StringComparer.Ordinal);
        public CampaignNode Current { get; private set; }
        public IReadOnlyCollection<string> Flags => flags;
        public bool WaitingForDuel => Current.IsDuel;

        public CampaignRuntime(CampaignScript script)
        {
            this.script = script ?? throw new ArgumentNullException(nameof(script));
            Reset();
        }

        public void Reset()
        {
            flags.Clear(); Current = script.Find(script.StartNodeId);
        }

        public IReadOnlyList<CampaignChoice> AvailableChoices()
        {
            var choices = new List<CampaignChoice>();
            if (WaitingForDuel) return choices;
            foreach (var choice in Current.Choices)
                if (MatchesRequires(choice.RequiresFlag) && MatchesForbids(choice.ForbidsFlag))
                    choices.Add(choice);
            return choices;
        }

        // Flags separadas por virgula permitem condicoes como "mage1,mage2,mage3".
        private static IEnumerable<string> ParseFlags(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) yield break;
            foreach (var token in value.Split(','))
                if (!string.IsNullOrWhiteSpace(token)) yield return token.Trim();
        }

        private bool MatchesRequires(string requirement)
        {
            foreach (var flag in ParseFlags(requirement)) if (!flags.Contains(flag)) return false;
            return true;
        }

        private bool MatchesForbids(string forbidden)
        {
            foreach (var flag in ParseFlags(forbidden)) if (flags.Contains(flag)) return false;
            return true;
        }

        private void AddFlags(string requested)
        {
            foreach (var flag in ParseFlags(requested)) flags.Add(flag);
        }

        public bool TryChoose(int availableChoiceIndex, out string error)
        {
            error = "";
            if (WaitingForDuel) { error = "Duelo pendente: nao e possivel avancar no dialogo"; return false; }
            var available = AvailableChoices();
            if (availableChoiceIndex < 0 || availableChoiceIndex >= available.Count)
            { error = "Escolha inexistente ou bloqueada"; return false; }
            var selected = available[availableChoiceIndex];
            AddFlags(selected.SetsFlag);
            Current = script.Find(selected.NextNodeId);
            return true;
        }

        /// <summary>So o futuro motor de duelo deve chamar este metodo, nunca um botao de historia.</summary>
        public bool TryResolveDuel(bool playerWon, out string error)
        {
            error = "";
            if (!WaitingForDuel) { error = "Evento atual nao e duelo"; return false; }
            AddFlags(playerWon ? Current.WinSetsFlag : Current.LoseSetsFlag);
            Current = script.Find(playerWon ? Current.WinNodeId : Current.LoseNodeId);
            return true;
        }
    }
}
