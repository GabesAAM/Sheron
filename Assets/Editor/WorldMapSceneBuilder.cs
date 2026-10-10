using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class WorldMapSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/WorldMap.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string LayoutName = "World Map Layout";
    private const string StagePrefabPath = "Assets/Prefabs/WorldMapStage.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/WorldMapPlayer.prefab";
    private const string PathPrefabPath = "Assets/Prefabs/WorldMapPath.prefab";

    private static readonly Vector3[] DefaultStagePositions =
    {
        new Vector3(-10f, 0.1f, -5f),
        new Vector3(-5f, 0.1f, 4f),
        new Vector3(0f, 0.1f, -3f),
        new Vector3(5f, 0.1f, 4f),
        new Vector3(10f, 0.1f, -5f)
    };

    private static readonly string[] DefaultStageNames =
    {
        "Green Hills", "Mushroom Woods", "Sky Bridge", "Crystal Cave", "Final Castle"
    };

    private static readonly Color[] DefaultStageColors =
    {
        new Color(0.25f, 0.75f, 0.35f),
        new Color(0.85f, 0.35f, 0.28f),
        new Color(0.3f, 0.65f, 0.9f),
        new Color(0.65f, 0.4f, 0.85f),
        new Color(0.95f, 0.7f, 0.2f)
    };

    static WorldMapSceneBuilder()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += BuildIfOpenAndEmpty;
    }

    [MenuItem("World Map/Build Editable World Map")]
    private static void BuildFromMenu()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        BuildIntoScene(EditorSceneManager.GetActiveScene(), true);
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == ScenePath)
        {
            EditorApplication.delayCall += BuildIfOpenAndEmpty;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path == ScenePath)
        {
            BuildIntoScene(activeScene, true);
        }
    }

    private static void BuildIfOpenAndEmpty()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path == ScenePath)
        {
            BuildIntoScene(activeScene, false);
        }
    }

    private static void BuildIntoScene(Scene scene, bool saveScene)
    {
        if (scene.path != ScenePath || scene != EditorSceneManager.GetActiveScene())
        {
            return;
        }

        WorldMapController controller = Object.FindObjectOfType<WorldMapController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("World Map");
            controller = controllerObject.AddComponent<WorldMapController>();
        }

        Transform existingLayout = controller.transform.Find(LayoutName);
        if (existingLayout != null)
        {
            if (!TryBindExistingLayout(controller, existingLayout))
            {
                Debug.LogError("World Map Layout exists but is missing its camera, player, route, or stages. Use World Map > Build Editable World Map after repairing the layout.", controller);
                return;
            }

            EditorUtility.SetDirty(controller);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
            }

            return;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Materials");

        Material grass = GetOrCreateMaterial("Assets/Materials/WorldMap_Grass.mat", new Color(0.26f, 0.62f, 0.3f));
        Material boardEdge = GetOrCreateMaterial("Assets/Materials/WorldMap_Board.mat", new Color(0.32f, 0.22f, 0.13f));
        Material pathMaterial = GetOrCreateMaterial("Assets/Materials/WorldMap_Path.mat", new Color(0.85f, 0.72f, 0.47f));
        Material platformMaterial = GetOrCreateMaterial("Assets/Materials/WorldMap_Platform.mat", new Color(0.9f, 0.86f, 0.7f));
        Material ringMaterial = GetOrCreateMaterial("Assets/Materials/WorldMap_Selection.mat", new Color(1f, 0.82f, 0.28f));
        Material[] markerMaterials = new Material[DefaultStageColors.Length];
        for (int i = 0; i < markerMaterials.Length; i++)
        {
            markerMaterials[i] = GetOrCreateMaterial("Assets/Materials/WorldMap_Stage" + (i + 1) + ".mat",
                DefaultStageColors[i]);
        }

        UnityEditor.SceneAsset sampleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath);
        Sprite characterSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CharacterPlaceholder.png");
        GameObject stagePrefab = GetOrCreateStagePrefab(sampleScene, platformMaterial, ringMaterial);
        GameObject playerPrefab = GetOrCreatePlayerPrefab(characterSprite);
        GameObject pathPrefab = GetOrCreatePathPrefab(pathMaterial);

        GameObject layoutObject = new GameObject(LayoutName);
        Transform layout = layoutObject.transform;
        layout.SetParent(controller.transform, false);

        CreateBlock(layout, "Map Foundation", Vector3.zero, new Vector3(31f, 0.6f, 22f), boardEdge);
        CreateBlock(layout, "Grassy World", new Vector3(0f, 0.04f, 0f), new Vector3(30.4f, 0.1f, 21.4f), grass);
        CreateDecorations(layout, grass, pathMaterial);

        WorldMapStageNode[] stageNodes = new WorldMapStageNode[DefaultStagePositions.Length];
        for (int i = 0; i < stageNodes.Length; i++)
        {
            GameObject stageObject = PrefabUtility.InstantiatePrefab(stagePrefab, scene) as GameObject;
            stageObject.name = "Stage " + (i + 1) + " - " + DefaultStageNames[i];
            stageObject.transform.SetParent(layout, false);
            stageObject.transform.localPosition = DefaultStagePositions[i];

            Transform marker = stageObject.transform.Find("Marker");
            Renderer markerRenderer = marker != null ? marker.GetComponent<Renderer>() : null;
            if (markerRenderer != null)
            {
                markerRenderer.sharedMaterial = markerMaterials[i];
                PrefabUtility.RecordPrefabInstancePropertyModifications(markerRenderer);
            }

            WorldMapStageNode stageNode = stageObject.GetComponent<WorldMapStageNode>();
            Transform ring = stageObject.transform.Find("Selection Ring");
            SpriteRenderer icon = stageObject.transform.Find("Icon").GetComponent<SpriteRenderer>();
            TextMesh numberLabel = stageObject.transform.Find("Number").GetComponent<TextMesh>();
            stageNode.Configure(i + 1, DefaultStageNames[i], ring, icon, numberLabel);
            stageNode.SetSceneAsset(sampleScene);
            stageNode.SetSelected(false);
            EditorUtility.SetDirty(stageNode);
            PrefabUtility.RecordPrefabInstancePropertyModifications(stageNode);
            stageNodes[i] = stageNode;
        }

        GameObject pathObject = PrefabUtility.InstantiatePrefab(pathPrefab, scene) as GameObject;
        pathObject.name = "Stage Route";
        pathObject.transform.SetParent(layout, false);
        LineRenderer route = pathObject.GetComponent<LineRenderer>();
        route.positionCount = stageNodes.Length;
        route.useWorldSpace = false;
        for (int i = 0; i < stageNodes.Length; i++)
        {
            route.SetPosition(i, stageNodes[i].transform.localPosition + Vector3.up * 0.18f);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(route);

        GameObject cameraObject = new GameObject("World Map Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(layout, false);
        cameraObject.transform.localPosition = new Vector3(0f, 25f, -14f);
        cameraObject.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
        Camera mapCamera = cameraObject.AddComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = 14f;
        mapCamera.nearClipPlane = 0.1f;
        mapCamera.farClipPlane = 100f;
        mapCamera.backgroundColor = new Color(0.52f, 0.79f, 0.9f);
        cameraObject.AddComponent<AudioListener>();

        GameObject sunObject = new GameObject("World Map Sun");
        sunObject.transform.SetParent(layout, false);
        sunObject.transform.localRotation = Quaternion.Euler(48f, -30f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;

        GameObject playerObject = PrefabUtility.InstantiatePrefab(playerPrefab, scene) as GameObject;
        playerObject.name = "World Map Player";
        playerObject.transform.SetParent(layout, false);
        playerObject.transform.localPosition = DefaultStagePositions[0] + Vector3.up * 1.45f;
        controller.ConfigureSceneReferences(mapCamera, playerObject.transform, stageNodes, route);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        if (saveScene)
        {
            EditorSceneManager.SaveScene(scene);
        }

        Selection.activeGameObject = controller.gameObject;
        Debug.Log("Editable world map created. Stage markers, route, camera, player, and prefabs are now in the Scene.", controller);
    }

    private static bool TryBindExistingLayout(WorldMapController controller, Transform layout)
    {
        Transform cameraTransform = layout.Find("World Map Camera");
        Transform playerTransform = layout.Find("World Map Player");
        Transform routeTransform = layout.Find("Stage Route");
        if (cameraTransform == null || playerTransform == null || routeTransform == null)
        {
            return false;
        }

        Camera mapCamera = cameraTransform.GetComponent<Camera>();
        LineRenderer route = routeTransform.GetComponent<LineRenderer>();
        WorldMapStageNode[] stageNodes = layout.GetComponentsInChildren<WorldMapStageNode>(true);
        if (mapCamera == null || route == null || stageNodes.Length < 2)
        {
            return false;
        }

        System.Array.Sort(stageNodes, (left, right) => left.StageNumber.CompareTo(right.StageNumber));
        controller.ConfigureSceneReferences(mapCamera, playerTransform, stageNodes, route);
        return true;
    }

    private static GameObject GetOrCreateStagePrefab(SceneAsset sampleScene, Material platformMaterial, Material ringMaterial)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StagePrefabPath);
        if (prefab != null)
        {
            return prefab;
        }

        GameObject root = new GameObject("World Map Stage");
        WorldMapStageNode stageNode = root.AddComponent<WorldMapStageNode>();
        CreateCylinder(root.transform, "Platform", new Vector3(0f, 0.13f, 0f),
            new Vector3(1.75f, 0.12f, 1.75f), platformMaterial);
        GameObject ring = CreateCylinder(root.transform, "Selection Ring", new Vector3(0f, 0.27f, 0f),
            new Vector3(2.05f, 0.035f, 2.05f), ringMaterial);
        ring.SetActive(false);
        GameObject marker = CreateBlock(root.transform, "Marker", new Vector3(0f, 0.75f, 0f),
            new Vector3(0.9f, 0.9f, 0.9f), platformMaterial);
        marker.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        GameObject iconObject = new GameObject("Icon", typeof(SpriteRenderer));
        iconObject.transform.SetParent(root.transform, false);
        iconObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        SpriteRenderer icon = iconObject.GetComponent<SpriteRenderer>();
        icon.sortingOrder = 2;

        GameObject numberObject = new GameObject("Number");
        numberObject.transform.SetParent(root.transform, false);
        numberObject.transform.localPosition = new Vector3(0f, 1.1f, -0.48f);
        TextMesh number = numberObject.AddComponent<TextMesh>();
        number.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        number.fontSize = 64;
        number.characterSize = 0.23f;
        number.anchor = TextAnchor.MiddleCenter;
        number.alignment = TextAlignment.Center;
        number.color = Color.white;
        stageNode.Configure(1, "Stage 1", ring.transform, icon, number);
        stageNode.SetSceneAsset(sampleScene);

        prefab = PrefabUtility.SaveAsPrefabAsset(root, StagePrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject GetOrCreatePlayerPrefab(Sprite characterSprite)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab != null)
        {
            return prefab;
        }

        GameObject player = new GameObject("World Map Player", typeof(SpriteRenderer));
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = characterSprite;
        spriteRenderer.sortingOrder = 10;
        prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        Object.DestroyImmediate(player);
        return prefab;
    }

    private static GameObject GetOrCreatePathPrefab(Material pathMaterial)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathPrefabPath);
        if (prefab != null)
        {
            return prefab;
        }

        GameObject path = new GameObject("World Map Path", typeof(LineRenderer));
        LineRenderer line = path.GetComponent<LineRenderer>();
        line.sharedMaterial = pathMaterial;
        line.startWidth = 0.36f;
        line.endWidth = 0.36f;
        line.useWorldSpace = false;
        line.alignment = LineAlignment.View;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        prefab = PrefabUtility.SaveAsPrefabAsset(path, PathPrefabPath);
        Object.DestroyImmediate(path);
        return prefab;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Standard"));
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject CreateBlock(Transform parent, string objectName, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(block.GetComponent<Collider>());
        return block;
    }

    private static GameObject CreateCylinder(Transform parent, string objectName, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = objectName;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        return cylinder;
    }

    private static void CreateDecorations(Transform parent, Material grass, Material path)
    {
        Vector3[] positions =
        {
            new Vector3(-12.5f, 0.65f, 1.2f), new Vector3(-8f, 0.65f, 7.5f),
            new Vector3(-1.5f, 0.65f, 7.7f), new Vector3(7.5f, 0.65f, 7.5f),
            new Vector3(12.5f, 0.65f, 1.5f), new Vector3(8f, 0.65f, -8f),
            new Vector3(-7.5f, 0.65f, -8f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hill.name = "Map Hill";
            hill.transform.SetParent(parent, false);
            hill.transform.localPosition = positions[i];
            hill.transform.localScale = new Vector3(2.6f, 0.65f, 1.7f);
            hill.GetComponent<Renderer>().sharedMaterial = grass;
            Object.DestroyImmediate(hill.GetComponent<Collider>());
        }

        for (int i = 0; i < 9; i++)
        {
            float x = -12f + i * 3f;
            CreateCylinder(parent, "Map Decoration Stone",
                new Vector3(x, 0.12f, (i % 2 == 0) ? 8.5f : -8.8f),
                new Vector3(0.4f, 0.06f, 0.4f), path);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}

