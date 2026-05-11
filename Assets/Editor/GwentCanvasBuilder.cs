#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Будує повну ієрархію Canvas у стилі Gwent з Witcher 3.
/// Всі кольори, розміри та відступи взяті 1:1 з JSX-дизайну.
///
/// ІНСТРУКЦІЯ:
///   1. Цей файл → Assets/Editor/GwentCanvasBuilder.cs
///   2. GwentUIManager, CardUI, HandUI, CardTooltip → Assets/Scripts/UI/
///   3. Зачекай компіляцію
///   4. Menu: Tools → Gwent → Build Witcher3 Canvas
/// </summary>
public class GwentCanvasBuilder : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  ПАЛІТРА КОЛЬОРІВ (точні hex з JSX)
    // ═══════════════════════════════════════════════════════════════

    // Фони
    static readonly Color C_BG_BOARD    = Hex("0a0806");
    static readonly Color C_BG_HEADER   = Hex("080604");
    static readonly Color C_BG_DARK     = Hex("0c0a06");
    static readonly Color C_BG_ACTIVE   = Hex("12100a");
    static readonly Color C_BG_ROW_INFO = Hex("0e0b06");
    static readonly Color C_BG_ROW_CARD = Hex("0c0904");
    static readonly Color C_BG_OVERLAY  = new Color(0f, 0f, 0f, 0.85f);
    static readonly Color C_BG_PANEL    = Hex("0e0b06");

    // Межі
    static readonly Color C_BORDER_MAIN = Hex("3a2a0a");
    static readonly Color C_BORDER_ROW  = Hex("2a1e0a");
    static readonly Color C_BORDER_GOLD = Hex("c8900a");
    static readonly Color C_BORDER_SEL  = Hex("f0d060");
    static readonly Color C_BORDER_CARD = Hex("5a4a2a");

    // Текст
    static readonly Color C_TEXT_GOLD    = Hex("c8900a");
    static readonly Color C_TEXT_CREAM   = Hex("c0a060");
    static readonly Color C_TEXT_MID     = Hex("806040");
    static readonly Color C_TEXT_DIM     = Hex("60502a");
    static readonly Color C_TEXT_VDIM    = Hex("3a2a0a");
    static readonly Color C_TEXT_ACTIVE  = Hex("e0c060");
    static readonly Color C_TEXT_SCORE   = Hex("90e050");
    static readonly Color C_TEXT_NOSCORE = Hex("405030");
    static readonly Color C_TEXT_BUTTON  = Hex("f0c040");
    static readonly Color C_TEXT_PASS    = Hex("e06040");

    // Score circles (ряди)
    static readonly Color C_SC_BG_ON  = Hex("0a1a0a");
    static readonly Color C_SC_BG_OFF = Hex("0a0a0a");
    static readonly Color C_SC_TX_ON  = Hex("90d060");
    static readonly Color C_SC_TX_OFF = Hex("3a3020");

    // Score circles (гравці)
    static readonly Color C_PSC_BG    = Hex("0a1208");
    static readonly Color C_PSC_BR_ON = Hex("4a7a3a");
    static readonly Color C_PSC_BR_OFF= Hex("1a2010");

    // Life gems
    static readonly Color C_LIFE_ON  = Hex("c82020");
    static readonly Color C_LIFE_OFF = Hex("1a0a0a");

    // Погода
    static readonly Color C_WX_BG_ON = Hex("0a1a2a");
    static readonly Color C_WX_BR_ON = Hex("2a6090");
    static readonly Color C_WX_TX_ON = Hex("60a8d0");
    static readonly Color C_WX_BG_OFF= Hex("0c0a06");

    // Карти
    static readonly Color C_CARD_BG      = Hex("0a0806");
    static readonly Color C_CARD_NAMEBAR = Hex("0e0b06");
    static readonly Color C_CARD_NAMETXT = Hex("c8a870");
    static readonly Color C_CARD_PWR_TXT = Hex("e0c890");
    static readonly Color C_CARD_HERO_BG = Hex("7a5000");
    static readonly Color C_CARD_HERO_TX = Hex("ffe080");
    static readonly Color C_CARD_STAR    = Hex("f0d060");
    static readonly Color C_CARD_ABIL_BG = Hex("1a0e00");
    static readonly Color C_CARD_ABIL_TX = Hex("c07030");

    // Фракції
    static readonly Color C_FACTION_HUMANS  = Hex("7a4a1a");
    static readonly Color C_FACTION_ELFS    = Hex("2a5a2a");
    static readonly Color C_FACTION_NEUTRAL = Hex("3a3028");

    // Кнопки
    static readonly Color C_BTN_PLAY_BG = Hex("1a1000");
    static readonly Color C_BTN_PASS_BG = Hex("1a0808");
    static readonly Color C_BTN_PASS_BR = Hex("703020");
    static readonly Color C_BTN_NEW_BG  = Hex("1a0a0a");

    // ═══════════════════════════════════════════════════════════════
    //  ТОЧКА ВХОДУ
    // ═══════════════════════════════════════════════════════════════
    [MenuItem("Tools/Gwent/Build Witcher3 Canvas")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog("Gwent Canvas Builder",
            "Будуємо Witcher-3 Canvas.\nІснуючий GwentCanvas буде видалений.", "Так", "Скасувати"))
            return;

        var old = GameObject.Find("GwentCanvas");
        if (old) DestroyImmediate(old);

        if (!Directory.Exists("Assets/Prefabs/UI"))
            Directory.CreateDirectory("Assets/Prefabs/UI");

        var canvas  = MakeCanvas();
        var prefab  = MakeCardPrefab();
        var uiMgr   = AddUIManager(canvas.transform, prefab);

        BuildLayout(canvas.transform, uiMgr);
        AutoLink(uiMgr);
        EnsureEventSystem();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = canvas;

        EditorUtility.DisplayDialog("Готово!",
            "GwentCanvas побудовано у стилі Witcher 3!\n\n" +
            "Залишилось прив'язати в Inspector → UIManager:\n" +
            "  • Game Manager\n  • Board Manager\n  • Player 1\n  • Player 2", "OK");
    }

    // ═══════════════════════════════════════════════════════════════
    //  CANVAS ROOT
    // ═══════════════════════════════════════════════════════════════
    static GameObject MakeCanvas()
    {
        var go = new GameObject("GwentCanvas");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 0;

        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // Board background
        var bg = GO("Background", go.transform);
        Fill(bg); Img(bg, C_BG_BOARD);
        return go;
    }

    // ═══════════════════════════════════════════════════════════════
    //  ГОЛОВНИЙ LAYOUT (VerticalLayoutGroup заповнює весь Canvas)
    // ═══════════════════════════════════════════════════════════════
    static void BuildLayout(Transform cv, GwentUIManager u)
    {
        var root = GO("MainLayout", cv); Fill(root);
        var v = root.AddComponent<VerticalLayoutGroup>();
        v.spacing = 0; v.childAlignment = TextAnchor.UpperCenter;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        v.childControlWidth = true;     v.childControlHeight = true;

        Header(root.transform, u);

        PlayerBar(root.transform, false, u);   // P2 top
        HandBar(root.transform,   false, u);   // P2 hand face-down
        Row(root.transform, "Siege",  false, u);
        Row(root.transform, "Ranged", false, u);
        Row(root.transform, "Melee",  false, u);

        WeatherStrip(root.transform, u);

        Row(root.transform, "Melee",  true, u);
        Row(root.transform, "Ranged", true, u);
        Row(root.transform, "Siege",  true, u);

        PlayerBar(root.transform, true, u);    // P1 bottom
        P1Hand(root.transform, u);
        GameLog(root.transform, u);

        RoundEndOverlay(cv, u);                // fullscreen overlay on top
    }

    // ═══════════════════════════════════════════════════════════════
    //  HEADER
    //  JSX: bg #080604, borderBottom 2px #3a2a0a, h ~44
    //       Title "⚔ GWENT ⚔" (#c8900a) | State (#60502a) | NewGame
    // ═══════════════════════════════════════════════════════════════
    static void Header(Transform p, GwentUIManager u)
    {
        var go = GO("Header", p); Img(go, C_BG_HEADER); LE(go, -1, 44, 1, 0);
        Divider(go.transform, false, C_BORDER_MAIN, 2);

        var h = HLG(go, 16, 16, 8, 8, 12);

        // Title
        var title = GO("Title", go.transform); LE(title, -1, -1, 1, 0);
        var tt = TMP(title, "⚔   GWENT   ⚔", 18, C_TEXT_GOLD);
        tt.alignment = TextAlignmentOptions.MidlineLeft;
        tt.fontStyle = FontStyles.Bold; tt.characterSpacing = 6;

        // State panel
        var sp = GO("StatePanel", go.transform); Img(sp, C_BG_DARK); LE(sp, 300, 28, 0, 0);
        var sph = HLG(sp, 12, 12, 4, 4, 0);
        sph.childAlignment = TextAnchor.MiddleCenter;
        var stGO = GO("StateText", sp.transform);
        var st = TMP(stGO, "ROUND 1 — PLAYER 1", 10, C_TEXT_DIM);
        st.alignment = TextAlignmentOptions.Center; st.characterSpacing = 2;
        u.turnIndicatorText = st;

        // New Game btn
        var nb = GO("NewGameBtn", go.transform); Img(nb, C_BTN_NEW_BG); LE(nb, 118, 28, 0, 0);
        StyleBtn(nb, C_BTN_NEW_BG, Hex("2a1a1a"), Hex("100808"));
        var nt = GO("T", nb.transform); Fill(nt);
        TMP(nt, "↺  NEW GAME", 10, Hex("806040")).alignment = TextAlignmentOptions.Center;
    }

    // ═══════════════════════════════════════════════════════════════
    //  PLAYER INFO BAR
    //  JSX: h 68, HStack, gem(42) + name/lives + scoreCircle(54) + counts + passBtn
    // ═══════════════════════════════════════════════════════════════
    static void PlayerBar(Transform p, bool isP1, GwentUIManager u)
    {
        var go = GO(isP1 ? "P1Bar" : "P2Bar", p);
        Img(go, C_BG_DARK); LE(go, -1, 68, 1, 0);
        Divider(go.transform, isP1, C_BORDER_ROW, 1);

        HLG(go, 12, 12, 8, 8, 12);

        // Faction gem
        var gem = GO("Gem", go.transform); LE(gem, 42, 42, 0, 0);
        Img(gem, isP1 ? C_FACTION_HUMANS : C_FACTION_ELFS);
        var gi = GO("Icon", gem.transform); Fill(gi);
        TMP(gi, isP1 ? "⚔" : "🌿", 20, Color.white).alignment = TextAlignmentOptions.Center;
        if (isP1) u.player1TurnHighlight = gem; else u.player2TurnHighlight = gem;

        // Name + lives
        var na = GO("NameArea", go.transform); LE(na, -1, -1, 1, 0);
        var nv = na.AddComponent<VerticalLayoutGroup>();
        nv.spacing = 5; nv.childAlignment = TextAnchor.MiddleLeft;
        nv.childForceExpandWidth = true; nv.childForceExpandHeight = false;
        nv.childControlWidth = true; nv.childControlHeight = true;

        var nmGO = GO("Name", na.transform); LE(nmGO, -1, 18, 1, 0);
        TMP(nmGO, isP1 ? "Player 1 — Humans" : "Player 2 — Elfs", 12, C_TEXT_MID);

        var lr = GO("Lives", na.transform); LE(lr, -1, 18, 1, 0);
        HLG(lr, 0, 0, 0, 0, 5);
        var l1g = GO("Life1", lr.transform); LE(l1g, 16, 16, 0, 0); var l1 = Img(l1g, C_LIFE_ON);
        var l2g = GO("Life2", lr.transform); LE(l2g, 16, 16, 0, 0); var l2 = Img(l2g, C_LIFE_ON);
        if (isP1) { u.p1Life1Image = l1; u.p1Life2Image = l2; }
        else      { u.p2Life1Image = l1; u.p2Life2Image = l2; }

        // Score circle (54x54)
        var sc = GO("ScoreCircle", go.transform); LE(sc, 54, 54, 0, 0); Img(sc, C_PSC_BG);
        var sv = sc.AddComponent<VerticalLayoutGroup>();
        sv.padding = new RectOffset(2, 2, 4, 4); sv.spacing = 0;
        sv.childAlignment = TextAnchor.MiddleCenter;
        sv.childForceExpandWidth = true; sv.childForceExpandHeight = true;
        sv.childControlWidth = true; sv.childControlHeight = true;

        var slbl = GO("Lbl", sc.transform); LE(slbl, -1, 14, 1, 0);
        TMP(slbl, "SCORE", 7, Hex("608040")).alignment = TextAlignmentOptions.Center;
        var sval = GO("Val", sc.transform); LE(sval, -1, 28, 1, 0);
        var svt = TMP(sval, "0", 22, C_TEXT_NOSCORE);
        svt.alignment = TextAlignmentOptions.Center; svt.fontStyle = FontStyles.Bold;
        if (isP1) u.p1ScoreText = svt; else u.p2ScoreText = svt;

        // Counts
        var cGO = GO("Counts", go.transform); LE(cGO, 100, -1, 0, 0);
        var cv2 = cGO.AddComponent<VerticalLayoutGroup>();
        cv2.spacing = 2; cv2.childForceExpandWidth = true; cv2.childForceExpandHeight = false;
        cv2.childControlWidth = true; cv2.childControlHeight = true;

        var hcGO = GO("Hand",    cGO.transform); LE(hcGO, -1, 14, 1, 0); var hct = TMP(hcGO, "Hand: 10", 10, C_TEXT_DIM);
        var dcGO = GO("Deck",    cGO.transform); LE(dcGO, -1, 14, 1, 0); var dct = TMP(dcGO, "Deck: 0",  10, C_TEXT_DIM);
        var grGO = GO("Graveyard",cGO.transform);LE(grGO, -1, 14, 1, 0); var grt = TMP(grGO, "Grave: 0", 10, C_TEXT_DIM);

        if (isP1) { u.p1HandCountText = hct; u.p1DeckCountText = dct; u.p1DiscardCountText = grt; }
        else      { u.p2HandCountText = hct; u.p2DeckCountText = dct; u.p2DiscardCountText = grt; }

        // Pass button
        var pb = GO(isP1 ? "P1PassBtn" : "P2PassBtn", go.transform);
        LE(pb, 88, 34, 0, 0); Img(pb, C_BTN_PASS_BG);
        var pbt = GO("T", pb.transform); Fill(pbt);
        TMP(pbt, "PASS", 11, C_TEXT_PASS).alignment = TextAlignmentOptions.Center;
        var btn = StyleBtn(pb, C_BTN_PASS_BG, Hex("2a1010"), Hex("0a0404"));
        if (isP1) u.p1PassButton = btn; else u.p2PassButton = btn;
    }

    // ═══════════════════════════════════════════════════════════════
    //  HAND BAR (P2 face-down / P1 container assigned later)
    // ═══════════════════════════════════════════════════════════════
    static void HandBar(Transform p, bool isP1, GwentUIManager u)
    {
        var go = GO(isP1 ? "P1HandBar" : "P2HandBar", p);
        Img(go, C_BG_HEADER); LE(go, -1, 62, 1, 0);
        HLG(go, 12, 12, 6, 6, 5);
        if (!isP1) u.p2HandContainer = go.transform;
    }

    // ═══════════════════════════════════════════════════════════════
    //  BOARD ROW
    //  JSX: HStack minHeight 90
    //    Left: w72, bg #0e0b06 | Cards: flex, bg #0c0904 | Right: w56, bg #0a0806
    // ═══════════════════════════════════════════════════════════════
    static void Row(Transform p, string label, bool isP1, GwentUIManager u)
    {
        string emoji = label == "Melee" ? "⚔" : label == "Ranged" ? "🏹" : "💣";
        string long_ = label == "Melee" ? "Close Combat" : label == "Ranged" ? "Long Range" : "Siege";
        string wxEmoji = label == "Melee" ? "🧊" : label == "Ranged" ? "🌫" : "🌧";

        var go = GO((isP1 ? "P1" : "P2") + label + "Row", p);
        Img(go, C_BG_BOARD); LE(go, -1, 92, 1, 0);
        Divider(go.transform, isP1, C_BORDER_ROW, 1);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 0;
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true; hlg.childControlWidth = true;

        // LEFT INFO (w72)
        var info = GO("Info", go.transform); Img(info, C_BG_ROW_INFO); LE(info, 72, -1, 0, 1);
        RightBorder(info.transform, C_BORDER_ROW);
        var iv = info.AddComponent<VerticalLayoutGroup>();
        iv.padding = new RectOffset(4, 4, 6, 6); iv.spacing = 3;
        iv.childAlignment = TextAnchor.MiddleCenter;
        iv.childForceExpandWidth = true; iv.childForceExpandHeight = false;
        iv.childControlWidth = true; iv.childControlHeight = true;

        var riGO = GO("Icon",  info.transform); LE(riGO, -1, 26, 1, 0);
        TMP(riGO, emoji, 18, Color.white).alignment = TextAlignmentOptions.Center;

        var rlGO = GO("Label", info.transform); LE(rlGO, -1, 18, 1, 0);
        var rlt = TMP(rlGO, long_.ToUpper(), 8, C_TEXT_MID);
        rlt.alignment = TextAlignmentOptions.Center; rlt.characterSpacing = 1;

        var wxGO = GO("WeatherIcon", info.transform); LE(wxGO, -1, 16, 1, 0);
        TMP(wxGO, wxEmoji, 13, C_WX_TX_ON).alignment = TextAlignmentOptions.Center;
        wxGO.SetActive(false);

        // CARDS AREA (flex)
        var cards = GO("Cards", go.transform); Img(cards, C_BG_ROW_CARD); LE(cards, -1, -1, 1, 1);
        RightBorder(cards.transform, C_BORDER_ROW);
        var ch = cards.AddComponent<HorizontalLayoutGroup>();
        ch.padding = new RectOffset(8, 8, 6, 6); ch.spacing = 5;
        ch.childForceExpandHeight = true; ch.childForceExpandWidth = false;
        ch.childControlHeight = true; ch.childControlWidth = true;
        ch.childAlignment = TextAnchor.MiddleLeft;

        // Assign container ref via reflection
        var field = typeof(GwentUIManager).GetField(
            (isP1 ? "p1" : "p2") + label + "Container",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (field != null) field.SetValue(u, cards.transform);

        // RIGHT SCORE PANEL (w56)
        var sp = GO("Score", go.transform); Img(sp, C_BG_BOARD); LE(sp, 56, -1, 0, 1);
        var spv = sp.AddComponent<VerticalLayoutGroup>();
        spv.padding = new RectOffset(8, 8, 8, 6); spv.spacing = 6;
        spv.childAlignment = TextAnchor.MiddleCenter;
        spv.childForceExpandWidth = true; spv.childForceExpandHeight = false;
        spv.childControlWidth = true; spv.childControlHeight = true;

        // Score circle (40x40)
        var sc = GO("Circle", sp.transform); LE(sc, 40, 40, 0, 0); Img(sc, C_SC_BG_OFF);
        var scv = sc.AddComponent<VerticalLayoutGroup>();
        scv.childAlignment = TextAnchor.MiddleCenter;
        scv.childForceExpandWidth = true; scv.childForceExpandHeight = true;
        scv.childControlWidth = true; scv.childControlHeight = true;
        var scValGO = GO("Val", sc.transform);
        var scvt = TMP(scValGO, "0", 16, C_SC_TX_OFF);
        scvt.alignment = TextAlignmentOptions.Center; scvt.fontStyle = FontStyles.Bold;

        var scoreField = typeof(GwentUIManager).GetField(
            (isP1 ? "p1" : "p2") + label + "ScoreText",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (scoreField != null) scoreField.SetValue(u, scvt);

        // Horn button (32x20)
        var hb = GO("Horn", sp.transform); LE(hb, 32, 20, 0, 0); Img(hb, C_BG_BOARD);
        StyleBtn(hb, C_BG_BOARD, Hex("1a1208"), Hex("080604"));
        var hbt = GO("T", hb.transform); Fill(hbt);
        TMP(hbt, "📯", 12, Hex("3a2a1a")).alignment = TextAlignmentOptions.Center;
    }

    // ═══════════════════════════════════════════════════════════════
    //  WEATHER STRIP
    //  JSX: bg #080604, borders #2a1e0a, h ~54
    //       "WEATHER" | 🧊 Frost | 🌫 Fog | 🌧 Rain
    // ═══════════════════════════════════════════════════════════════
    static void WeatherStrip(Transform p, GwentUIManager u)
    {
        var go = GO("WeatherStrip", p); Img(go, C_BG_HEADER); LE(go, -1, 54, 1, 0);
        Divider(go.transform, true, C_BORDER_ROW, 1);
        Divider(go.transform, false, C_BORDER_ROW, 1);

        var h = HLG(go, 16, 16, 8, 8, 20);
        h.childAlignment = TextAnchor.MiddleCenter;

        var wl = GO("Label", go.transform); LE(wl, 70, -1, 0, 0);
        TMP(wl, "WEATHER", 9, C_BORDER_ROW).alignment = TextAlignmentOptions.Center;

        string[] rows  = { "Melee", "Ranged", "Siege" };
        string[] icons = { "🧊", "🌫", "🌧" };
        string[] names = { "Frost", "Fog", "Rain" };

        for (int i = 0; i < 3; i++)
        {
            var card = GO(rows[i], go.transform); LE(card, 64, -1, 0, 0); Img(card, C_WX_BG_OFF);
            var cv2 = card.AddComponent<VerticalLayoutGroup>();
            cv2.padding = new RectOffset(8, 8, 5, 5); cv2.spacing = 2;
            cv2.childAlignment = TextAnchor.MiddleCenter;
            cv2.childForceExpandWidth = true; cv2.childForceExpandHeight = false;
            cv2.childControlWidth = true; cv2.childControlHeight = true;

            var icGO = GO("Icon", card.transform); LE(icGO, -1, 22, 1, 0);
            TMP(icGO, icons[i], 18, Color.white).alignment = TextAlignmentOptions.Center;
            var nmGO = GO("Name", card.transform); LE(nmGO, -1, 14, 1, 0);
            TMP(nmGO, names[i], 8, C_BORDER_ROW).alignment = TextAlignmentOptions.Center;

            if (rows[i] == "Melee")  u.frostIndicator = card;
            if (rows[i] == "Ranged") u.fogIndicator   = card;
            if (rows[i] == "Siege")  u.rainIndicator  = card;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  P1 HAND SECTION
    //  JSX: bg #080604, label + ScrollRect hand + selected bar
    // ═══════════════════════════════════════════════════════════════
    static void P1Hand(Transform p, GwentUIManager u)
    {
        var go = GO("P1HandSection", p); Img(go, C_BG_HEADER); LE(go, -1, 152, 1, 0);
        Divider(go.transform, true, C_BORDER_ROW, 1);

        var v = go.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(12, 12, 10, 8); v.spacing = 8;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        v.childControlWidth = true; v.childControlHeight = true;

        // Label
        var lbl = GO("Prompt", go.transform); LE(lbl, -1, 14, 1, 0);
        var lt = TMP(lbl, "YOUR HAND — SELECT A CARD TO PLAY", 8, C_BORDER_ROW);
        lt.characterSpacing = 3;
        // NOTE: turnIndicatorText is already wired to Header's state text.
        // Avoid overwriting it here.

        // ScrollRect
        var sr = GO("Scroll", go.transform); LE(sr, -1, 116, 1, 0);
        var scroll = sr.AddComponent<ScrollRect>();
        scroll.horizontal = true; scroll.vertical = false;
        scroll.scrollSensitivity = 20;

        var vp = GO("Viewport", sr.transform); Fill(vp);
        vp.AddComponent<Mask>().showMaskGraphic = false;
        Img(vp, Color.clear);
        scroll.viewport = vp.GetComponent<RectTransform>();

        var ct = GO("Content", vp.transform);
        var ctr = ct.GetComponent<RectTransform>();
        ctr.anchorMin = new Vector2(0, 0); ctr.anchorMax = new Vector2(0, 1);
        ctr.pivot = new Vector2(0, 0.5f);
        ctr.offsetMin = Vector2.zero; ctr.offsetMax = Vector2.zero;
        var ch = ct.AddComponent<HorizontalLayoutGroup>();
        ch.spacing = 6; ch.childForceExpandHeight = true; ch.childForceExpandWidth = false;
        ch.childControlHeight = true; ch.childControlWidth = true;
        ct.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = ctr;
        u.p1HandContainer = ct.transform;

        // Selected card bar (hidden by default)
        var sel = GO("SelectedBar", go.transform); Img(sel, C_BG_ROW_INFO); LE(sel, -1, 44, 1, 0);
        Outline4(sel.transform, C_BORDER_ROW, 1);
        sel.SetActive(false);

        var sh = HLG(sel, 14, 14, 6, 6, 12);
        var ia = GO("Info", sel.transform); LE(ia, -1, -1, 1, 0);
        var iv = ia.AddComponent<VerticalLayoutGroup>();
        iv.spacing = 2; iv.childForceExpandWidth = true; iv.childForceExpandHeight = false;
        iv.childControlWidth = true; iv.childControlHeight = true;

        var sn = GO("CardName", ia.transform); LE(sn, -1, 18, 1, 0);
        var snt = TMP(sn, "", 12, Hex("c8a060")); snt.fontStyle = FontStyles.Bold;
        var sd = GO("Details",  ia.transform); LE(sd, -1, 14, 1, 0);
        TMP(sd, "", 10, C_TEXT_DIM);

        var pb = GO("PlayBtn", sel.transform); LE(pb, 110, 34, 0, 0); Img(pb, C_BTN_PLAY_BG);
        Outline4(pb.transform, C_BORDER_GOLD, 1);
        StyleBtn(pb, C_BTN_PLAY_BG, Hex("2a2000"), Hex("0a0800"));
        var pt = GO("T", pb.transform); Fill(pt);
        var ptt = TMP(pt, "▶  PLAY", 13, C_TEXT_BUTTON);
        ptt.alignment = TextAlignmentOptions.Center;
        ptt.fontStyle = FontStyles.Bold; ptt.characterSpacing = 1;
    }

    // ═══════════════════════════════════════════════════════════════
    //  GAME LOG
    //  JSX: bg #080604, border-top #2a1e0a, h 72, scroll
    // ═══════════════════════════════════════════════════════════════
    static void GameLog(Transform p, GwentUIManager u)
    {
        var go = GO("GameLog", p); Img(go, C_BG_HEADER); LE(go, -1, 72, 1, 0);
        Divider(go.transform, true, C_BORDER_ROW, 1);

        var scroll = go.AddComponent<ScrollRect>();
        scroll.vertical = true; scroll.horizontal = false;
        scroll.scrollSensitivity = 20;

        var vp = GO("Viewport", go.transform); Fill(vp);
        vp.AddComponent<Mask>().showMaskGraphic = false; Img(vp, Color.clear);
        scroll.viewport = vp.GetComponent<RectTransform>();

        var ct = GO("Content", vp.transform);
        var ctr = ct.GetComponent<RectTransform>();
        ctr.anchorMin = new Vector2(0, 0); ctr.anchorMax = new Vector2(1, 0);
        ctr.pivot = new Vector2(0.5f, 0f);
        ctr.offsetMin = new Vector2(14, 6); ctr.offsetMax = new Vector2(-14, 6);
        var cv = ct.AddComponent<VerticalLayoutGroup>();
        cv.spacing = 1; cv.childForceExpandWidth = true; cv.childForceExpandHeight = false;
        cv.childControlWidth = true; cv.childControlHeight = true;
        ct.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = ctr;

        // Set initial scroll after viewport/content are assigned
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0f;

        // Initial log entry
        var e0 = GO("Entry0", ct.transform); LE(e0, -1, 14, 1, 0);
        var et = TMP(e0, "The game begins. Player 1's turn.", 10, C_TEXT_CREAM);
        et.fontStyle = FontStyles.Italic;
    }

    // ═══════════════════════════════════════════════════════════════
    //  ROUND END OVERLAY
    //  JSX: fixed black 85% overlay, centered panel #0e0b06, border 2px #c8900a
    // ═══════════════════════════════════════════════════════════════
    static void RoundEndOverlay(Transform cv, GwentUIManager u)
    {
        var ov = GO("RoundEndOverlay", cv); Fill(ov); Img(ov, C_BG_OVERLAY);
        ov.SetActive(false);
        u.roundEndPanel = ov;

        // Centered panel 360x340
        var pan = GO("Panel", ov.transform);
        var pr = pan.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0.5f); pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot = new Vector2(0.5f, 0.5f); pr.sizeDelta = new Vector2(360, 340);
        Img(pan, C_BG_PANEL);
        Outline4(pan.transform, C_BORDER_GOLD, 2);

        var pv = pan.AddComponent<VerticalLayoutGroup>();
        pv.padding = new RectOffset(40, 40, 32, 32); pv.spacing = 8;
        pv.childAlignment = TextAnchor.UpperCenter;
        pv.childForceExpandWidth = true; pv.childForceExpandHeight = false;
        pv.childControlWidth = true; pv.childControlHeight = true;

        // ⚔ icon
        var sw = GO("Sword", pan.transform); LE(sw, -1, 36, 1, 0);
        TMP(sw, "⚔", 28, C_TEXT_GOLD).alignment = TextAlignmentOptions.Center;

        // Round label
        var rl = GO("RoundLbl", pan.transform); LE(rl, -1, 18, 1, 0);
        var rlt = TMP(rl, "ROUND 1 OVER", 10, C_TEXT_DIM);
        rlt.alignment = TextAlignmentOptions.Center; rlt.characterSpacing = 4;
        u.roundEndTitleText = rlt;

        // Result text
        var res = GO("Result", pan.transform); LE(res, -1, 30, 1, 0);
        TMP(res, "", 20, Hex("c8a060")).alignment = TextAlignmentOptions.Center;

        // Score text
        var sc = GO("Score", pan.transform); LE(sc, -1, 20, 1, 0);
        var sct = TMP(sc, "", 13, C_TEXT_MID);
        sct.alignment = TextAlignmentOptions.Center;
        u.roundEndDetailsText = sct;

        // Life gems
        var gr = GO("GemsRow", pan.transform); LE(gr, -1, 36, 1, 0);
        var grh = gr.AddComponent<HorizontalLayoutGroup>();
        grh.spacing = 24; grh.childAlignment = TextAnchor.MiddleCenter;
        grh.childForceExpandWidth = false; grh.childForceExpandHeight = false;
        grh.childControlWidth = true; grh.childControlHeight = true;

        foreach (var label in new[] { "P1", "P2" })
        {
            var blk = GO(label, gr.transform); LE(blk, -1, -1, 0, 0);
            var bv = blk.AddComponent<VerticalLayoutGroup>();
            bv.spacing = 4; bv.childAlignment = TextAnchor.MiddleCenter;
            bv.childForceExpandWidth = true; bv.childForceExpandHeight = false;
            bv.childControlWidth = true; bv.childControlHeight = true;

            var lGO = GO("L", blk.transform); LE(lGO, 40, 14, 0, 0);
            TMP(lGO, label, 9, C_TEXT_DIM).alignment = TextAlignmentOptions.Center;

            var gems = GO("Gems", blk.transform); LE(gems, -1, 16, 0, 0);
            var gh = gems.AddComponent<HorizontalLayoutGroup>();
            gh.spacing = 4; gh.childAlignment = TextAnchor.MiddleCenter;
            gh.childForceExpandWidth = false; gh.childForceExpandHeight = false;
            gh.childControlWidth = true; gh.childControlHeight = true;

            var g1 = GO("G1", gems.transform); LE(g1, 14, 14, 0, 0); Img(g1, C_LIFE_ON);
            var g2 = GO("G2", gems.transform); LE(g2, 14, 14, 0, 0); Img(g2, C_LIFE_ON);
        }

        // Spacer
        var sp = GO("Spacer", pan.transform); LE(sp, -1, 8, 1, 0);

        // Match result (hidden)
        var mr = GO("MatchResult", pan.transform); LE(mr, -1, 26, 1, 0);
        TMP(mr, "", 18, Hex("f0c040")).alignment = TextAlignmentOptions.Center;
        mr.SetActive(false);

        // Continue button
        var cb = GO("ContinueBtn", pan.transform); LE(cb, 180, 38, 0, 0);
        Img(cb, C_BTN_PLAY_BG); Outline4(cb.transform, C_BORDER_GOLD, 1);
        var cbt = GO("T", cb.transform); Fill(cbt);
        var cbtt = TMP(cbt, "CONTINUE  →", 13, C_TEXT_BUTTON);
        cbtt.alignment = TextAlignmentOptions.Center; cbtt.characterSpacing = 1;
        u.roundEndContinueButton = StyleBtn(cb, C_BTN_PLAY_BG, Hex("2a2000"), Hex("0a0800"));
    }

    // ═══════════════════════════════════════════════════════════════
    //  CARD PREFAB
    //  JSX hand card: 72×110, board card: 52×78
    //  Layout: faction bar | power badge (TL) | hero star (TR) |
    //          art emoji (center) | ability strip | name bar (bottom)
    // ═══════════════════════════════════════════════════════════════
    static GameObject MakeCardPrefab()
    {
        const string PATH = "Assets/Prefabs/UI/CardUI_Prefab.prefab";

        var go = new GameObject("CardUI_Prefab");
        go.AddComponent<RectTransform>();

        // Size (hand mode default)
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 72; le.preferredHeight = 110;

        // Outer border Image
        Img(go, C_BORDER_CARD);

        // Inner background (inset 1.5px)
        var inner = GO("Inner", go.transform);
        var ir = inner.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one;
        ir.offsetMin = new Vector2(1.5f, 1.5f); ir.offsetMax = new Vector2(-1.5f, -1.5f);
        Img(inner, C_CARD_BG);

        // Faction bar (top, h28)
        var fb = GO("FactionBar", inner.transform);
        var fbr = fb.GetComponent<RectTransform>();
        fbr.anchorMin = new Vector2(0, 1); fbr.anchorMax = new Vector2(1, 1);
        fbr.offsetMin = new Vector2(0, -28); fbr.offsetMax = Vector2.zero;
        Img(fb, C_FACTION_HUMANS); // CardUI.cs overrides at runtime

        // Power badge (TL, 24x24)
        var pb = GO("PowerBadge", inner.transform);
        var pbr = pb.GetComponent<RectTransform>();
        pbr.anchorMin = new Vector2(0, 1); pbr.anchorMax = new Vector2(0, 1);
        pbr.pivot = new Vector2(0, 1); pbr.anchoredPosition = new Vector2(4, -4);
        pbr.sizeDelta = new Vector2(24, 24);
        Img(pb, Hex("1a1208"));
        var pbt = GO("T", pb.transform); Fill(pbt);
        var pbtt = TMP(pbt, "0", 14, C_CARD_PWR_TXT);
        pbtt.alignment = TextAlignmentOptions.Center; pbtt.fontStyle = FontStyles.Bold;

        // Hero star (TR, 16x16)
        var star = GO("HeroStar", inner.transform);
        var sr = star.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(1, 1); sr.anchorMax = new Vector2(1, 1);
        sr.pivot = new Vector2(1, 1); sr.anchoredPosition = new Vector2(-4, -4);
        sr.sizeDelta = new Vector2(16, 16);
        TMP(star, "★", 12, C_CARD_STAR).alignment = TextAlignmentOptions.Center;
        star.SetActive(false);

        // Art / emoji (center zone)
        var art = GO("Art", inner.transform);
        var ar = art.GetComponent<RectTransform>();
        ar.anchorMin = new Vector2(0, 0.28f); ar.anchorMax = new Vector2(1, 0.74f);
        ar.offsetMin = Vector2.zero; ar.offsetMax = Vector2.zero;
        TMP(art, "⚔", 22, Color.white).alignment = TextAlignmentOptions.Center;

        // Ability strip (above name bar, h14)
        var ab = GO("AbilityBar", inner.transform);
        var abr = ab.GetComponent<RectTransform>();
        abr.anchorMin = new Vector2(0, 0); abr.anchorMax = new Vector2(1, 0);
        abr.offsetMin = new Vector2(0, 28); abr.offsetMax = new Vector2(0, 42);
        Img(ab, C_CARD_ABIL_BG);
        var abt = GO("T", ab.transform); Fill(abt);
        TMP(abt, "", 8, C_CARD_ABIL_TX).alignment = TextAlignmentOptions.Center;
        ab.SetActive(false);

        // Name bar (bottom, h28)
        var nb = GO("NameBar", inner.transform);
        var nbr = nb.GetComponent<RectTransform>();
        nbr.anchorMin = new Vector2(0, 0); nbr.anchorMax = new Vector2(1, 0);
        nbr.offsetMin = Vector2.zero; nbr.offsetMax = new Vector2(0, 28);
        Img(nb, C_CARD_NAMEBAR);
        var nbt = GO("T", nb.transform); Fill(nbt);
        var nbtt = TMP(nbt, "Card Name", 7.5f, C_CARD_NAMETXT);
        nbtt.alignment = TextAlignmentOptions.Center; nbtt.textWrappingMode = TextWrappingModes.Normal;
        nbtt.overflowMode = TextOverflowModes.Truncate;

        go.AddComponent<CardUI>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PATH);
        DestroyImmediate(go);
        return prefab;
    }

    // ═══════════════════════════════════════════════════════════════
    //  UI MANAGER
    // ═══════════════════════════════════════════════════════════════
    static GwentUIManager AddUIManager(Transform p, GameObject prefab)
    {
        var go  = GO("UIManager", p);
        var mgr = go.AddComponent<GwentUIManager>();
        mgr.cardUIPrefab = prefab;
        return mgr;
    }

    static void AutoLink(GwentUIManager u)
    {
        // Замінили FindObjectOfType на FindFirstObjectByType
        var gm = Object.FindFirstObjectByType<GameManager>();
        var bm = Object.FindFirstObjectByType<BoardManager>();
        if (gm) u.gameManager  = gm;
        if (bm) u.boardManager = bm;
        
        // Замінили FindObjectsOfType на FindObjectsByType з сортуванням None (це працює швидше)
        foreach (var pm in Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None))
            if (pm.isPlayer1) u.player1 = pm; else u.player2 = pm;
    }

    static void EnsureEventSystem()
    {
        // Замінили FindObjectOfType на FindFirstObjectByType
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPER SHORTCUTS
    // ═══════════════════════════════════════════════════════════════
    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c);
        return c;
    }
    static GameObject GO(string n, Transform p)
    {
        var g = new GameObject(n); g.transform.SetParent(p, false);
        g.AddComponent<RectTransform>(); return g;
    }
    static Image Img(GameObject g, Color c)
    {
        var i = g.GetComponent<Image>() ?? g.AddComponent<Image>(); i.color = c; return i;
    }
    static TextMeshProUGUI TMP(GameObject g, string t, float s, Color c)
    {
        var tm = g.GetComponent<TextMeshProUGUI>() ?? g.AddComponent<TextMeshProUGUI>();
        tm.text = t; tm.fontSize = s; tm.color = c;
        tm.textWrappingMode = TextWrappingModes.NoWrap; tm.overflowMode = TextOverflowModes.Truncate;
        return tm;
    }
    static LayoutElement LE(GameObject g, float pw, float ph, float fw, float fh)
    {
        var le = g.GetComponent<LayoutElement>() ?? g.AddComponent<LayoutElement>();
        if (pw >= 0) le.preferredWidth  = pw; if (ph >= 0) le.preferredHeight = ph;
        if (fw >= 0) le.flexibleWidth   = fw; if (fh >= 0) le.flexibleHeight  = fh;
        return le;
    }
    static void Fill(GameObject g)
    {
        var r = g.GetComponent<RectTransform>() ?? g.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }
    static HorizontalLayoutGroup HLG(GameObject g, int pl, int pr, int pt, int pb, float sp)
    {
        var h = g.GetComponent<HorizontalLayoutGroup>() ?? g.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(pl, pr, pt, pb); h.spacing = sp;
        h.childForceExpandHeight = true; h.childForceExpandWidth = false;
        h.childControlHeight = true; h.childControlWidth = true;
        return h;
    }
    static Button StyleBtn(GameObject g, Color norm, Color hov, Color press)
    {
        var b = g.GetComponent<Button>() ?? g.AddComponent<Button>();
        var c = b.colors;
        c.normalColor = norm; c.highlightedColor = hov; c.pressedColor = press;
        c.selectedColor = norm; c.fadeDuration = 0.1f;
        b.colors = c;
        var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
        b.targetGraphic = g.GetComponent<Image>();
        return b;
    }
    // Top or bottom 1px border line
