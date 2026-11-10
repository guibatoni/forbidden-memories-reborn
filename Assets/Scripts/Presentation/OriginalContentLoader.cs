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
            public FusionData[] fusions;
            public RitualData[] rituals;
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
            public string effectText;
            public string effectSource;
            public string effectStatus;
            public string typeLabel;
            public string guardianStarA;
            public string guardianStarB;
        }
        [Serializable] private sealed class FusionData
        {
            public int materialA;
            public int materialB;
            public int result;
            public bool verified;
        }
        [Serializable] private sealed class RitualData
        {
            public int ritualCard;
            public int[] tributes;
            public int result;
            public bool verified;
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
                if (parsed.source == null || string.IsNullOrWhiteSpace(parsed.source.edition) ||
                    !(parsed.source.edition == "original-ps1" ||
                      parsed.source.edition.StartsWith("original-ps1-",StringComparison.Ordinal)))
                    throw new InvalidDataException("Fonte precisa informar edition=original-ps1 ou perfil original-ps1-*");
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
                    GuardianStar a = ParseStar(entry.guardianStarA);
                    GuardianStar b = ParseStar(entry.guardianStarB);
                    if ((a == GuardianStar.None) != (b == GuardianStar.None))
                        throw new InvalidDataException("Guardian Stars devem vir em par: #" + entry.id);
                    cards.Add(new CardDefinition(entry.id.ToString("000"),entry.name,kind,
                        entry.attack,entry.defense,entry.description,a,b,
                        entry.effectText,entry.effectSource,entry.effectStatus,
                        entry.typeLabel));
                }
                // Only fusions explicitly flagged as verified are accepted.
                // The flag is source metadata, not proof of binary equivalence.
                var fusions = new FusionCatalog();
                var lookup = new Dictionary<string,CardDefinition>(StringComparer.Ordinal);
                foreach (var card in cards) lookup.Add(card.Id,card);
                if (parsed.fusions != null)
                {
                    foreach (var entry in parsed.fusions)
                    {
                        if (!entry.verified)
                            throw new InvalidDataException("Fusao sem verified=true");
                        if (entry.materialA < 1 || entry.materialA > 722 ||
                            entry.materialB < 1 || entry.materialB > 722 ||
                            entry.result < 1 || entry.result > 722)
                            throw new InvalidDataException("ID de fusao fora de 1..722");
                        string aId=entry.materialA.ToString("000");
                        string bId=entry.materialB.ToString("000");
                        string rId=entry.result.ToString("000");
                        if (!lookup.ContainsKey(aId) || !lookup.ContainsKey(bId) ||
                            !lookup.TryGetValue(rId,out var result) || result.Kind != CardKind.Monster)
                            throw new InvalidDataException("Fusao referencia cartas desconhecidas ou resultado nao monstro");
                        fusions.Add(aId,bId,result);
                    }
                }
                var rituals = new RitualCatalog();
                if (parsed.rituals != null)
                {
                    foreach (var entry in parsed.rituals)
                    {
                        if (entry == null || !entry.verified ||
                            entry.ritualCard < 1 || entry.ritualCard > 722 ||
                            entry.result < 1 || entry.result > 722 ||
                            entry.tributes == null || entry.tributes.Length != 3)
                            throw new InvalidDataException("Receita de ritual PS1 incompleta");

                        string ritualId = entry.ritualCard.ToString("000");
                        string resultId = entry.result.ToString("000");
                        if (!lookup.TryGetValue(ritualId, out var ritualCard) ||
                            ritualCard.Kind != CardKind.Ritual ||
                            !lookup.TryGetValue(resultId, out var summon) ||
                            summon.Kind != CardKind.Monster)
                            throw new InvalidDataException("Ritual com tipos incompatíveis");

                        var materials = new string[3];
                        for (int i = 0; i < 3; i++)
                        {
                            int tribute = entry.tributes[i];
                            if (tribute < 1 || tribute > 722)
                                throw new InvalidDataException("ID de tributo fora do PS1");
                            string id = tribute.ToString("000");
                            if (!lookup.TryGetValue(id, out var card) ||
                                card.Kind != CardKind.Monster)
                                throw new InvalidDataException("Tributo nao e monstro: " + id);
                            materials[i] = id;
                        }
                        rituals.Add(ritualId, materials, summon);
                    }
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
                    cards,opponents,script,fusions,rituals);
                // Importacao de JSON nao garante que os bytes conferem com a ISO informada.
                status = $"Importado (nao validado contra a ISO): {cards.Count}/722 cartas, " +
                    $"{bundle.MissingDescriptions} descricoes vazias, {opponents.Count} oponentes, " +
                    $"{(script == null ? 0 : script.Nodes.Count)} eventos, " +
                    $"{fusions.Count} receitas importadas, {rituals.Count} rituais, " +
                    $"{cards.FindAll(c => c.HasEffectText).Count} textos de efeito informativos.";
                return true;
            }
            catch (Exception ex)
            {
                status = "Arquivo de dados rejeitado: " + ex.Message;
                Debug.LogError("REBORN Content: " + status);
                return false;
            }
        }

        private static GuardianStar ParseStar(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return GuardianStar.None;
            if (!Enum.TryParse(value.Trim(),true,out GuardianStar star) || star == GuardianStar.None)
                throw new InvalidDataException("Guardian Star desconhecida: " + value);
            return star;
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
