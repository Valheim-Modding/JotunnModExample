// JotunnModExample
// a Valheim mod skeleton using JötunnLib
// 
// File:    JotunnModExample.cs
// Project: JotunnModExample

using System;
using System.Reflection;
using BepInEx;
using UnityEngine;
using JotunnModExample.ConsoleCommands;
using JotunnLib.Managers;
using JotunnLib.Utils;
using JotunnLib.Configs;
using JotunnLib.Entities;
using System.Collections.Generic;
using BepInEx.Configuration;
using Version = System.Version;
using System.IO;

namespace JotunnModExample
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(JotunnLib.Main.ModGuid)]
    [NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Build)]
    internal class JotunnModExamplePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.jotunnmodexample";
        public const string PluginName = "JotunnModExample";
        public const string PluginVersion = "0.0.1";

        public AssetBundle TestAssets;
        public AssetBundle BlueprintRuneBundle;
        public Skills.SkillType TestSkillType = 0;

        private bool showMenu = false;
        private bool showGUIButton = false;
        private Texture2D testTex;
        private Sprite testSprite;
        private GameObject testPanel;
        private bool forceVersionMismatch = false;
        private System.Version currentVersion;
        private bool clonedItemsAdded = false;
        private GameObject backpackPrefab;
        private AssetBundle embeddedResourceBundle;

        // Init handlers
        private void Awake()
        {
            InputManager.Instance.InputRegister += registerInputs;
            LocalizationManager.Instance.LocalizationRegister += registerLocalization;

            loadAssets();
            addItemsWithConfigs();
            addMockedItems();
            addEmptyPiece();
            addCommands();
            addSkills();
            createConfigValues();

            // Hook ObjectDB.CopyOtherDB to add custom items cloned from vanilla items
            On.ObjectDB.CopyOtherDB += addClonedItems;

            // Get current version for the mod compatibility test
            currentVersion = new System.Version(Info.Metadata.Version.ToString());
            setVersion();
        }

        // Called every frame
        private void Update()
        {
            // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
            // we need to check that ZInput is ready to use first.
            if (ZInput.instance != null)
            {
                // Check if our button is pressed. This will only return true ONCE, right after our button is pressed.
                // If we hold the button down, it won't spam toggle our menu.
                if (ZInput.GetButtonDown("JotunnModExample_Menu"))
                {
                    showMenu = !showMenu;
                }

                if (ZInput.GetButtonDown("GUIManagerTest"))
                {
                    showGUIButton = !showGUIButton;
                }
            }

#if DEBUG
            if (Input.GetKeyDown(KeyCode.F6))
            { // Set a breakpoint here to break on F6 key press
            }
#endif
        }

        // Display our GUI if enabled
        private void OnGUI()
        {
            if (showMenu)
            {
                GUI.Box(new Rect(40, 40, 150, 250), "JotunnModExample");
            }

            if (showGUIButton)
            {
                if (testPanel == null)
                {
                    if (GUIManager.Instance == null)
                    {
                        Logger.LogError("GUIManager instance is null");
                        return;
                    }

                    if (GUIManager.PixelFix == null)
                    {
                        Logger.LogError("GUIManager pixelfix is null");
                        return;
                    }
                    testPanel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), 850, 600);

                    GUIManager.Instance.CreateButton("A Test Button - long dong schlongsen text", testPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0, 0), 250, 100).SetActive(true);
                    if (testPanel == null)
                    {
                        return;
                    }
                }
                testPanel.SetActive(!testPanel.activeSelf);
                showGUIButton = false;
            }
        }

        // Add custom key bindings
        private void registerInputs(object sender, EventArgs e)
        {
            InputManager.Instance.AddButton("JotunnModExample_Menu", KeyCode.Insert);
            InputManager.Instance.AddButton("GUIManagerTest", KeyCode.F8);
        }

        // Load assets
        private void loadAssets()
        {
            // Load texture
            testTex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_tex.jpg");
            testSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);

            // Load asset bundle from filesystem
            TestAssets = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/jotunnlibtest");
            JotunnLib.Logger.LogInfo(TestAssets);

            // Load asset bundle from filesystem
            BlueprintRuneBundle = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/blueprints");
            JotunnLib.Logger.LogInfo(BlueprintRuneBundle);

            embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("capeironbackpack", Assembly.GetExecutingAssembly());
            backpackPrefab = embeddedResourceBundle.LoadAsset<GameObject>("Assets/Evie/CapeIronBackpack.prefab");

            // Embedded Resources
            JotunnLib.Logger.LogInfo($"Embedded resources: {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
        }

        // Add new Items with item Configs
        private void addItemsWithConfigs()
        {
            // Add a custom piece table
            PieceManager.Instance.AddPieceTable(BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintPieceTable"));
            CreateBlueprintRune();
            CreateRunePieces();

            // Don't forget to unload the bundle to free the resources
            BlueprintRuneBundle.Unload(false);
        }

        private void CreateRunePieces()
        {
            // Create and add custom pieces
            GameObject makebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("make_blueprint");
            CustomPiece makebp = new CustomPiece(makebp_prefab, new PieceConfig
            {
                PieceTable = "_BlueprintPieceTable",
                AllowedInDungeons = false
            });
            PieceManager.Instance.AddPiece(makebp);
            GameObject placebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("piece_blueprint");
            CustomPiece placebp = new CustomPiece(placebp_prefab, new PieceConfig
            {
                PieceTable = "_BlueprintPieceTable",
                AllowedInDungeons = true,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig {Item = "Wood", Amount = 2}
                }
            });
            PieceManager.Instance.AddPiece(placebp);
        }

        private void CreateBlueprintRune()
        {
            // Create and add a custom item
            // CustomItem can be instantiated with an AssetBundle and will load the prefab from there
            CustomItem rune = new CustomItem(BlueprintRuneBundle, "BlueprintRune", false);
            ItemManager.Instance.AddItem(rune);

            // Create and add a recipe for the custom item
            CustomRecipe runeRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = "BlueprintRune",
                Amount = 1,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            ItemManager.Instance.AddRecipe(runeRecipe);
        }


        // Add new items with mocked prefabs
        private void addMockedItems()
        {
            // Load assets from resources
            //Stream assetstream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JotunnModExample.AssetsEmbedded.capeironbackpack");
            //if (assetstream == null) JotunnLib.Logger.LogWarning($"Requested asset stream could not be found.");
            //else
            //{
                
                if (!backpackPrefab) JotunnLib.Logger.LogWarning($"Failed to load asset from bundle: {embeddedResourceBundle}");
                else
                {
                    // Create and add a custom item
                    CustomItem CI = new CustomItem(backpackPrefab, true);
                    ItemManager.Instance.AddItem(CI);

                    // Create and add a custom recipe
                    Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.m_item = backpackPrefab.GetComponent<ItemDrop>();
                    recipe.m_craftingStation = Mock<CraftingStation>.Create("piece_workbench");
                    var ingredients = new List<Piece.Requirement>
                    {
                        MockRequirement.Create("LeatherScraps", 10),
                        MockRequirement.Create("DeerHide", 2),
                        MockRequirement.Create("Iron", 4),
                    };
                    recipe.m_resources = ingredients.ToArray();
                    CustomRecipe CR = new CustomRecipe(recipe, true, true);
                    ItemManager.Instance.AddRecipe(CR);

                    // Enable BoneReorder
                    BoneReorder.ApplyOnEquipmentChanged();
                }
                embeddedResourceBundle.Unload(false);
            }
        //}

        // Add a custom item from an "empty" prefab
        private void addEmptyPiece()
        {
            CustomPiece CP = new CustomPiece("$piece_lul", "Hammer");
            var piece = CP.Piece;
            piece.m_icon = testSprite;
            var prefab = CP.PiecePrefab;
            prefab.GetComponent<MeshRenderer>().material.mainTexture = testTex;
            PieceManager.Instance.AddPiece(CP);
        }

        // Add new items as copies of vanilla items - just works when vanilla prefabs are already loaded (ObjectDB.CopyOtherDB for example)
        // You can use the Cache of the PrefabManager in here
        private void addClonedItems(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            // You want that to run only once, JotunnLib has the item cached for the game session
            if (!clonedItemsAdded)
            {
                //Create a custom resource
                CustomItem recipeComponent = new CustomItem("CustomWood", "Wood");
                ItemManager.Instance.AddItem(recipeComponent);
                recipeComponent.ItemDrop.m_itemData.m_shared.m_name = "$item_customWood";
                recipeComponent.ItemDrop.m_itemData.m_shared.m_description = "$item_customWood_desc";

                // Create and add a custom item based on SwordBlackmetal
                CustomItem CI = new CustomItem("EvilSword", "SwordBlackmetal");
                ItemManager.Instance.AddItem(CI);

                // Replace vanilla properties of the custom item
                var itemDrop = CI.ItemDrop;
                itemDrop.m_itemData.m_shared.m_name = "$item_evilsword";
                itemDrop.m_itemData.m_shared.m_description = "$item_evilsword_desc";

                recipeEvilSword(itemDrop);

                clonedItemsAdded = true;
            }

            // Hook is prefix, we just need to be able to get the vanilla prefabs, JotunnLib registers them in ObjectDB
            orig(self, other);
        }

        private static void recipeEvilSword(ItemDrop itemDrop)
        {
            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_EvilSword";
            recipe.m_item = itemDrop;
            recipe.m_craftingStation = PrefabManager.Cache.GetPrefab<CraftingStation>("piece_workbench");
            recipe.m_resources = new Piece.Requirement[]
            {
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("Stone"),
                        m_amount = 1
                    },
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("CustomWood"),
                        m_amount = 1
                    }
            };
            CustomRecipe CR = new CustomRecipe(recipe, false, false);
            ItemManager.Instance.AddRecipe(CR);
        }

        // Registers localizations with configs
        void registerLocalization(object sender, EventArgs e)
        {
            // Add translations for the custom item in addClonedItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations =
                {
                    { "item_evilsword", "Sword of Darkness" },
                    { "item_evilsword_desc", "Bringing the light" }
                }
            });

            // Add translations for the custom piece in addEmptyItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations =
                {
                    { "piece_lul", "Lulz" }
                }
            });

             TextAsset[] textAssets = BlueprintRuneBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }
        }

        // Register new console commands
        private void addCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.AddConsoleCommand(new TpCommand());
            CommandManager.Instance.AddConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.AddConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.AddConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.AddConsoleCommand(new BetterSpawnCommand());
        }

        // Register new skills
        void addSkills()
        {
            // Test adding a skill with a texture
            Sprite testSkillSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);
            TestSkillType = SkillManager.Instance.AddSkill("com.jotunnlib.JotunnModExample.testskill", "TestingSkill", "A nice testing skill!", 1f, testSkillSprite);
            
            
            // Test adding a skill with a texture
            //TestSkillType = SkillManager.Instance.AddSkill("com.jotunnlib.JotunnModExample.testskill", "TestingSkill", "A nice testing skill!", 1f, testSkillSprite);
            TestSkillType = SkillManager.Instance.AddSkill(new SkillConfig
            {
                Identifier = "com.jotunnlib.JotunnModExample.testskill",
                Name = "TestingSkill",
                Description = "A nice testing skill!",
                Icon = testSkillSprite,
                IncreaseStep = 1f
            }, true);
            Logger.LogDebug(TestSkillType);
            //if(!TestSkillType) Logger.

        }

        // Create some sample configuration values to check server sync
        private void createConfigValues()
        {
            Config.SaveOnConfigSet = true;

            Config.Bind("JotunnLibTest", "StringValue1", "StringValue", new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "FloatValue1", 750f, new ConfigDescription("Server side float", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "IntegerValue1", 200, new ConfigDescription("Server side integer", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "BoolValue1", false, new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "KeycodeValue", KeyCode.F10,
                new ConfigDescription("Server side Keycode", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            // Add client config to test ModCompatibility
            Config.Bind("JotunnLibTest", "EnableVersionMismatch", false, new ConfigDescription("Enable to test ModCompatibility module", null));
            forceVersionMismatch = (bool)Config["JotunnLibTest", "EnableVersionMismatch"].BoxedValue;
            Config.SettingChanged += Config_SettingChanged;
        }

        // React on changed settings
        private void Config_SettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
        {
            if (e.ChangedSetting.Definition.Section == "JotunnLibTest" && e.ChangedSetting.Definition.Key == "EnableVersionMismatch")
            {
                forceVersionMismatch = (bool)e.ChangedSetting.BoxedValue;
            }
        }

        // Set version of the plugin for the mod compatibility test
        private void setVersion()
        {
            var propinfo = Info.Metadata.GetType().GetProperty("Version", BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

            // Change version number of this module if test is enabled
            if (forceVersionMismatch)
            {
                System.Version v = new System.Version(0, 0, 0);
                propinfo.SetValue(this.Info.Metadata, v, null);
            }
            else
            {
                propinfo.SetValue(Info.Metadata, currentVersion, null);
            }
        }
    }
}