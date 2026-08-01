using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailCraft.CameraSystem;
using RailCraft.Content;
using RailCraft.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RailCraft.Editor
{
    public static class FactorySceneBuilder
    {
        private const string ArtRoot = "Assets/RailCraft/Art";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string FactoryPrefabRoot = ArtRoot + "/Prefabs/Factory";
        private const string SceneRoot = "Assets/RailCraft/Scenes";
        private const string FactoryScenePath = SceneRoot + "/Factory.unity";
        private const string InputPath = "Assets/RailCraft/Input/RailCraftControls.inputactions";

        private static readonly StaticEditorFlags ArchitectureFlags =
            StaticEditorFlags.BatchingStatic
            | StaticEditorFlags.ContributeGI
            | StaticEditorFlags.OccluderStatic
            | StaticEditorFlags.OccludeeStatic
            | StaticEditorFlags.ReflectionProbeStatic;

        private sealed class FactoryMaterials
        {
            public Material Floor;
            public Material Wall;
            public Material Dark;
            public Material Yellow;
            public Material Blue;
            public Material White;
            public Material Orange;
            public Material Emissive;
        }

        [MenuItem("RailCraft/Build Factory Scene")]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine() => Build();

        public static void Build()
        {
            EnsureFolders();
            ValidatePrerequisites();
            var materials = LoadMaterials();
            var assemblyBay = BuildAssemblyBay(materials);
            var flow = LoadFlow();
            BuildFactoryScene(materials, assemblyBay, flow);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ValidatePrerequisites()
        {
            var issues = ModelContractValidator.ValidateProductionCatalog();
            if (issues.Count != 0)
                throw new InvalidOperationException(
                    "Factory scene requires a valid replaceable model catalog. " +
                    "Run RailCraft/Build Placeholder Assets once before building the scene. " +
                    string.Join("; ", issues));

            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (input == null || input.FindActionMap("Factory", false) == null)
                throw new InvalidOperationException($"Factory input actions are missing or invalid: {InputPath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/RailCraft/Art/Prefabs", "Factory");
            EnsureFolder("Assets/RailCraft", "Scenes");
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static FlowDefinition LoadFlow()
        {
            var path = Path.Combine(Application.dataPath, "RailCraft/Content/V1/flow.v1.json");
            return JsonUtility.FromJson<FlowDefinition>(File.ReadAllText(path));
        }

        private static FactoryMaterials LoadMaterials()
        {
            return new FactoryMaterials
            {
                Floor = CreateOrUpdateMaterial("FactoryFloor", new Color(0.13f, 0.16f, 0.18f)),
                Wall = CreateOrUpdateMaterial("FactoryWall", new Color(0.55f, 0.6f, 0.62f)),
                Dark = LoadMaterial("Steel"),
                Yellow = LoadMaterial("SafetyYellow"),
                Blue = LoadMaterial("RailBlue"),
                White = LoadMaterial("CardWhite"),
                Orange = LoadMaterial("SignalOrange"),
                Emissive = CreateOrUpdateMaterial("FactoryWorkLight", new Color(1f, 0.87f, 0.55f), true)
            };
        }

        private static Material LoadMaterial(string name)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{name}.mat");
            if (material == null)
                throw new InvalidOperationException($"Factory material is missing: {name}");
            return material;
        }

        private static Material CreateOrUpdateMaterial(string name, Color color, bool emissive = false)
        {
            var path = $"{MaterialRoot}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emissive ? color * 2.2f : Color.black);
                if (emissive)
                    material.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildAssemblyBay(FactoryMaterials materials)
        {
            var root = new GameObject("AssemblyBay");
            var platform = CreateCube(root.transform, "AssemblyPlatform",
                new Vector3(0f, 0.15f, 0f), new Vector3(10f, 0.3f, 8f), materials.Floor);
            MarkArchitectureStatic(platform);

            var rails = CreateChild(root.transform, "CraneRails");
            for (var side = -1; side <= 1; side += 2)
            {
                MarkArchitectureStatic(CreateCube(rails.transform, $"OverheadRail_{side}",
                    new Vector3(side * 4.2f, 5.8f, 0f), new Vector3(0.24f, 0.28f, 9f), materials.Dark));
                MarkArchitectureStatic(CreateCube(rails.transform, $"SupportA_{side}",
                    new Vector3(side * 4.2f, 2.9f, -3.7f), new Vector3(0.32f, 5.8f, 0.32f), materials.Dark));
                MarkArchitectureStatic(CreateCube(rails.transform, $"SupportB_{side}",
                    new Vector3(side * 4.2f, 2.9f, 3.7f), new Vector3(0.32f, 5.8f, 0.32f), materials.Dark));
            }
            MarkArchitectureStatic(CreateCube(rails.transform, "CraneBridge",
                new Vector3(0f, 5.65f, 0f), new Vector3(8.5f, 0.35f, 0.38f), materials.Yellow));

            var stripes = CreateChild(root.transform, "SafetyStripes");
            for (var index = 0; index < 16; index++)
            {
                var material = index % 2 == 0 ? materials.Yellow : materials.Dark;
                var z = -3.65f + index * 0.48f;
                MarkArchitectureStatic(CreateCube(stripes.transform, $"Stripe_{index:00}",
                    new Vector3(-4.7f, 0.325f, z), new Vector3(0.38f, 0.025f, 0.36f), material));
                MarkArchitectureStatic(CreateCube(stripes.transform, $"Stripe_{index + 16:00}",
                    new Vector3(4.7f, 0.325f, z), new Vector3(0.38f, 0.025f, 0.36f), material));
            }

            var lights = CreateChild(root.transform, "WorkLights");
            for (var x = -3; x <= 3; x += 2)
            {
                for (var z = -2; z <= 2; z += 2)
                {
                    MarkArchitectureStatic(CreateCube(lights.transform, $"Fixture_{x}_{z}",
                        new Vector3(x, 5.35f, z), new Vector3(1.2f, 0.08f, 0.28f), materials.Emissive));
                }
            }

            var markings = CreateChild(root.transform, "FloorMarkings");
            MarkArchitectureStatic(CreateCube(markings.transform, "CenterLine",
                new Vector3(0f, 0.325f, 0f), new Vector3(0.08f, 0.02f, 7f), materials.Yellow));
            for (var z = -3; z <= 3; z += 2)
                MarkArchitectureStatic(CreateCube(markings.transform, $"CrossLine_{z}",
                    new Vector3(0f, 0.325f, z), new Vector3(7.2f, 0.02f, 0.06f), materials.White));

            MarkArchitectureStatic(root);
            var path = FactoryPrefabRoot + "/AssemblyBay.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save factory prefab: {path}");
            return prefab;
        }

        private static void BuildFactoryScene(FactoryMaterials materials, GameObject assemblyBayPrefab,
            FlowDefinition flow)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Factory";
            var factoryRoot = new GameObject("FactoryRoot");

            BuildEnvironment(CreateChild(factoryRoot.transform, "Environment"), materials);
            BuildLighting(CreateChild(factoryRoot.transform, "BakedLighting"), materials);

            var assemblyBay = (GameObject)PrefabUtility.InstantiatePrefab(assemblyBayPrefab);
            assemblyBay.name = "AssemblyBay";
            assemblyBay.transform.SetParent(factoryRoot.transform, false);

            BuildStation(CreateChild(factoryRoot.transform, "PartsStagingArea"),
                new Vector3(-8f, 0f, -2f), new Vector3(4.8f, 0.25f, 8f), materials.Blue, materials);
            BuildStation(CreateChild(factoryRoot.transform, "CarbodyLoweringBay"),
                new Vector3(0f, 0f, 7f), new Vector3(9f, 0.25f, 3.5f), materials.Yellow, materials);
            BuildStation(CreateChild(factoryRoot.transform, "CommissioningConsole"),
                new Vector3(8f, 0f, 2f), new Vector3(3.2f, 0.25f, 3f), materials.Blue, materials);
            BuildStation(CreateChild(factoryRoot.transform, "InspectionStation"),
                new Vector3(8f, 0f, -3.5f), new Vector3(3.2f, 0.25f, 3f), materials.Orange, materials);
            BuildStation(CreateChild(factoryRoot.transform, "ReleaseBoard"),
                new Vector3(-8f, 0f, 5.5f), new Vector3(3.2f, 0.25f, 3f), materials.Yellow, materials);

            BuildBackgroundTrack(CreateChild(factoryRoot.transform, "BackgroundTrack"), materials);
            BuildHeadDisplay(CreateChild(factoryRoot.transform, "CR400AFHeadDisplay"));

            var processAnchors = CreateChild(factoryRoot.transform, "ProcessAnchors");
            var dropTargets = CreateChild(factoryRoot.transform, "DropTargets");
            var shotAnchors = BuildProcessAnchors(processAnchors.transform, dropTargets.transform, flow, materials);
            BuildCameraRig(CreateChild(factoryRoot.transform, "FactoryCameraRig"), shotAnchors);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.43f, 0.48f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.24f, 0.27f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.1f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.35f, 0.4f, 0.43f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 52f;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, FactoryScenePath))
                throw new InvalidOperationException($"Failed to save factory scene: {FactoryScenePath}");

            UpsertFactoryBuildScene();
        }

        private static void UpsertFactoryBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            var foundFactory = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == FactoryScenePath)
                {
                    if (foundFactory)
                        continue;
                    foundFactory = true;
                    scenes.Add(new EditorBuildSettingsScene(FactoryScenePath, true));
                    continue;
                }
                scenes.Add(scene);
            }

            if (!foundFactory)
                scenes.Add(new EditorBuildSettingsScene(FactoryScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void BuildEnvironment(GameObject environment, FactoryMaterials materials)
        {
            MarkArchitectureStatic(CreateCube(environment.transform, "FactoryFloor",
                new Vector3(0f, -0.12f, 0f), new Vector3(32f, 0.24f, 24f), materials.Floor));

            var walls = CreateChild(environment.transform, "ModularWalls");
            for (var x = -14; x <= 14; x += 4)
                MarkArchitectureStatic(CreateCube(walls.transform, $"BackWall_{x}",
                    new Vector3(x, 3f, 11.8f), new Vector3(3.9f, 6f, 0.25f), materials.Wall));
            for (var z = -10; z <= 10; z += 4)
            {
                MarkArchitectureStatic(CreateCube(walls.transform, $"LeftWall_{z}",
                    new Vector3(-15.8f, 3f, z), new Vector3(0.25f, 6f, 3.9f), materials.Wall));
                MarkArchitectureStatic(CreateCube(walls.transform, $"RightWall_{z}",
                    new Vector3(15.8f, 3f, z), new Vector3(0.25f, 6f, 3.9f), materials.Wall));
            }

            var roofBeams = CreateChild(environment.transform, "RoofBeams");
            for (var z = -10; z <= 10; z += 4)
                MarkArchitectureStatic(CreateCube(roofBeams.transform, $"RoofBeam_{z}",
                    new Vector3(0f, 6.2f, z), new Vector3(31.5f, 0.25f, 0.28f), materials.Dark));

            var signs = CreateChild(environment.transform, "GenericSafetySigns");
            MarkArchitectureStatic(CreateCube(signs.transform, "通用安全作业区标识",
                new Vector3(0f, 3.4f, 11.62f), new Vector3(4.5f, 1.1f, 0.08f), materials.Blue));
            MarkArchitectureStatic(CreateCube(signs.transform, "装配演示区域标识",
                new Vector3(-10f, 3f, 11.58f), new Vector3(3.6f, 0.9f, 0.08f), materials.Yellow));
            MarkArchitectureStatic(environment);
        }

        private static void BuildLighting(GameObject lighting, FactoryMaterials materials)
        {
            var sunObject = new GameObject("MainDirectionalLight");
            sunObject.transform.SetParent(lighting.transform, false);
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.lightmapBakeType = LightmapBakeType.Mixed;

            var fixtures = CreateChild(lighting.transform, "BakedWorkLights");
            for (var x = -9; x <= 9; x += 6)
            {
                for (var z = -6; z <= 6; z += 6)
                {
                    var fixture = CreateCube(fixtures.transform, $"LightFixture_{x}_{z}",
                        new Vector3(x, 5.75f, z), new Vector3(1.6f, 0.08f, 0.35f), materials.Emissive);
                    MarkArchitectureStatic(fixture);
                    var light = fixture.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(1f, 0.82f, 0.58f);
                    light.intensity = 2.2f;
                    light.range = 7.5f;
                    light.shadows = LightShadows.None;
                    light.lightmapBakeType = LightmapBakeType.Baked;
                }
            }

            var reflectionObject = new GameObject("FactoryReflectionProbe");
            reflectionObject.transform.SetParent(lighting.transform, false);
            reflectionObject.transform.position = new Vector3(0f, 2.5f, 0f);
            var reflection = reflectionObject.AddComponent<ReflectionProbe>();
            reflection.mode = ReflectionProbeMode.Baked;
            reflection.size = new Vector3(28f, 6f, 20f);
            reflection.resolution = 128;

            var probesObject = new GameObject("MovingItemLightProbes");
            probesObject.transform.SetParent(lighting.transform, false);
            var probes = probesObject.AddComponent<LightProbeGroup>();
            var positions = new List<Vector3>();
            for (var x = -10; x <= 10; x += 5)
            for (var z = -7; z <= 7; z += 3)
            for (var y = 1; y <= 4; y += 3)
                positions.Add(new Vector3(x, y, z));
            probes.probePositions = positions.ToArray();
        }

        private static void BuildStation(GameObject station, Vector3 position, Vector3 platformScale,
            Material accent, FactoryMaterials materials)
        {
            station.transform.position = position;
            MarkArchitectureStatic(CreateCube(station.transform, "Platform",
                new Vector3(0f, 0.125f, 0f), platformScale, materials.Dark));
            MarkArchitectureStatic(CreateCube(station.transform, "SafetyBorder",
                new Vector3(0f, 0.265f, -platformScale.z * 0.44f),
                new Vector3(platformScale.x * 0.9f, 0.03f, 0.08f), accent));
            MarkArchitectureStatic(CreateCube(station.transform, "GenericStationSign",
                new Vector3(0f, 1.45f, platformScale.z * 0.42f),
                new Vector3(Mathf.Min(platformScale.x * 0.65f, 2.7f), 0.75f, 0.1f), accent));
            MarkArchitectureStatic(station);
        }

        private static void BuildBackgroundTrack(GameObject track, FactoryMaterials materials)
        {
            track.transform.position = new Vector3(0f, 0f, 9.5f);
            for (var side = -1; side <= 1; side += 2)
                MarkArchitectureStatic(CreateCube(track.transform, $"Rail_{side}",
                    new Vector3(side * 0.75f, 0.12f, 0f), new Vector3(0.12f, 0.18f, 27f), materials.Dark));
            for (var index = -12; index <= 12; index++)
                MarkArchitectureStatic(CreateCube(track.transform, $"Sleeper_{index + 12:00}",
                    new Vector3(0f, 0.03f, index), new Vector3(2.4f, 0.1f, 0.18f), materials.Floor));
            MarkArchitectureStatic(track);
        }

        private static void BuildHeadDisplay(GameObject displayRoot)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/Vehicles/CR400AFHeadDisplay.prefab");
            if (prefab == null)
                throw new InvalidOperationException("CR400AF head display prefab is missing.");

            var display = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            display.name = "CR400AF 展示背景";
            display.transform.SetParent(displayRoot.transform, false);
            display.transform.localPosition = new Vector3(-4.8f, 0.1f, 9.3f);
            display.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            display.transform.localScale = Vector3.one * 0.85f;
            MarkArchitectureStatic(display);
        }

        private static List<FactoryCameraShot> BuildProcessAnchors(Transform processRoot,
            Transform targetRoot, FlowDefinition flow, FactoryMaterials materials)
        {
            if (flow?.steps == null || flow.steps.Length != 15)
                throw new InvalidOperationException("Factory scene requires exactly 15 flow steps.");

            var shots = new List<FactoryCameraShot>();
            foreach (var step in flow.steps.OrderBy(item => item.order))
            {
                var position = StepPosition(step.order);
                var anchor = new GameObject(step.id);
                anchor.transform.SetParent(processRoot, false);
                anchor.transform.localPosition = position;
                shots.Add(new FactoryCameraShot
                {
                    shotId = step.id,
                    focusAnchor = anchor.transform,
                    distance = step.order <= 11 ? 7.2f : 9f,
                    yaw = step.order <= 11 ? 38f : 25f,
                    pitch = step.order <= 11 ? 34f : 30f
                });

                var targetObject = new GameObject(step.dropTargetId);
                targetObject.transform.SetParent(targetRoot, false);
                targetObject.transform.localPosition = position;
                var collider = targetObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(1.4f, 0.65f, 1.05f);
                var snapAnchor = new GameObject("SnapAnchor");
                snapAnchor.transform.SetParent(targetObject.transform, false);
                var target = targetObject.AddComponent<DropTarget>();
                target.Configure(step.dropTargetId, step.id, snapAnchor.transform, 0.45f, 0.9f);
                var marker = CreateCube(targetObject.transform, "TargetMarker", Vector3.zero,
                    new Vector3(1.25f, 0.025f, 0.8f), materials.Orange);
                marker.GetComponent<Renderer>().enabled = false;
                var markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null)
                    UnityEngine.Object.DestroyImmediate(markerCollider);
            }
            return shots;
        }

        private static Vector3 StepPosition(int order)
        {
            if (order <= 11)
            {
                var index = order - 1;
                return new Vector3((index % 4 - 1.5f) * 1.55f, 0.6f,
                    (index / 4 - 1f) * 1.5f);
            }
            return order switch
            {
                12 => new Vector3(0f, 0.65f, 7f),
                13 => new Vector3(8f, 0.65f, 2f),
                14 => new Vector3(8f, 0.65f, -3.5f),
                15 => new Vector3(-8f, 0.65f, 5.5f),
                _ => Vector3.zero
            };
        }

        private static void BuildCameraRig(GameObject rig, List<FactoryCameraShot> processShots)
        {
            var focus = new GameObject("CurrentFocus");
            focus.transform.SetParent(rig.transform, false);
            focus.transform.localPosition = new Vector3(0f, 1f, 0f);

            var overviewAnchor = new GameObject("OverviewAnchor");
            overviewAnchor.transform.SetParent(rig.transform, false);
            overviewAnchor.transform.localPosition = new Vector3(0f, 1f, 0f);

            var cameraObject = new GameObject("FactoryCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rig.transform, false);
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 120f;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
            cameraObject.AddComponent<AudioListener>();

            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (controls == null)
                throw new InvalidOperationException($"Input action asset is missing: {InputPath}");
            var controller = rig.AddComponent<FactoryCameraController>();
            controller.Configure(camera, focus.transform, controls);

            var shots = new List<FactoryCameraShot>
            {
                new FactoryCameraShot
                {
                    shotId = "overview",
                    focusAnchor = overviewAnchor.transform,
                    distance = 15f,
                    yaw = 35f,
                    pitch = 38f
                }
            };
            shots.AddRange(processShots);
            var director = rig.AddComponent<CameraShotDirector>();
            director.Configure(controller, shots, 0.8f);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(director);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreateCube(Transform parent, string name,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void MarkArchitectureStatic(GameObject gameObject)
        {
            GameObjectUtility.SetStaticEditorFlags(gameObject, ArchitectureFlags);
            foreach (Transform child in gameObject.transform)
                MarkArchitectureStatic(child.gameObject);
        }
    }
}
