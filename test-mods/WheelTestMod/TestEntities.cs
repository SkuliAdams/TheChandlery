using System.Collections.Generic;
using SecretHistories.Fucine;
using SecretHistories.Fucine.DataImport;
using UnityEngine;

namespace WheelTestMod
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
                      $"Magical={IsMagical}, Weight={Weight}, ReqElements=[{string.Join(",", RequiredElements)}]");
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
        }
    }
}
