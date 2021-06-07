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

        private AssetBundle testAssets;
        private AssetBundle blueprintRuneBundle;
        private AssetBundle steelIngotBundle;
        private AssetBundle embeddedResourceBundle;

        private bool showGUI = false;

        private Texture2D testTex;
        private Sprite testSprite;
        private GameObject testPanel;

        private Skills.SkillType testSkill = 0;
        private GameObject backpackPrefab;
        private ButtonConfig evilSwordSpecial;
        private CustomStatusEffect evilSwordEffect;

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
                    showGUI = !showGUI;
                }

                // Use the name of the ButtonConfig to identify the button pressed
                if (evilSwordSpecial != null && MessageHud.instance != null)
                {
                    if (ZInput.GetButtonDown(evilSwordSpecial.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$evilsword_beevilmessage");
                    }
                }

                // Raise the test skill
                if (Player.m_localPlayer != null && ZInput.GetButtonDown("JotunnExampleMod_RaiseSkill"))
                {
                    Player.m_localPlayer.RaiseSkill(testSkill, 1f);
                }
            }
        }

        // Called every frame for rendering and handling GUI events
        private void OnGUI()
        {
            // Display an example panel with button if enabled
            if (showGUI)
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
                showGUI = false;
            }
        }

        // Create some sample configuration values to check server sync
        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            // Add server config which gets pushed to all clients connecting and can only be edited by admins
            // In local/single player games the player is always considered the admin
            Config.Bind("Server config", "StringValue1", "StringValue", new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "FloatValue1", 750f, new ConfigDescription("Server side float", new AcceptableValueRange<float>(0f, 1000f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "IntegerValue1", 200, new ConfigDescription("Server side integer", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "BoolValue1", false, new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "KeycodeValue", KeyCode.F10,
                new ConfigDescription("Server side Keycode", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));

            // Add a client side custom input key for the EvilSword
            Config.Bind("Client config", "EvilSwordSpecialAttack", KeyCode.B, new ConfigDescription("Key to unleash evil with the Evil Sword"));

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

        // Various forms of asset loading
        private void LoadAssets()
        {
            // Load texture from the filesystem
            testTex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_tex.jpg");
            testSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);

            // Load asset bundle from the filesystem
            testAssets = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/jotunnlibtest");
            Jotunn.Logger.LogInfo(testAssets);

            // Load asset bundle from embedded resources
            Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(JotunnModExample).Assembly.GetManifestResourceNames())}");
            embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("eviesbackpacks", typeof(JotunnModExample).Assembly);
            backpackPrefab = embeddedResourceBundle.LoadAsset<GameObject>("Assets/Evie/CapeSilverBackpack.prefab");
            steelIngotBundle = AssetUtils.LoadAssetBundleFromResources("steel", typeof(JotunnModExample).Assembly);
        }

        // Add custom key bindings
        private void AddInputs()
        {
            // Add key bindings on the fly
            InputManager.Instance.AddButton(PluginGUID, "JotunnModExample_Menu", KeyCode.Insert);

            // Add key bindings backed by a config value
            // Create a ButtonConfig to also add it as a custom key hint in AddClonedItems
            evilSwordSpecial = new ButtonConfig
            {
                Name = "EvilSwordSpecialAttack",
                Key = (KeyCode)Config["Client config", "EvilSwordSpecialAttack"].BoxedValue,
                HintToken = "$evilsword_beevil"
            };
            InputManager.Instance.AddButton(PluginGUID, evilSwordSpecial);

            // Add a key binding to test skill raising
            InputManager.Instance.AddButton(PluginGUID, "JotunnExampleMod_RaiseSkill", KeyCode.Home);
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
            Sprite testSkillSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);
            testSkill = SkillManager.Instance.AddSkill(new SkillConfig
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
            //ItemManager.Instance.AddRecipesFromJson("JotunnModExample/Assets/recipes.json");
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

            evilSwordEffect = new CustomStatusEffect(effect, fixReference: false);  // We dont need to fix refs here, because no mocks were used
            ItemManager.Instance.AddStatusEffect(evilSwordEffect);
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
            var steel_prefab = steelIngotBundle.LoadAsset<GameObject>("Steel");
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
            blueprintRuneBundle = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/testblueprints");
            Jotunn.Logger.LogInfo($"Loaded asset bundle: {blueprintRuneBundle}");

            // Load and add all our custom stuff to Jötunn
            CreateRunePieceTable();
            CreateBlueprintRune();
            CreateRunePieces();
            CreateRuneKeyHints();

            // Don't forget to unload the bundle to free the resources
            blueprintRuneBundle.Unload(false);
        }

        private void CreateRunePieceTable()
        {
            GameObject tablePrefab = blueprintRuneBundle.LoadAsset<GameObject>("_BlueprintTestTable");
            CustomPieceTable CPT = new CustomPieceTable(tablePrefab);
            PieceManager.Instance.AddPieceTable(CPT);
        }

        // Implementation of items and recipes via configs
        private void CreateBlueprintRune()
        {
            // Create and add a custom item
            var rune_prefab = blueprintRuneBundle.LoadAsset<GameObject>("BlueprintTestRune");
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
            var makebp_prefab = blueprintRuneBundle.LoadAsset<GameObject>("make_testblueprint");
            var makebp = new CustomPiece(makebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintTestTable"
                });
            PieceManager.Instance.AddPiece(makebp);

            // Load, create and add another custom piece for the rune. This piece uses more properties
            // of the PieceConfig - it can now be build in dungeons and has actual requirements to build it.
            var placebp_prefab = blueprintRuneBundle.LoadAsset<GameObject>("piece_testblueprint");
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
            TextAsset[] textAssets = blueprintRuneBundle.LoadAllAssets<TextAsset>();
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
                Icon = testSprite,
                PieceTable = "Hammer",
                ExtendStation = "piece_workbench", // Makes this piece a station extension
                Category = "Lulzies"  // Adds a custom category for the Hammer
            });

            if (CP != null)
            {
                // Add our test texture to the Unity MeshRenderer
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = testTex;

                PieceManager.Instance.AddPiece(CP);
            }

            // Create another "empty" custom piece
            CP = new CustomPiece("piece_lel", addZNetView: true, new PieceConfig
            {
                Name = "$piece_lel",
                Description = "$piece_lel_description",
                Icon = testSprite,
                PieceTable = "Hammer",
                ExtendStation = "piece_workbench", // Makes this piece a station extension
                Category = "Lulzies"  // Adds a custom category for the Hammer
            });

            if (CP != null)
            {
                // Add our test texture to the Unity MeshRenderer and make the material color grey
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = testTex;
                prefab.GetComponent<MeshRenderer>().material.color = Color.grey;

                PieceManager.Instance.AddPiece(CP);
            }
        }

        // Implementation of assets using mocks, adding recipe's manually without the config abstraction
        private void AddMockedItems()
        {
            if (!backpackPrefab) Jotunn.Logger.LogWarning($"Failed to load asset from bundle: {embeddedResourceBundle}");
            else
            {
                // Create and add a custom item
                CustomItem CI = new CustomItem(backpackPrefab, true);
                ItemManager.Instance.AddItem(CI);

                //Create and add a custom recipe
                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = "Recipe_CapeIronBackpack";
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

                //Enable BoneReorder
                BoneReorder.ApplyOnEquipmentChanged();
            }
            embeddedResourceBundle.Unload(false);
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

        // Implementation of key hints replacing vanilla keys and using custom keys
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
                    // New custom input
                    evilSwordSpecial,
                    // Override vanilla "Mouse Wheel" text
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$evilsword_scroll" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC);
        }
    }
}