// Top or bottom 1px border line
    static void Divider(Transform p, bool isTop, Color c, float h)
    {
        var g = GO(isTop ? "BorderTop" : "BorderBot", p);
        var r = g.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, isTop ? 1 : 0);
        r.anchorMax = new Vector2(1, isTop ? 1 : 0);
        r.offsetMin = new Vector2(0, isTop ? -h :  0);
        r.offsetMax = new Vector2(0, isTop ?  0 :  h);
        Img(g, c);
        
        var le = g.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        
        // Гарантує, що лінія намалюється ПОВЕРХ фонів, а не під ними
        g.transform.SetAsLastSibling(); 
    }

    // Right-side 1px border
    static void RightBorder(Transform p, Color c)
    {
        var g = GO("BorderRight", p);
        var r = g.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 1);
        r.offsetMin = new Vector2(-1, 0); r.offsetMax = Vector2.zero;
        Img(g, c);
        
        var le = g.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        
        g.transform.SetAsLastSibling();
    }

    // 4-sided outline border
    static void Outline4(Transform p, Color c, float t)
    {
        void Side(string n, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax)
        {
            var g = GO(n, p); var r = g.GetComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax;
            r.offsetMin = omin; r.offsetMax = omax;
            Img(g, c);
            
            var le = g.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            
            g.transform.SetAsLastSibling();
        }
        Side("T", new Vector2(0,1), new Vector2(1,1), new Vector2(0,-t), Vector2.zero);
        Side("B", new Vector2(0,0), new Vector2(1,0), Vector2.zero, new Vector2(0,t));
        Side("L", new Vector2(0,0), new Vector2(0,1), Vector2.zero, new Vector2(t,0));
        Side("R", new Vector2(1,0), new Vector2(1,1), new Vector2(-t,0), Vector2.zero);
    }
}
#endif