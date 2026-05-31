using System.Collections.Generic;
using SecretHistories;
using SecretHistories.Core;
using SecretHistories.Entities;
using SecretHistories.Services;
using SecretHistories.Spheres;
using SecretHistories.UI;
using UnityEngine;

namespace TheHouse;

internal static class RecipeRegistrar
{
    internal static void RegisterAll()
    {
        var compendium = Watchman.Get<Compendium>();

        foreach (var def in TerrainRegistry.GetAll())
        {
            var unlock = def.UnlockRecipe;
            if (unlock == null)
                continue;

            var recipeId = "terrain." + def.Id;

            if (compendium.EntityExists<Recipe>(recipeId))
            {
                if (TerrainRegistry.IsOverride(def.Id))
                {
                    var existing = compendium.GetEntityById<Recipe>(recipeId);
                    ApplyUnlockToRecipe(existing, unlock, recipeId, def.Id);
                    Debug.Log($"Chandlery Lionsmith: Replaced unlock recipe for override room '{def.Id}'");
                }
                else
                {
                    Debug.Log($"Chandlery Lionsmith: Recipe '{recipeId}' already exists — skipping");
                }
                continue;
            }

            var recipe = new Recipe();
            recipe.SetId(recipeId);
            ApplyUnlockToRecipe(recipe, unlock, recipeId, def.Id);

            if (compendium.TryAddEntity(recipe))
                Debug.Log($"Chandlery Lionsmith: Registered unlock recipe for '{def.Id}'");
            else
                Debug.LogWarning($"Chandlery Lionsmith: Failed to register recipe for '{def.Id}'");
        }
    }

    private static void ApplyUnlockToRecipe(Recipe recipe, UnlockRecipeDefinition unlock, string recipeId, string roomId)
    {
        recipe.ActionId = "terrain.unlock";
        recipe.Label = unlock.Label ?? recipe.Label ?? roomId;
        recipe.Preface = unlock.Preface ?? recipe.Preface ?? string.Empty;
        recipe.StartDescription = unlock.StartDescription ?? recipe.StartDescription ?? string.Empty;
        recipe.Desc = unlock.Description ?? recipe.Desc ?? string.Empty;
        recipe.Warmup = unlock.Warmup;
        recipe.Craftable = true;

        recipe.Reqs ??= new Dictionary<string, string>();
        recipe.ExtantReqs ??= new Dictionary<string, string>();
        recipe.Greq ??= new Dictionary<string, string>();
        recipe.Ngreq ??= new Dictionary<string, string>();
        recipe.FXReqs ??= new Dictionary<string, string>();
        recipe.Effects ??= new Dictionary<string, string>();
        recipe.FX = new Dictionary<string, string> { { roomId + ".open", "1" } };
        recipe.DeckEffects ??= new Dictionary<string, string>();
        recipe.XPans ??= new Dictionary<string, int>();
        recipe.Purge ??= new Dictionary<string, int>();
        recipe.HaltVerb ??= new Dictionary<string, int>();
        recipe.DeleteVerb ??= new Dictionary<string, int>();
        recipe.Aspects ??= new AspectsDictionary();
        recipe.Achievements ??= new List<string>();
        recipe.Mutations ??= new List<MutationEffect>();
        recipe.Alt ??= new List<LinkedRecipeDetails>();
        recipe.Lalt ??= new List<LinkedRecipeDetails>();
        recipe.Inductions ??= new List<LinkedRecipeDetails>();
        recipe.Linked ??= new List<LinkedRecipeDetails>();
        recipe.Slots ??= new List<SphereSpec>();

        var preslot = new SphereSpec(typeof(ThresholdSphere), "infoRecipeInput");
        preslot.Label = "#UI_ROOMINPUT_LABEL#";
        preslot.Description = "#UI_ROOMINPUT_DESCRIPTION#";
        preslot.Required = unlock.Aspects != null
            ? new AspectsDictionary(unlock.Aspects)
            : new AspectsDictionary();
        preslot.Essential = unlock.Essential != null
            ? new AspectsDictionary(unlock.Essential)
            : new AspectsDictionary();
        preslot.Forbidden = unlock.Forbidden != null
            ? new AspectsDictionary(unlock.Forbidden)
            : new AspectsDictionary();

        if (!preslot.Essential.ContainsKey("assistance"))
            preslot.Essential["assistance"] = 1;

        recipe.PreSlots = new List<SphereSpec> { preslot };
    }
}
