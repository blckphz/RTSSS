using UnityEngine;

public class biomesManager : MonoBehaviour
{
    [Header("Biomes")]
    [Tooltip(
        "Parent containing all biome GameObjects. " +
        "Example: Biomes/Dirt and Biomes/Grass."
    )]
    [SerializeField]
    private Transform biomeRoot;


    // ============================================================
    // SET BIOME
    // ============================================================

    public void SetBiome(string biomeName)
    {
        if (biomeRoot == null)
        {
            Debug.LogWarning(
                "[BiomesManager] Biome Root is not assigned.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(biomeName))
        {
            Debug.LogWarning(
                "[BiomesManager] Biome name is empty.",
                this
            );

            return;
        }


        // ========================================================
        // TURN OFF ALL BIOMES
        // ========================================================

        for (int i = 0; i < biomeRoot.childCount; i++)
        {
            Transform biome =
                biomeRoot.GetChild(i);

            if (biome == null)
            {
                continue;
            }

            biome.gameObject.SetActive(false);
        }


        // ========================================================
        // FIND REQUESTED BIOME
        // ========================================================

        Transform selectedBiome =
            biomeRoot.Find(biomeName);


        if (selectedBiome == null)
        {
            Debug.LogWarning(
                "[BiomesManager] Could not find biome '" +
                biomeName +
                "' under Biome Root.",
                this
            );

            return;
        }


        // ========================================================
        // TURN ON REQUESTED BIOME
        // ========================================================

        selectedBiome.gameObject.SetActive(true);


        Debug.Log(
            "[BiomesManager] Activated biome: " +
            biomeName,
            this
        );
    }


    // ============================================================
    // TURN OFF ALL BIOMES
    // ============================================================

    public void DisableAllBiomes()
    {
        if (biomeRoot == null)
        {
            return;
        }


        for (int i = 0; i < biomeRoot.childCount; i++)
        {
            Transform biome =
                biomeRoot.GetChild(i);

            if (biome == null)
            {
                continue;
            }

            biome.gameObject.SetActive(false);
        }
    }
}