#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.AI;
using RoobsWorld.World;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Unity MCP server for AI integration (Cursor). HTTP server on localhost:7777.
    /// Actions: create_object, edit_object, generate_prefab, run_build, trigger_blender_export,
    /// scatter_objects, generate_level, generate_full_level, populate_level, wire_events,
    /// generate_playable_dungeon (single command for full level + population + events),
    /// run_quick_setup (tags + build settings + first scene), run_tests (EditMode test runner).
    /// All Unity API calls run on the main thread via a queued dispatcher.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMCPServer
    {
        private static readonly string[] UrlPrefixes =
        {
            "http://localhost:7777/",
            "http://localhost:7797/"
        };
        private static HttpListener _listener;
        private static string _activeUrlPrefix;
        private static readonly object QueueLock = new object();
        private static readonly Queue<(HttpListenerContext context, string body)> Pending = new Queue<(HttpListenerContext, string)>();
        private static bool _quickSetupQueuedAfterPlayMode;

        static UnityMCPServer()
        {
            foreach (string prefix in UrlPrefixes)
            {
                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add(prefix);
                    _listener.Start();
                    _activeUrlPrefix = prefix;
                    _listener.BeginGetContext(OnRequest, null);
                    EditorApplication.update += DrainQueue;
                    Debug.Log("[MCP] Unity MCP Server started at " + _activeUrlPrefix);
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[MCP] Could not start server at " + prefix + ": " + ex.Message);
                    try { _listener?.Close(); } catch { }
                    _listener = null;
                }
            }

            Debug.LogWarning("[MCP] Unity MCP Server failed to bind any configured port.");
        }

        private static void OnRequest(IAsyncResult result)
        {
            if (_listener == null || !_listener.IsListening) return;
            HttpListenerContext context = null;
            try
            {
                context = _listener.EndGetContext(result);
                _listener.BeginGetContext(OnRequest, null);
            }
            catch (ObjectDisposedException) { return; }
            catch (Exception ex)
            {
                Debug.LogWarning("[MCP] Request error: " + ex.Message);
                return;
            }

            string body = "";
            if (context.Request.HttpMethod == "POST" && context.Request.InputStream != null)
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                    body = reader.ReadToEnd();
            }

            lock (QueueLock)
            {
                Pending.Enqueue((context, body));
            }
        }

        private static void DrainQueue()
        {
            (HttpListenerContext context, string body) item;
            lock (QueueLock)
            {
                if (Pending.Count == 0) return;
                item = Pending.Dequeue();
            }

            string responseString = "Unknown Command";
            try
            {
                responseString = HandleCommand(item.body);
            }
            catch (Exception ex)
            {
                responseString = "Error: " + ex.Message;
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                item.context.Response.ContentLength64 = buffer.Length;
                item.context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                item.context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MCP] Response error: " + ex.Message);
            }
        }

        private static string HandleCommand(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "Empty body";
            var cmd = JsonUtility.FromJson<AICommand>(json);
            if (cmd?.parameters == null) return "Invalid JSON";
            string action = (cmd.action ?? "").Trim().ToLowerInvariant();
            switch (action)
            {
                case "create_object": return CreateObject(cmd);
                case "edit_object": return EditObject(cmd);
                case "generate_prefab": return GeneratePrefab(cmd);
                case "run_build": return RunBuild(cmd);
                case "trigger_blender_export": return TriggerBlenderExport(cmd);
                case "scatter_objects": return ScatterObjects(cmd);
                case "generate_level": return GenerateLevel(cmd);
                case "generate_full_level": return GenerateFullLevel(cmd);
                case "populate_level": return PopulateLevel(cmd);
                case "wire_events": return WireEvents(cmd);
                case "generate_playable_dungeon": return GeneratePlayableDungeon(cmd);
                case "run_quick_setup": return RunQuickSetup();
                case "sync_models_to_resources": return SyncModelsToResources();
                case "validate_blender_assets": return ValidateBlenderAssets();
                case "sync_and_validate_blender_assets": return SyncAndValidateBlenderAssets();
                case "run_tests": return RunTests(forceRefresh: false);
                case "recompile_and_test": return RunTests(forceRefresh: true);
                case "set_component_field": return SetComponentField(cmd);
                default: return "Unknown action: " + cmd.action;
            }
        }

        #region Actions

        private static string CreateObject(AICommand cmd)
        {
            var p = cmd.parameters;
            string name = p.name ?? "AI_Object";
            GameObject go = new GameObject(name);
            if (p.componentTemplates != null && p.componentTemplates.Length > 0)
            {
                foreach (string comp in p.componentTemplates)
                {
                    var type = Type.GetType(comp + ", Assembly-CSharp") ?? Type.GetType(comp + ", RoobsWorld") ?? Type.GetType(comp);
                    if (type != null && typeof(Component).IsAssignableFrom(type))
                        go.AddComponent(type);
                }
            }
            else if (!string.IsNullOrEmpty(p.component))
            {
                var type = Type.GetType(p.component + ", Assembly-CSharp") ?? Type.GetType(p.component + ", RoobsWorld") ?? Type.GetType(p.component);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    go.AddComponent(type);
            }
            if (p.position.HasValue)
                go.transform.position = p.position.Value;
            return "Created GameObject " + go.name;
        }

        private static string EditObject(AICommand cmd)
        {
            var p = cmd.parameters;
            GameObject go = GameObject.Find(p.name ?? "");
            if (go == null) return "Object not found";
            if (p.position.HasValue) go.transform.position = p.position.Value;
            return "Edited GameObject " + go.name;
        }

        private static string SetComponentField(AICommand cmd)
        {
            var p = cmd.parameters;
            if (string.IsNullOrWhiteSpace(p.name)) return "Missing object name";
            if (string.IsNullOrWhiteSpace(p.component)) return "Missing component type";
            if (string.IsNullOrWhiteSpace(p.fieldName)) return "Missing field name";

            GameObject go = GameObject.Find(p.name);
            if (go == null) return "Object not found";

            var type = Type.GetType(p.component + ", Assembly-CSharp")
                ?? Type.GetType(p.component + ", RoobsWorld")
                ?? Type.GetType(p.component);
            if (type == null) return "Component type not found: " + p.component;

            var component = go.GetComponent(type);
            if (component == null) return "Component not found on object";

            var field = type.GetField(p.fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) return "Field not found: " + p.fieldName;

            object value;
            string valueType = (p.valueType ?? "").Trim().ToLowerInvariant();
            switch (valueType)
            {
                case "int":
                    value = p.intValue;
                    break;
                case "float":
                    value = p.floatValue;
                    break;
                case "bool":
                    value = p.boolValue;
                    break;
                case "string":
                    value = p.stringValue ?? "";
                    break;
                default:
                    return "Unsupported valueType. Use one of: int, float, bool, string.";
            }

            try
            {
                field.SetValue(component, value);
                EditorUtility.SetDirty(component);
                return $"Set {type.Name}.{field.Name} on {go.name}";
            }
            catch (Exception ex)
            {
                return "Set field failed: " + ex.Message;
            }
        }

        private static string GeneratePrefab(AICommand cmd)
        {
            var p = cmd.parameters;
            GameObject go = GameObject.Find(p.name ?? "");
            if (go == null) return "Object not found";
            string dir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            string path = dir + "/" + go.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            if (p.lodLevels != null && p.lodLevels.Length > 0)
            {
                LODGroup group = go.GetComponent<LODGroup>();
                if (group == null) group = go.AddComponent<LODGroup>();
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                var lods = new LOD[p.lodLevels.Length];
                for (int i = 0; i < p.lodLevels.Length; i++)
                    lods[i] = new LOD(Mathf.Clamp01(p.lodLevels[i]), renderers);
                group.SetLODs(lods);
                group.RecalculateBounds();
            }
            AssetDatabase.SaveAssets();
            return "Prefab generated at " + path;
        }

        private static string RunBuild(AICommand cmd)
        {
            var p = cmd.parameters;
            BuildTarget target = BuildTarget.StandaloneWindows64;
            string t = (p.buildTarget ?? "").ToLowerInvariant();
            if (t == "mac") target = BuildTarget.StandaloneOSX;
            else if (t == "linux") target = BuildTarget.StandaloneLinux64;
            else if (t == "android") target = BuildTarget.Android;
            else if (t == "ios") target = BuildTarget.iOS;
            string path = p.buildPath ?? "Builds/Build";
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) return "No scenes in Build Settings";
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            BuildPipeline.BuildPlayer(options);
            return "Build completed: " + path;
        }

        private static string TriggerBlenderExport(AICommand cmd)
        {
            var p = cmd.parameters;
            string blenderExe = p.blenderPath ?? "C:/Program Files/Blender Foundation/Blender/blender.exe";
            string scriptPath = p.scriptPath ?? "tools/blender_scripts/export_for_unity.py";
            try
            {
                System.Diagnostics.Process.Start(blenderExe, "-b -P \"" + scriptPath + "\"");
                return "Blender export triggered";
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        private static string ScatterObjects(AICommand cmd)
        {
            var p = cmd.parameters;
            if (string.IsNullOrEmpty(p.scatterPrefab)) return "No prefab specified";
            string path = "Assets/Prefabs/" + p.scatterPrefab + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return "Prefab not found: " + path;
            int count = Mathf.Max(1, p.scatterCount);
            Vector3 area = p.scatterArea ?? new Vector3(10f, 0f, 10f);
            for (int i = 0; i < count; i++)
            {
                float x = UnityEngine.Random.Range(-area.x / 2f, area.x / 2f);
                float z = UnityEngine.Random.Range(-area.z / 2f, area.z / 2f);
                UnityEngine.Object.Instantiate(prefab, new Vector3(x, 0f, z), Quaternion.identity);
            }
            return "Scattered " + count + " instances of " + p.scatterPrefab;
        }

        private static string GenerateLevel(AICommand cmd)
        {
            var p = cmd.parameters;
            if (p.levelSize == null || p.prefabs == null || p.prefabs.Length == 0)
                return "Invalid parameters: levelSize or prefabs missing";
            Vector3Int size = new Vector3Int(p.levelSize.x, p.levelSize.y, p.levelSize.z);
            int chunkSize = p.chunkSize > 0 ? p.chunkSize : 10;
            int prefabCount = 0;
            for (int x = 0; x < size.x; x += chunkSize)
            {
                for (int z = 0; z < size.z; z += chunkSize)
                {
                    string prefabName = p.prefabs[UnityEngine.Random.Range(0, p.prefabs.Length)];
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefabName + ".prefab");
                    if (prefab == null) continue;
                    Vector3 pos = new Vector3(x + UnityEngine.Random.Range(0f, chunkSize), 0f, z + UnityEngine.Random.Range(0f, chunkSize));
                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    obj.transform.position = pos;
                    if (p.lodLevels != null && p.lodLevels.Length > 0)
                    {
                        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                        var lods = new LOD[p.lodLevels.Length];
                        for (int i = 0; i < p.lodLevels.Length; i++)
                            lods[i] = new LOD(Mathf.Clamp01(p.lodLevels[i]), renderers);
                        var group = obj.AddComponent<LODGroup>();
                        group.SetLODs(lods);
                        group.RecalculateBounds();
                    }
                    if (p.autoPrefab)
                    {
                        string genPath = "Assets/Prefabs/Generated";
                        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Generated"))
                            AssetDatabase.CreateFolder("Assets/Prefabs", "Generated");
                        PrefabUtility.SaveAsPrefabAsset(obj, genPath + "/" + prefabName + "_" + x + "_" + z + ".prefab");
                    }
                    prefabCount++;
                }
            }
            return "Generated level: " + size.x + "x" + size.z + ", placed " + prefabCount + " prefabs";
        }

        private static string GenerateFullLevel(AICommand cmd)
        {
            var p = cmd.parameters;
            if (p.levelSize == null || p.prefabs == null || p.prefabs.Length == 0)
                return "Invalid parameters: levelSize or prefabs missing";
            Vector3Int size = new Vector3Int(p.levelSize.x, p.levelSize.y, p.levelSize.z);
            int chunkSize = p.chunkSize > 0 ? p.chunkSize : 10;
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("AI_Generated"))
                UnityEngine.Object.DestroyImmediate(obj);
            int prefabCount = 0;
            for (int x = 0; x < size.x; x += chunkSize)
            {
                for (int z = 0; z < size.z; z += chunkSize)
                {
                    string prefabName = p.prefabs[UnityEngine.Random.Range(0, p.prefabs.Length)];
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefabName + ".prefab");
                    if (prefab == null) continue;
                    Vector3 pos = new Vector3(x + UnityEngine.Random.Range(0f, chunkSize), 0f, z + UnityEngine.Random.Range(0f, chunkSize));
                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    obj.transform.position = pos;
                    try { obj.tag = "AI_Generated"; } catch { }
                    if (p.randomizeRotation)
                        obj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    if (p.randomizeScale)
                    {
                        float s = UnityEngine.Random.Range(p.scaleMin, p.scaleMax);
                        obj.transform.localScale = Vector3.one * s;
                    }
                    if (p.lodLevels != null && p.lodLevels.Length > 0)
                    {
                        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                        var lods = new LOD[p.lodLevels.Length];
                        for (int i = 0; i < p.lodLevels.Length; i++)
                            lods[i] = new LOD(Mathf.Clamp01(p.lodLevels[i]), renderers);
                        var group = obj.AddComponent<LODGroup>();
                        group.SetLODs(lods);
                        group.RecalculateBounds();
                    }
                    if (p.autoPrefab)
                    {
                        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Generated"))
                            AssetDatabase.CreateFolder("Assets/Prefabs", "Generated");
                        PrefabUtility.SaveAsPrefabAsset(obj, "Assets/Prefabs/Generated/" + prefabName + "_" + x + "_" + z + ".prefab");
                    }
                    prefabCount++;
                }
            }
            if (p.setupLighting)
            {
                if (GameObject.Find("Directional Light") == null)
                {
                    var lightGo = new GameObject("Directional Light");
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 1f;
                    lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }
            if (p.bakeNavMesh)
            {
                try
                {
                    NavMesh.RemoveAllNavMeshData();
                    // Full bake: add NavMeshSurface components to terrain/floor and use AI Navigation package.
                }
                catch (Exception)
                {
                    // NavMesh may require com.unity.ai.navigation for editor baking.
                }
            }
            return "Full level generated: " + size.x + "x" + size.z + ", placed " + prefabCount + " prefabs";
        }

        private static string PopulateLevel(AICommand cmd)
        {
            var p = cmd.parameters;
            GameObject[] chunks = GameObject.FindGameObjectsWithTag("AI_Generated");
            int spawnedEnemies = 0, spawnedLoot = 0;
            Vector3 spawnOff = p.spawnOffset ?? new Vector3(5f, 0f, 5f);
            foreach (GameObject chunk in chunks)
            {
                if (p.enemyPrefabs != null && p.enemyPrefabs.Length > 0)
                {
                    int enemyCount = UnityEngine.Random.Range(p.enemyMinPerChunk, p.enemyMaxPerChunk + 1);
                    for (int i = 0; i < enemyCount; i++)
                    {
                        string prefabName = p.enemyPrefabs[UnityEngine.Random.Range(0, p.enemyPrefabs.Length)];
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefabName + ".prefab");
                        if (prefab == null) continue;
                        Vector3 pos = chunk.transform.position + new Vector3(
                            UnityEngine.Random.Range(-spawnOff.x, spawnOff.x), spawnOff.y,
                            UnityEngine.Random.Range(-spawnOff.z, spawnOff.z));
                        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        enemy.transform.position = pos;
                        try { enemy.tag = "AI_Enemy"; } catch { }
                        if (p.attachPatrol)
                        {
                            var route = enemy.AddComponent<PatrolRoute>();
                            route.GenerateRandomRoute(pos, p.patrolRadius, p.patrolPoints);
                        }
                        spawnedEnemies++;
                    }
                }
                if (p.lootPrefabs != null && p.lootPrefabs.Length > 0)
                {
                    int lootCount = UnityEngine.Random.Range(p.lootMinPerChunk, p.lootMaxPerChunk + 1);
                    for (int i = 0; i < lootCount; i++)
                    {
                        string prefabName = p.lootPrefabs[UnityEngine.Random.Range(0, p.lootPrefabs.Length)];
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefabName + ".prefab");
                        if (prefab == null) continue;
                        Vector3 pos = chunk.transform.position + new Vector3(
                            UnityEngine.Random.Range(-spawnOff.x, spawnOff.x), spawnOff.y,
                            UnityEngine.Random.Range(-spawnOff.z, spawnOff.z));
                        GameObject loot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        loot.transform.position = pos;
                        try { loot.tag = "AI_Loot"; } catch { }
                        if (loot.GetComponent<LootPickup>() == null)
                            loot.AddComponent<LootPickup>();
                        spawnedLoot++;
                    }
                }
            }
            return "Population complete: " + spawnedEnemies + " enemies, " + spawnedLoot + " loot items.";
        }

        private static string WireEvents(AICommand cmd)
        {
            var p = cmd.parameters;
            int triggersCreated = 0;
            string[] enemyPrefabs = p.enemyPrefabs ?? Array.Empty<string>();
            string[] lootPrefabs = p.lootPrefabs ?? Array.Empty<string>();
            float radius = p.triggerRadius > 0 ? p.triggerRadius : 5f;
            Vector3 spawnOff = p.spawnOffset ?? new Vector3(5f, 0f, 5f);
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("AI_Generated"))
            {
                if (p.createEnemyTrigger && enemyPrefabs.Length > 0)
                {
                    GameObject triggerGo = new GameObject("AISpawnTrigger_" + obj.name);
                    triggerGo.transform.position = obj.transform.position;
                    var trigger = triggerGo.AddComponent<AISpawnTrigger>();
                    var list = new List<GameObject>();
                    foreach (string name in enemyPrefabs)
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
                        if (prefab != null) list.Add(prefab);
                    }
                    SerializeSpawnTrigger(trigger, list, radius, spawnOff);
                    triggersCreated++;
                }
            }
            return "Event wiring complete. " + triggersCreated + " enemy triggers created.";
        }

        private static void SerializeSpawnTrigger(AISpawnTrigger trigger, List<GameObject> prefabs, float radius, Vector3 spawnOffset)
        {
            var so = new SerializedObject(trigger);
            so.FindProperty("_prefabsToSpawn").ClearArray();
            for (int i = 0; i < prefabs.Count; i++)
            {
                so.FindProperty("_prefabsToSpawn").InsertArrayElementAtIndex(i);
                so.FindProperty("_prefabsToSpawn").GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_spawnOffset").vector3Value = spawnOffset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GeneratePlayableDungeon(AICommand cmd)
        {
            string a = GenerateFullLevel(cmd);
            string b = PopulateLevel(cmd);
            string c = WireEvents(cmd);
            return "generate_playable_dungeon done. " + a + " | " + b + " | " + c;
        }

        private static string RunQuickSetup()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (EditorApplication.isPlaying)
                        EditorApplication.isPlaying = false;

                    if (!_quickSetupQueuedAfterPlayMode)
                    {
                        _quickSetupQueuedAfterPlayMode = true;
                        EditorApplication.delayCall += RunQuickSetupWhenSafe;
                    }

                    return "Editor was in Play Mode. Stopping play and queuing quick setup.";
                }

                RoobsWorldQuickSetup.QuickSetup();
                return "Quick setup complete (tags, build settings, first scene).";
            }
            catch (Exception ex)
            {
                return "Quick setup error: " + ex.Message;
            }
        }

        private static void RunQuickSetupWhenSafe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
                EditorApplication.delayCall += RunQuickSetupWhenSafe;
                return;
            }

            _quickSetupQueuedAfterPlayMode = false;
            try
            {
                RoobsWorldQuickSetup.QuickSetup();
                Debug.Log("[MCP] Quick setup completed after exiting Play Mode.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MCP] Quick setup queued run failed: " + ex.Message);
            }
        }

        private static string SyncModelsToResources()
        {
            try
            {
                BlenderAssetSyncAndValidation.SyncModelsToResources();
                return "Blender models synced to Resources prefabs.";
            }
            catch (Exception ex)
            {
                return "Sync models error: " + ex.Message;
            }
        }

        private static string ValidateBlenderAssets()
        {
            try
            {
                BlenderAssetSyncAndValidation.ValidateWiring();
                return "Blender asset wiring validation executed. Check Unity Console for details.";
            }
            catch (Exception ex)
            {
                return "Validate wiring error: " + ex.Message;
            }
        }

        private static string SyncAndValidateBlenderAssets()
        {
            try
            {
                BlenderAssetSyncAndValidation.SyncAndValidate();
                return "Blender models synced and wiring validated.";
            }
            catch (Exception ex)
            {
                return "Sync and validate error: " + ex.Message;
            }
        }

        private static string RunTests(bool forceRefresh = false)
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (EditorApplication.isPlaying)
                        EditorApplication.isPlaying = false;
                    EditorApplication.delayCall += () => RunTests(forceRefresh);
                    return "Editor was in Play Mode. Stopping play and queuing EditMode tests.";
                }

                if (forceRefresh)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    CompilationPipeline.RequestScriptCompilation();
                    // Queue the actual test execution for after compilation finishes.
                    CompilationPipeline.compilationFinished += OnCompilationFinishedRunTests;
                    return "Recompiling scripts… tests will run automatically when compilation finishes. Watch the Unity console.";
                }

                ExecuteTests();
                return "EditMode test run started. Results will appear in the Unity Test Runner window.";
            }
            catch (Exception ex)
            {
                return "Test runner error: " + ex.Message;
            }
        }

        private static void OnCompilationFinishedRunTests(object _)
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinishedRunTests;
            ExecuteTestsWhenSafe();
        }

        private static void ExecuteTestsWhenSafe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
                EditorApplication.delayCall += ExecuteTestsWhenSafe;
                return;
            }

            ExecuteTests();
        }

        private static void ExecuteTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var results = new TestResults();
            api.RegisterCallbacks(results);
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        private class TestResults : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log($"[MCP] Tests finished — Pass: {result.PassCount}, Fail: {result.FailCount}, Skip: {result.SkipCount}");
            }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.FailCount > 0)
                    Debug.LogWarning($"[MCP] FAIL: {result.Test.FullName} — {result.Message}");
            }
        }

        #endregion

        #region DTOs

        [Serializable]
        public class AICommand
        {
            public string action;
            public AIParameters parameters;
        }

        [Serializable]
        public class AIParameters
        {
            public string name;
            public string component;
            public string[] componentTemplates;
            public Vector3? position;
            public string buildPath;
            public string buildTarget;
            public string blenderPath;
            public string scriptPath;
            public LevelSizeParam levelSize;
            public int chunkSize;
            public string[] prefabs;
            public float[] lodLevels;
            public bool autoPrefab;
            public bool randomizeRotation = true;
            public bool randomizeScale = true;
            public float scaleMin = 0.8f;
            public float scaleMax = 1.2f;
            public bool setupLighting = true;
            public bool bakeNavMesh;
            public string[] enemyPrefabs;
            public int enemyMinPerChunk = 1;
            public int enemyMaxPerChunk = 3;
            public string[] lootPrefabs;
            public int lootMinPerChunk = 1;
            public int lootMaxPerChunk = 2;
            public bool attachPatrol = true;
            public float patrolRadius = 5f;
            public int patrolPoints = 3;
            public Vector3? spawnOffset;
            public bool createEnemyTrigger = true;
            public bool createLootTrigger = true;
            public float triggerRadius = 5f;
            public string scatterPrefab;
            public int scatterCount = 10;
            public Vector3? scatterArea;
            public string fieldName;
            public string valueType;
            public int intValue;
            public float floatValue;
            public bool boolValue;
            public string stringValue;
        }

        [Serializable]
        public class LevelSizeParam
        {
            public int x, y, z;
        }

        #endregion
    }
}
#endif
