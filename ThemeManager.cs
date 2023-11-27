using BepInEx.Configuration;
using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TootTallyCore.Utils.Assets;
using TootTallySettings;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TootTallyCore.Utils.TootTallyNotifs;
using TootTallyCore.Graphics;

namespace TootTallyThemes
{
    public static class ThemeManager
    {
        private const string CONFIG_FIELD = "Themes";
        private const string DEFAULT_THEME = "Default";
        public static Text songyear, songgenre, songcomposer, songtempo, songduration, songdesctext;
        private static string _currentTheme;
        private static bool _isInitialized;

        public static void SetTheme(string themeName)
        {
            _currentTheme = themeName;
            Theme.isDefault = false;
            switch (themeName)
            {
                case "Day":
                    Theme.SetDayTheme();
                    break;
                case "Night":
                    Theme.SetNightTheme();
                    break;
                case "Random":
                    Theme.SetRandomTheme();
                    break;
                case "Default":
                    Theme.SetDefaultTheme();
                    break;
                default:
                    Theme.SetCustomTheme(themeName);
                    break;
            }
            GameObjectFactory.UpdatePrefabTheme();
        }


        public static void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (_currentTheme == Plugin.ThemeName.Value) return; //skip if theme did not change

            SetTheme(Plugin.ThemeName.Value);
            TootTallyNotifManager.DisplayNotif("New Theme Loaded!", Theme.themeColors.notification.defaultText);
        }

        public static void RefreshTheme()
        {
            SetTheme(_currentTheme);
            TootTallyNotifManager.DisplayNotif("Theme refreshed!", Theme.themeColors.notification.defaultText);
        }

        [HarmonyPatch(typeof(GameObjectFactory), nameof(GameObjectFactory.OnHomeControllerInitialize))]
        [HarmonyPrefix]
        public static void Initialize()
        {
            if (_isInitialized) return;

            SetTheme(Plugin.ThemeName.Value);
            _isInitialized = true;
        }

        [HarmonyPatch(typeof(GameObjectFactory), nameof(GameObjectFactory.OnHomeControllerInitialize))]
        [HarmonyPostfix]

        public static void OnHomeControllerUpdateFactoryTheme() => GameObjectFactory.UpdatePrefabTheme();

        [HarmonyPatch(typeof(GameObjectFactory), nameof(GameObjectFactory.OnLevelSelectControllerInitialize))]
        [HarmonyPostfix]

