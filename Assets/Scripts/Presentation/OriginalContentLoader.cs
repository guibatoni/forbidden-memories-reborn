using System;
using System.Collections.Generic;
using System.IO;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Le um JSON estruturado gerado a partir de uma ISO que o usuario possui.
    /// Nao faz engenharia reversa de ISO nem inclui dados protegidos no pacote.
    /// </summary>
    public static class OriginalContentLoader
    {
        private const string FileName = "original_export.json";
        public static string ExpectedPath => Path.Combine(Application.streamingAssetsPath,"RebornContent",FileName);

        [Serializable] private sealed class ExportFile
        {
            public int schema;
            public SourceData source;
            public CardData[] cards;
            public OpponentData[] opponents;
            public StoryData story;
        }
        [Serializable] private sealed class SourceData
        {
            public string edition;
            public string language;
            public string discSha256;
        }
        [Serializable] private sealed class CardData
        {
            public int id;
            public string name;
            public string kind;
            public int attack;
            public int defense;
            public string description;
        }
        [Serializable] private sealed class OpponentData
        {
            public int id;
            public string name;
            public string deckKey;
        }
        [Serializable] private sealed class StoryData
        {
            public string startNodeId;
            public StoryNodeData[] nodes;
        }
        [Serializable] private sealed class StoryNodeData
        {
            public string id;
            public string location;
            public string speaker;
            public string text;
            public int opponentId;
            public string winNodeId;
            public string loseNodeId;
            public string winSetsFlag;
            public string loseSetsFlag;
            public StoryChoiceData[] choices;
        }
        [Serializable] private sealed class StoryChoiceData
        {
            public string text;
            public string nextNodeId;
            public string requiresFlag;
            public string forbidsFlag;
            public string setsFlag;
        }

        public static bool TryLoad(out GameContentBundle bundle, out string status)
        {
            bundle = null;
            if (!File.Exists(ExpectedPath))
            {
                status = "Dados originais nao importados: usando cartas demonstrativas.";
                return false;
            }
            try
            {
                var parsed = JsonUtility.FromJson<ExportFile>(File.ReadAllText(ExpectedPath));
                if (parsed == null || parsed.schema != 1 || parsed.cards == null || parsed.cards.Length == 0)
                    throw new InvalidDataException("Schema 1 e lista de cartas sao obrigatorios");
                if (parsed.source == null || !string.Equals(parsed.source.edition,"original-ps1",StringComparison.Ordinal))
                    throw new InvalidDataException("Fonte precisa informar edition=original-ps1");
                var sha = parsed.source.discSha256 ?? "";
                if (sha.Length > 0 && (sha.Length != 64 || !IsHex(sha)))
                    throw new InvalidDataException("discSha256 deve ser SHA-256 de 64 caracteres hexadecimais");

                var cards = new List<CardDefinition>(parsed.cards.Length);
                foreach (var entry in parsed.cards)
                {
                    if (entry.id < 1 || entry.id > 722)
                        throw new InvalidDataException("ID fora do conjunto original de 1 a 722: " + entry.id);
                    if (!Enum.TryParse(entry.kind, true, out CardKind kind))
                        throw new InvalidDataException($"Tipo de carta desconhecido: {entry.kind} / #{entry.id}");
                    cards.Add(new CardDefinition(entry.id.ToString("000"),entry.name,kind,
                        entry.attack,entry.defense,entry.description));
                }
                var opponents = new List<OpponentDefinition>();
                if (parsed.opponents != null)
                    foreach (var entry in parsed.opponents)
                        opponents.Add(new OpponentDefinition(entry.id,entry.name,entry.deckKey));
                CampaignScript script = null;
                if (parsed.story != null && parsed.story.nodes != null && parsed.story.nodes.Length > 0)
                {
                    var nodes = new List<CampaignNode>();
                    foreach (var entry in parsed.story.nodes)
                    {
                        var choices = new List<CampaignChoice>();
                        if (entry.choices != null)
                            foreach (var choice in entry.choices)
                                choices.Add(new CampaignChoice(choice.text,choice.nextNodeId,
                                    choice.requiresFlag,choice.forbidsFlag,choice.setsFlag));
                        nodes.Add(new CampaignNode(entry.id,entry.location,entry.speaker,entry.text,
                            entry.opponentId,entry.winNodeId,entry.loseNodeId,choices,
                            entry.winSetsFlag,entry.loseSetsFlag));
                    }
                    script = new CampaignScript(parsed.story.startNodeId,nodes,opponents);
                }
                bundle = new GameContentBundle(parsed.source.edition,parsed.source.language,sha,
                    cards,opponents,script);
                // Importacao de JSON nao garante que os bytes conferem com a ISO informada.
                status = $"Importado (nao validado contra a ISO): {cards.Count}/722 cartas, " +
                    $"{bundle.MissingDescriptions} descricoes vazias, {opponents.Count} oponentes, " +
                    $"{(script == null ? 0 : script.Nodes.Count)} eventos.";
                return true;
            }
            catch (Exception ex)
            {
                status = "Arquivo de dados rejeitado: " + ex.Message;
                Debug.LogError("REBORN Content: " + status);
                return false;
            }
        }

        private static bool IsHex(string value)
        {
            foreach (var c in value)
                if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F'))
                    return false;
            return true;
        }
    }
}
