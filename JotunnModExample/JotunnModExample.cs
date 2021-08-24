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
using Jotunn.GUI;
using Jotunn.Managers;
using Jotunn.Utils;
using JotunnModExample.ConsoleCommands;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        public const string PluginVersion = "2.2.4";

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
        private ButtonConfig CreateColorPickerButton;
        private ButtonConfig CreateGradientPickerButton;

        // Variable button backed by a config
        private ConfigEntry<KeyCode> EvilSwordSpecialConfig;
        private ButtonConfig EvilSwordSpecialButton;

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
            AddKitbashedPieces();

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
                    TogglePanel();
                }

                // Raise the test skill
                if (Player.m_localPlayer != null && ZInput.GetButtonDown(RaiseSkillButton.Name))
                {
                    Player.m_localPlayer.RaiseSkill(TestSkill);
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

                // Show ColorPicker or GradientPicker via GUIManager
                if (ZInput.GetButtonDown(CreateColorPickerButton.Name))
                {
                    CreateColorPicker();
                }
                if (ZInput.GetButtonDown(CreateGradientPickerButton.Name))
                {
                    CreateGradientPicker();
                }
            }
        }

        // Toggle our test panel with button
        private void TogglePanel()
        {
            // Create the panel if it does not exist
            if (!TestPanel)
            {
                if (GUIManager.Instance == null)
                {
                    Logger.LogError("GUIManager instance is null");
                    return;
                }

                if (!GUIManager.CustomGUIFront)
                {
                    Logger.LogError("GUIManager CustomGUI is null");
                    return;
                }

                // Create the panel object
                TestPanel = GUIManager.Instance.CreateWoodpanel(
                    parent: GUIManager.CustomGUIFront.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 850,
                    height: 600,
                    draggable: false);
                TestPanel.SetActive(false);

                // Add the Jötunn draggable Component to the panel
                // Note: This is normally automatically added when using CreateWoodpanel()
                DragWindowCntrl.ApplyDragWindowCntrl(TestPanel);

                // Create the text object
                GameObject textObject = GUIManager.Instance.CreateText(
                    text: "Jötunn, the Valheim Lib",
                    parent: TestPanel.transform,
                    anchorMin: new Vector2(0.5f, 1f),
                    anchorMax: new Vector2(0.5f, 1f),
                    position: new Vector2(0f, -100f),
                    font: GUIManager.Instance.AveriaSerifBold,
                    fontSize: 30,
                    color: GUIManager.Instance.ValheimOrange,
                    outline: true,
                    outlineColor: Color.black,
                    width: 350f,
                    height: 40f,
                    addContentSizeFitter: false);

                // Create the button object
                GameObject buttonObject = GUIManager.Instance.CreateButton(
                    text: "A Test Button - long dong schlongsen text",
                    parent: TestPanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(0, 0),
                    width: 250,
                    height: 100);
                buttonObject.SetActive(true);

                // Add a listener to the button to close the panel again
                Button button = buttonObject.GetComponent<Button>();
                button.onClick.AddListener(TogglePanel);
            }

            // Switch the current state
            bool state = !TestPanel.activeSelf;

            // Set the active state of the panel
            TestPanel.SetActive(state);

            // Toggle input for the player and camera while displaying the GUI
            GUIManager.BlockInput(state);
        }

        // Create a new ColorPicker when hovering a piece
        private void CreateColorPicker()
        {
            if (GUIManager.Instance == null)
            {
                Logger.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                Logger.LogError("GUIManager CustomGUI is null");
                return;
            }

            // Check the main scene and if the ColorPicker is not already displayed
            if (SceneManager.GetActiveScene().name == "main" && ColorPicker.done)
            {
                // Get the hovered piece and add our ColorChanger component to it
                var hovered = Player.m_localPlayer.GetHoverObject();
                var current = hovered?.GetComponentInChildren<Renderer>();
                if (current != null)
                {
                    current.gameObject.AddComponent<ColorChanger>();
                }
                else
                {
                    var parent = hovered?.transform.parent.gameObject.GetComponentInChildren<Renderer>();
                    if (parent != null)
                    {
                        parent.gameObject.AddComponent<ColorChanger>();
                    }
                }
            }

        }

        // Create a new GradientPicker
        private void CreateGradientPicker()
        {
            if (GUIManager.Instance == null)
            {
                Logger.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                Logger.LogError("GUIManager CustomGUI is null");
                return;
            }

            // Check the main scene and if the GradientPicker is not already displayed
            if (SceneManager.GetActiveScene().name == "main" && GradientPicker.done)
            {
                // Get the hovered piece and add our GradientChanger component to it
                var hovered = Player.m_localPlayer.GetHoverObject();
                var current = hovered?.GetComponentInChildren<Renderer>();
                if (current != null)
                {
                    current.gameObject.AddComponent<GradientChanger>();
                }
                else
                {
                    var parent = hovered?.transform.parent.gameObject.GetComponentInChildren<Renderer>();
                    if (parent != null)
                    {
                        parent.gameObject.AddComponent<GradientChanger>();
                    }
                }
            }
        }

        // Create some sample configuration values
        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            // Add client config which can be edited in every local instance independently
            StringConfig = Config.Bind("Client config", "LocalString", "Some string", "Client side string");
            FloatConfig = Config.Bind("Client config", "LocalFloat", 0.5f, 
                new ConfigDescription("Client side float with a value range", new AcceptableValueRange<float>(0f, 1f)));
            IntegerConfig = Config.Bind("Client config", "LocalInteger", 2, 
                new ConfigDescription("Client side integer without a range"));
            BoolConfig = Config.Bind("Client config", "LocalBool", false, 
                new ConfigDescription("Client side bool / checkbox"));

            // Add server config which gets pushed to all clients connecting and can only be edited by admins
            // In local/single player games the player is always considered the admin
            Config.Bind("Server config", "StringValue1", "StringValue", 
                new ConfigDescription("Server side string", null, 
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "FloatValue1", 750f, 
                new ConfigDescription("Server side float", 
                new AcceptableValueRange<float>(0f, 1000f), 
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "IntegerValue1", 200, 
                new ConfigDescription("Server side integer", null, 
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("Server config", "BoolValue1", false, 
                new ConfigDescription("Server side bool", null, 
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            // Colored text configs
            Config.Bind("Client config", "ColoredValue", false,
                new ConfigDescription("Colored key and description text", null, 
                new ConfigurationManagerAttributes { EntryColor = Color.blue, DescriptionColor = Color.yellow }));

            // Invisible configs
            Config.Bind("Client config", "InvisibleInt", 150,
                new ConfigDescription("Invisible int, testing browsable=false", null, 
                new ConfigurationManagerAttributes() { Browsable = false }));

            // Add a client side custom input key for the EvilSword
            EvilSwordSpecialConfig = Config.Bind("Client config", "EvilSword Special Attack", KeyCode.B, 
                new ConfigDescription("Key to unleash evil with the Evil Sword"));

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

        // Examples for reading and writing configuration values
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
                ActiveInCustomGUI = true  // Enable this button in custom GUI
            };
            InputManager.Instance.AddButton(PluginGUID, ShowGUIButton);

            RaiseSkillButton = new ButtonConfig
            {
                Name = "JotunnExampleMod_RaiseSkill",
                Key = KeyCode.RightControl,
                ActiveInGUI = true,    // Enable this button in vanilla GUI (e.g. the console)
                ActiveInCustomGUI = true  // Enable this button in custom GUI
            };
            InputManager.Instance.AddButton(PluginGUID, RaiseSkillButton);

            CreateColorPickerButton = new ButtonConfig
            {
                Name = "JotunnModExample_ColorPicker",
                Key = KeyCode.PageUp
            };
            InputManager.Instance.AddButton(PluginGUID, CreateColorPickerButton);

            CreateGradientPickerButton = new ButtonConfig
            {
                Name = "JotunnModExample_GradientPicker",
                Key = KeyCode.PageDown
            };
            InputManager.Instance.AddButton(PluginGUID, CreateGradientPickerButton);

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

        // Adds Kitbashed pieces
        private void AddKitbashedPieces()
        {
            // A simple kitbash piece, we will begin with the "empty" prefab as the base
            CustomPiece simpleKitbashPiece = new CustomPiece("piece_simple_kitbash", true, "Hammer");
            simpleKitbashPiece.FixReference = true;
            simpleKitbashPiece.Piece.m_icon = TestSprite;
            PieceManager.Instance.AddPiece(simpleKitbashPiece);

            // Now apply our Kitbash to the piece
            KitbashManager.Instance.AddKitbash(simpleKitbashPiece.PiecePrefab, new KitbashConfig
            {
                Layer = "piece",
                KitbashSources = new List<KitbashSourceConfig>
                {
                    new KitbashSourceConfig
                    {
                        Name = "eye_1",
                        SourcePrefab = "Ruby",
                        SourcePath = "attach/model",
                        Position = new Vector3(0.528f, 0.1613345f, -0.253f),
                        Rotation = Quaternion.Euler(0, 180, 0f),
                        Scale = new Vector3(0.02473f, 0.05063999f, 0.05064f)
                    },
                    new KitbashSourceConfig
                    {
                        Name = "eye_2",
                        SourcePrefab = "Ruby",
                        SourcePath = "attach/model",
                        Position = new Vector3(0.528f, 0.1613345f, 0.253f),
                        Rotation = Quaternion.Euler(0, 180, 0f),
                        Scale = new Vector3(0.02473f, 0.05063999f, 0.05064f)
                    },
                    new KitbashSourceConfig
                    {
                        Name = "mouth",
                        SourcePrefab = "draugr_bow",
                        SourcePath = "attach/bow",
                        Position = new Vector3(0.53336f, -0.315f, -0.001953f),
                        Rotation = Quaternion.Euler(-0.06500001f, -2.213f, -272.086f),
                        Scale = new Vector3(0.41221f, 0.41221f, 0.41221f)
                    }
                }
            });

            // A more complex Kitbash piece, this has a prepared GameObject for Kitbash to build upon
            AssetBundle kitbashAssetBundle = AssetUtils.LoadAssetBundleFromResources("kitbash", typeof(JotunnModExample).Assembly);
            try
            {
                KitbashObject kitbashObject = KitbashManager.Instance.AddKitbash(kitbashAssetBundle.LoadAsset<GameObject>("piece_odin_statue"), new KitbashConfig
                {
                    Layer = "piece",
                    KitbashSources = new List<KitbashSourceConfig>
                    {
                        new KitbashSourceConfig
                        {
                            SourcePrefab = "piece_artisanstation",
                            SourcePath = "ArtisanTable_Destruction/ArtisanTable_Destruction.007_ArtisanTable.019",
                            TargetParentPath = "new",
                            Position = new Vector3(-1.185f, -0.465f, 1.196f),
                            Rotation = Quaternion.Euler(-90f, 0, 0),
                            Scale = Vector3.one,Materials = new string[]{
                                "obsidian_nosnow",
                                "bronze"
                            }
                        },
                        new KitbashSourceConfig
                        {
                            SourcePrefab = "guard_stone",
                            SourcePath = "new/default",
                            TargetParentPath = "new/pivot",
                            Position = new Vector3(0, 0.0591f ,0),
                            Rotation = Quaternion.identity,
                            Scale = Vector3.one * 0.2f,
                            Materials = new string[]{
                                "bronze",
                                "obsidian_nosnow"
                            }
                        },
                    }
                });
                kitbashObject.OnKitbashApplied += () =>
                {
                    // We've added a CapsuleCollider to the skeleton, this is no longer needed
                    Destroy(kitbashObject.Prefab.transform.Find("new/pivot/default").GetComponent<MeshCollider>());
                };
                PieceManager.Instance.AddPiece(new CustomPiece(kitbashObject.Prefab, new PieceConfig
                {
                    PieceTable = "Hammer",
                    Requirements = new RequirementConfig[]
                    {
                        new RequirementConfig { Item = "Obsidian" , Recover = true},
                        new RequirementConfig { Item = "Bronze", Recover = true }
                    }
                }));
            }
            finally
            {
                kitbashAssetBundle.Unload(false);
            }
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