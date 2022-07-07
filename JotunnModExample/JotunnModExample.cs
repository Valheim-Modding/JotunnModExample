// JotunnModExample
// A Valheim mod using Jötunn
// Used to demonstrate the libraries capabilities
// 
// File:    JotunnModExample.cs
// Project: JotunnModExample

using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.GUI;
using Jotunn.Managers;
using Jotunn.Utils;
using JotunnModExample.ConsoleCommands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        // BepInEx' plugin metadata
        public const string PluginGUID = "com.jotunn.JotunnModExample";
        public const string PluginName = "JotunnModExample";
        public const string PluginVersion = "2.7.1";

        // Your mod's custom localization
        private CustomLocalization Localization;

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

        // Variable button backed by a KeyCode and a GamepadButton config
        private ConfigEntry<KeyCode> EvilSwordSpecialConfig;
        private ConfigEntry<InputManager.GamepadButton> EvilSwordGamepadConfig;
        private ButtonConfig EvilSwordSpecialButton;

        // Variable BepInEx Shortcut backed by a config
        private ConfigEntry<KeyboardShortcut> ShortcutConfig;
        private ButtonConfig ShortcutButton;

        // Configuration values
        private ConfigEntry<string> StringConfig;
        private ConfigEntry<float> FloatConfig;
        private ConfigEntry<int> IntegerConfig;
        private ConfigEntry<bool> BoolConfig;

        // Custom skill
        private Skills.SkillType TestSkill = 0;

        // Custom status effect
        private CustomStatusEffect EvilSwordEffect;

        // Custom RPC
        public static CustomRPC UselessRPC;

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
            AddConePiece();

            // Add custom items cloned from vanilla items
            PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;

            // Add custom pieces cloned from vanilla pieces
            PrefabManager.OnVanillaPrefabsAvailable += CreateDeerRugPiece;

            // Add a cloned item with custom variants
            PrefabManager.OnVanillaPrefabsAvailable += AddVariants;

            // Add a cloned item with a runtime-rendered icon
            PrefabManager.OnVanillaPrefabsAvailable += AddItemsWithRenderedIcons;

            // Create custom locations and vegetation
            PrefabManager.OnVanillaPrefabsAvailable += AddCustomLocationsAndVegetation;
            ZoneManager.OnVanillaLocationsAvailable += AddClonedVanillaLocationsAndVegetations;
            ZoneManager.OnVanillaLocationsAvailable += ModifyVanillaLocationsAndVegetation;

            // Create custom creatures and spawns
            AddCustomCreaturesAndSpawns();
            // Hook creature manager to get access to vanilla creature prefabs
            CreatureManager.OnVanillaCreaturesAvailable += ModifyAndCloneVanillaCreatures;

            // Add a custom command for our custom RPC call
            CommandManager.Instance.AddConsoleCommand(new UselessRPCommand());

            // Create your RPC as early as possible so it gets registered with the game
            UselessRPC = NetworkManager.Instance.AddRPC("UselessRPC", UselessRPCServerReceive, UselessRPCClientReceive);

            // Add map overlays to the minimap on world load
            MinimapManager.OnVanillaMapAvailable += CreateMapOverlay;

            // Add map overlays to the minimap after map data has been loaded
            MinimapManager.OnVanillaMapDataLoaded += CreateMapDrawing;
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
                // Our button is configured to block all other input, so we just want to query
                // ZInput when our custom item is equipped.
                if (EvilSwordSpecialButton != null && MessageHud.instance != null &&
                    Player.m_localPlayer != null && Player.m_localPlayer.m_visEquipment.m_rightItem == "EvilSword")
                {
                    if (ZInput.GetButton(EvilSwordSpecialButton.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$evilsword_beevilmessage");
                    }
                }

                // KeyboardShortcuts are also injected into the ZInput system
                if (ShortcutButton != null && MessageHud.instance != null)
                {
                    if (ZInput.GetButtonDown(ShortcutButton.Name) && MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$lulzcut_message");
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
                TestPanel.AddComponent<DragWindowCntrl>();

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
                    position: new Vector2(0, -250f),
                    width: 250f,
                    height: 60f);
                buttonObject.SetActive(true);

                // Add a listener to the button to close the panel again
                Button button = buttonObject.GetComponent<Button>();
                button.onClick.AddListener(TogglePanel);

                // Create a dropdown
                var dropdownObject = GUIManager.Instance.CreateDropDown(
                    parent: TestPanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(-250f, -250f),
                    fontSize: 16,
                    width: 100f,
                    height: 30f);
                dropdownObject.GetComponent<Dropdown>().AddOptions(new List<string>
                {
                    "bla", "blubb", "börks", "blarp", "harhar"
                });

                // Create an input field
                GUIManager.Instance.CreateInputField(
                    parent: TestPanel.transform,
                    anchorMin: new Vector2(0.5f, 0.5f),
                    anchorMax: new Vector2(0.5f, 0.5f),
                    position: new Vector2(250f, -250f),
                    contentType: InputField.ContentType.Standard,
                    placeholderText: "input...",
                    fontSize: 16,
                    width: 160f,
                    height: 30f);
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
            Config.Bind("ServerConfig", "KeyboardShortcutValue",
                new KeyboardShortcut(KeyCode.A, KeyCode.LeftControl),
                    new ConfigDescription("Server side KeyboardShortcut", null,
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
            // Also add an alternative Gamepad button for the EvilSword
            EvilSwordGamepadConfig = Config.Bind("Client config", "EvilSword Special Attack Gamepad", InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("Button to unleash evil with the Evil Sword"));

            // BepInEx' KeyboardShortcut class is supported, too
            ShortcutConfig = Config.Bind("Client config", "Keycodes with modifiers",
                new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl, KeyCode.LeftAlt),
                new ConfigDescription("Secret key combination"));

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
            SteelIngotBundle = AssetUtils.LoadAssetBundleFromResources("steel");
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
            // Also adds the alternative Config for the gamepad button
            // The HintToken is used for the custom KeyHint of the EvilSword
            EvilSwordSpecialButton = new ButtonConfig
            {
                Name = "EvilSwordSpecialAttack",
                Config = EvilSwordSpecialConfig,        // Keyboard input
                GamepadConfig = EvilSwordGamepadConfig, // Gamepad input
                HintToken = "$evilsword_beevil",        // Displayed KeyHint
                BlockOtherInputs = true   // Blocks all other input for this Key / Button
            };
            InputManager.Instance.AddButton(PluginGUID, EvilSwordSpecialButton);

            // Supply your KeyboardShortcut configs to ShortcutConfig instead.
            ShortcutButton = new ButtonConfig
            {
                Name = "SecretShortcut",
                ShortcutConfig = ShortcutConfig,
                HintToken = "$lulzcut"
            };
            InputManager.Instance.AddButton(PluginGUID, ShortcutButton);
        }

        // Adds hardcoded localizations
        private void AddLocalizations()
        {
            // Create a custom Localization instance and add it to the Manager
            Localization = new CustomLocalization();
            LocalizationManager.Instance.AddLocalization(Localization);

            // Add translations for our custom skill
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"skill_TestingSkill", "TestLocalizedSkillName" }
            });

            // Add translations for the custom item in AddClonedItems
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"item_evilsword", "Sword of Darkness"}, {"item_evilsword_desc", "Bringing the light"},
                {"evilsword_shwing", "Woooosh"}, {"evilsword_scroll", "*scroll*"},
                {"evilsword_beevil", "Be evil"}, {"evilsword_beevilmessage", ":reee:"},
                {"evilsword_effectname", "Evil"}, {"evilsword_effectstart", "You feel evil"},
                {"evilsword_effectstop", "You feel nice again"}
            });

            // Add translations for the custom piece in AddPieceCategories
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                { "piece_lul", "Lulz" }, { "piece_lul_description", "Do it for them" },
                { "piece_lel", "Lölz" }, { "piece_lel_description", "Härhärhär" }
            });

            // Add translations for the custom items in AddVariants
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                { "lulz_shield", "Lulz Shield" }, { "lulz_shield_desc", "Lough at your enemies" },
                { "lulz_sword", "Lulz Sword" }, { "lulz_sword_desc", "Lulz on a stick" }
            });

            // Add translations for the KeyboardShortcut
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"lulzcut", "lol at 'em"}, {"lulzcut_message", "Trololol"}
            });

            // Add translations for the rendered tree
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"rendered_tree", "Rendered Tree"}, {"rendered_tree_desc", "A powerful tree, that can render its own icon. Magic!"}
            });
        }

        // Register new console commands
        private void AddCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.AddConsoleCommand(new TpCommand());
            CommandManager.Instance.AddConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.AddConsoleCommand(new SkinColorCommand());
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
            RecipeConfig meatConfig = new RecipeConfig();
            meatConfig.Item = "CookedMeat"; // Name of the item prefab to be crafted
            meatConfig.AddRequirement(new RequirementConfig("Stone", 2)); // Resources and amount needed for it to be crafted
            meatConfig.AddRequirement(new RequirementConfig("Wood", 1));
            ItemManager.Instance.AddRecipe(new CustomRecipe(meatConfig));

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
            var cookConfig = new CookingConversionConfig();
            cookConfig.FromItem = "CookedMeat";
            cookConfig.ToItem = "CookedLoxMeat";
            cookConfig.CookTime = 2f;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(cookConfig));

            // Add an item conversion for the Fermenter. You can specify how much new items the conversion yields.
            var fermentConfig = new FermenterConversionConfig();
            fermentConfig.ToItem = "CookedLoxMeat";
            fermentConfig.FromItem = "Coal";
            fermentConfig.ProducedItems = 10;
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(fermentConfig));

            // Add an item conversion for the smelter
            var smelterConfig = new SmelterConversionConfig();
            smelterConfig.FromItem = "Stone";
            smelterConfig.ToItem = "CookedLoxMeat";
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(smelterConfig));

            // Load and create a custom item to use in another conversion
            var steel_prefab = SteelIngotBundle.LoadAsset<GameObject>("Steel");
            var ingot = new CustomItem(steel_prefab, fixReference: false);
            ItemManager.Instance.AddItem(ingot);

            // Create a conversion for the blastfurnace, the custom item is the new outcome
            var blastConfig = new SmelterConversionConfig();
            blastConfig.Station = "blastfurnace"; // Override the default "smelter" station of the SmelterConversionConfig
            blastConfig.FromItem = "Iron";
            blastConfig.ToItem = "Steel"; // This is our custom prefabs name we have loaded just above
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(blastConfig));

            // Add an incinerator conversion. This one is special since the incinerator conversion script 
            // takes one or more items to produce any amount of a new item
            var incineratorConfig = new IncineratorConversionConfig();
            incineratorConfig.Requirements.Add(new IncineratorRequirementConfig("Wood", 1));
            incineratorConfig.Requirements.Add(new IncineratorRequirementConfig("Stone", 1));
            incineratorConfig.ToItem = "Coins";
            incineratorConfig.ProducedItems = 20;
            incineratorConfig.RequireOnlyOneIngredient = false; // true = only one of the requirements is needed to produce the output
            incineratorConfig.Priority = 5;                     // Higher priorities get preferred when multiple requirements are met
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(incineratorConfig));
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
            ItemConfig runeConfig = new ItemConfig();
            runeConfig.Amount = 1;
            runeConfig.AddRequirement(new RequirementConfig("Stone", 1));
            // Prefab did not use mocked refs so no need to fix them
            var runeItem = new CustomItem(BlueprintRuneBundle, "BlueprintTestRune", fixReference: false, runeConfig);
            ItemManager.Instance.AddItem(runeItem);
        }

        // Implementation of pieces via configs.
        private void CreateRunePieces()
        {
            // Create and add a custom piece for the rune. Add the prefab name of the PieceTable to the config.
            PieceConfig makeConfig = new PieceConfig();
            makeConfig.PieceTable = "_BlueprintTestTable";
            var makePiece = new CustomPiece(BlueprintRuneBundle, "make_testblueprint", fixReference: false, makeConfig);
            PieceManager.Instance.AddPiece(makePiece);

            // Load, create and add another custom piece for the rune. This piece uses more properties
            // of the PieceConfig - it can now be build in dungeons and has actual requirements to build it.
            var placeConfig = new PieceConfig();
            placeConfig.PieceTable = "_BlueprintTestTable";
            placeConfig.AllowedInDungeons = true;
            placeConfig.AddRequirement(new RequirementConfig("Wood", 2));
            var placePiece = new CustomPiece(BlueprintRuneBundle, "piece_testblueprint", fixReference: false, placeConfig);
            PieceManager.Instance.AddPiece(placePiece);

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
                Localization.AddJsonFile(lang, textAsset.ToString());
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
            KeyHintManager.Instance.AddKeyHint(KHC_base);

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
            KeyHintManager.Instance.AddKeyHint(KHC_make);

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
            KeyHintManager.Instance.AddKeyHint(KHC_piece);

            // Add additional localization manually
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"bprune_make", "Capture Blueprint"}, {"bprune_piece", "Place Blueprint"}
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

            if (CP.PiecePrefab)
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

            if (CP.PiecePrefab)
            {
                // Add our test texture to the Unity MeshRenderer and make the material color grey
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = TestTex;
                prefab.GetComponent<MeshRenderer>().material.color = Color.grey;

                PieceManager.Instance.AddPiece(CP);
            }
            
            // Add lal piece for vegetation
            Sprite var4 = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/test_var4.png");
            CP = new CustomPiece("piece_lal", true, new PieceConfig
            {
                Name = "Lalalal",
                Description = "<3",
                Icon = var4,
                PieceTable = "Hammer",
                ExtendStation = "piece_workbench", // Test station extension
                Category = "Lulzies"  // Test custom category
            });
            PieceManager.Instance.AddPiece(CP);
            CP.PiecePrefab.GetComponent<MeshRenderer>().material.mainTexture = var4.texture;

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

            // Load completely mocked "Shit Sword" (Cheat Sword copy)
            var cheatybundle = AssetUtils.LoadAssetBundleFromResources("cheatsword");
            var cheaty = cheatybundle.LoadAsset<GameObject>("Cheaty");
            ItemManager.Instance.AddItem(new CustomItem(cheaty, fixReference: true));
            cheatybundle.Unload(false);
        }

        private void AddConePiece()
        {
            AssetBundle pieceBundle = AssetUtils.LoadAssetBundleFromResources("pieces");

            PieceConfig cylinder = new PieceConfig();
            cylinder.Name = "$cylinder_display_name";
            cylinder.PieceTable = "Hammer";
            cylinder.AddRequirement(new RequirementConfig("Wood", 2, 0, true));

            PieceManager.Instance.AddPiece(new CustomPiece(pieceBundle, "Cylinder", fixReference: false, cylinder));
        }

        private void CreateDeerRugPiece()
        {
            PieceConfig rug = new PieceConfig();
            rug.Name = "$our_rug_deer_display_name";
            rug.PieceTable = "Hammer";
            rug.Category = "Misc";
            rug.AddRequirement(new RequirementConfig("Wood", 2, 0, true));

            PieceManager.Instance.AddPiece(new CustomPiece("our_rug_deer", "rug_deer", rug));

            // You want that to run only once, Jotunn has the piece cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= CreateDeerRugPiece;
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
            AssetBundle kitbashAssetBundle = AssetUtils.LoadAssetBundleFromResources("kitbash");
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
                            Scale = Vector3.one,Materials = new[]{
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
                            Materials = new[]{
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

                PieceManager.Instance.AddPiece(
                    new CustomPiece(kitbashObject.Prefab, fixReference: false,
                    new PieceConfig
                    {
                        PieceTable = "Hammer",
                        Requirements = new[]
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
            // Create a custom resource based on Wood
            ItemConfig customWoodConfig = new ItemConfig();
            customWoodConfig.Name = "$item_customWood";
            customWoodConfig.Description = "$item_customWood_desc";
            customWoodConfig.AddRequirement(new RequirementConfig("Wood", 1));
            CustomItem recipeComponent = new CustomItem("CustomWood", "Wood", customWoodConfig);
            ItemManager.Instance.AddItem(recipeComponent);

            // Create and add a custom item based on SwordBlackmetal
            ItemConfig evilSwordConfig = new ItemConfig();
            evilSwordConfig.Name = "$item_evilsword";
            evilSwordConfig.Description = "$item_evilsword_desc";
            evilSwordConfig.CraftingStation = "piece_workbench";
            evilSwordConfig.AddRequirement(new RequirementConfig("Stone", 1));
            evilSwordConfig.AddRequirement(new RequirementConfig("CustomWood", 1));

            CustomItem evilSword = new CustomItem("EvilSword", "SwordBlackmetal", evilSwordConfig);
            ItemManager.Instance.AddItem(evilSword);

            // Add our custom status effect to it
            evilSword.ItemDrop.m_itemData.m_shared.m_equipStatusEffect = EvilSwordEffect.StatusEffect;

            // Show a different KeyHint for the sword.
            KeyHintsEvilSword();

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;
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
            KeyHintManager.Instance.AddKeyHint(KHC);
        }

        // Clone the wooden shield and the bronze sword and add own variations to it
        private void AddVariants()
        {
            Sprite var1 = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/test_var1.png");
            Sprite var2 = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/test_var2.png");
            Sprite var3 = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/test_var3.png");
            Sprite var4 = AssetUtils.LoadSpriteFromFile("JotunnModExample/Assets/test_var4.png");
            Texture2D styleTex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_varpaint.png");

            ItemConfig shieldConfig = new ItemConfig();
            shieldConfig.Name = "$lulz_shield";
            shieldConfig.Description = "$lulz_shield_desc";
            shieldConfig.AddRequirement(new RequirementConfig("Wood", 1));
            shieldConfig.Icons = new Sprite[] { var1, var2, var3, var4 };
            shieldConfig.StyleTex = styleTex;
            ItemManager.Instance.AddItem(new CustomItem("item_lulzshield", "ShieldWood", shieldConfig));

            ItemConfig swordConfig = new ItemConfig();
            swordConfig.Name = "$lulz_sword";
            swordConfig.Description = "$lulz_sword_desc";
            swordConfig.AddRequirement(new RequirementConfig("Stone", 1));
            swordConfig.Icons = new Sprite[] { var1, var2, var3, var4 };
            swordConfig.StyleTex = styleTex;
            ItemManager.Instance.AddItem(new CustomItem("item_lulzsword", "SwordBronze", swordConfig));

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddVariants;
        }

        // Create rendered icons from prefabs
        private void AddItemsWithRenderedIcons()
        {
            // Use the vanilla beech tree prefab to render our icon from
            GameObject beech = PrefabManager.Instance.GetPrefab("Beech1");

            Sprite renderedIcon = RenderManager.Instance.Render(beech, RenderManager.IsometricRotation);

            // Create the custom item with the rendered icon
            ItemConfig treeItemConfig = new ItemConfig();
            treeItemConfig.Name = "$rendered_tree";
            treeItemConfig.Description = "$rendered_tree_desc";
            treeItemConfig.Icons = new Sprite[] { renderedIcon };
            treeItemConfig.AddRequirement(new RequirementConfig("Wood", 2, 0, true));

            ItemManager.Instance.AddItem(new CustomItem("item_MyTree", "BeechSeeds", treeItemConfig));

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddItemsWithRenderedIcons;
        }

        private void AddCustomLocationsAndVegetation()
        {
            AssetBundle locationsAssetBundle = AssetUtils.LoadAssetBundleFromResources("custom_locations");
            try
            {
                // Create location from AssetBundle using spawners and random spawns
                var spawnerLocation = locationsAssetBundle.LoadAsset<GameObject>("SpawnerLocation");

                // Create a location container from your prefab if you want to alter it before adding the location to the manager.
                /*
                var spawnerLocation =
                    ZoneManager.Instance.CreateLocationContainer(
                        locationsAssetBundle.LoadAsset<GameObject>("SpawnerLocation"));
                */

                var spawnerConfig = new LocationConfig();
                spawnerConfig.Biome = Heightmap.Biome.Meadows;
                spawnerConfig.Quantity = 100;
                spawnerConfig.Priotized = true;
                spawnerConfig.ExteriorRadius = 2f;
                spawnerConfig.MinAltitude = 1f;
                spawnerConfig.ClearArea = true;

                ZoneManager.Instance.AddCustomLocation(new CustomLocation(spawnerLocation, true, spawnerConfig));

                // Use empty location containers for locations instantiated in code
                var lulzCubePrefab = PrefabManager.Instance.GetPrefab("piece_lul");
                var cubesLocation = ZoneManager.Instance.CreateLocationContainer("lulzcube_location");

                // Stack of lulzcubes to easily spot the instances
                for (int i = 0; i < 10; i++)
                {
                    var lulzCube = Instantiate(lulzCubePrefab, cubesLocation.transform);
                    lulzCube.name = lulzCubePrefab.name;
                    lulzCube.transform.localPosition = new Vector3(0, i + 3, 0);
                    lulzCube.transform.localRotation = Quaternion.Euler(0, i * 30, 0);
                }

                var cubesConfig = new LocationConfig();
                cubesConfig.Biome = Heightmap.Biome.Meadows;
                cubesConfig.Quantity = 100;
                cubesConfig.Priotized = true;
                cubesConfig.ExteriorRadius = 2f;
                cubesConfig.ClearArea = true;

                ZoneManager.Instance.AddCustomLocation(new CustomLocation(cubesLocation, false, cubesConfig));

                // Use vegetation for singular prefabs
                var singleLulz = new VegetationConfig();
                singleLulz.Biome = Heightmap.Biome.Meadows;
                singleLulz.BlockCheck = true;

                ZoneManager.Instance.AddCustomVegetation(new CustomVegetation(lulzCubePrefab, false, singleLulz));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while adding custom locations: {ex}");
            }
            finally
            {
                // Custom locations and vegetations are added every time the game loads, we don't need to add every time
                PrefabManager.OnVanillaPrefabsAvailable -= AddCustomLocationsAndVegetation;
                locationsAssetBundle.Unload(false);
            }
        }

        private void AddClonedVanillaLocationsAndVegetations()
        {
            try
            {
                var lulzCubePrefab = PrefabManager.Instance.GetPrefab("piece_lal");

                // Create a clone of a vanilla location
                CustomLocation myEikthyrLocation =
                    ZoneManager.Instance.CreateClonedLocation("MyEikthyrAltar", "Eikthyrnir");
                myEikthyrLocation.ZoneLocation.m_exteriorRadius = 1f; // Easy to place :D
                myEikthyrLocation.ZoneLocation.m_quantity = 20; // MOAR

                // Stack of lulzcubes to easily spot the instances
                for (int i = 0; i < 40; i++)
                {
                    var lulzCube = Instantiate(lulzCubePrefab, myEikthyrLocation.ZoneLocation.m_prefab.transform);
                    lulzCube.name = lulzCubePrefab.name;
                    lulzCube.transform.localPosition = new Vector3(0, i + 3, 0);
                    lulzCube.transform.localRotation = Quaternion.Euler(0, i * 30, 0);
                }

                // Add more seed carrots to the meadows & black forest
                ZoneSystem.ZoneVegetation pickableSeedCarrot = ZoneManager.Instance.GetZoneVegetation("Pickable_SeedCarrot");

                var carrotSeed = new VegetationConfig(pickableSeedCarrot);
                carrotSeed.Min = 3;
                carrotSeed.Max = 10;
                carrotSeed.GroupSizeMin = 3;
                carrotSeed.GroupSizeMax = 10;
                carrotSeed.GroupRadius = 10;
                carrotSeed.Biome = ZoneManager.AnyBiomeOf(Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest);

                ZoneManager.Instance.AddCustomVegetation(new CustomVegetation(pickableSeedCarrot.m_prefab, false, carrotSeed));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while adding cloned locations: {ex}");
            }
            finally
            {
                // Custom locations and vegetations are added every time the game loads, we don't need to add every time
                ZoneManager.OnVanillaLocationsAvailable -= AddClonedVanillaLocationsAndVegetations;
            }
        }

        private void ModifyVanillaLocationsAndVegetation()
        {
            var lulzCubePrefab = PrefabManager.Instance.GetPrefab("piece_lel");

            // Modify existing locations
            var eikhtyrLocation = ZoneManager.Instance.GetZoneLocation("Eikthyrnir");
            eikhtyrLocation.m_exteriorRadius = 20f; //More space around the altar

            var eikhtyrCube = Instantiate(lulzCubePrefab, eikhtyrLocation.m_prefab.transform);
            eikhtyrCube.name = lulzCubePrefab.name;
            eikhtyrCube.transform.localPosition = new Vector3(-8.52f, 5.37f, -0.92f);

            // Modify existing vegetation
            var raspberryBush = ZoneManager.Instance.GetZoneVegetation("RaspberryBush");
            raspberryBush.m_groupSizeMin = 10;
            raspberryBush.m_groupSizeMax = 30;

            // Not unregistering this hook, it needs to run every world load
        }
        
        // Add custom made creatures using world spawns and drop lists
        private void AddCustomCreaturesAndSpawns()
        {
            AssetBundle creaturesAssetBundle = AssetUtils.LoadAssetBundleFromResources("creatures");
            try
            {
                // Load LulzCube test texture and sprite
                var lulztex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_tex.jpg");
                var lulzsprite = Sprite.Create(lulztex, new Rect(0f, 0f, lulztex.width, lulztex.height), Vector2.zero);

                // Create an optional drop/consume item for this creature
                CreateDropConsumeItem(lulzsprite, lulztex);

                // Load and create a custom animal creature
                CreateAnimalCreature(creaturesAssetBundle, lulztex);
                
                // Load and create a custom monster creature
                CreateMonsterCreature(creaturesAssetBundle, lulztex);
                
                // Add localization for all stuff added
                Localization.AddTranslation("English", new Dictionary<string, string>
                {
                    {"item_lulzanimalparts", "Parts of a Lulz Animal"},
                    {"item_lulzanimalparts_desc", "Remains of a LulzAnimal. It still giggles when touched."},
                    {"creature_lulzanimal", "Lulz Animal"},
                    {"creature_lulzmonster", "Lulz Monster"}
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while adding custom creatures: {ex}");
            }
            finally
            {
                creaturesAssetBundle.Unload(false);
            }
        }

        private void CreateDropConsumeItem(Sprite lulzsprite, Texture2D lulztex)
        {
            // Create a little lulz cube as the drop and consume item for both creatures
            ItemConfig lulzCubeConfig = new ItemConfig();
            lulzCubeConfig.Name = "$item_lulzanimalparts";
            lulzCubeConfig.Description = "$item_lulzanimalparts_desc";
            lulzCubeConfig.Icons = new[] {lulzsprite};
            var lulzItem = new CustomItem("item_lul", true, lulzCubeConfig);

            lulzItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 20;
            lulzItem.ItemPrefab.AddComponent<Rigidbody>();

            // Set our lulzcube test texture on the first material found
            lulzItem.ItemPrefab.GetComponentInChildren<MeshRenderer>().material.mainTexture = lulztex;

            // Make it smol
            lulzItem.ItemPrefab.GetComponent<ZNetView>().m_syncInitialScale = true;
            lulzItem.ItemPrefab.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);

            // Add to the ItemManager
            ItemManager.Instance.AddItem(lulzItem);
        }

        private void CreateAnimalCreature(AssetBundle creaturesAssetBundle, Texture2D lulztex)
        {
            // Load creature prefab from AssetBundle
            var lulzAnimalPrefab = creaturesAssetBundle.LoadAsset<GameObject>("LulzAnimal");

            // Set our lulzcube test texture on the first material found
            lulzAnimalPrefab.GetComponentInChildren<MeshRenderer>().material.mainTexture = lulztex;

            // Create a custom creature using our drop item and spawn configs
            var lulzAnimalConfig = new CreatureConfig();
            lulzAnimalConfig.Name = "$creature_lulzanimal";
            lulzAnimalConfig.Faction = Character.Faction.AnimalsVeg;
            lulzAnimalConfig.AddDropConfig(new DropConfig {
                Item = "item_lul",
                Chance = 100f,
                LevelMultiplier = false,
                MinAmount = 1,
                MaxAmount = 3,
                //OnePerPlayer = true
            });
            lulzAnimalConfig.AddSpawnConfig(new SpawnConfig {
                Name = "Jotunn_LulzAnimalSpawn1",
                SpawnChance = 100f,
                SpawnInterval = 1f,
                SpawnDistance = 1f,
                MaxSpawned = 10,
                Biome = Heightmap.Biome.Meadows
            });
            lulzAnimalConfig.AddSpawnConfig(new SpawnConfig {
                Name = "Jotunn_LulzAnimalSpawn2",
                SpawnChance = 50f,
                SpawnInterval = 2f,
                SpawnDistance = 2f,
                MaxSpawned = 5,
                Biome = ZoneManager.AnyBiomeOf(Heightmap.Biome.BlackForest, Heightmap.Biome.Plains)
            });

            // Add it to the manager
            CreatureManager.Instance.AddCreature(new CustomCreature(lulzAnimalPrefab, false, lulzAnimalConfig));
        }

        private void CreateMonsterCreature(AssetBundle creaturesAssetBundle, Texture2D lulztex)
        {
            // Load creature prefab from AssetBundle
            var lulzMonsterPrefab = creaturesAssetBundle.LoadAsset<GameObject>("LulzMonster");

            // Set our lulzcube test texture on the first material found
            lulzMonsterPrefab.GetComponentInChildren<MeshRenderer>().material.mainTexture = lulztex;

            // Create a custom creature using our consume item and spawn configs
            var lulzMonsterConfig = new CreatureConfig();
            lulzMonsterConfig.Name = "$creature_lulzmonster";
            lulzMonsterConfig.Faction = Character.Faction.ForestMonsters;
            lulzMonsterConfig.UseCumulativeLevelEffects = true;
            lulzMonsterConfig.AddConsumable("item_lul");
            lulzMonsterConfig.AddSpawnConfig(new SpawnConfig
            {
                Name = "Jotunn_LulzMonsterSpawn1",
                SpawnChance = 100f,
                MaxSpawned = 1,
                Biome = Heightmap.Biome.Meadows
            });
            lulzMonsterConfig.AddSpawnConfig(new SpawnConfig
            {
                Name = "Jotunn_LulzMonsterSpawn2",
                SpawnChance = 50f,
                MaxSpawned = 1,
                Biome = ZoneManager.AnyBiomeOf(Heightmap.Biome.BlackForest, Heightmap.Biome.Plains)
            });

            // Add it to the manager
            CreatureManager.Instance.AddCreature(new CustomCreature(lulzMonsterPrefab, true, lulzMonsterConfig));
        }

        // Modify and clone vanilla creatures
        private void ModifyAndCloneVanillaCreatures()
        {
            // Clone a vanilla creature with and add new spawn information
            var lulzetonConfig = new CreatureConfig();
            lulzetonConfig.AddSpawnConfig(new SpawnConfig
            {
                Name = "Jotunn_SkelSpawn1",
                SpawnChance = 100,
                SpawnInterval = 20f,
                SpawnDistance = 1f,
                Biome = Heightmap.Biome.Meadows,
                MinLevel = 3
            });

            var lulzeton = new CustomCreature("Lulzeton", "Skeleton_NoArcher", lulzetonConfig);
            var lulzoid = lulzeton.Prefab.GetComponent<Humanoid>();
            lulzoid.m_walkSpeed = 0.1f;
            CreatureManager.Instance.AddCreature(lulzeton);

            // Get a vanilla creature prefab and change some values
            var skeleton = CreatureManager.Instance.GetCreaturePrefab("Skeleton_NoArcher");
            var humanoid = skeleton.GetComponent<Humanoid>();
            humanoid.m_walkSpeed = 2;

            // Unregister the hook, modified and cloned creatures are kept over the whole game session
            CreatureManager.OnVanillaCreaturesAvailable -= ModifyAndCloneVanillaCreatures;
        }

        // Custom console command to invoke the custom RPC call
        public class UselessRPCommand : ConsoleCommand
        {
            public override string Name => "useless_rpc";

            public override string Help => "Send random data chunks over a custom RPC";

            private int[] Sizes = { 0, 1, 2, 4 };

            public override void Run(string[] args)
            {
                // Sanitize user's input
                if (args.Length != 1 || !Sizes.Any(x => x.Equals(int.Parse(args[0]))))
                {
                    Console.instance.Print($"Usage: {Name} [{string.Join("|", Sizes)}]");
                    return;
                }

                // Create a ZPackage and fill it with random bytes
                ZPackage package = new ZPackage();
                System.Random random = new System.Random();
                byte[] array = new byte[int.Parse(args[0]) * 1024 * 1024];
                random.NextBytes(array);
                package.Write(array);

                // Invoke the RPC with the server as the target and our random data package as the payload
                Jotunn.Logger.LogMessage($"Sending {args[0]}MB blob to server.");
                UselessRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
            }

            public override List<string> CommandOptionList()
            {
                return Sizes.Select(x => x.ToString()).ToList();
            }
        }

        // React to the RPC call on a server
        private IEnumerator UselessRPCServerReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob, processing");

            string dot = string.Empty;
            for (int i = 0; i < 5; ++i)
            {
                dot += ".";
                Jotunn.Logger.LogMessage(dot);
                yield return new WaitForSeconds(1f);
            }

            Jotunn.Logger.LogMessage($"Broadcasting to all clients");
            UselessRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(package.GetArray()));
        }

        // React to the RPC call on a client
        private IEnumerator UselessRPCClientReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob, processing");
            yield return null;

            string dot = string.Empty;
            for (int i = 0; i < 10; ++i)
            {
                dot += ".";
                Jotunn.Logger.LogMessage(dot);
                yield return new WaitForSeconds(.5f);
            }
        }
        
        // Map overlay showing the zone boundaries
        private void CreateMapOverlay()
        {
            // Get or create a map overlay instance by name
            var zoneOverlay = MinimapManager.Instance.GetMapOverlay("ZoneOverlay");

            // Create a Color array with space for every pixel of the map
            int mapSize = zoneOverlay.TextureSize * zoneOverlay.TextureSize;
            Color[] mainPixels = new Color[mapSize];
            
            // Iterate over the dimensions of the overlay and set a color for
            // every pixel in our mainPixels array wherever a zone boundary is
            Color color = Color.white;
            int zoneSize = 64;
            int index = 0;
            for (int x = 0; x < zoneOverlay.TextureSize; ++x)
            {
                for (int y = 0; y < zoneOverlay.TextureSize; ++y, ++index)
                {
                    if (x % zoneSize == 0 || y % zoneSize == 0)
                    {
                        mainPixels[index] = color;
                    }
                }
            }

            // Set the pixel array on the overlay texture
            // This is much faster than setting every pixel individually
            zoneOverlay.OverlayTex.SetPixels(mainPixels);

            // Apply the changes to the overlay
            // This also triggers the MinimapManager to display this overlay
            zoneOverlay.OverlayTex.Apply();
        }
        
        // Draw a square starting at every map pin
        private void CreateMapDrawing()
        {
            // Get or create a map drawing instance by name
            var pinOverlay = MinimapManager.Instance.GetMapDrawing("PinOverlay");

            // Create Color arrays which can be set as a block on the texture
            // Note: "Populate" is an extension method provided by Jötunn
            // filling the new array with the provided value
            int squareSize = 10;
            Color[] colorPixels = new Color[squareSize*squareSize].Populate(Color.blue);
            Color[] filterPixels = new Color[squareSize*squareSize].Populate(MinimapManager.FilterOff);
            Color[] heightPixels = new Color[squareSize*squareSize].Populate(MinimapManager.MeadowHeight);

            // Loop every loaded pin
            foreach (var p in Minimap.instance.m_pins)
            {
                // Translate the world position of the pin to the overlay position
                var pos = MinimapManager.Instance.WorldToOverlayCoords(p.m_pos, pinOverlay.TextureSize);
                
                // Set a block of pixels on the MainTex to make the map use our color instead of the vanilla one
                pinOverlay.MainTex.SetPixels((int)pos.x, (int)pos.y, squareSize, squareSize, colorPixels);
                
                // Set a block of pixels on the ForestFilter and FogFilter, removing forest and fog from the map
                pinOverlay.ForestFilter.SetPixels((int)pos.x, (int)pos.y, squareSize, squareSize, filterPixels);
                pinOverlay.FogFilter.SetPixels((int)pos.x, (int)pos.y, squareSize, squareSize, filterPixels);

                // Set a block of pixels on the HeightFilter so our square will always be drawn at meadow height
                pinOverlay.HeightFilter.SetPixels((int)pos.x, (int)pos.y, squareSize, squareSize, heightPixels);
            }
            
            // Apply the changes to all textures
            // This also triggers the MinimapManager to display this drawing
            pinOverlay.MainTex.Apply();
            pinOverlay.FogFilter.Apply();
            pinOverlay.ForestFilter.Apply();
            pinOverlay.HeightFilter.Apply();
        }
    }
}