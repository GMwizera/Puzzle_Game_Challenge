using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the kitchen from the Boxx-Games "Free Kitchen" pack and places every puzzle station in it.
// Called by KitchenSetup before it wires the puzzles. Running it again rebuilds the environment
// and re-places the stations, so it is safe to repeat.
//
// Floor plan (metres, room centred on the origin, looking at the back wall):
//   back wall  z = -1.57 : sink cabinet | STOVE (knob, igniter, pot) | PREP COUNTER (ingredients + bowls) | tall cabinet
//   right wall x = +2.70 : window, FRIDGE (hides ingredients)
//   centre               : kitchen table with the MORTAR
//   left                 : SERVING TABLE with the tray, a clear walk from the DOOR to the dining room (z = +1.58)
public static class KitchenLayout
{
    const string Pack = "Assets/Kitchen/FreeKitchen/Prefabs/Version01/";
    const string Kenney = "Assets/ThirdParty/kenney_building-kit/Models/OBJ format/";
    const string FlameMaterialPath = "Assets/Materials/M_FlameParticle.mat";
    const string UiArt = "Assets/Art/UI/";
    const int DefaultMask = 1 << 0;
    const int InteractableLayer = 6;

    // Order of the counter slots (and the prep bowls behind them), left to right.
    static readonly string[] CounterOrder = { "leaves", "onion", "eggplant", "oil" };

    public static void Build(StringBuilder log)
    {
        Transform stations = Root("Stations");
        RescueGameplayObjects(stations);
        DeleteOldEnvironment();

        Transform env = BuildEnvironment();
        Physics.SyncTransforms();

        PlaceStations(env, stations);
        PlaceLights();
        PlacePlayer();
        Lighting();
        Physics.SyncTransforms();

        log.AppendLine("• Kitchen rebuilt from the Free Kitchen pack: closed room with window, door and a dining room.");
        log.AppendLine("• Stations placed: prep counter, stove (real knobs), fridge, mortar table, serving table.");
    }

    // ---------- Environment ----------

    // Move puzzle objects out of the old furniture before it is deleted.
    static void RescueGameplayObjects(Transform stations)
    {
        string[] names = { "Hinge", "CounterZone", "TrayZone", "Tray", "mortar", "Knob", "Igniter", "FlameLight", "Pot", "Bowls", "Plate" };
        foreach (string n in names)
        {
            Transform t = Find(n);
            if (t != null && t.parent != stations) Undo.SetTransformParent(t, stations, "Rescue " + n);
        }
    }

    static void DeleteOldEnvironment()
    {
        string[] roots = { "Environment", "Room", "Furniture", "Vest_Wall_Back", "Vest_Wall_Left", "Vest_Wall_Right", "Clues", "FlameTarget" };
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (roots.Contains(go.name)) Undo.DestroyObjectImmediate(go);
    }

    static Transform BuildEnvironment()
    {
        var env = new GameObject("Environment").transform;
        Undo.RegisterCreatedObjectUndo(env.gameObject, "Build kitchen");

        GameObject room = Spawn(Pack + "FreeRoom_Version01.prefab", env, Vector3.zero, 0f);
        room.name = "Kitchen";
        PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // Free the prep counter, and give the room shell collision (the pack ships it without).
        DestroyNamed(room.transform, "FreeToaster");
        AddShellColliders(room.transform);
        foreach (Light l in room.GetComponentsInChildren<Light>()) l.intensity *= 0.6f;

        // Kitchen table set moves right to make room for the serving table and the door.
        var shift = new Vector3(0.75f, 0f, 0f);
        var moving = room.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name is "FreeTable" or "FreeCarpet" or "FreeLampPart" or "FreeLamp" or "Point Light" or "FreeChair").ToList();
        // Only move top-most matches, so children of a moved object are not shifted twice.
        foreach (Transform t in moving.Where(t => !moving.Any(o => o != t && t.IsChildOf(o))))
            t.position += shift;

