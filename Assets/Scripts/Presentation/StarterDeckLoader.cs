using System;
using System.IO;
using Reborn.Core;
using UnityEngine;

namespace Reborn.Presentation
{
    /// <summary>Loads only starter tables extracted from the same original disc.</summary>
    public static class StarterDeckLoader
    {
        [Serializable] private sealed class FileData
        {
            public int schema;
            public SourceData source;
            public PoolData[] starterPools;
        }
        [Serializable] private sealed class SourceData
        {
            public string edition;
            public string discSha256;
        }
        [Serializable] private sealed class PoolData
        {
            public int group;
            public int draws;
            public int[] weights;
        }

        public static string ExpectedPath => Path.Combine(
            Application.streamingAssetsPath, "RebornContent", "starter_pools.json");

        public static bool TryLoad(GameContentBundle content,
            out StarterDeckCatalog catalog, out string status)
        {
            catalog = null;
            if (content == null)
            {
                status = "Catalogo de cartas Original nao carregado.";
                return false;
            }
            if (!File.Exists(ExpectedPath))
            {
                status = "Sete pools iniciais nao encontrados. Reimporte sua imagem " +
                    "Original com IMPORTAR_ORIGINAL.ps1 para gerar starter_pools.json.";
                return false;
            }
            try
            {
                var obj = JsonUtility.FromJson<FileData>(
                    File.ReadAllText(ExpectedPath));
                if (obj == null || obj.schema != 1 || obj.source == null ||
                    obj.starterPools == null || obj.starterPools.Length != 7 ||
                    !string.Equals(obj.source.discSha256, content.SourceSha256,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        "Starter pools incompletos ou SHA diferente do catalogo");
                var rows = new int[7][];
                foreach (var pool in obj.starterPools)
                {
                    if (pool == null || pool.group < 0 || pool.group >= 7 ||
                        rows[pool.group] != null ||
                        pool.draws != StarterDeckCatalog.DrawCounts[pool.group])
                        throw new InvalidDataException("Grupo de starter original invalido");
                    rows[pool.group] = pool.weights;
                }
                catalog = new StarterDeckCatalog(content.Cards, rows);
                // Preflight sample before exposing the starting deck selector.
                catalog.Deal(new System.Random(194));
                status = "Sete pools iniciais do disco carregados. " +
                    "Quatro opcoes independentes podem ser sorteadas.";
                return true;
            }
            catch (Exception ex)
            {
                status = "Pools iniciais rejeitados: " + ex.Message;
                Debug.LogWarning("REBORN: " + status);
                return false;
            }
        }
    }
}
