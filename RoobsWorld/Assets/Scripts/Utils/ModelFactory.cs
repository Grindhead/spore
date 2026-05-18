using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Utils
{
    public static class ModelFactory
    {
        public static GameObject CreateTree(Vector3 position, float scale = 1f, int seed = 0)
        {
            var tree = new GameObject("Tree");
            tree.transform.position = position;

            var trunkMesh = ProceduralMeshFactory.CreateTreeTrunk(0.15f * scale, 0.06f * scale, 3f * scale);
            var trunkObj = new GameObject("Trunk");
            trunkObj.transform.SetParent(tree.transform, false);
            trunkObj.transform.localPosition = Vector3.zero;
            var trunkMf = trunkObj.AddComponent<MeshFilter>();
            trunkMf.mesh = trunkMesh;
            var trunkMr = trunkObj.AddComponent<MeshRenderer>();
            trunkMr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateBarkTexture(), new Color(0.45f, 0.3f, 0.15f));
            trunkObj.AddComponent<MeshCollider>().convex = true;

            var rng = new System.Random(seed);
            float canopyScale = (0.8f + (float)rng.NextDouble() * 0.8f) * scale;
            var canopyMesh = ProceduralMeshFactory.CreateTreeCanopy(canopyScale);
            var canopyObj = new GameObject("Canopy");
            canopyObj.transform.SetParent(tree.transform, false);
            canopyObj.transform.localPosition = Vector3.up * 3f * scale;
            var canopyMf = canopyObj.AddComponent<MeshFilter>();
            canopyMf.mesh = canopyMesh;
            var canopyMr = canopyObj.AddComponent<MeshRenderer>();
            float greenVariation = 0.4f + (float)rng.NextDouble() * 0.3f;
            canopyMr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateLeafTexture(),
                new Color(0.15f, greenVariation, 0.1f));
            canopyObj.AddComponent<SphereCollider>().radius = canopyScale;

            return tree;
        }

        public static GameObject CreateRock(Vector3 position, float scale = 1f, int seed = 0)
        {
            var rockObj = new GameObject("Rock");
            rockObj.transform.position = position;
            var mf = rockObj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateRock(scale, seed);
            var mr = rockObj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateRockTexture(), new Color(0.55f, 0.52f, 0.48f));
            rockObj.AddComponent<MeshCollider>().convex = true;
            return rockObj;
        }

        public static GameObject CreateHut(Vector3 position, float width, float height, Color color)
        {
            var hutObj = new GameObject("Hut");
            hutObj.transform.position = position;
            var mf = hutObj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateHut(width, height);
            var mr = hutObj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateBuildingSurfaceTexture(color), color);
            hutObj.AddComponent<MeshCollider>().convex = true;
            return hutObj;
        }

        public static GameObject CreateCityBuilding(Vector3 position, float width, float height, Color color, int seed = 0)
        {
            var obj = new GameObject("CityBuilding");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateCityBuilding(width, height, seed);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateBuildingSurfaceTexture(color), color);
            obj.AddComponent<BoxCollider>();
            return obj;
        }

        public static GameObject CreateFirePit(Vector3 position, float radius = 0.75f)
        {
            var obj = new GameObject("FirePit");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateFirePit(radius);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateRockTexture(), new Color(0.35f, 0.2f, 0.1f));
            obj.AddComponent<MeshCollider>().convex = true;
            return obj;
        }

        public static GameObject CreateNest(Vector3 position, float radius = 2f)
        {
            var obj = new GameObject("NestVisual");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateNestMesh(radius);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateNestTexture(), new Color(0.7f, 0.55f, 0.25f));
            return obj;
        }

        public static GameObject CreateFoodSource(Vector3 position, Color color)
        {
            var obj = new GameObject("FoodSource");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateFoodBerry(0.2f);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = TempAssetBuilder.CreateTempMaterial(color, "FoodMat");
            obj.AddComponent<SphereCollider>().radius = 0.2f;
            return obj;
        }

        public static GameObject CreateMountain(Vector3 position, float baseRadius, float height, int seed = 0)
        {
            var obj = new GameObject("Mountain");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateMountain(baseRadius, height, seed);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateMountainTexture(), new Color(0.5f, 0.45f, 0.4f));
            obj.AddComponent<MeshCollider>().convex = true;
            return obj;
        }

        public static GameObject CreateSpiceNode(Vector3 position, float scale = 1f)
        {
            var obj = new GameObject("SpiceNode");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * scale;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(0.5f, 10);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateSpiceTexture(), new Color(0.9f, 0.5f, 0.1f));
            obj.AddComponent<SphereCollider>();
            return obj;
        }

        public static GameObject CreateFlagPole(Vector3 position, Color flagColor, float height = 5f)
        {
            var obj = new GameObject("FlagPole");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateFlagPole(height);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = TempAssetBuilder.CreateTempMaterial(flagColor, "FlagMat");
            return obj;
        }

        public static GameObject CreateCityWall(Vector3 position, float radius, float height, Color color)
        {
            var obj = new GameObject("CityWall");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateWallRing(radius, height, 0.3f);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateBuildingSurfaceTexture(color * 0.5f),
                new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.5f));
            return obj;
        }

        public static GameObject CreateSpaceship(Vector3 position)
        {
            var obj = new GameObject("Spaceship");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateSpaceship();
            var mr = obj.AddComponent<MeshRenderer>();
            var shipMat = TempAssetBuilder.CreateTempMaterial(new Color(0.3f, 0.6f, 0.9f), "ShipMat");
            shipMat.SetFloat("_Smoothness", 0.8f);
            mr.sharedMaterial = shipMat;
            obj.AddComponent<MeshCollider>().convex = true;
            return obj;
        }

        public static GameObject CreateStar(Vector3 position, Color color, float size)
        {
            var obj = new GameObject("Star");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * size;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateStarMesh(0.5f);
            var mr = obj.AddComponent<MeshRenderer>();
            var starMat = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateStarTexture(color), color);
            starMat.SetFloat("_Smoothness", 0.9f);
            starMat.EnableKeyword("_EMISSION");
            starMat.SetColor("_EmissionColor", color * 2f);
            mr.sharedMaterial = starMat;
            return obj;
        }

        public static GameObject CreatePlanet(Vector3 position, float radius, Color color)
        {
            var obj = new GameObject("Planet");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(radius, 16);
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreatePlanetTexture(color), Color.white);
            obj.AddComponent<SphereCollider>().radius = radius;
            return obj;
        }

        public static GameObject CreateCell(Vector3 position, Color color, float scale = 1f)
        {
            color = ProceduralTextureFactory.Opaque(color);
            float visualScale = scale * BalanceConstants.CellEntityVisualScaleMultiplier;
            var cellBodyPrefab = BlenderAssetLibrary.LoadCellPartPrefab("cell_body");
            if (cellBodyPrefab != null)
            {
                var prefabCell = Object.Instantiate(cellBodyPrefab);
                prefabCell.name = "Cell";
                prefabCell.transform.position = position;
                prefabCell.transform.localScale = Vector3.one * visualScale;
                BlenderAssetLibrary.DisableColliders(prefabCell);
                BlenderAssetLibrary.TintRenderers(prefabCell, color, 0.22f);

                var sphere = prefabCell.GetComponent<SphereCollider>();
                if (sphere == null)
                    sphere = prefabCell.AddComponent<SphereCollider>();
                sphere.radius = 0.5f;
                sphere.isTrigger = true;

                AddCellEyes(prefabCell.transform, visualScale);
                AddCellCrestSpikes(prefabCell.transform, color, visualScale);
                FitCellColliderToVisual(prefabCell, sphere, 0.5f);
                MenuMicrobeVisuals.Apply(prefabCell, color, 4100 + Mathf.RoundToInt(visualScale * 100f), 4);
                CellSwimVisual.Ensure(prefabCell, CellSwimVisual.SwimProfile.Npc);
                return prefabCell;
            }

            var obj = new GameObject("Cell");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * visualScale;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateCellMesh();
            var mr = obj.AddComponent<MeshRenderer>();
            var tex = ProceduralTextureFactory.CreateCreatureSkinTexture(color, color * 0.8f);
            var cellMat = ProceduralTextureFactory.CreateMaterialWithTexture(tex, Color.white);
            cellMat.SetFloat("_Smoothness", 0.7f);
            cellMat.EnableKeyword("_EMISSION");
            cellMat.SetColor("_EmissionColor", color * 0.22f);
            mr.sharedMaterial = cellMat;
            var collider = obj.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            AddCellEyes(obj.transform, visualScale);
            AddCellCrestSpikes(obj.transform, color, visualScale);
            FitCellColliderToVisual(obj, collider, 0.5f);
            MenuMicrobeVisuals.Apply(obj, color, 4100 + Mathf.RoundToInt(visualScale * 100f), 4);
            CellSwimVisual.Ensure(obj, CellSwimVisual.SwimProfile.Npc);
            return obj;
        }

        private static void FitCellColliderToVisual(GameObject cellRoot, SphereCollider sphere, float minimumRadius)
        {
            if (cellRoot == null || sphere == null)
                return;

            var renderers = cellRoot.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                sphere.radius = Mathf.Max(minimumRadius, sphere.radius);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var centerOffset = bounds.center - cellRoot.transform.position;
            centerOffset.y = 0f;
            float centerOffsetWorld = centerOffset.magnitude;
            float boundsRadiusWorld = Mathf.Sqrt(
                bounds.extents.x * bounds.extents.x +
                bounds.extents.z * bounds.extents.z);
            float requiredWorldRadius = centerOffsetWorld + boundsRadiusWorld;

            float lossyScale = Mathf.Abs(cellRoot.transform.lossyScale.x);
            if (lossyScale < 0.0001f) lossyScale = 1f;

            float requiredLocalRadius = requiredWorldRadius / lossyScale;
            sphere.radius = Mathf.Max(minimumRadius, requiredLocalRadius);
        }

        private static void AddCellEyes(Transform parent, float scale)
        {
            float eyeRadius = Mathf.Clamp(0.05f * scale, 0.03f, 0.08f);
            CreateCellEye(parent, new Vector3(0.11f, 0.08f, 0.4f) * scale, eyeRadius, "CellEye_R");
            CreateCellEye(parent, new Vector3(-0.11f, 0.08f, 0.4f) * scale, eyeRadius, "CellEye_L");
        }

        private static void CreateCellEye(Transform parent, Vector3 localPos, float radius, string name)
        {
            var eye = new GameObject(name);
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;

            var eyeFilter = eye.AddComponent<MeshFilter>();
            eyeFilter.mesh = ProceduralMeshFactory.CreateOrganicSphere(radius, 8);
            var eyeRenderer = eye.AddComponent<MeshRenderer>();
            eyeRenderer.sharedMaterial = TempAssetBuilder.CreateTempMaterial(new Color(1f, 0.98f, 0.9f), "CellEyeMat");

            var pupil = new GameObject("Pupil");
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(0f, 0f, radius * 0.62f);
            var pupilFilter = pupil.AddComponent<MeshFilter>();
            pupilFilter.mesh = ProceduralMeshFactory.CreateOrganicSphere(radius * 0.32f, 6);
            var pupilRenderer = pupil.AddComponent<MeshRenderer>();
            pupilRenderer.sharedMaterial = TempAssetBuilder.CreateTempMaterial(new Color(0.08f, 0.08f, 0.08f), "CellPupilMat");
        }

        private static void AddCellCrestSpikes(Transform parent, Color bodyColor, float scale)
        {
            int spikes = Mathf.Clamp(Mathf.RoundToInt(4f + scale * 2f), 4, 7);
            for (int i = 0; i < spikes; i++)
            {
                float t = i / Mathf.Max(1f, spikes - 1f);
                float angle = Mathf.Lerp(-0.8f, 0.8f, t);
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0.35f, Mathf.Cos(angle)).normalized;

                var spike = new GameObject($"CellCrest_{i}");
                spike.transform.SetParent(parent, false);
                spike.transform.localPosition = dir * (0.42f * scale);
                spike.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);

                var mf = spike.AddComponent<MeshFilter>();
                mf.mesh = CreaturePartMeshFactory.CreateSpike(0.11f * scale, 0.018f * scale);
                var mr = spike.AddComponent<MeshRenderer>();
                mr.sharedMaterial = TempAssetBuilder.CreateTempMaterial(
                    Color.Lerp(bodyColor, new Color(0.95f, 0.85f, 0.65f), 0.55f),
                    "CellCrestMat");
            }
        }

        public static GameObject CreateCellFood(Vector3 position, Color color, float scale = 0.15f, string variantPrefab = null)
        {
            float visualScale = scale * BalanceConstants.CellEntityVisualScaleMultiplier;
            string prefabName = string.IsNullOrWhiteSpace(variantPrefab) ? "cell_food_algae" : variantPrefab;
            var foodPrefab = BlenderAssetLibrary.LoadCellPartPrefab(prefabName);
            if (foodPrefab != null)
            {
                var food = Object.Instantiate(foodPrefab);
                food.name = "CellFood";
                food.transform.position = position;
                food.transform.localScale = Vector3.one * visualScale;
                BlenderAssetLibrary.DisableColliders(food);
                BlenderAssetLibrary.TintRenderers(food, color, 0.2f);

                var sphere = food.GetComponent<SphereCollider>();
                if (sphere == null)
                    sphere = food.AddComponent<SphereCollider>();
                sphere.radius = 0.5f;
                sphere.isTrigger = true;
                ApplyPlanktonStyle(food, color);
                MenuMicrobeVisuals.Apply(food, color, 5200 + prefabName.GetHashCode(), 2, 1.15f);
                CellSwimVisual.Ensure(food, CellSwimVisual.SwimProfile.Plankton);
                return food;
            }

            var obj = new GameObject("CellFood");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * visualScale;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(0.5f, 6);
            var mr = obj.AddComponent<MeshRenderer>();
            var foodMat = TempAssetBuilder.CreateTempMaterial(color, "CellFoodMat");
            foodMat.SetFloat("_Smoothness", 0.5f);
            mr.sharedMaterial = foodMat;
            var foodCollider = obj.AddComponent<SphereCollider>();
            foodCollider.radius = 0.5f;
            foodCollider.isTrigger = true;
            ApplyPlanktonStyle(obj, color);
            MenuMicrobeVisuals.Apply(obj, color, 5200 + color.GetHashCode(), 2, 1.15f);
            CellSwimVisual.Ensure(obj, CellSwimVisual.SwimProfile.Plankton);
            return obj;
        }

        private static void ApplyPlanktonStyle(GameObject root, Color baseColor)
        {
            if (root == null) return;

            // Break up perfect circles so food reads as tiny drifting plankton.
            var scale = root.transform.localScale;
            root.transform.localScale = new Vector3(
                scale.x * Random.Range(0.68f, 0.95f),
                scale.y * Random.Range(0.62f, 0.92f),
                scale.z * Random.Range(1.15f, 1.85f));
            root.transform.localRotation = Quaternion.Euler(
                Random.Range(-20f, 20f),
                Random.Range(0f, 360f),
                Random.Range(-20f, 20f));

            if (root.GetComponentInChildren<MeshRenderer>() == null) return;

            // Add a bright inner core so each particle feels organic instead of geometric.
            var core = new GameObject("PlanktonCore");
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(
                Random.Range(-0.08f, 0.08f),
                Random.Range(-0.08f, 0.08f),
                Random.Range(0.04f, 0.18f));
            core.transform.localScale = Vector3.one * Random.Range(0.16f, 0.24f);

            var mf = core.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(0.5f, 5);
            var mr = core.AddComponent<MeshRenderer>();
            var coreColor = Color.Lerp(baseColor, Color.white, 0.55f);
            var mat = TempAssetBuilder.CreateTempMaterial(coreColor, "PlanktonCoreMat");
            mat.SetFloat("_Smoothness", 0.7f);
            mr.sharedMaterial = mat;
        }

        public static GameObject CreateCellPickup(Vector3 position, Color color)
        {
            Vector3 pickupScale = Vector3.one * (0.2f * BalanceConstants.CellEntityVisualScaleMultiplier);
            var pickupPrefab = BlenderAssetLibrary.LoadCellPartPrefab("cell_pickup");
            if (pickupPrefab != null)
            {
                var pickup = Object.Instantiate(pickupPrefab);
                pickup.name = "CellPickup";
                pickup.transform.position = position;
                pickup.transform.localScale = pickupScale;
                BlenderAssetLibrary.DisableColliders(pickup);
                BlenderAssetLibrary.TintRenderers(pickup, color, 0.5f);

                var sphere = pickup.GetComponent<SphereCollider>();
                if (sphere == null)
                    sphere = pickup.AddComponent<SphereCollider>();
                sphere.radius = 0.5f;
                sphere.isTrigger = true;
                MenuMicrobeVisuals.Apply(pickup, color, 6300 + color.GetHashCode(), 6, 1.25f);
                CellSwimVisual.Ensure(pickup, CellSwimVisual.SwimProfile.Pickup);
                return pickup;
            }

            var obj = new GameObject("CellPickup");
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateCellMesh();
            var mr = obj.AddComponent<MeshRenderer>();
            var pickupMat = TempAssetBuilder.CreateTempMaterial(color, "PickupMat");
            pickupMat.SetFloat("_Smoothness", 0.6f);
            pickupMat.EnableKeyword("_EMISSION");
            pickupMat.SetColor("_EmissionColor", color * 0.5f);
            mr.sharedMaterial = pickupMat;
            obj.transform.localScale = pickupScale;
            var pickupCollider = obj.AddComponent<SphereCollider>();
            pickupCollider.radius = 0.5f;
            pickupCollider.isTrigger = true;
            MenuMicrobeVisuals.Apply(obj, color, 6300 + color.GetHashCode(), 6, 1.25f);
            CellSwimVisual.Ensure(obj, CellSwimVisual.SwimProfile.Pickup);
            return obj;
        }

        public static GameObject CreateToxinCloud(Vector3 position, float radius)
        {
            float visualDiameter = radius * 2f * BalanceConstants.CellEntityVisualScaleMultiplier;
            var cloudPrefab = BlenderAssetLibrary.LoadCellPartPrefab("cell_toxin_cloud");
            if (cloudPrefab != null)
            {
                var cloud = Object.Instantiate(cloudPrefab);
                cloud.name = "ToxinCloudVisual";
                cloud.transform.position = position;
                cloud.transform.localScale = Vector3.one * visualDiameter;
                BlenderAssetLibrary.DisableColliders(cloud);
                BlenderAssetLibrary.TintRenderers(cloud, new Color(0.48f, 0.92f, 0.26f), 0.15f);
                ApplyTransparentTint(cloud, new Color(0.48f, 0.92f, 0.26f, 0.24f));
                return cloud;
            }

            var obj = new GameObject("ToxinCloudVisual");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * visualDiameter;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = ProceduralMeshFactory.CreateOrganicSphere(0.5f, 8);
            var mr = obj.AddComponent<MeshRenderer>();
            var mat = TempAssetBuilder.CreateTempMaterial(new Color(0.4f, 0.9f, 0.1f, 0.25f), "ToxinMat");
            mat.SetFloat("_Surface", 1);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mr.material = mat;
            return obj;
        }

        private static void ApplyTransparentTint(GameObject root, Color tint)
        {
            if (root == null) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;

                var mat = renderer.material;
                if (mat == null) continue;

                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
                if (mat.HasProperty("_SrcBlend"))
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend"))
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite"))
                    mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", tint);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", tint);
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", tint * 0.35f);
            }
        }

        public static GameObject CreateGround(float size, Material material)
        {
            return CreateGround(size, material, size / 10f);
        }

        public static GameObject CreateGround(float size, Material material, float uvTiling)
        {
            var obj = new GameObject("Ground");
            var mf = obj.AddComponent<MeshFilter>();

            int segs = 10;
            float half = size * 0.5f;
            var mesh = new Mesh { name = "GroundPlane" };
            int vertCount = (segs + 1) * (segs + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int y = 0; y <= segs; y++)
            {
                for (int x = 0; x <= segs; x++)
                {
                    float px = Mathf.Lerp(-half, half, (float)x / segs);
                    float pz = Mathf.Lerp(-half, half, (float)y / segs);
                    verts[v] = new Vector3(px, 0, pz);
                    normals[v] = Vector3.up;
                    uvs[v] = new Vector2((float)x / segs * uvTiling, (float)y / segs * uvTiling);
                    v++;
                }
            }

            int triCount = segs * segs * 6;
            var tris = new int[triCount];
            int t = 0;
            int cols = segs + 1;
            for (int row = 0; row < segs; row++)
            {
                for (int col = 0; col < segs; col++)
                {
                    int a = row * cols + col;
                    int b = a + cols;
                    tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
                }
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mf.mesh = mesh;

            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = material;
            obj.AddComponent<MeshCollider>();
            return obj;
        }

        public static GameObject CreateResourceNode(Vector3 position, ResourceNodeType type)
        {
            Color color;
            string label;
            float scale;

            switch (type)
            {
                case ResourceNodeType.Food:
                    color = new Color(0.8f, 0.2f, 0.2f);
                    label = "FoodBush";
                    scale = 0.8f;
                    break;
                case ResourceNodeType.Wood:
                    color = new Color(0.5f, 0.3f, 0.1f);
                    label = "WoodPile";
                    scale = 1f;
                    break;
                default:
                    color = new Color(0.6f, 0.6f, 0.55f);
                    label = "StoneDeposit";
                    scale = 1.2f;
                    break;
            }

            var obj = new GameObject(label);
            obj.transform.position = position;
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = type == ResourceNodeType.Wood
                ? ProceduralMeshFactory.CreateTreeTrunk(0.2f, 0.15f, 0.6f)
                : ProceduralMeshFactory.CreateRock(scale, position.GetHashCode());
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = TempAssetBuilder.CreateTempMaterial(color, label + "Mat");
            obj.AddComponent<SphereCollider>().radius = scale * 0.5f;
            return obj;
        }

        public static GameObject CreateCreatureModel(Vector3 position, Color bodyColor)
        {
            bodyColor = ProceduralTextureFactory.Opaque(bodyColor);
            var dna = CreatureDNA.CreateDefault();
            dna.Colors.PrimaryColor = bodyColor;
            dna.Colors.SecondaryColor = Color.Lerp(bodyColor, Color.black, 0.35f);
            dna.Colors.AccentColor = Color.Lerp(Color.white, bodyColor, 0.28f);
            return CreateCreatureModel(position, dna);
        }

        public static GameObject CreateCreatureModel(Vector3 position, CreatureDNA dna)
        {
            return CreateCreatureModel(position, dna, true);
        }

        public static GameObject CreateCreatureModel(Vector3 position, CreatureDNA dna, bool renderSegmentGeometry)
        {
            dna ??= CreatureDNA.CreateDefault();
            var root = new GameObject("Creature");
            root.transform.position = position;
            root.transform.localScale = dna.Body != null && dna.Body.BodyScale != Vector3.zero
                ? dna.Body.BodyScale
                : Vector3.one;
            CreatureModelBuilder.Build(root, dna, CreaturePartCatalog.GetDefaultCatalog(), renderSegmentGeometry);
            return root;
        }

        private static void CreateEyePair(Transform parent, Vector3 rightPos)
        {
            CreateEye(parent, rightPos);
            CreateEye(parent, new Vector3(-rightPos.x, rightPos.y, rightPos.z));
        }

        private static void CreateEye(Transform parent, Vector3 pos)
        {
            var eye = new GameObject("Eye");
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = pos;
            var emf = eye.AddComponent<MeshFilter>();
            emf.mesh = CreaturePartMeshFactory.CreateEye();
            var emr = eye.AddComponent<MeshRenderer>();
            emr.material = TempAssetBuilder.CreateTempMaterial(Color.white, "EyeMat");

            var pupil = new GameObject("Pupil");
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = Vector3.forward * 0.03f;
            var pmf = pupil.AddComponent<MeshFilter>();
            pmf.mesh = CreaturePartMeshFactory.CreatePupil();
            var pmr = pupil.AddComponent<MeshRenderer>();
            pmr.material = TempAssetBuilder.CreateTempMaterial(Color.black, "PupilMat");
        }

        private static void CreateModelLeg(Transform parent, Vector3 pos, Color color)
        {
            var leg = new GameObject("Leg");
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = pos;
            var mf = leg.AddComponent<MeshFilter>();
            mf.mesh = CreaturePartMeshFactory.CreateLeg();
            var mr = leg.AddComponent<MeshRenderer>();
            mr.material = TempAssetBuilder.CreateTempMaterial(color, "LegMat");
        }

        public enum ResourceNodeType { Food, Wood, Stone }
    }
}
