using System;
using HarmonyLib;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using SecretHistories.UI;
using UnityEngine;
using TheHouse.Wheel;

namespace WheelTestMod
{
    public static class WheelTestModInit
    {
        private static bool _initialised;

        public static void Initialise()
        {
            if (_initialised)
                return;

            Debug.Log("[WheelTestMod] Initialising — registering Wheel test configurations...");

            // === Phase 2-3 test: custom properties on existing entity types ===

            Wheel.ClaimProperty<Element, string>("wheelTestString",
                defaultValue: "default-string");

            Wheel.ClaimProperty<Element, int>("wheelTestInt",
                defaultValue: 42);

            Wheel.ClaimProperty<Element, bool>("wheelTestBool",
                defaultValue: false);

            Wheel.ClaimProperty<Recipe, string>("wheelRecipeNote",
                defaultValue: "");

            Wheel.ClaimProperty<Verb, string>("wheelVerbTag",
                defaultValue: "untagged");

            // === Phase 4 test: ignored properties ===

            Wheel.AddIgnoredProperty<Element>("wheelLegacyField");

            // === Phase 5 test: import molding (pre-processing) ===

            Wheel.AddImportMolding<Element>(PreProcessElementData);

            Debug.Log("[WheelTestMod] Registration complete.");

            _initialised = true;
        }

        private static void PreProcessElementData(EntityData entityData)
        {
            if (entityData.ValuesTable.ContainsKey("wheelLegacyFormat"))
            {
                entityData.ValuesTable["wheelTestString"] = entityData.ValuesTable["wheelLegacyFormat"];
                entityData.ValuesTable.Remove("wheelLegacyFormat");
            }
        }
    }
}