        // Two chairs would block the counter and the fridge.
        foreach (Transform chair in moving.Where(t => t.name == "FreeChair" && (t.position.z < 0f || t.position.x > 1f)).ToList())
            Object.DestroyImmediate(chair.gameObject);

        BuildWalls(env);
        BuildDiningRoom(env);

        Spawn(Pack + "FreeTable.prefab", env, new Vector3(-1.94f, 0f, 0f), 0f).name = "ServingTable";
        return env;
    }

    // The pack's room only has two walls. The window wall and the door wall use Kenney pieces (2.4 m tall, 2 m wide).
    static void BuildWalls(Transform env)
    {
        var walls = new GameObject("Walls").transform;
        walls.SetParent(env, false);

        Piece("wall-window-square", walls, new Vector3(2.65f, 0f, -0.93f), 0f, 1f);
        Piece("wall", walls, new Vector3(2.70f, 0f, 0.825f), 0f, 0.755f);

        Piece("wall-doorway-square", walls, new Vector3(-1.75f, 0f, 1.48f), 90f, 1f);
        Piece("wall", walls, new Vector3(0.25f, 0f, 1.53f), 90f, 1f);
        Piece("wall", walls, new Vector3(2.00f, 0f, 1.53f), 90f, 0.75f);
    }

    // The dining room behind the door is a second copy of the pack's room, turned around so its two
    // tiled walls close the far side. Only its table set is kept: the payoff the player walks into to win.
    static void BuildDiningRoom(Transform env)
    {
        GameObject shell = Spawn(Pack + "FreeRoom_Version01.prefab", env, new Vector3(0f, 0f, 3.16f), 180f);
        shell.name = "DiningRoom";
        PrefabUtility.UnpackPrefabInstance(shell, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        string[] keep = { "FreeRoom", "FreeRoomCeiling", "FreeTable", "FreeChair", "FreeCarpet", "FreeLampPart", "FreeLamp", "Point Light" };
        foreach (Transform child in shell.transform.Cast<Transform>().ToList())
            if (!keep.Contains(child.name)) Object.DestroyImmediate(child.gameObject);
        AddShellColliders(shell.transform);

        // The open side of the dining room.
        Piece("wall", shell.transform, new Vector3(-2.70f, 0f, 2.58f), 0f, 1f);
        Piece("wall", shell.transform, new Vector3(-2.70f, 0f, 4.335f), 0f, 0.755f);

        // Dinner is laid out for the guests.
        Physics.SyncTransforms();
        Transform table = shell.transform.Cast<Transform>().FirstOrDefault(c => c.name == "FreeTable");
        if (table == null) return;
        Vector3 c0 = MeshBounds(table.gameObject).center;
        float top = SurfaceY(c0.x, c0.z, 1.5f, 0.76f);
        Spawn(Pack + "FreePlate.prefab", shell.transform, new Vector3(c0.x - 0.2f, top, c0.z), 0f);
        Spawn(Pack + "FreePlate.prefab", shell.transform, new Vector3(c0.x + 0.2f, top, c0.z), 0f);
        Spawn(Pack + "FreeCup.prefab", shell.transform, new Vector3(c0.x, top, c0.z + 0.22f), 30f);
        Spawn(Pack + "FreeVase.prefab", shell.transform, new Vector3(c0.x, top, c0.z - 0.2f), 0f);
    }

    static void AddShellColliders(Transform room)
    {
        foreach (Transform t in room.Cast<Transform>().Where(t => t.name is "FreeRoom" or "FreeRoomCeiling"))
            if (t.GetComponent<Collider>() == null) t.gameObject.AddComponent<MeshCollider>();
    }

    // ---------- Stations ----------

    static void PlaceStations(Transform env, Transform stations)
    {
        Transform room = env.Find("Kitchen");

        // Prep counter (Layer 0) with the prep bowls behind the ingredient spots (Layer 3).
        float counterTop = SurfaceY(0.41f, -1.1f, 1.2f, 0.88f);
        float[] slotX = { 0.0f, 0.27f, 0.54f, 0.81f };
        Transform counterZone = Find("CounterZone");
        if (counterZone != null)
        {
            Reset(counterZone, stations, new Vector3(0.41f, counterTop, -1.07f));
            var box = counterZone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.1f, 0f);
            box.size = new Vector3(1.15f, 0.2f, 0.3f);

            var zone = new SerializedObject(counterZone.GetComponent<PlacementZone>());
            SerializedProperty ids = zone.FindProperty("acceptedIds");
            SerializedProperty slots = zone.FindProperty("slots");
            ids.arraySize = CounterOrder.Length;
            for (int i = 0; i < CounterOrder.Length; i++)
            {
                ids.GetArrayElementAtIndex(i).stringValue = CounterOrder[i];
                var slot = slots.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (slot == null) continue;
                slot.position = new Vector3(slotX[i], counterTop, -1.07f);

                // Coloured ring (matching the bowl behind it) shows where each ingredient goes.
                foreach (Transform child in slot.Cast<Transform>().ToList()) Object.DestroyImmediate(child.gameObject);
                Transform bowl = Find("Bowl_" + char.ToUpper(CounterOrder[i][0]) + CounterOrder[i].Substring(1));
                Decal("SlotMarker", slot, slot.position + Vector3.up * 0.003f, Quaternion.Euler(90f, 0f, 0f), 0.13f, "Ring.png",
                    bowl != null ? BowlColour(bowl) * new Color(1f, 1f, 1f, 0.8f) : new Color(1f, 1f, 1f, 0.5f));
            }
            zone.ApplyModifiedProperties();
        }

        Transform bowls = Find("Bowls");
        if (bowls != null)
            foreach (Transform bowl in bowls)
            {
                int i = System.Array.IndexOf(CounterOrder, bowl.name.Replace("Bowl_", "").ToLowerInvariant());
                if (i >= 0) PlaceOn(bowl, new Vector3(slotX[i], counterTop, -1.40f));
            }

        // Stove (Layer 2): the knob and igniter sit on two of the stove's own knobs.
        Renderer stove = room.GetComponentsInChildren<Renderer>().First(r => r.name == "FreeStove");
        Bounds sb = stove.bounds;
        float knobRow = sb.center.x; // middle knob; the outer two are 16.7 cm either side
        float front = sb.max.z + 0.02f;
        const float knobY = 0.768f;
        Transform knob = Find("Knob");
        Transform igniter = Find("Igniter");
        if (knob != null) Hotspot(knob, stations, new Vector3(knobRow - 0.167f, knobY, front));
        if (igniter != null)
        {
            Hotspot(igniter, stations, new Vector3(knobRow + 0.167f, knobY, front));

            // A red ring around the stove's real knob marks it as the igniter; it pulses on the stove layer.
            Transform old = igniter.Find("ButtonMarker");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform ring = Decal("ButtonMarker", igniter, new Vector3(igniter.position.x, knobY, sb.max.z + 0.004f),
                Quaternion.LookRotation(Vector3.back), 0.075f, "Ring.png", new Color(1f, 0.25f, 0.15f));
            var clue = new SerializedObject(ring.gameObject.AddComponent<LayerClue>());
            clue.FindProperty("layerIndex").intValue = 2;
            clue.FindProperty("idleAlpha").floatValue = 0.85f;
            clue.FindProperty("doneAlpha").floatValue = 0.85f;
            clue.ApplyModifiedProperties();
        }

        // Front-left burner, above the gas knob: pot, flame and flame light.
        var burner = new Vector3(knobRow - 0.19f, 0f, sb.center.z + 0.13f);
        burner.y = SurfaceY(burner.x, burner.z, 1.15f, sb.max.y);
        Transform pot = SwapPot(stations);
        if (pot != null) PlaceOn(pot, burner);
        SetupFlame(stations, burner + Vector3.up * 0.01f);

        // Fridge (Layer 0 exploration): the big door opens and hides two ingredients.
        Transform fridgeDoor = room.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "FreeRefrigerator_DoorBig");
        Renderer fridge = room.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name == "FreeRefrigerator");
        float fridgeFloor = 0.71f;
        if (fridge != null)
        {
            Bounds fb = fridge.bounds;
            float y = SurfaceY(fb.center.x + 0.1f, fb.center.z, 1.5f, -1f);
            if (y > 0.4f && y < 1.4f) fridgeFloor = y;
        }
        if (fridgeDoor != null) SetupFridgeDoor(fridgeDoor);

        // Ingredients: fridge, spice shelf and kitchen table.
        float tableTop = SurfaceY(0.55f, 0.5f, 1.5f, 0.76f);
        float shelfTop = SurfaceY(-1.35f, -1.46f, 1.8f, 1.37f);
        Ingredient("onion", 0.45f, new Vector3(2.0f, fridgeFloor, 0.28f));
        Ingredient("leaves", 0.6f, new Vector3(2.1f, fridgeFloor, 0.6f));
        Ingredient("oil", 0.45f, new Vector3(-1.35f, shelfTop, -1.46f));
        Ingredient("eggplant", 0.55f, new Vector3(0.28f, tableTop, 0.75f));
        Ingredient("banana", 0.33f, new Vector3(0.82f, tableTop, 0.28f), 70f);

        // Mortar (Layer 1) on the kitchen table.
        Transform mortar = Find("mortar");
        if (mortar != null)
        {
            Undo.SetTransformParent(mortar, stations, "Mortar");
            mortar.rotation = Quaternion.identity;
            mortar.localScale = mortar.localScale / mortar.lossyScale.x * 0.8f;
            PlaceOn(mortar, new Vector3(0.55f, tableTop, 0.5f));
        }

        // Serving table (Layer 4) with the tray, beside the door.
        float serveTop = SurfaceY(-1.94f, 0f, 1.5f, 0.76f);
        Transform tray = Find("Tray");
        if (tray != null)
        {
            tray.rotation = Quaternion.identity;
            tray.localScale = tray.localScale / tray.lossyScale.x * 0.5f;
            PlaceOn(tray, new Vector3(-1.94f, serveTop, 0f));
        }
        Transform trayZone = Find("TrayZone");
        if (trayZone != null)
        {
            Reset(trayZone, stations, new Vector3(-1.94f, serveTop, 0f));
            var box = trayZone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.12f, 0f);
            box.size = new Vector3(0.5f, 0.25f, 0.5f);
            foreach (Transform slot in trayZone) slot.position = new Vector3(-1.94f, serveTop + 0.02f, 0f);
        }

        // The finished dish only appears beside the stove once the cooking layer is solved.
        Transform dish = Find("Plate");
        if (dish != null)
        {
            dish.gameObject.SetActive(true);
            dish.rotation = Quaternion.identity;
            dish.localScale = dish.localScale / dish.lossyScale.x * 0.45f;
            var rb = dish.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            PlaceOn(dish, new Vector3(-1.29f, SurfaceY(-1.29f, -1.2f, 1.15f, 0.88f), -1.2f));
            if (dish.GetComponent<RevealOnLayer>() == null) Undo.AddComponent<RevealOnLayer>(dish.gameObject);
        }

        // Door to the dining room, hinged on the left of the doorway and opening outwards.
        Transform hinge = Find("Hinge");
        if (hinge != null)
        {
            hinge.SetParent(stations, true);
            hinge.SetPositionAndRotation(new Vector3(-2.215f, 0f, 1.48f), Quaternion.Euler(0f, 90f, 0f));
            hinge.localScale = Vector3.one;
            foreach (Transform leaf in hinge)
            {
                leaf.localPosition = Vector3.zero;
                leaf.localRotation = Quaternion.identity;
                leaf.localScale = Vector3.one;
            }
            var door = new SerializedObject(hinge.GetComponent<Door>());
            door.FindProperty("openAngle").floatValue = -100f;
            door.ApplyModifiedProperties();
        }

        Transform exit = Find("ExitTrigger");
        if (exit != null)
        {
            exit.SetPositionAndRotation(new Vector3(-1.75f, 1f, 2.2f), Quaternion.identity);
            var box = exit.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(1.2f, 2f, 0.6f);
        }

        // Anchors for the clue boards (used by KitchenSetup.BuildClueBoards).
        if (fridgeDoor != null)
        {
            Bounds db = MeshBounds(fridgeDoor.gameObject);
            Anchor("Anchor_MarketBoard", fridgeDoor, new Vector3(db.min.x - 0.012f, 1.38f, db.center.z), Quaternion.LookRotation(Vector3.right));
        }
        WallAnchor("Anchor_BlueFlame", env, new Vector3(knobRow + 0.19f, 1.05f, 0f));
        WallAnchor("Anchor_PotOrder", env, new Vector3(0.41f, 1.14f, 0f));
    }

    // Replaces the old Kenney pan with the pack's big pot, keeping the puzzle and its settings.
    static Transform SwapPot(Transform stations)
    {
        Transform old = Find("Pot");
        if (old == null) return null;
        if (old.GetComponentsInChildren<MeshFilter>().Any(m => m.sharedMesh != null && m.sharedMesh.name.Contains("PotBig"))) return old;

        GameObject pot = Spawn(Pack + "FreePotBig.prefab", stations, old.position, 0f);
        PrefabUtility.UnpackPrefabInstance(pot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        DestroyNamed(pot.transform, "FreePotBigLid");

        var puzzle = Undo.AddComponent<SequencePuzzle>(pot);
        EditorUtility.CopySerialized(old.GetComponent<SequencePuzzle>(), puzzle);
        var audio = Undo.AddComponent<AudioSource>(pot);
        EditorUtility.CopySerialized(old.GetComponent<AudioSource>(), audio);

        var so = new SerializedObject(puzzle);
        so.FindProperty("audioSource").objectReferenceValue = audio;
        so.ApplyModifiedProperties();

        Transform smoke = old.Find("Smoke");
        if (smoke != null)
        {
            Undo.SetTransformParent(smoke, pot.transform, "Smoke");
            smoke.localPosition = new Vector3(0f, 0.18f, 0f);
            smoke.localScale = Vector3.one;
        }

        Undo.DestroyObjectImmediate(old.gameObject);
        pot.name = "Pot";
        return pot.transform;
    }

    // A small particle flame whose colour the StoveKnob changes (replaces the placeholder sphere).
    static void SetupFlame(Transform stations, Vector3 at)
    {
        StoveKnob knob = Object.FindObjectsByType<StoveKnob>(FindObjectsInactive.Include).FirstOrDefault();
        if (knob == null) return;
        var so = new SerializedObject(knob);

        var oldRenderer = so.FindProperty("flameRenderer").objectReferenceValue as Renderer;
        if (oldRenderer != null && oldRenderer.GetComponent<ParticleSystem>() == null) Undo.DestroyObjectImmediate(oldRenderer.gameObject);

        Transform flame = Find("BurnerFlame");
        if (flame == null || flame.GetComponent<ParticleSystem>() == null)
        {
            var go = new GameObject("BurnerFlame", typeof(ParticleSystem));
            Undo.RegisterCreatedObjectUndo(go, "Flame");
            flame = go.transform;
            ConfigureFlame(go.GetComponent<ParticleSystem>());
        }
        flame.SetParent(stations, true);
        flame.SetPositionAndRotation(at, Quaternion.Euler(-90f, 0f, 0f));
        so.FindProperty("flameRenderer").objectReferenceValue = flame.GetComponent<ParticleSystemRenderer>();

        Transform light = Find("FlameLight");
        if (light != null)
        {
            light.SetParent(stations, true);
            light.position = at + Vector3.up * 0.12f;
            light.localScale = Vector3.one;
            light.gameObject.SetActive(true);
            so.FindProperty("flameLight").objectReferenceValue = light.GetComponent<Light>();
        }
        so.ApplyModifiedProperties();
    }

    static void ConfigureFlame(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.rateOverTime = 90f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = 0.085f;
        shape.radiusThickness = 0.15f; // a ring of flame, like a gas burner

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = FlameMaterial();
    }

    static Material FlameMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(FlameMaterialPath);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/Dot.png"));
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 2f); // additive
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.One);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(mat, FlameMaterialPath);
        return mat;
    }

    static void SetupFridgeDoor(Transform door)
    {
        foreach (Transform t in door.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = InteractableLayer;
        if (door.GetComponentInChildren<Collider>() == null)
        {
            var box = Undo.AddComponent<BoxCollider>(door.gameObject);
            Bounds b = MeshBounds(door.gameObject);
            box.center = door.InverseTransformPoint(b.center);
            Vector3 size = door.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        var hinge = door.GetComponent<Door>();
        if (hinge == null) hinge = Undo.AddComponent<Door>(door.gameObject);
        var ds = new SerializedObject(hinge);
        ds.FindProperty("openAngle").floatValue = -100f;
        ds.ApplyModifiedProperties();

        var use = door.GetComponent<OpenOnUse>();
        if (use == null) use = Undo.AddComponent<OpenOnUse>(door.gameObject);
        var us = new SerializedObject(use);
        us.FindProperty("door").objectReferenceValue = hinge;
        us.FindProperty("prompt").stringValue = "Open fridge";
        us.ApplyModifiedProperties();
    }

    static void Ingredient(string name, float scale, Vector3 at, float yaw = 0f)
    {
        Transform t = Find("Ingredients")?.Find(name);
        if (t == null) return;

        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        t.localScale = Vector3.one * scale;
        var rb = t.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; // rests exactly where placed until picked up
        PlaceOn(t, at);
    }

    // ---------- Lights, player, lighting ----------

    static void PlaceLights()
    {
        SetLight("Light_Counter", new Vector3(0.41f, 2.05f, -0.85f), 2.2f);
        SetLight("Light_Stove", new Vector3(-0.66f, 2.0f, -0.7f), 2.2f);
        SetLight("Light_Mortar", new Vector3(0.55f, 1.95f, 0.5f), 2.2f);
        SetLight("Light_Tray", new Vector3(-1.94f, 1.95f, 0f), 2.2f);
    }

    static void SetLight(string name, Vector3 at, float range)
    {
        Transform t = Find(name);
        if (t == null) return;
        t.position = at;
        var light = t.GetComponent<Light>();
        light.range = range;
        light.intensity = 1.2f; // StationLight boosts the active one; glossy tiles blow out above this
        light.color = new Color(1f, 0.9f, 0.75f);
    }

    static void PlacePlayer()
    {
        Transform player = Find("Player");
        if (player == null) return;
        var at = new Vector3(-0.9f, 1.0f, 0.95f);
        Vector3 look = new Vector3(-0.1f, 0f, -1.2f) - new Vector3(at.x, 0f, at.z);
        player.SetPositionAndRotation(at, Quaternion.LookRotation(look));
    }

    static void Lighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.39f, 0.36f);

        // Daylight comes in through the window on the right wall.
        Transform sun = Find("Directional Light");
        if (sun != null)
        {
            sun.rotation = Quaternion.Euler(35f, -100f, 0f);
            var light = sun.GetComponent<Light>();
            light.shadows = LightShadows.None; // interior scene: shadowed sunlight only produced artefacts
            light.intensity = 0.8f;
        }
    }

    // ---------- Helpers ----------

    static GameObject Spawn(string path, Transform parent, Vector3 position, float yaw)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return go;
    }

    // Kenney building piece, scaled along its length, with a mesh collider so the player can't walk through.
    static GameObject Piece(string model, Transform parent, Vector3 position, float yaw, float lengthScale)
    {
        GameObject go = Spawn(Kenney + model + ".obj", parent, position, yaw);
        go.transform.localScale = new Vector3(1f, 1f, lengthScale);
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            if (mf.GetComponent<Collider>() == null) mf.gameObject.AddComponent<MeshCollider>();
        return go;
    }

    static Transform Root(string name)
    {
        GameObject go = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }
        return go.transform;
    }

    static Transform Find(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name) return root.transform;
            Transform t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(c => c.name == name);
            if (t != null) return t;
        }
        return null;
    }

    static void DestroyNamed(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToList())
            Object.DestroyImmediate(t.gameObject);
    }

    static void Reset(Transform t, Transform parent, Vector3 position)
    {
        Undo.SetTransformParent(t, parent, "Place " + t.name);
        t.SetPositionAndRotation(position, Quaternion.identity);
        t.localScale = Vector3.one;
    }

    // Invisible hotspot over one of the stove's real knobs, facing the player.
    static void Hotspot(Transform t, Transform parent, Vector3 position)
    {
        Reset(t, parent, position);
        t.rotation = Quaternion.Euler(0f, 180f, 0f);
        var box = t.GetComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = new Vector3(0.08f, 0.08f, 0.07f);
    }

    // Sits an object on a surface: bottom of its meshes at point.y, centred on point.x/z.
    static void PlaceOn(Transform t, Vector3 point)
    {
        Bounds b = MeshBounds(t.gameObject);
        t.position += new Vector3(point.x - b.center.x, point.y - b.min.y + 0.002f, point.z - b.center.z);
    }

    static float SurfaceY(float x, float z, float fromY, float fallback)
    {
        return Physics.Raycast(new Vector3(x, fromY, z), Vector3.down, out RaycastHit hit, fromY + 1f, DefaultMask, QueryTriggerInteraction.Ignore)
            ? hit.point.y : fallback;
    }

    static void Anchor(string name, Transform parent, Vector3 position, Quaternion rotation)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(position, rotation);
    }

    // Finds the back wall behind a point and puts an anchor on it, facing into the room.
    static void WallAnchor(string name, Transform env, Vector3 from)
    {
        if (Physics.Raycast(from, Vector3.back, out RaycastHit hit, 3f, DefaultMask, QueryTriggerInteraction.Ignore))
            Anchor(name, env, hit.point + hit.normal * 0.012f, Quaternion.LookRotation(-hit.normal));
        else
            Anchor(name, env, new Vector3(from.x, from.y, -1.56f), Quaternion.LookRotation(Vector3.back));
    }

    // A flat world-space sprite (ring, dot...) of the given size in metres.
    static RectTransform Decal(string name, Transform parent, Vector3 position, Quaternion rotation, float metres, string sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        rt.sizeDelta = new Vector2(100f, 100f);
        rt.SetPositionAndRotation(position, rotation);
        rt.localScale = Vector3.one * (metres / 100f) / Mathf.Max(parent.lossyScale.x, 0.0001f);

        var img = new GameObject("Image", typeof(RectTransform), typeof(Image));
        var irt = (RectTransform)img.transform;
        irt.SetParent(rt, false);
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = irt.offsetMax = Vector2.zero;
        var image = img.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiArt + sprite);
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    static Color BowlColour(Transform bowl)
    {
        foreach (Renderer r in bowl.GetComponentsInChildren<Renderer>())
            foreach (Material m in r.sharedMaterials)
                if (m != null && m.name.StartsWith("M_Bowl"))
                    return m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
        return Color.white;
    }

    static Bounds MeshBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