        public static void OnLevelSelectUpdateFactoryTheme() => GameObjectFactory.UpdatePrefabTheme();

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.Start))]
        [HarmonyPostfix]
        public static void ChangeThemeOnLevelSelectControllerStartPostFix(LevelSelectController __instance)
        {
            if (Theme.isDefault) return;

            foreach (GameObject btn in __instance.btns)
            {
                btn.transform.Find("ScoreText").gameObject.GetComponent<Text>().color = Theme.themeColors.leaderboard.text;
            }

            #region SongButton
            try
            {
                GameObject btnBGPrefab = UnityEngine.Object.Instantiate(__instance.btnbgs[0].gameObject);
                UnityEngine.Object.DestroyImmediate(btnBGPrefab.transform.Find("Image").gameObject);

                for (int i = 0; i < 7; i++) //songbuttons only, not the arrow ones
                {
                    Image img = __instance.btnbgs[i];
                    img.sprite = AssetManager.GetSprite("SongButtonBackground.png");
                    img.transform.parent.Find("Text").GetComponent<Text>().color = i == 0 ? Theme.themeColors.songButton.textOver : Theme.themeColors.songButton.text;

                    GameObject btnBGShadow = UnityEngine.Object.Instantiate(btnBGPrefab, img.gameObject.transform.parent);
                    btnBGShadow.name = "Shadow";
                    OverwriteGameObjectSpriteAndColor(btnBGShadow, "SongButtonShadow.png", Theme.themeColors.songButton.shadow);

                    GameObject btnBGOutline = UnityEngine.Object.Instantiate(btnBGPrefab, img.gameObject.transform);
                    btnBGOutline.name = "Outline";
                    OverwriteGameObjectSpriteAndColor(btnBGOutline, "SongButtonOutline.png", i == 0 ? Theme.themeColors.songButton.outlineOver : Theme.themeColors.songButton.outline);

                    img.transform.Find("Image").GetComponent<Image>().color = Theme.themeColors.songButton.square;
                    img.color = Theme.themeColors.songButton.background;
                }

                for (int i = 7; i < __instance.btnbgs.Length; i++) //these are the arrow ones :}
                    __instance.btnbgs[i].color = Theme.themeColors.songButton.background;
                UnityEngine.Object.DestroyImmediate(btnBGPrefab);
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            #region SongTitle
            try
            {
                __instance.songtitlebar.GetComponent<Image>().color = Theme.themeColors.title.titleBar;
                __instance.scenetitle.GetComponent<Text>().color = Theme.themeColors.title.titleShadow;
                GameObject.Find("MainCanvas/FullScreenPanel/title/GameObject").GetComponent<Text>().color = Theme.themeColors.title.title;
                __instance.longsongtitle.color = Theme.themeColors.title.songName;
                __instance.longsongtitle.GetComponent<Outline>().effectColor = Theme.themeColors.title.outline;
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            #region Lines
            try
            {
                GameObject lines = __instance.btnspanel.transform.Find("RightLines").gameObject;
                lines.GetComponent<RectTransform>().anchoredPosition += new Vector2(-2, 0);
                LineRenderer redLine = lines.transform.Find("Red").GetComponent<LineRenderer>();
                redLine.startColor = Theme.themeColors.leaderboard.panelBody;
                redLine.endColor = Theme.themeColors.leaderboard.scoresBody;
                for (int i = 1; i < 8; i++)
                {
                    LineRenderer yellowLine = lines.transform.Find("Yellow" + i).GetComponent<LineRenderer>();
                    yellowLine.startColor = Theme.themeColors.leaderboard.panelBody;
                    yellowLine.endColor = Theme.themeColors.leaderboard.scoresBody;
                }
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            #region Capsules
            try
            {
                GameObject capsules = GameObject.Find("MainCanvas/FullScreenPanel/capsules").gameObject;
                GameObject capsulesPrefab = UnityEngine.Object.Instantiate(capsules);

                foreach (Transform t in capsulesPrefab.transform) UnityEngine.Object.Destroy(t.gameObject);
                RectTransform rectTrans = capsulesPrefab.GetComponent<RectTransform>();
                rectTrans.localScale = Vector3.one;
                rectTrans.anchoredPosition = Vector2.zero;


                GameObject capsulesYearShadow = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesYearShadow, "YearCapsule.png", Theme.themeColors.capsules.yearShadow);
                capsulesYearShadow.GetComponent<RectTransform>().anchoredPosition += new Vector2(5, -3);

                GameObject capsulesYear = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesYear, "YearCapsule.png", Theme.themeColors.capsules.year);

                songyear = UnityEngine.Object.Instantiate(__instance.songyear, capsulesYear.transform);

                GameObject capsulesGenreShadow = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesGenreShadow, "GenreCapsule.png", Theme.themeColors.capsules.genreShadow);
                capsulesGenreShadow.GetComponent<RectTransform>().anchoredPosition += new Vector2(5, -3);

                GameObject capsulesGenre = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesGenre, "GenreCapsule.png", Theme.themeColors.capsules.genre);
                songgenre = UnityEngine.Object.Instantiate(__instance.songgenre, capsulesGenre.transform);

                GameObject capsulesComposerShadow = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesComposerShadow, "ComposerCapsule.png", Theme.themeColors.capsules.composerShadow);
                capsulesComposerShadow.GetComponent<RectTransform>().anchoredPosition += new Vector2(5, -3);

                GameObject capsulesComposer = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesComposer, "ComposerCapsule.png", Theme.themeColors.capsules.composer);
                songcomposer = UnityEngine.Object.Instantiate(__instance.songcomposer, capsulesComposer.transform);

                GameObject capsulesTempo = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesTempo, "BPMTimeCapsule.png", Theme.themeColors.capsules.tempo);
                songtempo = UnityEngine.Object.Instantiate(__instance.songtempo, capsulesTempo.transform);
                songduration = UnityEngine.Object.Instantiate(__instance.songduration, capsulesTempo.transform);

                GameObject capsulesDescTextShadow = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesDescTextShadow, "DescCapsule.png", Theme.themeColors.capsules.descriptionShadow);
                capsulesDescTextShadow.GetComponent<RectTransform>().anchoredPosition += new Vector2(5, -3);

                GameObject capsulesDescText = UnityEngine.Object.Instantiate(capsulesPrefab, capsules.transform);
                OverwriteGameObjectSpriteAndColor(capsulesDescText, "DescCapsule.png", Theme.themeColors.capsules.description);
                songdesctext = UnityEngine.Object.Instantiate(__instance.songdesctext, capsulesDescText.transform);

                UnityEngine.Object.DestroyImmediate(capsules.GetComponent<Image>());
                UnityEngine.Object.DestroyImmediate(capsulesPrefab);
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            #region PlayButton
            try
            {
                GameObject playButtonBG = __instance.playbtn.transform.Find("BG").gameObject;
                GameObject playBGPrefab = UnityEngine.Object.Instantiate(playButtonBG, __instance.playbtn.transform);
                foreach (Transform t in playBGPrefab.transform) UnityEngine.Object.Destroy(t.gameObject);

                GameObject playBackgroundImg = UnityEngine.Object.Instantiate(playBGPrefab, __instance.playbtn.transform);
                playBackgroundImg.name = "playBackground";
                OverwriteGameObjectSpriteAndColor(playBackgroundImg, "PlayBackground.png", Theme.themeColors.playButton.background);

                GameObject playOutline = UnityEngine.Object.Instantiate(playBGPrefab, __instance.playbtn.transform);
                playOutline.name = "playOutline";
                OverwriteGameObjectSpriteAndColor(playOutline, "PlayOutline.png", Theme.themeColors.playButton.outline);

                GameObject playText = UnityEngine.Object.Instantiate(playBGPrefab, __instance.playbtn.transform);
                playText.name = "playText";
                OverwriteGameObjectSpriteAndColor(playText, "PlayText.png", Theme.themeColors.playButton.text);

                GameObject playShadow = UnityEngine.Object.Instantiate(playBGPrefab, __instance.playbtn.transform);
                playShadow.name = "playShadow";
                OverwriteGameObjectSpriteAndColor(playShadow, "PlayShadow.png", Theme.themeColors.playButton.shadow);

                UnityEngine.Object.DestroyImmediate(playButtonBG);
                UnityEngine.Object.DestroyImmediate(playBGPrefab);
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            #region BackButton
            try
            {
                GameObject backButtonBG = __instance.backbutton.transform.Find("BG").gameObject;
                GameObject backBGPrefab = UnityEngine.Object.Instantiate(backButtonBG, __instance.backbutton.transform);
                foreach (Transform t in backBGPrefab.transform) UnityEngine.Object.Destroy(t.gameObject);

                GameObject backBackgroundImg = UnityEngine.Object.Instantiate(backBGPrefab, __instance.backbutton.transform);
                backBackgroundImg.name = "backBackground";
                OverwriteGameObjectSpriteAndColor(backBackgroundImg, "BackBackground.png", Theme.themeColors.backButton.background);

                GameObject backOutline = UnityEngine.Object.Instantiate(backBGPrefab, __instance.backbutton.transform);
                backOutline.name = "backOutline";
                OverwriteGameObjectSpriteAndColor(backOutline, "BackOutline.png", Theme.themeColors.backButton.outline);

                GameObject backText = UnityEngine.Object.Instantiate(backBGPrefab, __instance.backbutton.transform);
                backText.name = "backText";
                OverwriteGameObjectSpriteAndColor(backText, "BackText.png", Theme.themeColors.backButton.text);

                GameObject backShadow = UnityEngine.Object.Instantiate(backBGPrefab, __instance.backbutton.transform);
                backShadow.name = "backShadow";
                OverwriteGameObjectSpriteAndColor(backShadow, "BackShadow.png", Theme.themeColors.backButton.shadow);

                UnityEngine.Object.DestroyImmediate(backButtonBG);
                UnityEngine.Object.DestroyImmediate(backBGPrefab);
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            #region RandomButton
            try
            {
                __instance.btnrandom.transform.Find("Text").GetComponent<Text>().color = Theme.themeColors.randomButton.text;
                __instance.btnrandom.transform.Find("icon").GetComponent<Image>().color = Theme.themeColors.randomButton.text;
                __instance.btnrandom.transform.Find("btn-shadow").GetComponent<Image>().color = Theme.themeColors.randomButton.shadow;

                GameObject randomButtonPrefab = UnityEngine.Object.Instantiate(__instance.btnrandom.transform.Find("btn").gameObject);
                RectTransform randomRectTransform = randomButtonPrefab.GetComponent<RectTransform>();
                randomRectTransform.anchoredPosition = Vector2.zero;
                randomRectTransform.localScale = Vector3.one;
                UnityEngine.Object.DestroyImmediate(__instance.btnrandom.transform.Find("btn").gameObject);

                GameObject randomButtonBackground = UnityEngine.Object.Instantiate(randomButtonPrefab, __instance.btnrandom.transform);
                randomButtonBackground.name = "RandomBackground";
                OverwriteGameObjectSpriteAndColor(randomButtonBackground, "RandomBackground.png", Theme.themeColors.randomButton.background);
                __instance.btnrandom.transform.Find("Text").SetParent(randomButtonBackground.transform);
                __instance.btnrandom.transform.Find("icon").SetParent(randomButtonBackground.transform);

                GameObject randomButtonOutline = UnityEngine.Object.Instantiate(randomButtonPrefab, __instance.btnrandom.transform);
                randomButtonOutline.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1);
                randomButtonOutline.name = "RandomOutline";
                OverwriteGameObjectSpriteAndColor(randomButtonOutline, "RandomOutline.png", Theme.themeColors.randomButton.outline);

                /*GameObject randomButtonIcon = GameObject.Instantiate(randomButtonPrefab, __instance.btnrandom.transform);
                randomButtonIcon.name = "RandomIcon";
                OverwriteGameObjectSpriteAndColor(randomButtonIcon, "RandomIcon.png", GameTheme.themeColors.randomButton.text);*/

                UnityEngine.Object.DestroyImmediate(__instance.btnrandom.GetComponent<Image>());
                UnityEngine.Object.DestroyImmediate(randomButtonPrefab);

                EventTrigger randomBtnEvents = __instance.btnrandom.AddComponent<EventTrigger>();
                EventTrigger.Entry pointerEnterEvent = new EventTrigger.Entry();
                pointerEnterEvent.eventID = EventTriggerType.PointerEnter;
                pointerEnterEvent.callback.AddListener((data) => OnPointerEnterRandomEvent(__instance));
                randomBtnEvents.triggers.Add(pointerEnterEvent);

                EventTrigger.Entry pointerExitEvent = new EventTrigger.Entry();
                pointerExitEvent.eventID = EventTriggerType.PointerExit;
                pointerExitEvent.callback.AddListener((data) => OnPointerLeaveRandomEvent(__instance));
                randomBtnEvents.triggers.Add(pointerExitEvent);
            }
            catch (Exception e)
            {
                Plugin.LogError("THEME CRASH: " + e.Message);
            }
            #endregion

            #region PointerArrow
            try
            {
                GameObject arrowPointerPrefab = UnityEngine.Object.Instantiate(__instance.pointerarrow.gameObject);
                OverwriteGameObjectSpriteAndColor(__instance.pointerarrow.gameObject, "pointerBG.png", Theme.themeColors.pointer.background);

                GameObject arrowPointerShadow = UnityEngine.Object.Instantiate(arrowPointerPrefab, __instance.pointerarrow.transform);
                arrowPointerShadow.name = "Shadow";
                arrowPointerShadow.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                OverwriteGameObjectSpriteAndColor(arrowPointerShadow, "pointerShadow.png", Theme.themeColors.pointer.shadow);

                GameObject arrowPointerPointerOutline = UnityEngine.Object.Instantiate(arrowPointerPrefab, __instance.pointerarrow.transform);
                arrowPointerPointerOutline.name = "Outline";
                arrowPointerPointerOutline.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                OverwriteGameObjectSpriteAndColor(arrowPointerPointerOutline, "pointerOutline.png", Theme.themeColors.pointer.outline);

                UnityEngine.Object.DestroyImmediate(arrowPointerPrefab);
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            #region Background
            try
            {
                __instance.bgdots.GetComponent<RectTransform>().eulerAngles = new Vector3(0, 0, 165.5f);
                __instance.bgdots.transform.Find("Image").GetComponent<Image>().color = Theme.themeColors.background.dots;
                __instance.bgdots.transform.Find("Image (1)").GetComponent<Image>().color = Theme.themeColors.background.dots;
                __instance.bgdots2.transform.Find("Image").GetComponent<Image>().color = Theme.themeColors.background.dots2;
                GameObject extraDotsBecauseGameDidntLeanTweenFarEnoughSoWeCanSeeTheEndOfTheTextureFix = UnityEngine.Object.Instantiate(__instance.bgdots.transform.Find("Image").gameObject, __instance.bgdots.transform.Find("Image").transform);
                extraDotsBecauseGameDidntLeanTweenFarEnoughSoWeCanSeeTheEndOfTheTextureFix.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1010);
                GameObject.Find("bgcamera").GetComponent<Camera>().backgroundColor = Theme.themeColors.background.background;
                GameObject.Find("BG Shape").GetComponent<Image>().color = Theme.themeColors.background.shape;
                GameObject MainCanvas = GameObject.Find("MainCanvas").gameObject;
                MainCanvas.transform.Find("FullScreenPanel/diamond").GetComponent<Image>().color = Theme.themeColors.background.diamond;
            }
            catch (Exception e)
            {
                Plugin.LogError(e.Message);
            }
            #endregion

            //CapsulesTextColor
            songyear.color = Theme.themeColors.leaderboard.text;
            songgenre.color = Theme.themeColors.leaderboard.text;
            songduration.color = Theme.themeColors.leaderboard.text;
            songcomposer.color = Theme.themeColors.leaderboard.text;
            songtempo.color = Theme.themeColors.leaderboard.text;
            songdesctext.color = Theme.themeColors.leaderboard.text;
            OnAdvanceSongsPostFix(__instance);

        }

        #region hoverAndUnHoverSongButtons
        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.hoverBtn))]
        [HarmonyPostfix]
        public static void OnHoverBtnPostfix(LevelSelectController __instance, object[] __args)
        {
            if (Theme.isDefault) return;
            if ((int)__args[0] >= 7)
            {
                __instance.btnbgs[(int)__args[0]].GetComponent<Image>().color = Theme.themeColors.songButton.outline;
                return;
            }
            __instance.btnbgs[(int)__args[0]].GetComponent<Image>().color = Theme.themeColors.songButton.background;
            __instance.btnbgs[(int)__args[0]].transform.Find("Outline").GetComponent<Image>().color = Theme.themeColors.songButton.outlineOver;
            __instance.btnbgs[(int)__args[0]].transform.parent.Find("Text").GetComponent<Text>().color = Theme.themeColors.songButton.textOver;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.unHoverBtn))]
        [HarmonyPostfix]
        public static void OnUnHoverBtnPostfix(LevelSelectController __instance, object[] __args)
        {
            if (Theme.isDefault) return;
            if ((int)__args[0] >= 7)
            {
                __instance.btnbgs[(int)__args[0]].GetComponent<Image>().color = Theme.themeColors.songButton.background;
                return;
            }
            __instance.btnbgs[(int)__args[0]].GetComponent<Image>().color = Theme.themeColors.songButton.background;
            __instance.btnbgs[(int)__args[0]].transform.Find("Outline").GetComponent<Image>().color = Theme.themeColors.songButton.outline;
            __instance.btnbgs[(int)__args[0]].transform.parent.Find("Text").GetComponent<Text>().color = Theme.themeColors.songButton.text;
        }
        #endregion

        #region PlayAndBackEvents
        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.hoverPlay))]
        [HarmonyPrefix]
        public static bool OnHoverPlayBypassIfThemeNotDefault(LevelSelectController __instance)
        {
            if (Theme.isDefault) return true;
            __instance.hoversfx.Play();
            __instance.playhovering = true;
            __instance.playbtnobj.transform.Find("playBackground").GetComponent<Image>().color = Theme.themeColors.playButton.backgroundOver;
            __instance.playbtnobj.transform.Find("playOutline").GetComponent<Image>().color = Theme.themeColors.playButton.outlineOver;
            __instance.playbtnobj.transform.Find("playText").GetComponent<Image>().color = Theme.themeColors.playButton.textOver;
            __instance.playbtnobj.transform.Find("playShadow").GetComponent<Image>().color = Theme.themeColors.playButton.shadowOver;
            return false;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.unHoverPlay))]
        [HarmonyPrefix]
        public static bool OnUnHoverPlayBypassIfThemeNotDefault(LevelSelectController __instance)
        {
            if (Theme.isDefault) return true;
            __instance.playhovering = false;
            __instance.playbtnobj.transform.Find("playBackground").GetComponent<Image>().color = Theme.themeColors.playButton.background;
            __instance.playbtnobj.transform.Find("playOutline").GetComponent<Image>().color = Theme.themeColors.playButton.outline;
            __instance.playbtnobj.transform.Find("playText").GetComponent<Image>().color = Theme.themeColors.playButton.text;
            __instance.playbtnobj.transform.Find("playShadow").GetComponent<Image>().color = Theme.themeColors.playButton.shadow;
            return false;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.hoverBack))]
        [HarmonyPrefix]
        public static bool OnHoverBackBypassIfThemeNotDefault(LevelSelectController __instance)
        {
            if (Theme.isDefault) return true;
            __instance.hoversfx.Play();
            __instance.backbutton.gameObject.transform.Find("backBackground").GetComponent<Image>().color = Theme.themeColors.backButton.backgroundOver;
            __instance.backbutton.gameObject.transform.Find("backOutline").GetComponent<Image>().color = Theme.themeColors.backButton.outlineOver;
            __instance.backbutton.gameObject.transform.Find("backText").GetComponent<Image>().color = Theme.themeColors.backButton.textOver;
            __instance.backbutton.gameObject.transform.Find("backShadow").GetComponent<Image>().color = Theme.themeColors.backButton.shadowOver;
            return false;
        }

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.hoverOutBack))]
        [HarmonyPrefix]
        public static bool OnHoverOutBackBypassIfThemeNotDefault(LevelSelectController __instance)
        {
            if (Theme.isDefault) return true;
            __instance.backbutton.gameObject.transform.Find("backBackground").GetComponent<Image>().color = Theme.themeColors.backButton.background;
            __instance.backbutton.gameObject.transform.Find("backOutline").GetComponent<Image>().color = Theme.themeColors.backButton.outline;
            __instance.backbutton.gameObject.transform.Find("backText").GetComponent<Image>().color = Theme.themeColors.backButton.text;
            __instance.backbutton.gameObject.transform.Find("backShadow").GetComponent<Image>().color = Theme.themeColors.backButton.shadow;
            return false;
        }

        public static void OnPointerEnterRandomEvent(LevelSelectController __instance)
        {
            __instance.hoversfx.Play();
            __instance.btnrandom.transform.Find("RandomBackground").GetComponent<Image>().color = Theme.themeColors.randomButton.backgroundOver;
            __instance.btnrandom.transform.Find("RandomOutline").GetComponent<Image>().color = Theme.themeColors.randomButton.outlineOver;
            __instance.btnrandom.transform.Find("RandomBackground/icon").GetComponent<Image>().color = Theme.themeColors.randomButton.textOver;
            __instance.btnrandom.transform.Find("RandomBackground/Text").GetComponent<Text>().color = Theme.themeColors.randomButton.textOver;
            __instance.btnrandom.transform.Find("btn-shadow").GetComponent<Image>().color = Theme.themeColors.randomButton.shadowOver;
        }
        public static void OnPointerLeaveRandomEvent(LevelSelectController __instance)
        {
            __instance.btnrandom.transform.Find("RandomBackground").GetComponent<Image>().color = Theme.themeColors.randomButton.background;
            __instance.btnrandom.transform.Find("RandomOutline").GetComponent<Image>().color = Theme.themeColors.randomButton.outline;
            __instance.btnrandom.transform.Find("RandomBackground/icon").GetComponent<Image>().color = Theme.themeColors.randomButton.text;
            __instance.btnrandom.transform.Find("RandomBackground/Text").GetComponent<Text>().color = Theme.themeColors.randomButton.text;
            __instance.btnrandom.transform.Find("btn-shadow").GetComponent<Image>().color = Theme.themeColors.randomButton.shadow;
        }

        #endregion

        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.advanceSongs))]
        [HarmonyPostfix]
        public static void OnAdvanceSongsPostFix(LevelSelectController __instance)
        {
            for (int i = 0; i < 10; i++)
            {
                if (!Theme.isDefault)
                    __instance.diffstars[i].color = Color.Lerp(Theme.themeColors.diffStar.gradientStart, Theme.themeColors.diffStar.gradientEnd, i / 9f);
                else
                    __instance.diffstars[i].color = Color.white;
            }
            if (Theme.isDefault || songyear == null) return;
            songyear.text = __instance.songyear.text;
            songgenre.text = __instance.songgenre.text;
            songduration.text = __instance.songduration.text;
            songcomposer.text = __instance.songcomposer.text;
            songtempo.text = __instance.songtempo.text;
            songdesctext.text = __instance.songdesctext.text;
        }
        [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.sortTracks))]
        [HarmonyPostfix]
        public static void OnSortTracksPostFix(LevelSelectController __instance) => OnAdvanceSongsPostFix(__instance);

        [HarmonyPatch(typeof(WaveController), nameof(WaveController.Start))]
        [HarmonyPostfix]
        public static void WaveControllerFuckeryOverwrite(WaveController __instance)
        {
            if (Theme.isDefault) return;

            foreach (SpriteRenderer sr in __instance.wavesprites)
                sr.color = __instance.gameObject.name == "BGWave" ? Theme.themeColors.background.waves : Theme.themeColors.background.waves2;
        }

        public static void OverwriteGameObjectSpriteAndColor(GameObject gameObject, string spriteName, Color spriteColor)
        {
            gameObject.GetComponent<Image>().sprite = AssetManager.GetSprite(spriteName);
            gameObject.GetComponent<Image>().color = spriteColor;
        }
    }
}
