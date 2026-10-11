using System.Collections.Generic;
using System.IO;
using SlotMachine.Audio;
using SlotMachine.Core;
using SlotMachine.Gameplay;
using SlotMachine.Reels;
using SlotMachine.RNG;
using SlotMachine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SlotMachine.Editor
{
    public static class SlotMachineSceneBuilder
    {
        [MenuItem("SlotMachine/Configure All Sprites")]
        public static void ConfigureSprites()
        {
            string spriteDir = "Assets/Art/Sprites";
            if (!Directory.Exists(spriteDir)) return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteDir });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool modified = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        modified = true;
                    }
                    if (importer.spritePixelsPerUnit != 100)
                    {
                        importer.spritePixelsPerUnit = 100;
                        modified = true;
                    }
                    if (importer.alphaIsTransparency != true)
                    {
                        importer.alphaIsTransparency = true;
                        modified = true;
                    }
                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        modified = true;
                    }
                    if (modified)
                    {
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                    }
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[SlotMachine]</color> All Sprites configured successfully!");
        }

        [MenuItem("SlotMachine/Build Full Game Scene")]
        public static void BuildScene()
        {
            ConfigureSprites();

            // Create a new clean scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera with AudioListener
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.14f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            // 2. Event System with InputSystemUIInputModule
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            // 3. Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 4. Background Gradient
            var bgGo = CreateUIObject("Background", canvasGo.transform);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Assets/Art/Sprites/bg_gradient.png");
            bgImg.color = Color.white;

            // Load all Symbol Sprites
            var sSeven = LoadSprite("Assets/Art/Sprites/slot-symbol1.png");
            var sCherry = LoadSprite("Assets/Art/Sprites/slot-symbol2.png");
            var sBell = LoadSprite("Assets/Art/Sprites/slot-symbol3.png");
            var sBar = LoadSprite("Assets/Art/Sprites/slot-symbol4.png");
            var sWild = LoadSprite("Assets/Art/Sprites/slot-symbol5_wild.png");
            var reelBgSprite = LoadSprite("Assets/Art/Sprites/reel_background.png");

            var symbolDefs = new List<SymbolDefinition>
            {
                new SymbolDefinition { id = SymbolId.Seven, displayName = "Lucky 7", sprite = sSeven, threeMatchMultiplier = 100, rngWeight = 8 },
                new SymbolDefinition { id = SymbolId.Bell, displayName = "Bell", sprite = sBell, threeMatchMultiplier = 50, rngWeight = 16, isBonus = true },
                new SymbolDefinition { id = SymbolId.Bar, displayName = "BAR", sprite = sBar, threeMatchMultiplier = 25, rngWeight = 26 },
                new SymbolDefinition { id = SymbolId.Cherry, displayName = "Cherry", sprite = sCherry, threeMatchMultiplier = 10, twoMatchMultiplier = 2, rngWeight = 38 },
                new SymbolDefinition { id = SymbolId.Wild, displayName = "Wild Star", sprite = sWild, threeMatchMultiplier = 150, rngWeight = 6, isWild = true }
            };

            // 5. Machine Master Container
            var machineRoot = CreateUIObject("SlotMachine_Cabinet", canvasGo.transform);
            var machineRootRect = machineRoot.GetComponent<RectTransform>();
            machineRootRect.anchoredPosition = new Vector2(0, 90f);
            machineRootRect.sizeDelta = new Vector2(816, 624);

            // 5a. Reels Mask Container
            var reelsMaskGo = CreateUIObject("ReelsWindow_Mask", machineRoot.transform);
            var reelsMaskRect = reelsMaskGo.GetComponent<RectTransform>();
            reelsMaskRect.anchoredPosition = new Vector2(-42.790535f, -38.5f);
            reelsMaskRect.sizeDelta = new Vector2(490.8329f, 210f);
            reelsMaskGo.AddComponent<RectMask2D>();

            // ReelManager setup
            var reelManagerGo = CreateUIObject("ReelManager", reelsMaskGo.transform);
            var reelManagerRect = reelManagerGo.GetComponent<RectTransform>();
            reelManagerRect.anchoredPosition = Vector2.zero;
            reelManagerRect.sizeDelta = new Vector2(490.8329f, 210f);
            var reelManager = reelManagerGo.AddComponent<ReelManager>();
            SetPrivateField(reelManager, "minSpinDuration", 2.2f);
            SetPrivateField(reelManager, "reelStopInterval", 0.5f);
            var reelsList = new List<Reel>();

            float[] reelXPositions = { -171.1f, 0f, 172.2f };
            for (int r = 0; r < 3; r++)
            {
                var reelGo = CreateUIObject($"Reel_{r}", reelManagerGo.transform);
                var reelRect = reelGo.GetComponent<RectTransform>();
                reelRect.anchoredPosition = new Vector2(reelXPositions[r], 0f);
                reelRect.sizeDelta = new Vector2(143.5f, 210f);

                // Per-reel background cut from slot-machine5.png
                var reelBgGo = CreateUIObject("Reel_BG", reelGo.transform);
                var reelBgRect = reelBgGo.GetComponent<RectTransform>();
                reelBgRect.anchoredPosition = Vector2.zero;
                reelBgRect.sizeDelta = new Vector2(108f, 210f);
                var reelBgImg = reelBgGo.AddComponent<Image>();
                reelBgImg.sprite = reelBgSprite;
                reelBgImg.raycastTarget = false;

                var reel = reelGo.AddComponent<Reel>();
                var slots = new List<ReelSymbol>();

                // Create 5 symbol slots with increased spacing (rowHeight = 105f)
                float rowHeight = 105f;
                for (int s = 0; s < 5; s++)
                {
                    var slotGo = CreateUIObject($"Slot_{s}", reelGo.transform);
                    var slotRect = slotGo.GetComponent<RectTransform>();
                    slotRect.anchoredPosition = new Vector2(0f, (2 - s) * rowHeight);
                    slotRect.sizeDelta = new Vector2(88, 88);
                    var slotImg = slotGo.AddComponent<Image>();
                    slotImg.preserveAspect = true;
                    var symbolSlot = slotGo.AddComponent<ReelSymbol>();
                    slots.Add(symbolSlot);
                }

                reel.Initialize(r, symbolDefs, new WeightedRNG(), slots);
                reelManager.AddReel(reel);
                reelsList.Add(reel);
            }

            // 5b. Machine Frame Overlay (slot-machine4.png)
            var frameOverlayGo = CreateUIObject("MachineFrameOverlay", machineRoot.transform);
            var frameOverlayRect = frameOverlayGo.GetComponent<RectTransform>();
            frameOverlayRect.sizeDelta = new Vector2(816, 624);
            var frameOverlayImg = frameOverlayGo.AddComponent<Image>();
            frameOverlayImg.sprite = LoadSprite("Assets/Art/Sprites/slot-machine4.png");
            frameOverlayImg.raycastTarget = false;

            // 5c. Interactive Lever - Accurately sized to the lever handle (92x270)
            var leverGo = CreateUIObject("Lever", machineRoot.transform);
            var leverRect = leverGo.GetComponent<RectTransform>();
            leverRect.anchoredPosition = new Vector2(310.5f, -118.5f);
            leverRect.sizeDelta = new Vector2(92f, 270f);
            var leverImg = leverGo.AddComponent<Image>();
            leverImg.sprite = LoadSprite("Assets/Art/Sprites/lever_handle_up.png");
            leverImg.preserveAspect = true;
            var leverController = leverGo.AddComponent<LeverController>();
            leverController.Configure(
                LoadSprite("Assets/Art/Sprites/lever_handle_up.png"),
                LoadSprite("Assets/Art/Sprites/lever_handle_down.png"),
                new Vector2(310.5f, -118.5f),
                new Vector2(92f, 270f),
                new Vector2(310.5f, -281.5f),
                new Vector2(92f, 60f)
            );

            // 6. UI Controls & HUD
            var hudRoot = CreateUIObject("HUD_Root", canvasGo.transform);
            var hudRect = hudRoot.GetComponent<RectTransform>();
            hudRect.anchoredPosition = new Vector2(0, -320f);
            hudRect.sizeDelta = new Vector2(1100, 260);

            // 6a. Center Message / Info Box (slot_machine_Middle_box.png)
            var infoBoxGo = CreateUIObject("InfoBox", hudRoot.transform);
            var infoBoxRect = infoBoxGo.GetComponent<RectTransform>();
            infoBoxRect.anchoredPosition = new Vector2(0, 80f);
            infoBoxRect.sizeDelta = new Vector2(700, 75);
            var infoBoxImg = infoBoxGo.AddComponent<Image>();
            infoBoxImg.sprite = LoadSprite("Assets/Art/Sprites/slot_machine_Middle_box.png");

            var messageTextGo = CreateUIObject("MessageText", infoBoxGo.transform);
            var msgTextRect = messageTextGo.GetComponent<RectTransform>();
            msgTextRect.sizeDelta = new Vector2(660, 60);
            var messageText = messageTextGo.AddComponent<Text>();
            messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            messageText.fontSize = 22;
            messageText.fontStyle = FontStyle.Bold;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = Color.yellow;
            messageText.text = "WELCOME! PULL THE LEVER OR HIT SPIN TO PLAY!";

            // 6b. Dashboard Panel (Balance, Bet, Win)
            var statsBarGo = CreateUIObject("StatsBar", hudRoot.transform);
            var statsBarRect = statsBarGo.GetComponent<RectTransform>();
            statsBarRect.anchoredPosition = new Vector2(0, 0f);
            statsBarRect.sizeDelta = new Vector2(980, 70);

            // Balance box
            var balBox = CreateStatBox("BalanceBox", statsBarGo.transform, new Vector2(-360, 0), "CREDITS", "1,000", out Text balanceText);
            // Bet box with - and + buttons
            var betBox = CreateBetControlBox("BetBox", statsBarGo.transform, new Vector2(0, 0), "TOTAL BET", "10", out Text betText, out Button btnMinus, out Button btnPlus);
            // Win box
            var winBox = CreateStatBox("WinBox", statsBarGo.transform, new Vector2(360, 0), "PAID WIN", "0", out Text winText);

            // 6c. Action Buttons Row (AutoSpin, MaxBet, Spin, Paytable)
            var buttonsBarGo = CreateUIObject("ButtonsBar", hudRoot.transform);
            var buttonsBarRect = buttonsBarGo.GetComponent<RectTransform>();
            buttonsBarRect.anchoredPosition = new Vector2(0, -75f);
            buttonsBarRect.sizeDelta = new Vector2(980, 70);

            var paytableBtn = CreateStyledButton("PaytableBtn", buttonsBarGo.transform, new Vector2(-360, 0), new Vector2(160, 56), "PAYTABLE", new Color(0.2f, 0.4f, 0.8f));
            var autoSpinBtn = CreateStyledButton("AutoSpinBtn", buttonsBarGo.transform, new Vector2(-180, 0), new Vector2(160, 56), "AUTO SPIN", new Color(0.3f, 0.25f, 0.5f));
            var maxBetBtn = CreateStyledButton("MaxBetBtn", buttonsBarGo.transform, new Vector2(180, 0), new Vector2(160, 56), "MAX BET", new Color(0.8f, 0.45f, 0.1f));
            var spinBtn = CreateStyledButton("SpinBtn", buttonsBarGo.transform, new Vector2(360, 0), new Vector2(180, 60), "SPIN!", new Color(0.1f, 0.75f, 0.25f));

            // 7. Free Spins Banner (Top of screen)
            var fsBannerGo = CreateUIObject("FreeSpinsBanner", canvasGo.transform);
            var fsBannerRect = fsBannerGo.GetComponent<RectTransform>();
            fsBannerRect.anchoredPosition = new Vector2(0, 480f);
            fsBannerRect.sizeDelta = new Vector2(750, 60);
            var fsBannerImg = fsBannerGo.AddComponent<Image>();
            fsBannerImg.sprite = LoadSprite("Assets/Art/Sprites/slot_machine_Middle_box.png");
            var fsTextGo = CreateUIObject("FreeSpinsText", fsBannerGo.transform);
            var fsTextRect = fsTextGo.GetComponent<RectTransform>();
            fsTextRect.sizeDelta = new Vector2(720, 50);
            var freeSpinsText = fsTextGo.AddComponent<Text>();
            freeSpinsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            freeSpinsText.fontSize = 24;
            freeSpinsText.fontStyle = FontStyle.Bold;
            freeSpinsText.alignment = TextAnchor.MiddleCenter;
            freeSpinsText.color = Color.cyan;
            freeSpinsText.text = "FREE SPINS: 5 REMAINING (2X MULTIPLIER)";
            fsBannerGo.SetActive(false);

            // 8. Mega Jackpot Banner
            var jpBannerGo = CreateUIObject("JackpotBanner", canvasGo.transform);
            var jpBannerRect = jpBannerGo.GetComponent<RectTransform>();
            jpBannerRect.anchoredPosition = new Vector2(0, 400f);
            jpBannerRect.sizeDelta = new Vector2(850, 70);
            var jpBannerImg = jpBannerGo.AddComponent<Image>();
            jpBannerImg.sprite = LoadSprite("Assets/Art/Sprites/slot_machine_Middle_box.png");
            var jpTextGo = CreateUIObject("JackpotText", jpBannerGo.transform);
            var jpTextRect = jpTextGo.GetComponent<RectTransform>();
            jpTextRect.sizeDelta = new Vector2(820, 60);
            var jackpotText = jpTextGo.AddComponent<Text>();
            jackpotText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            jackpotText.fontSize = 28;
            jackpotText.fontStyle = FontStyle.BoldAndItalic;
            jackpotText.alignment = TextAnchor.MiddleCenter;
            jackpotText.color = Color.yellow;
            jackpotText.text = "★ ★ ★ MEGA JACKPOT WINNER! ★ ★ ★";
            jpBannerGo.SetActive(false);

            // 9. Paytable Modal Dialog
            var paytableModal = CreatePaytableModal(canvasGo.transform, symbolDefs);

            // 10. Refill Modal Dialog
            var refillModal = CreateRefillModal(canvasGo.transform, out Button refillYes, out Button refillNo);

            // 11. Audio Setup
            var audioGo = new GameObject("SoundManager");
            var soundManager = audioGo.AddComponent<SoundManager>();
            SetPrivateField(soundManager, "buttonClickClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/button_click.mp3"));
            SetPrivateField(soundManager, "leverPullClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/lever_pull.mp3"));
            SetPrivateField(soundManager, "reelSpinLoopClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/reel_spin_loop.mp3"));
            SetPrivateField(soundManager, "reelStopClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/reel_stop.mp3"));
            SetPrivateField(soundManager, "coinCountClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/coin_tick.mp3"));
            SetPrivateField(soundManager, "winSmallClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/win_standard.mp3"));
            SetPrivateField(soundManager, "winJackpotClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/win_jackpot.mp3"));
            SetPrivateField(soundManager, "bonusTriggerClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/bonus_trigger.mp3"));

            // 12. Gameplay Logic Setup
            var gameManagerGo = new GameObject("GameManager");
            var playerWallet = gameManagerGo.AddComponent<PlayerWallet>();
            var uiManager = gameManagerGo.AddComponent<UIManager>();
            var slotController = gameManagerGo.AddComponent<SlotMachineController>();

            // Set UI Manager private fields
            SetPrivateField(uiManager, "balanceText", balanceText);
            SetPrivateField(uiManager, "betText", betText);
            SetPrivateField(uiManager, "winText", winText);
            SetPrivateField(uiManager, "messageText", messageText);
            SetPrivateField(uiManager, "freeSpinsText", freeSpinsText);
            SetPrivateField(uiManager, "spinButton", spinBtn);
            SetPrivateField(uiManager, "betPlusButton", btnPlus);
            SetPrivateField(uiManager, "betMinusButton", btnMinus);
            SetPrivateField(uiManager, "maxBetButton", maxBetBtn);
            SetPrivateField(uiManager, "autoSpinButton", autoSpinBtn);
            SetPrivateField(uiManager, "paytableButton", paytableBtn);
            SetPrivateField(uiManager, "freeSpinsBanner", fsBannerGo);
            SetPrivateField(uiManager, "jackpotBanner", jpBannerGo);
            SetPrivateField(uiManager, "paytableModal", paytableModal);
            SetPrivateField(uiManager, "refillModal", refillModal);
            SetPrivateField(uiManager, "refillYesButton", refillYes);
            SetPrivateField(uiManager, "refillNoButton", refillNo);
            SetPrivateField(uiManager, "leverController", leverController);

            // Set SlotMachineController fields
            SetPrivateField(slotController, "reelManager", reelManager);
            SetPrivateField(slotController, "playerWallet", playerWallet);
            SetPrivateField(slotController, "uiManager", uiManager);
            SetPrivateField(slotController, "soundManager", soundManager);
            slotController.SetSymbolDefinitions(symbolDefs);

            // Save Scene
            string scenePath = "Assets/Scenes/SlotMachineScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"<color=green>[SlotMachine]</color> Saved updated game scene at {scenePath}");

            // Configure EditorBuildSettings
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static GameObject CreateStatBox(string name, Transform parent, Vector2 pos, string label, string defaultValue, out Text valText)
        {
            var box = CreateUIObject(name, parent);
            var rect = box.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200, 64);

            var img = box.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.18f, 0.95f);

            var lblGo = CreateUIObject("Label", box.transform);
            var lblRect = lblGo.GetComponent<RectTransform>();
            lblRect.anchoredPosition = new Vector2(0, 16);
            lblRect.sizeDelta = new Vector2(190, 24);
            var lblText = lblGo.AddComponent<Text>();
            lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lblText.fontSize = 14;
            lblText.alignment = TextAnchor.MiddleCenter;
            lblText.color = new Color(0.7f, 0.8f, 1f);
            lblText.text = label;

            var valGo = CreateUIObject("Value", box.transform);
            var valRect = valGo.GetComponent<RectTransform>();
            valRect.anchoredPosition = new Vector2(0, -12);
            valRect.sizeDelta = new Vector2(190, 32);
            valText = valGo.AddComponent<Text>();
            valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valText.fontSize = 24;
            valText.fontStyle = FontStyle.Bold;
            valText.alignment = TextAnchor.MiddleCenter;
            valText.color = Color.white;
            valText.text = defaultValue;

            return box;
        }

        private static GameObject CreateBetControlBox(string name, Transform parent, Vector2 pos, string label, string defaultValue, out Text valText, out Button btnMinus, out Button btnPlus)
        {
            var box = CreateUIObject(name, parent);
            var rect = box.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(260, 64);

            var img = box.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.18f, 0.95f);

            var lblGo = CreateUIObject("Label", box.transform);
            var lblRect = lblGo.GetComponent<RectTransform>();
            lblRect.anchoredPosition = new Vector2(0, 16);
            lblRect.sizeDelta = new Vector2(250, 24);
            var lblText = lblGo.AddComponent<Text>();
            lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lblText.fontSize = 14;
            lblText.alignment = TextAnchor.MiddleCenter;
            lblText.color = new Color(0.7f, 0.8f, 1f);
            lblText.text = label;

            var valGo = CreateUIObject("Value", box.transform);
            var valRect = valGo.GetComponent<RectTransform>();
            valRect.anchoredPosition = new Vector2(0, -12);
            valRect.sizeDelta = new Vector2(120, 32);
            valText = valGo.AddComponent<Text>();
            valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valText.fontSize = 24;
            valText.fontStyle = FontStyle.Bold;
            valText.alignment = TextAnchor.MiddleCenter;
            valText.color = Color.white;
            valText.text = defaultValue;

            // Minus Button
            var minusGo = CreateUIObject("BtnMinus", box.transform);
            var minusRect = minusGo.GetComponent<RectTransform>();
            minusRect.anchoredPosition = new Vector2(-95, -12);
            minusRect.sizeDelta = new Vector2(36, 36);
            var minusImg = minusGo.AddComponent<Image>();
            minusImg.sprite = LoadSprite("Assets/Art/Sprites/btn_arrow_left_normal.png");
            btnMinus = minusGo.AddComponent<Button>();

            // Plus Button
            var plusGo = CreateUIObject("BtnPlus", box.transform);
            var plusRect = plusGo.GetComponent<RectTransform>();
            plusRect.anchoredPosition = new Vector2(95, -12);
            plusRect.sizeDelta = new Vector2(36, 36);
            var plusImg = plusGo.AddComponent<Image>();
            plusImg.sprite = LoadSprite("Assets/Art/Sprites/btn_arrow_right_normal.png");
            btnPlus = plusGo.AddComponent<Button>();

            return box;
        }

        private static Button CreateStyledButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color color)
        {
            var btnGo = CreateUIObject(name, parent);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = btnGo.AddComponent<Image>();
            img.color = color;
            var btn = btnGo.AddComponent<Button>();

            var textGo = CreateUIObject("Text", btnGo.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.sizeDelta = size;
            var txt = textGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = text;

            return btn;
        }

        private static PaytableModal CreatePaytableModal(Transform canvas, List<SymbolDefinition> symbols)
        {
            var modalGo = CreateUIObject("PaytableModal", canvas);
            var modalRect = modalGo.GetComponent<RectTransform>();
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(860, 560);

            var bgImg = modalGo.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Assets/Art/Sprites/popup.png");

            var titleGo = CreateUIObject("Title", modalGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 220);
            titleRect.sizeDelta = new Vector2(700, 50);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.yellow;
            titleText.text = "★ PAYTABLE & GAME RULES ★";

            float startY = 140f;
            for (int i = 0; i < symbols.Count; i++)
            {
                var rowGo = CreateUIObject($"Row_{symbols[i].displayName}", modalGo.transform);
                var rowRect = rowGo.GetComponent<RectTransform>();
                rowRect.anchoredPosition = new Vector2(0, startY - (i * 68f));
                rowRect.sizeDelta = new Vector2(720, 60);

                var iconGo = CreateUIObject("Icon", rowGo.transform);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchoredPosition = new Vector2(-280, 0);
                iconRect.sizeDelta = new Vector2(50, 50);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = symbols[i].sprite;
                iconImg.preserveAspect = true;

                var descGo = CreateUIObject("Desc", rowGo.transform);
                var descRect = descGo.GetComponent<RectTransform>();
                descRect.anchoredPosition = new Vector2(40, 0);
                descRect.sizeDelta = new Vector2(540, 50);
                var descText = descGo.AddComponent<Text>();
                descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                descText.fontSize = 18;
                descText.fontStyle = FontStyle.Bold;
                descText.alignment = TextAnchor.MiddleLeft;
                descText.color = Color.white;

                string bonusInfo = symbols[i].isBonus ? " (3 Trigger 5 FREE SPINS @ 2X!)" : "";
                string wildInfo = symbols[i].isWild ? " (Wild substitutes ALL symbols!)" : "";
                string cherryInfo = symbols[i].twoMatchMultiplier > 0 ? " (2 Cherries = 2x)" : "";
                descText.text = $"{symbols[i].displayName}: 3 of a kind = {symbols[i].threeMatchMultiplier}x Bet{cherryInfo}{bonusInfo}{wildInfo}";
            }

            var closeBtnGo = CreateUIObject("CloseButton", modalGo.transform);
            var closeRect = closeBtnGo.GetComponent<RectTransform>();
            closeRect.anchoredPosition = new Vector2(380, 230);
            closeRect.sizeDelta = new Vector2(48, 48);
            var closeImg = closeBtnGo.AddComponent<Image>();
            closeImg.sprite = LoadSprite("Assets/Art/Sprites/btn_close_normal.png");
            var closeBtn = closeBtnGo.AddComponent<Button>();

            var paytableComp = modalGo.AddComponent<PaytableModal>();
            SetPrivateField(paytableComp, "modalRoot", modalGo);
            SetPrivateField(paytableComp, "closeButton", closeBtn);

            modalGo.SetActive(false);
            return paytableComp;
        }

        private static GameObject CreateRefillModal(Transform canvas, out Button yesBtn, out Button noBtn)
        {
            var modalGo = CreateUIObject("RefillModal", canvas);
            var modalRect = modalGo.GetComponent<RectTransform>();
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(640, 360);

            var bgImg = modalGo.AddComponent<Image>();
            bgImg.sprite = LoadSprite("Assets/Art/Sprites/popup.png");

            var titleGo = CreateUIObject("Title", modalGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 90);
            titleRect.sizeDelta = new Vector2(580, 80);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "OUT OF CREDITS!\n\nClaim 1,000 Free Bonus Credits?";

            var yesGo = CreateUIObject("YesBtn", modalGo.transform);
            var yesRect = yesGo.GetComponent<RectTransform>();
            yesRect.anchoredPosition = new Vector2(-120, -70);
            yesRect.sizeDelta = new Vector2(170, 60);
            var yesImg = yesGo.AddComponent<Image>();
            yesImg.sprite = LoadSprite("Assets/Art/Sprites/btn_yes.png");
            yesBtn = yesGo.AddComponent<Button>();

            var noGo = CreateUIObject("NoBtn", modalGo.transform);
            var noRect = noGo.GetComponent<RectTransform>();
            noRect.anchoredPosition = new Vector2(120, -70);
            noRect.sizeDelta = new Vector2(170, 60);
            var noImg = noGo.AddComponent<Image>();
            noImg.sprite = LoadSprite("Assets/Art/Sprites/btn_no.png");
            noBtn = noGo.AddComponent<Button>();

            modalGo.SetActive(false);
            return modalGo;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
