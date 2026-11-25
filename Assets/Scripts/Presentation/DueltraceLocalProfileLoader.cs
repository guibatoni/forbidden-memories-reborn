using System;
using System.Collections.Generic;
using System.IO;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>
    /// Read-only fallback to the already-installed DUELTRACE Original profile.
    /// Does not depend on Python, copy a disc, or modify any DUELTRACE file.
    /// The full same-disc Reborn JSON always takes precedence (it also has
    /// descriptions, Guardian Stars and rituals not present in this profile).
    /// </summary>
    internal static class DueltraceLocalProfileLoader
    {
        [Serializable] private sealed class Registry
        {
            public RegistryEntry[] entries;
        }
        [Serializable] private sealed class RegistryEntry
        {
            public string file_name;
            public string profile_path;
            public string sha256;
        }
        [Serializable] private sealed class Profile
        {
            public string rom_sha256;
            public ProfileVerified verified;
            public ProfileCard[] cards;
            public ProfileFusion[] fusion_recipes;
            public ProfileOpponent[] opponents;
        }
        [Serializable] private sealed class ProfileVerified
        {
            public string cartas;
        }
        [Serializable] private sealed class ProfileCard
        {
            public int id;
            public string name;
            public int type_id;
            public string type_label;
            public string description;
            public int atk;
            public int @def;
            public string effect_text;
            public string effect_source;
            public string effect_status;
        }
        [Serializable] private sealed class ProfileFusion
        {
            public int a;
            public int b;
            public int result;
            public string validation;
        }
        [Serializable] private sealed class ProfileOpponent
        {
            public int id;
            public string name;
        }

        public static bool TryLoad(out GameContentBundle bundle, out string status)
        {
            bundle = null;
            var home = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "ForbiddenMemoriesMultiMod");
            var registryFile = Path.Combine(home, "registro_mods.json");
            if (!File.Exists(registryFile))
            {
                status = "DUELTRACE nao encontrado em " + registryFile +
                    ". Execute IMPORTAR_DO_DUELTRACE.ps1 antes do Play.";
                return false;
            }
            try
            {
                var registry = JsonUtility.FromJson<Registry>(
                    File.ReadAllText(registryFile));
                if (registry == null || registry.entries == null)
                    throw new InvalidDataException("Registro DUELTRACE nao contem perfis");
                var candidates = new List<RegistryEntry>();
                var homePrefix = Path.GetFullPath(home)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                foreach (var entry in registry.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.file_name) ||
                        entry.file_name.IndexOf("Original",
                            StringComparison.OrdinalIgnoreCase) < 0 ||
                        string.IsNullOrWhiteSpace(entry.profile_path))
                        continue;
                    // Never follow arbitrary paths from a tampered registry.
                    var resolved = Path.GetFullPath(entry.profile_path);
                    if (!resolved.StartsWith(homePrefix,
                        StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (File.Exists(resolved)) candidates.Add(entry);
                }
                if (candidates.Count != 1)
                    throw new InvalidDataException(
                        "Selecione um unico perfil Original no DUELTRACE (" +
                        candidates.Count + " candidatos). Ou importe um profile.json " +
                        "especifico com IMPORTAR_DO_DUELTRACE.ps1.");
                var selected = candidates[0];
                var profile = JsonUtility.FromJson<Profile>(
                    File.ReadAllText(Path.GetFullPath(selected.profile_path)));
                if (profile == null || profile.verified == null ||
                    profile.verified.cartas != "verificado" ||
                    profile.cards == null || profile.cards.Length != 722)
                    throw new InvalidDataException(
                        "O perfil Original nao possui 722 cartas validadas");
                if (!ValidSha(profile.rom_sha256) ||
                    !string.Equals(profile.rom_sha256, selected.sha256,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("SHA-256 do perfil nao confere com o registro");
                var cards = new CardDefinition[722];
                foreach (var c in profile.cards)
                {
                    if (c == null || c.id < 1 || c.id > 722 ||
                        cards[c.id - 1] != null || string.IsNullOrWhiteSpace(c.name) ||
                        c.type_id < 0 || c.type_id > 23 ||
                        string.IsNullOrWhiteSpace(c.type_label) ||
                        c.atk < 0 || c.@def < 0)
                        throw new InvalidDataException("ID, nome ou tipo de carta invalido");
                    CardKind kind = c.type_id == 20 ? CardKind.Spell :
                        c.type_id == 21 ? CardKind.Trap :
                        c.type_id == 22 ? CardKind.Ritual :
                        c.type_id == 23 ? CardKind.Equip : CardKind.Monster;
                    string effect = c.effect_text ?? "";
                    string effectSource = c.effect_source ?? "";
                    string effectStatus = c.effect_status ?? "";
                    if (!string.IsNullOrWhiteSpace(effect) &&
                        (string.IsNullOrWhiteSpace(effectSource) ||
                         string.IsNullOrWhiteSpace(effectStatus) ||
                         effectStatus == "nao_identificado"))
                        throw new InvalidDataException("Efeito sem procedencia: #" + c.id);
                    // DUELTRACE profile lacks Guardian Stars: deliberately None,
                    // so no made-up bonus or visually invented attribute appears.
                    cards[c.id - 1] = new CardDefinition(
                        c.id.ToString("000"), c.name, kind, c.atk, c.@def,
                        c.description ?? "", GuardianStar.None, GuardianStar.None,
                        effect, effectSource, effectStatus, c.type_label);
                }
                for (int i = 0; i < cards.Length; i++)
                    if (cards[i] == null)
                        throw new InvalidDataException("Carta nao encontrada: #" + (i+1));

                var fusions = new FusionCatalog();
                var importedRecipes = new HashSet<string>(StringComparer.Ordinal);
                if (profile.fusion_recipes != null)
                    foreach (var f in profile.fusion_recipes)
                    {
                        // Manually observed fusions only. Reviewed batches need
                        // the Python bridge's 7-bank fingerprint validation.
                        if (f == null || f.validation != "observada_manualmente_no_jogo")
                            continue;
                        if (f.a < 1 || f.a > 722 || f.b < 1 || f.b > 722 ||
                            f.result < 1 || f.result > 722 ||
                            cards[f.result-1].Kind != CardKind.Monster)
                            throw new InvalidDataException("Fusao confirmada com ID invalido");
                        string a = f.a.ToString("000");
                        string b = f.b.ToString("000");
                        var key = string.CompareOrdinal(a,b) <= 0 ? a+"|"+b : b+"|"+a;
                        if (!importedRecipes.Add(key))
                            throw new InvalidDataException("Fusao confirmada duplicada: " + key);
                        fusions.Add(a,b,cards[f.result-1]);
                    }
                var opponents = new List<OpponentDefinition>();
                var seenOpponents = new HashSet<int>();
                if (profile.opponents != null)
                    foreach (var opponent in profile.opponents)
                    {
                        if (opponent == null || opponent.id < 0 ||
                            string.IsNullOrWhiteSpace(opponent.name) ||
                            !seenOpponents.Add(opponent.id))
                            throw new InvalidDataException("Oponente DUELTRACE invalido");
                        opponents.Add(new OpponentDefinition(opponent.id,
                            opponent.name, "dueltrace-" + opponent.id));
                    }
                bundle = new GameContentBundle("original-ps1-dueltrace-live", "und",
                    profile.rom_sha256, cards, opponents, null,
                    fusions, new RitualCatalog());
                status = "DUELTRACE ORIGINAL LIDO DIRETAMENTE: 722 cartas reais, " +
                    fusions.Count + " fusoes confirmadas; descricoes disponiveis: " +
                    (722 - bundle.MissingDescriptions) +
                    ". Guardian Stars, rituais e imagens devem ser complementados " +
                    "com importacao local da mesma ISO. Sem cartas p01-p08.";
                return true;
            }
            catch (Exception ex)
            {
                status = "Perfil Original DUELTRACE nao carregado: " + ex.Message +
                    ". Use tools/IMPORTAR_DO_DUELTRACE.ps1 e reinicie o Play.";
                Debug.LogWarning("REBORN: " + status);
                return false;
            }
        }

        private static bool ValidSha(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64) return false;
            foreach (var c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') ||
                      (c >= 'A' && c <= 'F'))) return false;
            return true;
        }
    }
}
