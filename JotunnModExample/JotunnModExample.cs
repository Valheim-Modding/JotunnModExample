// JotunnModExample
// A Valheim mod using Jötunn
// Used to demonstrate the libraries capabilities
// 
// File:    JotunnModExample.cs
// Project: JotunnModExample

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using JotunnModExample.ConsoleCommands;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace JotunnModExample
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [BepInDependency("cinnabun.backpacks-v1.0.0", BepInDependency.DependencyFlags.SoftDependency)]
    internal class JotunnModExample : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.JotunnModExample";
        public const string PluginName = "JotunnModExample";
        public const string PluginVersion = "1.0.0";

        // Asset and prefab loading
        private AssetBundle TestAssets;
        private AssetBundle BlueprintRuneBundle;
        private AssetBundle SteelIngotBundle;
        private AssetBundle EmbeddedResourceBundle;
        private GameObject BackpackPrefab;

        // Test assets
        private Texture2D TestTex;
        private Sprite TestSprite;
        private GameObject TestPanel;

        // Fixed buttons
        private ButtonConfig ShowGUIButton;
        private ButtonConfig RaiseSkillButton;

        // Variable button backed by a config
        private ConfigEntry<KeyCode> EvilSwordSpecialConfig;
        private ButtonConfig EvilSwordSpecialButton;

        // Menu toggle
        private bool ShowGUI = false;

        // Configuration values
        private ConfigEntry<string> StringConfig;
        private ConfigEntry<float> FloatConfig;
        private ConfigEntry<int> IntegerConfig;
        private ConfigEntry<bool> BoolConfig;

        // Custom skill
        private Skills.SkillType TestSkill = 0;

        // Custom status effect
        private CustomStatusEffect EvilSwordEffect;

        private void Awake()
        {
            // Load, create and init your custom mod stuff
            CreateConfigValues();
            LoadAssets();
            AddInputs();
            AddLocalizations();
            AddCommands();
            AddSkills();
            AddRecipes();
            AddStatusEffects();
            AddCustomItemConversions();
            AddItemsWithConfigs();
            AddPieceCategories();
            AddMockedItems();

            // Add custom items cloned from vanilla items
            ItemManager.OnVanillaItemsAvailable += AddClonedItems;

            // Add a cloned item with custom variants
            ItemManager.OnVanillaItemsAvailable += AddVariants;
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
                if (ZInput.GetButtonDown(ShowGUIButton.Name))
                {
                    ShowGUI = !ShowGUI;
                }

                // Raise the test skill
                if (Player.m_localPlayer != null && ZInput.GetButtonDown(RaiseSkillButton.Name))
                {
                    Player.m_localPlayer.RaiseSkill(TestSkill, 1f);
                }

                // Use the name of the ButtonConfig to identify the button pressed
                // without knowing what key the user bound to this button in his configuration.
                if (EvilSwordSpecialButton != null && MessageHud.instance != null)
                {
                    if (ZInput.GetButtonDown(EvilSwordSpecialButton.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$evilsword_beevilmessage");
                    }
                }

            }
        }

        // Called every frame for rendering and handling GUI events
        private void OnGUI()
        {
            // Display an example panel with button if enabled
            if (ShowGUI)
            {
                if (TestPanel == null)
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
                    TestPanel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), 850, 600);

                    GUIManager.Instance.CreateButton("A Test Button - long dong schlongsen text", TestPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0, 0), 250, 100).SetActive(true);
                    if (TestPanel == null)
                    {
                        return;
                    }
                }
                TestPanel.SetActive(!TestPanel.activeSelf);
                ShowGUI = false;
            }
        }

        // Create some sample configuration values
        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            // Add client config which can be edited in every local instance independently
            StringConfig = Config.Bind("Client config", "LocalString", "Some string", "Client side string");
            FloatConfig = Config.Bind("Client config", "LocalFloat", 0.5f, new ConfigDescription("Client side float with a value range", new AcceptableValueRange<float>(0f, 1f)));
            IntegerConfig = Config.Bind("Client config", "LocalInteger", 2, new ConfigDescription("Client side integer without a range"));
            BoolConfig = Config.Bind("Client config", "LocalBool", false, new ConfigDescription("Client side bool / checkbox"));

            // Add server config which gets pushed to all clients connecting and can only be edited by admins
            // In local/single player games the player is always considered the admin
            Config.Bind("Server config", "StringValue1", "StringValue", new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "FloatValue1", 750f, new ConfigDescription("Server side float", new AcceptableValueRange<float>(0f, 1000f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "IntegerValue1", 200, new ConfigDescription("Server side integer", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "BoolValue1", false, new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            // Colored text configs
            Config.Bind("Client config", "ColoredValue", false,
                new ConfigDescription("Colored key and description text", null, new ConfigurationManagerAttributes { EntryColor = Color.blue, DescriptionColor = Color.yellow }));

            // Invisible configs
            Config.Bind("Client config", "InvisibleInt", 150,
                new ConfigDescription("Invisible int, testing browsable=false", null, new ConfigurationManagerAttributes() { Browsable = false }));

            // Add a client side custom input key for the EvilSword
            EvilSwordSpecialConfig = Config.Bind("Client config", "EvilSword Special Attack", KeyCode.B, new ConfigDescription("Key to unleash evil with the Evil Sword"));

            // You can subscribe to a global event when config got synced initially and on changes
            SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
            {
                if (attr.InitialSynchronization)
                {
                    Jotunn.Logger.LogMessage("Initial Config sync event received");
                }
                else
                {
                    Jotunn.Logger.LogMessage("Config sync event received");
                }
            };
        }

        private void ReadAndWriteConfigValues()
        {
            // Reading configuration entry
            string readValue = StringConfig.Value;
            // or
            float readBoxedValue = (float)Config["Client config", "LocalFloat"].BoxedValue;

            // Writing configuration entry
            IntegerConfig.Value = 150;
            // or
            Config["Client config", "LocalBool"].BoxedValue = true;
        }

        // Various forms of asset loading
        private void LoadAssets()
        {
            // Load texture from the filesystem
            TestTex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_tex.jpg");
            TestSprite = Sprite.Create(TestTex, new Rect(0f, 0f, TestTex.width, TestTex.height), Vector2.zero);

            // Load asset bundle from the filesystem
            TestAssets = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/jotunnlibtest");
            Jotunn.Logger.LogInfo(TestAssets);

            // Load asset bundle from embedded resources
            Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(JotunnModExample).Assembly.GetManifestResourceNames())}");
            EmbeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("eviesbackpacks", typeof(JotunnModExample).Assembly);
            BackpackPrefab = EmbeddedResourceBundle.LoadAsset<GameObject>("Assets/Evie/CapeSilverBackpack.prefab");
            SteelIngotBundle = AssetUtils.LoadAssetBundleFromResources("steel", typeof(JotunnModExample).Assembly);
        }

        // Add custom key bindings
        private void AddInputs()
        {
            // Add key bindings on the fly
            ShowGUIButton = new ButtonConfig
            {
                Name = "JotunnModExample_Menu",
                Key = KeyCode.Insert,
                ActiveInGUI = true    // Enable this button also when in GUI (e.g. the console)
            };
            InputManager.Instance.AddButton(PluginGUID, ShowGUIButton);

            RaiseSkillButton = new ButtonConfig
            {
                Name = "JotunnExampleMod_RaiseSkill",
                Key = KeyCode.Home
            };
            InputManager.Instance.AddButton(PluginGUID, RaiseSkillButton);

            // Add key bindings backed by a config value
            // The HintToken is used for the custom KeyHint of the EvilSword
            EvilSwordSpecialButton = new ButtonConfig
            {
                Name = "EvilSwordSpecialAttack",
                Config = EvilSwordSpecialConfig,
                HintToken = "$evilsword_beevil"
            };
            InputManager.Instance.AddButton(PluginGUID, EvilSwordSpecialButton);
        }

        // Adds localizations with configs
        private void AddLocalizations()
        {
            // Add translations for our custom skill
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    {"skill_TestingSkill", "TestLocalizedSkillName" }
                }
            });

            // Add translations for the custom item in AddClonedItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    {"item_evilsword", "Sword of Darkness"}, {"item_evilsword_desc", "Bringing the light"},
                    {"evilsword_shwing", "Woooosh"}, {"evilsword_scroll", "*scroll*"},
                    {"evilsword_beevil", "Be evil"}, {"evilsword_beevilmessage", ":reee:"},
                    {"evilsword_effectname", "Evil"}, {"evilsword_effectstart", "You feel evil"},
                    {"evilsword_effectstop", "You feel nice again"}
                }
            });

            // Add translations for the custom piece in AddPieceCategories
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    { "piece_lul", "Lulz" }, { "piece_lul_description", "Do it for them" },
                    { "piece_lel", "Lölz" }, { "piece_lel_description", "Härhärhär" }
                }
            });

            // Add translations for the custom item in AddVariants
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    { "lulz_shield", "Lulz Shield" }, { "lulz_shield_desc", "Lough at your enemies" }
                }
            });
        }

        // Register new console commands
        private void AddCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.AddConsoleCommand(new TpCommand());
            CommandManager.Instance.AddConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.AddConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.AddConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.AddConsoleCommand(new BetterSpawnCommand());
        }

        // Add a new test skill
        void AddSkills()
        {
            // Test adding a skill with a texture
            Sprite testSkillSprite = Sprite.Create(TestTex, new Rect(0f, 0f, TestTex.width, TestTex.height), Vector2.zero);
            TestSkill = SkillManager.Instance.AddSkill(new SkillConfig
            {
                Identifier = "com.jotunn.JotunnModExample.testskill",
                Name = "TestingSkill",
                Description = "A nice testing skill!",
                Icon = testSkillSprite,
                IncreaseStep = 1f
            });
        }

        // Add custom recipes
        private void AddRecipes()
        {
            // Create a custom recipe with a RecipeConfig
            CustomRecipe meatRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = "CookedMeat",                    // Name of the item prefab to be crafted
                Requirements = new RequirementConfig[]  // Resources and amount needed for it to be crafted
                {
                    new RequirementConfig { Item = "Stone", Amount = 2 },
                    new RequirementConfig { Item = "Wood", Amount = 1 }
                }
            });
            ItemManager.Instance.AddRecipe(meatRecipe);

            // Load recipes from JSON file
            ItemManager.Instance.AddRecipesFromJson("JotunnModExample/Assets/recipes.json");
        }

        // Add new status effects
        private void AddStatusEffects()
        {
            // Create a new status effect. The base class "StatusEffect" does not do very much except displaying messages
            // A Status Effect is normally a subclass of StatusEffects which has methods for further coding of the effects (e.g. SE_Stats).
            StatusEffect effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.name = "EvilStatusEffect";
            effect.m_name = "$evilsword_effectname";
            effect.m_icon = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/reee.png");
            effect.m_startMessageType = MessageHud.MessageType.Center;
            effect.m_startMessage = "$evilsword_effectstart";
            effect.m_stopMessageType = MessageHud.MessageType.Center;
            effect.m_stopMessage = "$evilsword_effectstop";

            EvilSwordEffect = new CustomStatusEffect(effect, fixReference: false);  // We dont need to fix refs here, because no mocks were used
            ItemManager.Instance.AddStatusEffect(EvilSwordEffect);
        }

        // Add custom item conversions
        private void AddCustomItemConversions()
        {
            // Add an item conversion for the CookingStation. The items must have an "attach" child GameObject to display it on the station.
            var cookConversion = new CustomItemConversion(new CookingConversionConfig
            {
                FromItem = "CookedMeat",
                ToItem = "CookedLoxMeat",
                CookTime = 2f
            });
            ItemManager.Instance.AddItemConversion(cookConversion);

            // Add an item conversion for the Fermenter. You can specify how much new items the conversion yields.
            var fermentConversion = new CustomItemConversion(new FermenterConversionConfig
            {
                FromItem = "Coal",
                ToItem = "CookedLoxMeat",
                ProducedItems = 10
            });
            ItemManager.Instance.AddItemConversion(fermentConversion);

            // Add an item conversion for the smelter
            var smeltConversion = new CustomItemConversion(new SmelterConversionConfig
            {
                //Station = "smelter",  // Use the default from the config
                FromItem = "Stone",
                ToItem = "CookedLoxMeat"
            });
            ItemManager.Instance.AddItemConversion(smeltConversion);

            // Load and create a custom item to use in another conversion
            var steel_prefab = SteelIngotBundle.LoadAsset<GameObject>("Steel");
            var ingot = new CustomItem(steel_prefab, fixReference: false);
            ItemManager.Instance.AddItem(ingot);

            // Create a conversion for the blastfurnace, the custom item is the new outcome
            var blastConversion = new CustomItemConversion(new SmelterConversionConfig
            {
                Station = "blastfurnace", // Override the default "smelter" station of the SmelterConversionConfig
                FromItem = "Iron",
                ToItem = "Steel" // This is our custom prefabs name we have loaded just above 
            });
            ItemManager.Instance.AddItemConversion(blastConversion);
        }

        // Add new assets via item Configs
        private void AddItemsWithConfigs()
        {
            // Load asset bundle from the filesystem
            BlueprintRuneBundle = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/testblueprints");
            Jotunn.Logger.LogInfo($"Loaded asset bundle: {BlueprintRuneBundle}");

            // Load and add all our custom stuff to Jötunn
            CreateRunePieceTable();
            CreateBlueprintRune();
            CreateRunePieces();
            CreateRuneKeyHints();

            // Don't forget to unload the bundle to free the resources
            BlueprintRuneBundle.Unload(false);
        }

        private void CreateRunePieceTable()
        {
            GameObject tablePrefab = BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintTestTable");
            CustomPieceTable CPT = new CustomPieceTable(tablePrefab);
            PieceManager.Instance.AddPieceTable(CPT);
        }

        // Implementation of items and recipes via configs
        private void CreateBlueprintRune()
        {
            // Create and add a custom item
            var rune_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("BlueprintTestRune");
            var rune = new CustomItem(rune_prefab, fixReference: false,
                new ItemConfig
                {
                    Amount = 1,
                    Requirements = new[]
                    {
                        new RequirementConfig
                        {
                            Item = "Stone",
                            //Amount = 1,           // These are all the defaults, so no need to specify
                            //AmountPerLevel = 0,
                            //Recover = false 
                        }
                    }
                });
            ItemManager.Instance.AddItem(rune);
        }

        // Implementation of pieces via configs.
        private void CreateRunePieces()
        {
            // Create and add a custom piece for the rune. Add the prefab name of the PieceTable to the config.
            var makebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("make_testblueprint");
            var makebp = new CustomPiece(makebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintTestTable"
                });
            PieceManager.Instance.AddPiece(makebp);

            // Load, create and add another custom piece for the rune. This piece uses more properties
            // of the PieceConfig - it can now be build in dungeons and has actual requirements to build it.
            var placebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("piece_testblueprint");
            var placebp = new CustomPiece(placebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintTestTable",
                    AllowedInDungeons = true,
                    Requirements = new[]
                    {
                        new RequirementConfig
                        {
                            Item = "Wood",
                            Amount = 2,
                            //AmountPerLevel = 0,   // Amount is changed, all other Properties are left at default
                            //Recover = false 
                        }
                    }
                });
            PieceManager.Instance.AddPiece(placebp);

            // Also add localizations for the rune 
            BlueprintRuneLocalizations();
        }

        // Add localisations from asset bundles
        private void BlueprintRuneLocalizations()
        {
            TextAsset[] textAssets = BlueprintRuneBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }
        }

        // Add KeyHints for specific Pieces
        private void CreateRuneKeyHints()
        {
            // Override "default" KeyHint with an empty config
            KeyHintConfig KHC_base = new KeyHintConfig
            {
                Item = "BlueprintTestRune"
            };
            GUIManager.Instance.AddKeyHint(KHC_base);

            // Add custom KeyHints for specific pieces
            KeyHintConfig KHC_make = new KeyHintConfig
            {
                Item = "BlueprintTestRune",
                Piece = "make_testblueprint",
                ButtonConfigs = new[]
                {
                    // Override vanilla "Attack" key text
                    new ButtonConfig { Name = "Attack", HintToken = "$bprune_make" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC_make);

            KeyHintConfig KHC_piece = new KeyHintConfig
            {
                Item = "BlueprintTestRune",
                Piece = "piece_testblueprint",
                ButtonConfigs = new[]
                {
                    // Override vanilla "Attack" key text
                    new ButtonConfig { Name = "Attack", HintToken = "$bprune_piece" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC_piece);

            // Add additional localization manually
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations = {
                    {"bprune_make", "Capture Blueprint"}, {"bprune_piece", "Place Blueprint"}
                }
            });
        }

        // Implementation of custom pieces from an "empty" prefab with new piece categories
        private void AddPieceCategories()
        {
            // Create a new CustomPiece as an "empty" GameObject. Also set addZNetView to true 
            // so it will be saved and shared with all clients of a server.
            CustomPiece CP = new CustomPiece("piece_lul", addZNetView: true, new PieceConfig
            {
                Name = "$piece_lul",
                Description = "$piece_lul_description",
                Icon = TestSprite,
                PieceTable = "Hammer",
                ExtendStation = "piece_workbench", // Makes this piece a station extension
                Category = "Lulzies"  // Adds a custom category for the Hammer
            });

            if (CP != null)
            {
                // Add our test texture to the Unity MeshRenderer
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = TestTex;

                PieceManager.Instance.AddPiece(CP);
            }

            // Create another "empty" custom piece
            CP = new CustomPiece("piece_lel", addZNetView: true, new PieceConfig
            {
                Name = "$piece_lel",
                Description = "$piece_lel_description",
                Icon = TestSprite,
                PieceTable = "Hammer",
                ExtendStation = "piece_workbench", // Makes this piece a station extension
                Category = "Lulzies"  // Adds a custom category for the Hammer
            });

            if (CP != null)
            {
                // Add our test texture to the Unity MeshRenderer and make the material color grey
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = TestTex;
                prefab.GetComponent<MeshRenderer>().material.color = Color.grey;

                PieceManager.Instance.AddPiece(CP);
            }
        }

        // Implementation of assets using mocks, adding recipe's manually without the config abstraction
        private void AddMockedItems()
        {
            if (!BackpackPrefab) Jotunn.Logger.LogWarning($"Failed to load asset from bundle: {EmbeddedResourceBundle}");
            else
            {
                // Create and add a custom item
                CustomItem CI = new CustomItem(BackpackPrefab, true);
                ItemManager.Instance.AddItem(CI);

                //Create and add a custom recipe
                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = "Recipe_CapeIronBackpack";
                recipe.m_item = BackpackPrefab.GetComponent<ItemDrop>();
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

                //Enable BoneReorder
                BoneReorder.ApplyOnEquipmentChanged();
            }
            EmbeddedResourceBundle.Unload(false);
        }

        // Implementation of cloned items
        private void AddClonedItems()
        {
            try
            {
                // Create a custom resource based on Wood
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

                // Create the recipe for the sword
                RecipeEvilSword(itemDrop);

                // Show a different KeyHint for the sword.
                KeyHintsEvilSword();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding cloned item: {ex.Message}");
            }
            finally
            {
                // You want that to run only once, Jotunn has the item cached for the game session
                ItemManager.OnVanillaItemsAvailable -= AddClonedItems;
            }
        }

        // Implementation of assets via using manual recipe creation and prefab cache's
        private void RecipeEvilSword(ItemDrop itemDrop)
        {
            // Create and add a recipe for the copied item
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
            // Since we got the vanilla prefabs from the cache, no referencing is needed
            CustomRecipe CR = new CustomRecipe(recipe, fixReference: false, fixRequirementReferences: false);
            ItemManager.Instance.AddRecipe(CR);
        }

        // Implementation of key hints replacing vanilla keys and using custom keys.
        // KeyHints appear in the same order in which they are defined in the config.
        private void KeyHintsEvilSword()
        {
            // Create custom KeyHints for the item
            KeyHintConfig KHC = new KeyHintConfig
            {
                Item = "EvilSword",
                ButtonConfigs = new[]
                {
                    // Override vanilla "Attack" key text
                    new ButtonConfig { Name = "Attack", HintToken = "$evilsword_shwing" },
                    // User our custom button defined earlier, syncs with the backing config value
                    EvilSwordSpecialButton,
                    // Override vanilla "Mouse Wheel" text
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$evilsword_scroll" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC);
        }

        // Clone the wooden shield and add own variations to it
        private void AddVariants()
        {
            try
            {
                Sprite var1 = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/test_var1.png");
                Sprite var2 = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/test_var2.png");
                Texture2D styleTex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_varpaint.png");
                CustomItem CI = new CustomItem("item_lulvariants", "ShieldWood", new ItemConfig
                {
                    Name = "$lulz_shield",
                    Description = "$lulz_shield_desc",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig{ Item = "Wood", Amount = 1 }
                    },
                    Icons = new Sprite[]
                    {
                        var1, var2
                    },
                    StyleTex = styleTex
                });
                ItemManager.Instance.AddItem(CI);
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding variant item: {ex}");
            }
            finally
            {
                // You want that to run only once, Jotunn has the item cached for the game session
                ItemManager.OnVanillaItemsAvailable -= AddVariants;
            }
        }
    }
}