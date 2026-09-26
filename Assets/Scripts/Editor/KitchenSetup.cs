using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// One-click scene setup: Tools > Isombe Kitchen > Apply Scene Setup.
// Safe to run more than once: it only fills in what is missing and rebuilds the clue boards.
public static class KitchenSetup
{
    const string ScenePath = "Assets/Scenes/Kitchen.unity";
    const string UiArt = "Assets/Art/UI/";
    const string FoodPreviews = "Assets/ThirdParty/kenney_food-kit/Previews/";
    const int InteractableLayer = 6;
    const int PlaceZoneLayer = 7;

    static readonly Color BoardColor = new Color(0.12f, 0.09f, 0.07f, 0.94f);
    static readonly Color BoardEdge = new Color(0.85f, 0.65f, 0.35f, 1f);
    static readonly Color SlotColor = new Color(1f, 0.96f, 0.88f, 0.12f);

    [MenuItem("Tools/Isombe Kitchen/Apply Scene Setup")]
    static void Apply()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        var log = new StringBuilder();
        Undo.SetCurrentGroupName("Isombe Kitchen setup");

        SetBuildScenes(log);
        KitchenLayout.Build(log);
        RemoveStraySequencePuzzles(log);
        SetupIgniter(log);
        ConvertPrepBowls(log);
        SetupPot(log);
        SetupPrompts(log);
        WireCrosshair(log);
        AddProgressPips(log);
        AddGuidanceUI(log);
        AddStationLights(log);
        BuildClueBoards(log);

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[KitchenSetup]\n" + log);
        EditorUtility.DisplayDialog("Isombe Kitchen", log.ToString(), "OK");
    }

    // ---------- Project ----------

    static void SetBuildScenes(StringBuilder log)
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        log.AppendLine("• Build Settings: Kitchen is the only (first) scene.");
    }

    // ---------- Puzzles ----------

    // The cooking puzzle lives on the Pot. Any other copy (one ended up on the Knob) is removed.
    static void RemoveStraySequencePuzzles(StringBuilder log)
    {
        foreach (SequencePuzzle puzzle in FindAll<SequencePuzzle>())
        {
            if (puzzle.name == "Pot") continue;
            log.AppendLine($"• Removed stray SequencePuzzle from '{puzzle.name}'.");
            Undo.DestroyObjectImmediate(puzzle);
        }
    }

    // Bowl_Oil, Bowl_Onion, ... become prep bowls: carryable pickups that start empty and fill
    // as their ingredient is gathered (or, for the leaves, once pounding is done).
    static void ConvertPrepBowls(StringBuilder log)
    {
        int count = 0;
        foreach (Transform t in FindAll<Transform>().Where(t => t.name.StartsWith("Bowl_")).ToList())
        {
            GameObject bowl = t.gameObject;
            string id = t.name.Substring("Bowl_".Length).ToLowerInvariant();

            // The old click-to-add SequenceItem script was deleted; clear its empty slot.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(bowl);

            var rb = GetOrAdd<Rigidbody>(bowl);
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var pickup = bowl.GetComponent<Pickup>();
            if (pickup == null)
            {
                pickup = Undo.AddComponent<Pickup>(bowl);
                var so = new SerializedObject(pickup);
                so.FindProperty("prompt").stringValue = "Pick up";
                so.ApplyModifiedProperties();
            }
            pickup.itemId = id;

            var prep = GetOrAdd<PrepBowl>(bowl);
            var ps = new SerializedObject(prep);
            ps.FindProperty("fillOnLayer").intValue = id == "leaves" ? 1 : -1;
            ps.ApplyModifiedProperties();

            bowl.layer = InteractableLayer;
            EditorUtility.SetDirty(bowl);
            count++;
        }
        log.AppendLine($"• {count} bowls are now prep bowls: grey until their ingredient is gathered (leaves: after pounding), then carried to the pot.");
    }

    // The pot becomes a drop target: a trigger on the PlaceZone layer that takes bowls in order.
    static void SetupPot(StringBuilder log)
    {
        SequencePuzzle puzzle = FindAll<SequencePuzzle>().FirstOrDefault();
        if (puzzle == null) return;

        GameObject pot = puzzle.gameObject;
        pot.layer = PlaceZoneLayer;

        var box = GetOrAdd<BoxCollider>(pot);
        box.isTrigger = true;
        Bounds b = GetBounds(pot);
        box.center = pot.transform.InverseTransformPoint(b.center);
        Vector3 size = pot.transform.InverseTransformVector(b.size * 1.15f);
        box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

        var so = new SerializedObject(puzzle);
        SetIfEmpty(so, "timer", FindAll<GameTimer>().FirstOrDefault());
        so.ApplyModifiedProperties();
        log.AppendLine("• Pot is now a drop zone: carry the bowls in and it checks the order.");
    }

    // Layer 2 needs an igniter next to the knob. The knob is an invisible hotspot over the stove model's
    // own knob, so the igniter is a matching hotspot beside it, marked with a red button that pulses on its layer.
    static void SetupIgniter(StringBuilder log)
    {
        StoveKnob knob = FindAll<StoveKnob>().FirstOrDefault();
        if (knob == null)
        {
            log.AppendLine("• ⚠ No StoveKnob found, igniter skipped.");
            return;
        }

        Igniter igniter = FindAll<Igniter>().FirstOrDefault();
        if (igniter == null)
        {
            Transform stove = knob.transform.parent;
            var button = new GameObject("Igniter") { layer = InteractableLayer };
            Undo.RegisterCreatedObjectUndo(button, "Create igniter");
            button.transform.SetParent(stove, false);
            button.transform.localRotation = knob.transform.localRotation;

            var knobBox = knob.GetComponent<BoxCollider>();
            var box = button.AddComponent<BoxCollider>();
            box.size = knobBox != null ? knobBox.size : new Vector3(0.15f, 0.15f, 0.1f);

            // Beside the knob, on the side nearer the burner.
            Vector3 local = knob.transform.localPosition;
            Transform flame = FindByName("Flame");
            float dir = flame != null && flame.parent == stove ? Mathf.Sign(flame.localPosition.x - local.x) : -1f;
            button.transform.localPosition = local + new Vector3(dir * box.size.x * 2.5f, 0f, 0f);

            AddButtonMarker(button.transform, box);
            igniter = button.AddComponent<Igniter>();
            log.AppendLine("• Created an Igniter (red button) beside the stove knob. Nudge it in the Scene view if it needs to line up with the model.");
        }

        var so = new SerializedObject(igniter);
        SetIfEmpty(so, "knob", knob);
        SetIfEmpty(so, "timer", FindAll<GameTimer>().FirstOrDefault());
        SetIfEmpty(so, "audioSource", knob.GetComponent<AudioSource>());
        SetIfEmpty(so, "igniteClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ignite Clip.mp3"));
        SetIfEmpty(so, "failClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Fail Clip.mp3"));
        so.ApplyModifiedProperties();
        log.AppendLine("• Igniter wired to knob, timer, audio and clips.");
    }

    // A small red ring-and-dot on the stove front, about 5 cm across in the world.
    static void AddButtonMarker(Transform button, BoxCollider box)
    {
        var go = new GameObject("ButtonMarker", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(LayerClue));
        var rt = (RectTransform)go.transform;
        rt.SetParent(button, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        rt.sizeDelta = new Vector2(100, 100);
        rt.localPosition = new Vector3(0f, 0f, -box.size.z / 2f - 0.01f);
        rt.localScale = Vector3.one * (0.0005f / Mathf.Max(button.lossyScale.x, 0.0001f));

        var so = new SerializedObject(go.GetComponent<LayerClue>());
        so.FindProperty("layerIndex").intValue = 2;
        so.FindProperty("idleAlpha").floatValue = 0.8f;
        so.FindProperty("doneAlpha").floatValue = 0.8f;
        so.ApplyModifiedProperties();

        Img("Ring", rt, LoadSprite(UiArt + "Ring.png"), new Color(1f, 0.3f, 0.2f), Vector2.zero, new Vector2(100, 100));
        Img("Button", rt, LoadSprite(UiArt + "Dot.png"), new Color(0.8f, 0.08f, 0.05f), Vector2.zero, new Vector2(64, 64));
    }

    // Replace generic "Use" prompts with verbs that say what the object does (without solving anything).
    static void SetupPrompts(StringBuilder log)
    {
        SetPrompt(FindAll<StoveKnob>(), "Turn knob");
        SetPrompt(FindAll<Igniter>(), "Press igniter");
        SetPrompt(FindAll<MortarPuzzle>(), "Pound");
        SetPrompt(FindAll<Pickup>(), "Pick up");
        log.AppendLine("• Interaction prompts renamed (Turn knob, Press igniter, Pound, Pick up).");
    }

    static void SetPrompt<T>(IEnumerable<T> items, string prompt) where T : Interactable
    {
        foreach (T item in items)
        {
            var so = new SerializedObject(item);
            SerializedProperty p = so.FindProperty("prompt");
            if (p.stringValue != "Use") continue; // keep anything already customised
            p.stringValue = prompt;
            so.ApplyModifiedProperties();
        }
    }

    // ---------- HUD ----------

    static void WireCrosshair(StringBuilder log)
    {
        Transform crosshair = FindByName("Crosshair");
        PlayerInteractor interactor = FindAll<PlayerInteractor>().FirstOrDefault();
        if (crosshair == null || interactor == null) return;

        var so = new SerializedObject(interactor);
        SetIfEmpty(so, "crosshair", crosshair.GetComponent<Graphic>());
        so.ApplyModifiedProperties();
        log.AppendLine("• Crosshair now reacts when aiming at something usable.");
    }

    static void AddProgressPips(StringBuilder log)
    {
        if (FindAll<ProgressPips>().Any()) return;

        Transform progress = FindByName("ProgressText");
        if (progress == null) return;

        var src = (RectTransform)progress;
        var go = new GameObject("ProgressPips", typeof(RectTransform), typeof(ProgressPips));
        Undo.RegisterCreatedObjectUndo(go, "Create progress pips");
        var rt = (RectTransform)go.transform;
        rt.SetParent(src.parent, false);
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.sizeDelta = new Vector2(src.sizeDelta.x, 24f);
        rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, src.pivot.y > 0.5f ? -src.rect.height - 6f : -30f);
        rt.SetSiblingIndex(src.GetSiblingIndex() + 1);

        var so = new SerializedObject(go.GetComponent<ProgressPips>());
        so.FindProperty("dotSprite").objectReferenceValue = LoadSprite(UiArt + "Dot.png");
        so.FindProperty("ringSprite").objectReferenceValue = LoadSprite(UiArt + "Ring.png");
        so.ApplyModifiedProperties();
        log.AppendLine("• Added progress pips under the progress text.");
    }

    // Intro card (story, goal, controls) and a goal line under the progress dots that names
    // the current step without saying how to solve it.
    static void AddGuidanceUI(StringBuilder log)
    {
        PuzzleManager pm = FindAll<PuzzleManager>().FirstOrDefault();
        Transform progress = FindByName("ProgressText");
        if (pm == null || progress == null) return;

        var style = progress.GetComponent<TMP_Text>();
        var hud = (RectTransform)progress.parent;
        var so = new SerializedObject(pm);

        if (so.FindProperty("objectiveText").objectReferenceValue == null)
        {
            var above = (RectTransform)(FindByName("ProgressPips") ?? progress);
            TMP_Text goal = Text("ObjectiveText", hud, style, "", style.fontSize * 0.75f, new Color(1f, 0.85f, 0.55f), Vector2.zero, new Vector2(700, 40));
            var rt = goal.rectTransform;
            rt.anchorMin = above.anchorMin;
            rt.anchorMax = above.anchorMax;
            rt.pivot = above.pivot;
            rt.anchoredPosition = above.anchoredPosition + new Vector2(0f, -above.rect.height - 8f);
            goal.alignment = style.alignment;
            goal.fontStyle = FontStyles.Italic;
            so.FindProperty("objectiveText").objectReferenceValue = goal;
            log.AppendLine("• Added a goal line under the progress (e.g. \"Fire up the stove\").");
        }

        if (so.FindProperty("introPanel").objectReferenceValue == null)
        {
            Transform canvas = progress.GetComponentInParent<Canvas>().rootCanvas.transform;
            var panel = new GameObject("IntroPanel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panel, "Create intro");
            var prt = (RectTransform)panel.transform;
            prt.SetParent(canvas, false);
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.03f, 0.02f, 0.02f, 0.9f);

            Img("Edge", prt, LoadSprite(UiArt + "Card.png"), BoardEdge, Vector2.zero, new Vector2(830, 530), true);
            Img("Card", prt, LoadSprite(UiArt + "Card.png"), BoardColor, Vector2.zero, new Vector2(820, 520), true);

            Text("Title", prt, style, "ISOMBE", 76, BoardEdge, new Vector2(0, 185), new Vector2(760, 90)).fontStyle = FontStyles.Bold;
            Text("Subtitle", prt, style, "a kitchen puzzle", 26, new Color(1f, 1f, 1f, 0.6f), new Vector2(0, 125), new Vector2(760, 40)).fontStyle = FontStyles.Italic;
            Text("Story", prt, style,
                "Your guests arrive in <b>6 minutes</b> and the isombe isn't started.\n" +
                "Cook it one step at a time. The kitchen lights up where you're needed,\n" +
                "and the walls hold the clues.\n\n" +
                "Wrong moves cost time. If the clock runs out, dinner burns.",
                26, Color.white, new Vector2(0, 10), new Vector2(740, 190));
            Text("Controls", prt, style, "<b>WASD</b> move     <b>Mouse</b> look     <b>E / Click</b> interact",
                22, new Color(1f, 1f, 1f, 0.7f), new Vector2(0, -135), new Vector2(760, 40));
            Text("Start", prt, style, "Press E to start", 30, new Color(1f, 0.85f, 0.55f), new Vector2(0, -205), new Vector2(760, 50)).fontStyle = FontStyles.Bold;

            prt.SetAsLastSibling();
            so.FindProperty("introPanel").objectReferenceValue = panel;
            log.AppendLine("• Added an intro card: story, goal and controls. Press E to start.");
        }

        so.ApplyModifiedProperties();
    }

    // The lights above each station guide the player: current station bright and pulsing, others dim.
    static void AddStationLights(StringBuilder log)
    {
        var stations = new (string light, int[] layers)[]
        {
            ("Light_Counter", new[] { 0, 3 }), // gather here; the prep bowls are here too
            ("Light_Mortar", new[] { 1 }),
            ("Light_Stove", new[] { 2, 3 }),
            ("Light_Tray", new[] { 4 }),
        };

        int count = 0;
        foreach (var (lightName, layers) in stations)
        {
            Transform t = FindByName(lightName);
            if (t == null || t.GetComponent<Light>() == null) continue;

            var so = new SerializedObject(GetOrAdd<StationLight>(t.gameObject));
            SerializedProperty arr = so.FindProperty("layers");
            arr.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++) arr.GetArrayElementAtIndex(i).intValue = layers[i];
            so.ApplyModifiedProperties();
            count++;
        }
        log.AppendLine($"• {count} station lights now point the way: the current station glows and pulses, the rest dim.");
    }

    static TMP_Text Text(string name, RectTransform parent, TMP_Text style, string text, float size, Color color, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = style.font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    // ---------- Clues ----------

    // Three diegetic boards on the walls, each tied to a layer through LayerClue:
    //   Layer 0  market board above the counter: pictures of the 4 real ingredients (the banana is not there)
    //   Layer 2  blue flame icon above the stove: the colour the knob must show before igniting
    //   Layer 3  coloured dots with arrows above the bowls: the order the bowls go into the pot
    static void BuildClueBoards(StringBuilder log)
    {
        Transform old = FindByName("Clues");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var root = new GameObject("Clues");
        Undo.RegisterCreatedObjectUndo(root, "Create clues");
        Walls walls = new Walls(FindByName("Room"));

        Transform counter = FindByName("CounterZone") ?? FindByName("Counter");
        if (counter != null)
        {
            string[] pictures = { "onion", "cabbage", "eggplant", "bottle-oil" }; // shuffled so it does not give away the cooking order
            var board = Board("Clue_MarketBoard", root.transform, 0, new Vector2(300, 300));
            if (!AtAnchor(board, "Anchor_MarketBoard")) walls.Place(board, counter.position, 1.75f);
            for (int i = 0; i < pictures.Length; i++)
            {
                Vector2 pos = new Vector2(i % 2 == 0 ? -65 : 65, i < 2 ? 65 : -65);
                Img("Slot", board, LoadSprite(UiArt + "Card.png"), SlotColor, pos, new Vector2(115, 115), true);
                Img(pictures[i], board, LoadSprite(FoodPreviews + pictures[i] + ".png"), Color.white, pos, new Vector2(105, 105));
            }
            log.AppendLine("• Clue (Layer 0): market board with the 4 ingredients above the counter.");
        }

        Transform stove = FindAll<StoveKnob>().FirstOrDefault()?.transform.parent;
        if (stove != null)
        {
            var board = Board("Clue_BlueFlame", root.transform, 2, new Vector2(170, 210));
            if (!AtAnchor(board, "Anchor_BlueFlame")) walls.Place(board, stove.position, 1.8f);
            Img("Glow", board, LoadSprite(UiArt + "Dot.png"), new Color(0.2f, 0.45f, 1f, 0.25f), new Vector2(0, -5), new Vector2(150, 150));
            Img("Flame", board, LoadSprite(UiArt + "Flame.png"), new Color(0.25f, 0.55f, 1f), new Vector2(0, 5), new Vector2(120, 150));
            log.AppendLine("• Clue (Layer 2): blue flame icon above the stove.");
        }

        List<PrepBowl> bowls = FindAll<PrepBowl>();
        SequencePuzzle puzzle = FindAll<SequencePuzzle>().FirstOrDefault();
        if (puzzle != null && bowls.Count > 0)
        {
            SerializedProperty order = new SerializedObject(puzzle).FindProperty("correctOrder");
            int n = order.arraySize;
            float step = 110f;
            var board = Board("Clue_PotOrder", root.transform, 3, new Vector2(n * step + 40, 150));

            Vector3 centre = bowls.Aggregate(Vector3.zero, (sum, b) => sum + b.transform.position) / bowls.Count;
            if (!AtAnchor(board, "Anchor_PotOrder")) walls.Place(board, centre, 1.75f);

            for (int i = 0; i < n; i++)
            {
                string id = order.GetArrayElementAtIndex(i).stringValue;
                PrepBowl bowl = bowls.FirstOrDefault(b => b.GetComponent<Pickup>().itemId == id);
                float x = -((n - 1) * step) / 2f + i * step;

                Img("Ring", board, LoadSprite(UiArt + "Ring.png"), new Color(1f, 1f, 1f, 0.35f), new Vector2(x, 0), new Vector2(76, 76));
                Img(id, board, LoadSprite(UiArt + "Dot.png"), bowl != null ? BowlColour(bowl) : Color.white, new Vector2(x, 0), new Vector2(60, 60));
                if (i < n - 1)
                    Img("Arrow", board, LoadSprite(UiArt + "Chevron.png"), new Color(1f, 1f, 1f, 0.6f), new Vector2(x + step / 2f, 0), new Vector2(26, 26));
            }
            log.AppendLine("• Clue (Layer 3): coloured dots + arrows above the bowls showing the pot order.");
        }
    }

    // Puts a board on an anchor made by KitchenLayout (e.g. on the fridge door). Returns false if there is none.
    static bool AtAnchor(RectTransform board, string anchorName)
    {
        Transform anchor = FindByName(anchorName);
        if (anchor == null) return false;
        board.SetParent(anchor.parent, true);
        board.SetPositionAndRotation(anchor.position, anchor.rotation);
        board.localScale = Vector3.one * 0.001f / Mathf.Max(anchor.parent.lossyScale.x, 0.0001f);
        return true;
    }

    static RectTransform Board(string name, Transform parent, int layerIndex, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(LayerClue));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one * 0.001f; // 1 UI pixel = 1 mm

        var so = new SerializedObject(go.GetComponent<LayerClue>());
        so.FindProperty("layerIndex").intValue = layerIndex;
        so.ApplyModifiedProperties();

        Img("Edge", rt, LoadSprite(UiArt + "Card.png"), BoardEdge, Vector2.zero, size + new Vector2(10, 10), true);
        Img("Board", rt, LoadSprite(UiArt + "Card.png"), BoardColor, Vector2.zero, size, true);
        return rt;
    }

    static Image Img(string name, RectTransform parent, Sprite sprite, Color color, Vector2 pos, Vector2 size, bool sliced = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = !sliced;
        if (sliced) img.type = Image.Type.Sliced;
        return img;
    }

    static Color BowlColour(Component bowl)
    {
        foreach (Renderer r in bowl.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || !m.name.StartsWith("M_Bowl")) continue;
                Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                c.a = 1f;
                return c;
            }
        }
        return Color.white;
    }

    // Finds the wall nearest a point and hangs a board on its inner face, facing into the room.
    class Walls
    {
        readonly List<Bounds> bounds = new();
        readonly Vector3 centre;

        public Walls(Transform room)
        {
            if (room == null) return;
            centre = room.position;
            foreach (Renderer r in room.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith("Wall")) bounds.Add(r.bounds);
        }

        public void Place(RectTransform board, Vector3 near, float height)
        {
            Undo.RecordObject(board, "Place clue");
            if (bounds.Count == 0)
            {
                board.position = new Vector3(near.x, height, near.z);
                return;
            }

            Bounds wall = bounds.OrderBy(b => (b.ClosestPoint(near) - near).sqrMagnitude).First();
            bool alongX = wall.size.x > wall.size.z; // wall runs along X, so it faces +Z or -Z
            Vector3 inward = alongX
                ? new Vector3(0, 0, Mathf.Sign(centre.z - wall.center.z))
                : new Vector3(Mathf.Sign(centre.x - wall.center.x), 0, 0);

            Vector3 face = wall.center + Vector3.Scale(wall.extents, inward);
            Vector3 pos = alongX
                ? new Vector3(Mathf.Clamp(near.x, wall.min.x + 0.3f, wall.max.x - 0.3f), height, face.z)
                : new Vector3(face.x, height, Mathf.Clamp(near.z, wall.min.z + 0.3f, wall.max.z - 0.3f));

            board.position = pos + inward * 0.02f;
            board.rotation = Quaternion.LookRotation(-inward);
        }
    }

    // ---------- Helpers ----------

    static Sprite LoadSprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter imp && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            if (path.EndsWith("Card.png")) imp.spriteBorder = new Vector4(30, 30, 30, 30);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void SetIfEmpty(SerializedObject so, string property, Object value)
    {
        SerializedProperty p = so.FindProperty(property);
        if (p != null && p.objectReferenceValue == null) p.objectReferenceValue = value;
    }

    static List<T> FindAll<T>() where T : Component
    {
        var list = new List<T>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<T>(true));
        return list;
    }

    static Transform FindByName(string name) => FindAll<Transform>().FirstOrDefault(t => t.name == name);

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    // World bounds of an object's meshes (particles and UI ignored).
    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>()
            .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.2f);

        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
