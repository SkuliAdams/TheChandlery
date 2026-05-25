using System.Collections.Generic;
using SecretHistories.Entities;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using TheHouse.Wheel;
using UnityEngine;

namespace ChandleryWheelTests
{
    [FucineImportable("wheel_test_items")]
    public class TestItem : AbstractEntity<TestItem>
    {
        [FucineValue(DefaultValue = 0)]
        public int Value { get; set; }

        [FucineValue(DefaultValue = "")]
        public string Description { get; set; }

        [FucineList(ValidateValueAs = typeof(Element))]
        public List<string> RequiredElements { get; set; }

        [FucineValue(DefaultValue = false)]
        public bool IsMagical { get; set; }

        [FucineValue(DefaultValue = 1.0)]
        public float Weight { get; set; }

        [WheelFucineEverValue(0f, 0f)]
        public Vector2 Position { get; set; }

        public TestItem(EntityData importDataForEntity, ContentImportLog log)
            : base(importDataForEntity, log)
        {
        }

        public TestItem()
        {
            RequiredElements = new List<string>();
        }

        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
            Debug.Log($"[WheelTestMod] Loaded TestItem '{Id}': Value={Value}, Desc='{Description}', " +
                      $"Magical={IsMagical}, Weight={Weight}, Position={Position}, " +
                      $"ReqElements=[{string.Join(",", RequiredElements)}]");
        }
    }

    [FucineImportable("wheel_test_configs")]
    public class TestConfig : AbstractEntity<TestConfig>
    {
        [FucineValue(DefaultValue = "")]
        public string SettingName { get; set; }

        [FucineValue(DefaultValue = 0.0)]
        public float NumericValue { get; set; }

        [FucineDict]
        public Dictionary<string, string> Params { get; set; }

        [WheelFucineEverValue(DefaultValue = 0)]
        public int LegacyValue { get; set; }

        public TestConfig(EntityData importDataForEntity, ContentImportLog log)
            : base(importDataForEntity, log)
        {
        }

        public TestConfig()
        {
            Params = new Dictionary<string, string>();
        }

        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
            Debug.Log($"[WheelTestMod] Loaded TestConfig '{Id}': SettingName='{SettingName}', " +
                      $"NumericValue={NumericValue}, LegacyValue={LegacyValue}, " +
                      $"Params=[{string.Join(", ", Params)}]");
        }
    }

    [FucineImportable("wheel_test_quickspec")]
    public class QuickSpecItem : AbstractEntity<QuickSpecItem>, IQuickSpecEntity
    {
        [FucineValue(DefaultValue = 0)]
        public int Value { get; set; }

        [FucineValue(DefaultValue = "")]
        public string Description { get; set; }

        public QuickSpecItem(EntityData importDataForEntity, ContentImportLog log)
            : base(importDataForEntity, log)
        {
        }

        public QuickSpecItem()
        {
        }

        public void QuickSpec(string value)
        {
            Description = value;
        }

        protected override void OnPostImportForSpecificEntity(ContentImportLog log, Compendium populatedCompendium)
        {
            Debug.Log($"[WheelTestMod] Loaded QuickSpecItem '{Id}': Value={Value}, Desc='{Description}'");
        }
    }
}
