using System.Collections.Generic;
using System.Text;
using System;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Scripting;
using System.Security.Cryptography;
using System.Threading;
using UnityEditor.Graphs;

namespace ZyGame.Replacement
{
    public class SkinnedMeshCombiner //: MonoBehaviour
    {
        //Private constants
        private const int MAX_VERTICES_FOR_16BITS_MESH = 50000; //NOT change this

        //Private variables
        private Vector3 thisOriginalPosition = Vector3.zero;
        private Vector3 thisOriginalRotation = Vector3.zero;
        private Vector3 thisOriginalScale = Vector3.one;

        //Enums of script
        public enum LogTypeOf
        {
            Assert,
            Error,
            Exception,
            Log,
            Warning
        }
        public enum MergeMethod
        {
            OneMeshPerMaterial,
            AllInOne,
            JustMaterialColors,
            OnlyAnima2dMeshes
        }
        public enum AtlasSize
        {
            Pixels32x32,
            Pixels64x64,
            Pixels128x128,
            Pixels256x256,
            Pixels512x512,
            Pixels1024x1024,
            Pixels2048x2048,
            Pixels4096x4096,
            Pixels8192x8192
        }
        public enum AnimQuality
        {
            UseQualitySettings,
            Bad,
            Good,
            VeryGood
        }
        public enum MipMapEdgesSize
        {
            Pixels0x0,
            Pixels16x16,
            Pixels32x32,
            Pixels64x64,
            Pixels128x128,
            Pixels256x256,
            Pixels512x512,
            Pixels1024x1024,
        }
        public enum AtlasPadding
        {
            Pixels0x0,
            Pixels2x2,
            Pixels4x4,
            Pixels8x8,
            Pixels16x16,
        }
        public enum MergeTiledTextures
        {
            SkipAll,
            ImprovedMode,
            LegacyMode
        }
        public enum BlendShapesSupport
        {
            Disabled,
            Enabled,
            FullSupport
        }
        public enum CombineOnStart
        {
            Disabled,
            OnStart,
            OnAwake
        }
        public enum RootBoneToUse
        {
            Automatic,
            Manual
        }

        //Classes of script
        [Serializable]
        public class LogOfMerge
        {
            public string content;
            public LogTypeOf logType;

            public LogOfMerge(string content, LogTypeOf logType)
            {
                this.content = content;
                this.logType = logType;
            }
        }
        [Serializable]
        public class OneMeshPerMaterialParams
        {
            public bool mergeOnlyEqualRootBones = false;
        }
        [Serializable]
        public class JustMaterialColorsParams
        {
            public Material materialToUse;
            public bool mergeOnlyEqualsRootBones = false;
            public bool useDefaultColorProperty = true;
            public string colorPropertyToFind = "_Color";
            public string mainTexturePropertyToInsert = "_MainTex";
        }
        [Serializable]
        public class AllInOneParams
        {
            public Material materialToUse;
            public AtlasSize atlasResolution = AtlasSize.Pixels512x512;
            public MipMapEdgesSize mipMapEdgesSize = MipMapEdgesSize.Pixels64x64;
            public AtlasPadding atlasPadding = AtlasPadding.Pixels0x0;
            public MergeTiledTextures mergeTiledTextures = MergeTiledTextures.LegacyMode;
            public bool mergeOnlyEqualsRootBones = false;
            public bool useDefaultMainTextureProperty = true;
            public string mainTexturePropertyToFind = "_MainTex";
            public string mainTexturePropertyToInsert = "_MainTex";
            public bool metallicMapSupport = false;
            public string metallicMapPropertyToFind = "_MetallicGlossMap";
            public string metallicMapPropertyToInsert = "_MetallicGlossMap";
            public bool specularMapSupport = false;
            public string specularMapPropertyToFind = "_SpecGlossMap";
            public string specularMapPropertyToInsert = "_SpecGlossMap";
            public bool normalMapSupport = false;
            public string normalMapPropertyToFind = "_BumpMap";
            public string normalMapPropertyToInsert = "_BumpMap";
            public bool normalMap2Support = false;
            public string normalMap2PropertyFind = "_DetailNormalMap";
            public string normalMap2PropertyToInsert = "_DetailNormalMap";
            public bool heightMapSupport = false;
            public string heightMapPropertyToFind = "_ParallaxMap";
            public string heightMapPropertyToInsert = "_ParallaxMap";
            public bool occlusionMapSupport = false;
            public string occlusionMapPropertyToFind = "_OcclusionMap";
            public string occlusionMapPropertyToInsert = "_OcclusionMap";
            public bool detailAlbedoMapSupport = false;
            public string detailMapPropertyToFind = "_DetailAlbedoMap";
            public string detailMapPropertyToInsert = "_DetailAlbedoMap";
            public bool detailMaskSupport = false;
            public string detailMaskPropertyToFind = "_DetailMask";
            public string detailMaskPropertyToInsert = "_DetailMask";
            public bool pinkNormalMapsFix = true;
        }

        public GameObject[] resultMergeOriginalGameObjects = null;
        public GameObject resultMergeGameObject = null;
        public MergeMethod mergeMethod = MergeMethod.AllInOne;
        public OneMeshPerMaterialParams oneMeshPerMaterialParams = new OneMeshPerMaterialParams();
        public JustMaterialColorsParams justMaterialColorsParams = new JustMaterialColorsParams();
        public AllInOneParams allInOneParams = new AllInOneParams();
        public BlendShapesSupport blendShapesSupport = BlendShapesSupport.Disabled;
        public float blendShapesMultiplier = 1.0f;
        public RootBoneToUse rootBoneToUse = RootBoneToUse.Automatic;
        public Transform manualRootBoneToUse = null;
        public bool autoManagePosition = true;
        public bool compatibilityMode = true;
        public bool combineInactives = false;
        public CombineOnStart combineOnStart = CombineOnStart.Disabled;
        public string nameOfThisMerge = "Combined Meshes";
        public bool highlightUvVertices = false;

        public bool isMeshesCombined()
        {
            //Return if the meshes is combined
            if (resultMergeGameObject != null)
            {
                return true;
            }
            return false;
        }

        public void CombineMeshes(MergeMethod mergeMethod, GameObject gameObject)
        {
            this.mergeMethod = mergeMethod;
            DoCombineMeshs_AllInOne(gameObject);
        }

        public void DoCombineMeshs_AllInOne(GameObject gameObject)
        {
            //Verify if the meshes are already merged
            if (resultMergeGameObject != null)
            {
                LaunchLog("Currently, this character's meshes are already merged. Please, before making a new merge, undo the merge previously done.", LogTypeOf.Warning);
                return;
            }
            allInOneParams.atlasPadding = AtlasPadding.Pixels0x0;
            allInOneParams.atlasResolution = AtlasSize.Pixels2048x2048;
            //Reset position, rotation and scale and store it (to avoid problems with matrix or blendshapes positioning for example)
            if (autoManagePosition == true)
            {
                thisOriginalPosition = gameObject.transform.position;
                thisOriginalRotation = gameObject.transform.eulerAngles;
                thisOriginalScale = gameObject.transform.localScale;
                gameObject.transform.position = Vector3.zero;
                gameObject.transform.eulerAngles = Vector3.zero;
                gameObject.transform.localScale = Vector3.one;
            }

            //Try to merge. If occurs error, stop merge
            try
            {
                //Validate all variables
                ValidateAllVariables();
                DateTime timeOfStart = DateTime.Now;
                int verticesCount = 0;
                int mergedMeshes = 0;
                int drawCallReduction = 0;
                int materialCount = 0;
                int originalUvLenght = 0;
                //Get all GameObjects to merge
                GameObject[] gameObjectsToMerge = GetAllItemsForCombine(gameObject, false, true);

                //Get all Skinned Mesh Renderers to merge
                SkinnedMeshRenderer[] skinnedMeshesToMerge = GetAllSkinnedMeshsValidatedToCombine(gameObjectsToMerge);

                //Stop the merge if not have meshes to merge
                if (skinnedMeshesToMerge == null || skinnedMeshesToMerge.Length < 1)
                {
                    LaunchLog("The merge has been canceled. There may not be enough meshes to be combined. At least 1 valid mesh is required for the merge process to be possible. Also, there is the possibility that all the meshes found, are invalid or have been ignored during the merge process.", LogTypeOf.Warning);
                    StopMergeByErrorWhileMerging(null);
                    return;
                }

                //Verify if exists different root bones
                if (ExistsDifferentRootBones(skinnedMeshesToMerge, true) == true && allInOneParams.mergeOnlyEqualsRootBones == true)
                {
                    StopMergeByErrorWhileMerging(null);
                    return;
                }

                //Count vertices
                verticesCount = CountVerticesInAllMeshes(skinnedMeshesToMerge);

                //------------------------------- START OF MERGE CODE --------------------------------

                //Prepare the storage
                List<CombineInstance> combinesToMerge = new List<CombineInstance>();
                List<Transform> bonesToMerge = new List<Transform>();
                List<Matrix4x4> bindPosesToMerge = new List<Matrix4x4>();
                List<TexturesSubMeshes> texturesAndSubMeshes = new List<TexturesSubMeshes>();

                //Prepare the progress bar to read mesh progress (It is used only in editor to show on progress bar)
                int totalSubMeshsInAllSkinnedMeshes = 0;
                foreach (SkinnedMeshRenderer meshRenderer in skinnedMeshesToMerge)
                    totalSubMeshsInAllSkinnedMeshes += meshRenderer.sharedMesh.subMeshCount;
                int totalSkinnedMeshesVerifiedAtHere = 0;

                //Obtains the data of each merge
                int totalVerticesVerifiedAtHere = 0;
                foreach (SkinnedMeshRenderer meshRender in skinnedMeshesToMerge)
                {
                    //Get the data of merge for each submesh of this mesh
                    for (int i = 0; i < meshRender.sharedMesh.subMeshCount; i++)
                    {
                        //Show progress bar
                        float progressOfThisMeshRead = ((float)totalSkinnedMeshesVerifiedAtHere) / ((float)totalSubMeshsInAllSkinnedMeshes + 1);

                        //Add bone to list of bones to merge and set bones bindposes
                        Transform[] currentMeshBones = meshRender.bones;
                        for (int x = 0; x < currentMeshBones.Length; x++)
                        {
                            bonesToMerge.Add(currentMeshBones[x]);
                            if (compatibilityMode == true)
                                bindPosesToMerge.Add(meshRender.sharedMesh.bindposes[x] * meshRender.transform.worldToLocalMatrix);
                            if (compatibilityMode == false)
                                bindPosesToMerge.Add(currentMeshBones[x].worldToLocalMatrix * meshRender.transform.worldToLocalMatrix);
                        }

                        //Configure the Combine Instances for each submesh or mesh
                        CombineInstance combineInstance = new CombineInstance();
                        combineInstance.mesh = meshRender.sharedMesh;
                        combineInstance.subMeshIndex = i;
                        combineInstance.transform = meshRender.transform.localToWorldMatrix;
                        combinesToMerge.Add(combineInstance);

                        //Get the entire UV map of this submesh
                        Vector2[] uvMapOfThisSubMesh = combineInstance.mesh.SMCGetSubmesh(i).uv;

                        //Check if UV of this mesh uses a tiled texture (first, get the bounds values of UV of this mesh)
                        TexturesSubMeshes.UvBounds boundDataOfUv = GetBoundValuesOfSubMeshUv(uvMapOfThisSubMesh);
                        //If merge of tiled meshs is legacy, force all textures to be a normal textures, to rest of merge run as normal textures
                        if (allInOneParams.mergeTiledTextures == MergeTiledTextures.LegacyMode)
                        {
                            boundDataOfUv.majorX = 1.0f;
                            boundDataOfUv.majorY = 1.0f;
                            boundDataOfUv.minorX = 0.0f;
                            boundDataOfUv.minorY = 0.0f;
                        }
                        boundDataOfUv.RoundBoundsValuesAndCalculateSpaceNeededToTiling(); //<- This is necessary to avoid calcs problemns with float precision of Unity

                        //If UV of this mesh, use a tiled texture, create another item to storage the data for only this submesh
                        if (isTiledTexture(boundDataOfUv) == true)
                        {
                            //Create another texture and respective submeshes to store it
                            TexturesSubMeshes thisTextureAndSubMesh = new TexturesSubMeshes();

                            //Calculate and get original resolution of main texture of this material
                            Texture2D mainTextureOfThisMaterial = (Texture2D)meshRender.sharedMaterials[i].GetTexture(allInOneParams.mainTexturePropertyToFind);
                            Vector2Int mainTextureSize = Vector2Int.zero;
                            Vector2Int mainTextureSizeWithEdges = Vector2Int.zero;
                            if (mainTextureOfThisMaterial == null)
                                mainTextureSize = new Vector2Int(64, 64);
                            if (mainTextureOfThisMaterial != null)
                                mainTextureSize = new Vector2Int(mainTextureOfThisMaterial.width, mainTextureOfThisMaterial.height);
                            mainTextureSizeWithEdges = new Vector2Int(mainTextureSize.x + (GetEdgesSizeForTextures() * 2), mainTextureSize.y + (GetEdgesSizeForTextures() * 2));

                            //Fill this class
                            thisTextureAndSubMesh.material = meshRender.sharedMaterials[i];
                            thisTextureAndSubMesh.isTiledTexture = true;
                            thisTextureAndSubMesh.mainTextureResolution = mainTextureSize;
                            thisTextureAndSubMesh.mainTextureResolutionWithEdges = mainTextureSizeWithEdges;
                            thisTextureAndSubMesh.mainTexture = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.mainTexturePropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.MainTexture, true, progressOfThisMeshRead);
                            if (allInOneParams.metallicMapSupport == true)
                                thisTextureAndSubMesh.metallicMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.metallicMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.MetallicMap, true, progressOfThisMeshRead);
                            if (allInOneParams.specularMapSupport == true)
                                thisTextureAndSubMesh.specularMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.specularMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.SpecularMap, true, progressOfThisMeshRead);
                            if (allInOneParams.normalMapSupport == true)
                                thisTextureAndSubMesh.normalMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.normalMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.NormalMap, true, progressOfThisMeshRead);
                            if (allInOneParams.normalMap2Support == true)
                                thisTextureAndSubMesh.normalMap2 = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.normalMap2PropertyFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.NormalMap, true, progressOfThisMeshRead);
                            if (allInOneParams.heightMapSupport == true)
                                thisTextureAndSubMesh.heightMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.heightMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.HeightMap, true, progressOfThisMeshRead);
                            if (allInOneParams.occlusionMapSupport == true)
                                thisTextureAndSubMesh.occlusionMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.occlusionMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.OcclusionMap, true, progressOfThisMeshRead);
                            if (allInOneParams.detailAlbedoMapSupport == true)
                                thisTextureAndSubMesh.detailMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.detailMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.DetailMap, true, progressOfThisMeshRead);
                            if (allInOneParams.detailMaskSupport == true)
                                thisTextureAndSubMesh.detailMask = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.detailMaskPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.DetailMask, true, progressOfThisMeshRead);

                            //Create this mesh data. get all UV values from this submesh
                            TexturesSubMeshes.UserSubMeshes userSubMesh = new TexturesSubMeshes.UserSubMeshes();
                            userSubMesh.uvBoundsOfThisSubMesh = boundDataOfUv;
                            userSubMesh.startOfUvVerticesInIndex = totalVerticesVerifiedAtHere;
                            userSubMesh.originalUvVertices = new Vector2[uvMapOfThisSubMesh.Length];
                            for (int v = 0; v < userSubMesh.originalUvVertices.Length; v++)
                                userSubMesh.originalUvVertices[v] = uvMapOfThisSubMesh[v];
                            thisTextureAndSubMesh.userSubMeshes.Add(userSubMesh);

                            //Save the created class
                            texturesAndSubMeshes.Add(thisTextureAndSubMesh);
                        }

                        //If UV of this mesh, use a normal texture
                        if (isTiledTexture(boundDataOfUv) == false)
                        {
                            //Try to find a texture and respective submeshes that already is created that is using this texture
                            TexturesSubMeshes textureOfThisSubMesh = GetTheTextureSubMeshesOfMaterial(meshRender.sharedMaterials[i], texturesAndSubMeshes);

                            //If not found
                            if (textureOfThisSubMesh == null)
                            {
                                //Create another texture and respective submeshes to store it
                                TexturesSubMeshes thisTextureAndSubMesh = new TexturesSubMeshes();

                                //Calculate and get original resolution of main texture of this material
                                Texture2D mainTextureOfThisMaterial = (Texture2D)meshRender.sharedMaterials[i].GetTexture(allInOneParams.mainTexturePropertyToFind);
                                Vector2Int mainTextureSize = Vector2Int.zero;
                                Vector2Int mainTextureSizeWithEdges = Vector2Int.zero;
                                if (mainTextureOfThisMaterial == null)
                                    mainTextureSize = new Vector2Int(64, 64);
                                if (mainTextureOfThisMaterial != null)
                                    mainTextureSize = new Vector2Int(mainTextureOfThisMaterial.width, mainTextureOfThisMaterial.height);
                                mainTextureSizeWithEdges = new Vector2Int(mainTextureSize.x + (GetEdgesSizeForTextures() * 2), mainTextureSize.y + (GetEdgesSizeForTextures() * 2));

                                //Fill this class
                                thisTextureAndSubMesh.material = meshRender.sharedMaterials[i];
                                thisTextureAndSubMesh.isTiledTexture = false;
                                thisTextureAndSubMesh.mainTextureResolution = mainTextureSize;
                                thisTextureAndSubMesh.mainTextureResolutionWithEdges = mainTextureSizeWithEdges;
                                thisTextureAndSubMesh.mainTexture = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.mainTexturePropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.MainTexture, true, progressOfThisMeshRead);
                                if (allInOneParams.metallicMapSupport == true)
                                    thisTextureAndSubMesh.metallicMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.metallicMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.MetallicMap, true, progressOfThisMeshRead);
                                if (allInOneParams.specularMapSupport == true)
                                    thisTextureAndSubMesh.specularMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.specularMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.SpecularMap, true, progressOfThisMeshRead);
                                if (allInOneParams.normalMapSupport == true)
                                    thisTextureAndSubMesh.normalMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.normalMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.NormalMap, true, progressOfThisMeshRead);
                                if (allInOneParams.normalMap2Support == true)
                                    thisTextureAndSubMesh.normalMap2 = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.normalMap2PropertyFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.NormalMap, true, progressOfThisMeshRead);
                                if (allInOneParams.heightMapSupport == true)
                                    thisTextureAndSubMesh.heightMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.heightMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.HeightMap, true, progressOfThisMeshRead);
                                if (allInOneParams.occlusionMapSupport == true)
                                    thisTextureAndSubMesh.occlusionMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.occlusionMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.OcclusionMap, true, progressOfThisMeshRead);
                                if (allInOneParams.detailAlbedoMapSupport == true)
                                    thisTextureAndSubMesh.detailMap = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.detailMapPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.DetailMap, true, progressOfThisMeshRead);
                                if (allInOneParams.detailMaskSupport == true)
                                    thisTextureAndSubMesh.detailMask = GetValidatedCopyOfTexture(thisTextureAndSubMesh.material, allInOneParams.detailMaskPropertyToFind, thisTextureAndSubMesh.mainTextureResolutionWithEdges.x, thisTextureAndSubMesh.mainTextureResolutionWithEdges.y, boundDataOfUv, TextureType.DetailMask, true, progressOfThisMeshRead);

                                //Create this mesh data. get all UV values from this submesh
                                TexturesSubMeshes.UserSubMeshes userSubMesh = new TexturesSubMeshes.UserSubMeshes();
                                userSubMesh.uvBoundsOfThisSubMesh = boundDataOfUv;
                                userSubMesh.startOfUvVerticesInIndex = totalVerticesVerifiedAtHere;
                                userSubMesh.originalUvVertices = new Vector2[uvMapOfThisSubMesh.Length];
                                for (int v = 0; v < userSubMesh.originalUvVertices.Length; v++)
                                    userSubMesh.originalUvVertices[v] = uvMapOfThisSubMesh[v];
                                thisTextureAndSubMesh.userSubMeshes.Add(userSubMesh);

                                //Save the created class
                                texturesAndSubMeshes.Add(thisTextureAndSubMesh);
                            }

                            //If found
                            if (textureOfThisSubMesh != null)
                            {
                                //Create this mesh data and add to textures that already exists. get all UV values from this submesh
                                TexturesSubMeshes.UserSubMeshes userSubMesh = new TexturesSubMeshes.UserSubMeshes();
                                userSubMesh.uvBoundsOfThisSubMesh = boundDataOfUv;
                                userSubMesh.startOfUvVerticesInIndex = totalVerticesVerifiedAtHere;
                                userSubMesh.originalUvVertices = new Vector2[uvMapOfThisSubMesh.Length];
                                for (int v = 0; v < userSubMesh.originalUvVertices.Length; v++)
                                    userSubMesh.originalUvVertices[v] = uvMapOfThisSubMesh[v];
                                textureOfThisSubMesh.userSubMeshes.Add(userSubMesh);
                            }
                        }

                        //Increment stats
                        mergedMeshes += 1;
                        drawCallReduction += 1;
                        materialCount = texturesAndSubMeshes.Count;
                        originalUvLenght += uvMapOfThisSubMesh.Length;

                        //Add the total vertices verified
                        totalVerticesVerifiedAtHere += uvMapOfThisSubMesh.Length;

                        //Update the value of progress bar of readed meshes
                        totalSkinnedMeshesVerifiedAtHere += 1;
                    }
                }

                //Show progress bar

                //Combine all submeshes into one mesh with submeshes with all materials
                Mesh finalMesh = new Mesh();
                if (verticesCount <= MAX_VERTICES_FOR_16BITS_MESH)
                    finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                if (verticesCount > MAX_VERTICES_FOR_16BITS_MESH)
                    finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                finalMesh.name = "Combined Meshes (All In One)";
                finalMesh.CombineMeshes(combinesToMerge.ToArray(), true, true);

                //Do recalculations where is desired
                finalMesh.RecalculateBounds();

                //Create the holder GameObject
                resultMergeGameObject = new GameObject(nameOfThisMerge);
                resultMergeGameObject.transform.SetParent(gameObject.transform);
                SkinnedMeshRenderer smrender = resultMergeGameObject.AddComponent<SkinnedMeshRenderer>();
                smrender.sharedMesh = finalMesh;
                smrender.bones = bonesToMerge.ToArray();
                smrender.sharedMesh.bindposes = bindPosesToMerge.ToArray();
                smrender.rootBone = GetCorrectRootBoneFromAllOriginalSkinnedMeshRenderers(skinnedMeshesToMerge);
                smrender.sharedMaterials = new Material[] { GetValidatedCopyOfMaterial(allInOneParams.materialToUse, true, true) };
                smrender.sharedMaterials[0].name = "Combined Materials (All In One)";

                //Process and merge blendshapes of all original skinned mesh renderers, if full support for blendshapes is desired
                //if (blendShapesSupport == BlendShapesSupport.FullSupport)
                MergeAndGetAllBlendShapeDataOfSkinnedMeshRenderers(skinnedMeshesToMerge, finalMesh, smrender);

                //Create all atlas using all collected textures
                AtlasData atlasGenerated = CreateAllAtlas(texturesAndSubMeshes, GetAtlasMaxResolution(), GetAtlasPadding(), true);

                //Show progress bar

                //If the UV map of this mesh is inexistent
                if (smrender.sharedMesh.uv.Length == 0)
                {
                    LaunchLog("It was not possible to create a UV map for the combined mesh. Originally, this character's meshes do not have a UV mapping. Create a UV mapping for this character or try using a blending method that does not work with UV mapping, such as One Mesh Per Material.", LogTypeOf.Error);
                    StopMergeByErrorWhileMerging(null);
                    return;
                }

                //Process each submesh UV data and create a new entire UV map for combined mesh
                Vector2[] newUvMapForCombinedMesh = new Vector2[smrender.sharedMesh.uv.Length];
                foreach (TexturesSubMeshes thisTexture in texturesAndSubMeshes)
                {
                    //Convert all vertices of all submeshes of this texture, to positive, if is a tiled texture
                    if (thisTexture.isTiledTexture == true)
                        thisTexture.ConvertAllSubMeshsVerticesToPositive();

                    //Process each submesh registered as user of this texture
                    foreach (TexturesSubMeshes.UserSubMeshes submesh in thisTexture.userSubMeshes)
                    {
                        //If this is a normal texture, not is a tiled texture (merge with the basic UV mapping algorthm)
                        if (thisTexture.isTiledTexture == false)
                        {
                            //Change all vertex of UV to positive, where vertex position is major than 1 or minor than 0, because the entire UV will resized to fit in your respective texture in atlas
                            for (int i = 0; i < submesh.originalUvVertices.Length; i++)
                            {
                                if (submesh.originalUvVertices[i].x < 0)
                                    submesh.originalUvVertices[i].x = submesh.originalUvVertices[i].x * -1;
                                if (submesh.originalUvVertices[i].y < 0)
                                    submesh.originalUvVertices[i].y = submesh.originalUvVertices[i].y * -1;
                            }

                            //Calculates the highest point of the UV map of each mesh, for know how to reduces to fit in texture atlas, checks which is the largest coordinate found in the list of UV vertices, in X or Y and stores it
                            Vector2 highestVertexCoordinatesForThisSubmesh = Vector2.zero;
                            for (int i = 0; i < submesh.originalUvVertices.Length; i++)
                                highestVertexCoordinatesForThisSubmesh = new Vector2(Mathf.Max(submesh.originalUvVertices[i].x, highestVertexCoordinatesForThisSubmesh.x), Mathf.Max(submesh.originalUvVertices[i].y, highestVertexCoordinatesForThisSubmesh.y));

                            //Calculate the percentage that the edge of this texture uses, calculates the size of the uv for each texture, to ignore the edges
                            Vector2 percentEdgeUsageOfCurrentTexture = thisTexture.GetEdgesPercentUsageOfThisTextures();

                            //Get index of this main texture submesh in atlas rects
                            int mainTextureIndexInAtlas = atlasGenerated.GetRectIndexOfThatMainTexture(thisTexture.mainTexture);

                            //Process all uv vertices of this submesh
                            for (int i = 0; i < submesh.originalUvVertices.Length; i++)
                            {
                                //Create the vertice
                                Vector2 thisVertex = Vector2.zero;

                                //If the UV map of this mesh is not larger than the texture
                                if (highestVertexCoordinatesForThisSubmesh.x <= 1)
                                    thisVertex.x = Mathf.Lerp(atlasGenerated.atlasRects[mainTextureIndexInAtlas].xMin, atlasGenerated.atlasRects[mainTextureIndexInAtlas].xMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.x, 1 - percentEdgeUsageOfCurrentTexture.x, submesh.originalUvVertices[i].x));
                                if (highestVertexCoordinatesForThisSubmesh.y <= 1)
                                    thisVertex.y = Mathf.Lerp(atlasGenerated.atlasRects[mainTextureIndexInAtlas].yMin, atlasGenerated.atlasRects[mainTextureIndexInAtlas].yMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.y, 1 - percentEdgeUsageOfCurrentTexture.y, submesh.originalUvVertices[i].y));

                                //If the UV map is larger than the texture
                                if (highestVertexCoordinatesForThisSubmesh.x > 1)
                                    thisVertex.x = Mathf.Lerp(atlasGenerated.atlasRects[mainTextureIndexInAtlas].xMin, atlasGenerated.atlasRects[mainTextureIndexInAtlas].xMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.x, 1 - percentEdgeUsageOfCurrentTexture.x, submesh.originalUvVertices[i].x / highestVertexCoordinatesForThisSubmesh.x));
                                if (highestVertexCoordinatesForThisSubmesh.y > 1)
                                    thisVertex.y = Mathf.Lerp(atlasGenerated.atlasRects[mainTextureIndexInAtlas].yMin, atlasGenerated.atlasRects[mainTextureIndexInAtlas].yMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.y, 1 - percentEdgeUsageOfCurrentTexture.y, submesh.originalUvVertices[i].y / highestVertexCoordinatesForThisSubmesh.y));

                                //Save this vertice edited in uv map of combined mesh
                                newUvMapForCombinedMesh[i + submesh.startOfUvVerticesInIndex] = thisVertex;
                            }
                        }

                        //If this is a tiled texture, not is a normal texture
                        if (thisTexture.isTiledTexture == true)
                        {
                            //Calculates the highest point of the UV map of each mesh, for know how to reduces to fit in texture atlas, checks which is the largest coordinate found in the list of UV vertices, in X or Y and stores it
                            Vector2 highestVertexCoordinatesForThisSubmesh = Vector2.zero;
                            for (int i = 0; i < submesh.originalUvVertices.Length; i++)
                                highestVertexCoordinatesForThisSubmesh = new Vector2(Mathf.Max(submesh.originalUvVertices[i].x, highestVertexCoordinatesForThisSubmesh.x), Mathf.Max(submesh.originalUvVertices[i].y, highestVertexCoordinatesForThisSubmesh.y));

                            //Calculate the percentage that the edge of this texture uses, calculates the size of the uv for each texture, to ignore the edges
                            Vector2 percentEdgeUsageOfCurrentTexture = thisTexture.GetEdgesPercentUsageOfThisTextures();

                            //Get index of this main texture submesh in atlas rects
                            int mainTextureIndexInAtlas = atlasGenerated.GetRectIndexOfThatMainTexture(thisTexture.mainTexture);

                            //Process all uv vertices of this submesh
                            for (int i = 0; i < submesh.originalUvVertices.Length; i++)
                            {
                                //Create the vertice
                                Vector2 thisVertex = Vector2.zero;

                                //If the UV map is larger than the texture
                                thisVertex.x = Mathf.Lerp(atlasGenerated.atlasRects[mainTextureIndexInAtlas].xMin, atlasGenerated.atlasRects[mainTextureIndexInAtlas].xMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.x, 1 - percentEdgeUsageOfCurrentTexture.x, submesh.originalUvVertices[i].x / highestVertexCoordinatesForThisSubmesh.x));
                                thisVertex.y = Mathf.Lerp(atlasGenerated.atlasRects[mainTextureIndexInAtlas].yMin, atlasGenerated.atlasRects[mainTextureIndexInAtlas].yMax, Mathf.Lerp(percentEdgeUsageOfCurrentTexture.y, 1 - percentEdgeUsageOfCurrentTexture.y, submesh.originalUvVertices[i].y / highestVertexCoordinatesForThisSubmesh.y));

                                //Save this vertice edited in uv map of combined mesh
                                newUvMapForCombinedMesh[i + submesh.startOfUvVerticesInIndex] = thisVertex;
                            }
                        }
                    }
                }

                //Show progress bar

                //Apply the new UV map merged using modification of all UV vertex of each submesh
                smrender.sharedMesh.uv = newUvMapForCombinedMesh;

                //Apply all atlas too
                ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.mainTexturePropertyToInsert, atlasGenerated.mainTextureAtlas);
                if (allInOneParams.metallicMapSupport == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.metallicMapPropertyToInsert, atlasGenerated.metallicMapAtlas);
                if (allInOneParams.specularMapSupport == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.specularMapPropertyToInsert, atlasGenerated.specularMapAtlas);
                if (allInOneParams.normalMapSupport == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.normalMapPropertyToInsert, atlasGenerated.normalMapAtlas);
                if (allInOneParams.normalMap2Support == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.normalMap2PropertyToInsert, atlasGenerated.normalMap2Atlas);
                if (allInOneParams.heightMapSupport == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.heightMapPropertyToInsert, atlasGenerated.heightMapAtlas);
                if (allInOneParams.occlusionMapSupport == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.occlusionMapPropertyToInsert, atlasGenerated.occlusionMapAtlas);
                if (allInOneParams.detailAlbedoMapSupport == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.detailMapPropertyToInsert, atlasGenerated.detailMapAtlas);
                if (allInOneParams.detailMaskSupport == true)
                    ApplyAtlasInPropertyOfMaterial(smrender.sharedMaterials[0], allInOneParams.detailMaskPropertyToInsert, atlasGenerated.detailMaskAtlas);

                //If is desired to hightlight UV vertices
                if (highlightUvVertices == true)
                {
                    for (int i = 0; i < smrender.sharedMesh.uv.Length; i++)
                        atlasGenerated.mainTextureAtlas.SetPixel((int)(atlasGenerated.mainTextureAtlas.width * smrender.sharedMesh.uv[i].x), (int)(atlasGenerated.mainTextureAtlas.height * smrender.sharedMesh.uv[i].y), Color.yellow);
                    atlasGenerated.mainTextureAtlas.Apply();
                }
            }
            //If occurs a error on merge, catch it
            catch (Exception exception)
            {
                StopMergeByErrorWhileMerging(exception);
            }
        }

        //Can be used in all methods

        private class BlendShapeData
        {
            public string blendShapeFrameName = "";
            public int blendShapeFrameIndex = -1;
            public float blendShapeCurrentValue = 0.0f;
            public List<Vector3> startDeltaVertices = new List<Vector3>();
            public List<Vector3> startDeltaNormals = new List<Vector3>();
            public List<Vector3> startDeltaTangents = new List<Vector3>();
            public List<Vector3> finalDeltaVertices = new List<Vector3>();
            public List<Vector3> finalDeltaNormals = new List<Vector3>();
            public List<Vector3> finalDeltaTangents = new List<Vector3>();
            public string blendShapeNameOnCombinedMesh = "";
        }

        //Used in One Mesh Per Material

        private class SubMeshToCombine
        {
            //Class that stores a mesh of skinned mesh renderer and respective submesh index, to combine
            public Transform transform;
            public SkinnedMeshRenderer skinnedMeshRenderer;
            public int subMeshIndex;

            public SubMeshToCombine(Transform transform, SkinnedMeshRenderer skinnedMeshRenderer, int subMeshIndex)
            {
                this.transform = transform;
                this.skinnedMeshRenderer = skinnedMeshRenderer;
                this.subMeshIndex = subMeshIndex;
            }
        }

        private class SubMeshesCombined
        {
            //Class that stores various submeshes, merged, with their respective material and data
            public Matrix4x4 localToWorldMatrix;
            public Mesh subMeshesMerged;
            public Transform[] bonesToMerge;
            public Matrix4x4[] bindPosesToMerge;
            public Material thisMaterial;

            public SubMeshesCombined(Matrix4x4 localToWorldMatrix, Mesh subMeshesMerged, Transform[] bonesToMerge, Matrix4x4[] bindPosesToMerge, Material thisMaterial)
            {
                //Store the data
                this.localToWorldMatrix = localToWorldMatrix;
                this.subMeshesMerged = subMeshesMerged;
                this.bonesToMerge = bonesToMerge;
                this.bindPosesToMerge = bindPosesToMerge;
                this.thisMaterial = thisMaterial;
            }
        }

        //Used in All In One

        private class TexturesSubMeshes
        {
            public class UvBounds
            {
                //This class stores a data of size of a submesh uv, data like major value of x and y, etc
                public float majorX = 0;
                public float majorY = 0;
                public float minorX = 0;
                public float minorY = 0;
                public float spaceMinorX = 0;
                public float spaceMajorX = 0;
                public float spaceMinorY = 0;
                public float spaceMajorY = 0;
                public float edgesUseX = 0.0f;
                public float edgesUseY = 0.0f;

                public float Round(float value, int places)
                {
                    return float.Parse(value.ToString("F" + places.ToString()));
                }

                public void RoundBoundsValuesAndCalculateSpaceNeededToTiling()
                {
                    //Round all values
                    majorX = Round(majorX, 4);
                    majorY = Round(majorY, 4);
                    minorX = Round(minorX, 4);
                    minorY = Round(minorY, 4);

                    //Calculate aditional space to left of texture
                    if (minorX >= 0.0f)
                        spaceMinorX = 0.0f;
                    if (minorX < 0.0f)
                        spaceMinorX = minorX * -1.0f;

                    //Calculate aditional space to down of texture
                    if (minorY >= 0.0f)
                        spaceMinorY = 0.0f;
                    if (minorY < 0.0f)
                        spaceMinorY = minorY * -1.0f;

                    //Calculate aditional space to up of texture
                    if (majorY >= 1.0f)
                        spaceMajorY = majorY - 1.0f;

                    //Calculate aditional space to right of texture
                    if (majorX >= 1.0f)
                        spaceMajorX = majorX - 1.0f;
                }
            }

            public class UserSubMeshes
            {
                //This class stores data of a submesh that uses this texture
                public UvBounds uvBoundsOfThisSubMesh = new UvBounds();
                public int startOfUvVerticesInIndex = 0;
                public Vector2[] originalUvVertices = null;
            }

            //This class stores textures and all submeshes data that uses this texture. If is tilled texture, this is repeated and used only by one submesh
            public Material material;
            public Texture2D mainTexture;
            public Texture2D metallicMap;
            public Texture2D specularMap;
            public Texture2D normalMap;
            public Texture2D normalMap2;
            public Texture2D heightMap;
            public Texture2D occlusionMap;
            public Texture2D detailMap;
            public Texture2D detailMask;
            public bool isTiledTexture = false;
            public Vector2Int mainTextureResolution;
            public Vector2Int mainTextureResolutionWithEdges;
            public List<UserSubMeshes> userSubMeshes = new List<UserSubMeshes>();

            //Return the edges percent usage, getting from 0 submesh of this texture
            public Vector2 GetEdgesPercentUsageOfThisTextures()
            {
                return new Vector2(userSubMeshes[0].uvBoundsOfThisSubMesh.edgesUseX, userSubMeshes[0].uvBoundsOfThisSubMesh.edgesUseY);
            }

            //Convert all vertices of all submeshes to positive values
            public void ConvertAllSubMeshsVerticesToPositive()
            {
                //Convert all vertices for each submesh
                foreach (UserSubMeshes submesh in userSubMeshes)
                {
                    //Calculate all minor values of vertices of this submehs
                    float[] xAxis = new float[submesh.originalUvVertices.Length];
                    float[] yAxis = new float[submesh.originalUvVertices.Length];
                    for (int i = 0; i < submesh.originalUvVertices.Length; i++)
                    {
                        xAxis[i] = submesh.originalUvVertices[i].x;
                        yAxis[i] = submesh.originalUvVertices[i].y;
                    }
                    Vector2 minorValues = new Vector2(Mathf.Min(xAxis), Mathf.Min(yAxis));

                    //Modify all values of all vertices to positive
                    for (int i = 0; i < submesh.originalUvVertices.Length; i++)
                    {
                        //Get original value
                        Vector2 originalValue = submesh.originalUvVertices[i];

                        //Create the modifyied value
                        Vector2 newValue = Vector2.zero;

                        //Modify the value
                        if (originalValue.x >= 0.0f)
                            newValue.x = originalValue.x + ((minorValues.x < 0.0f) ? (minorValues.x * -1) : 0);
                        if (originalValue.y >= 0.0f)
                            newValue.y = originalValue.y + ((minorValues.y < 0.0f) ? (minorValues.y * -1) : 0);

                        //Convert all negative values to positive, and invert the values, to invert negative texture maping to positive
                        if (originalValue.x < 0.0f)
                            newValue.x = (minorValues.x * -1) - (originalValue.x * -1);
                        if (originalValue.y < 0.0f)
                            newValue.y = (minorValues.y * -1) - (originalValue.y * -1);

                        //Apply the new value
                        submesh.originalUvVertices[i] = newValue;
                    }
                }
            }
        }

        private class AtlasData
        {
            //This class store a atlas data
            public Texture2D mainTextureAtlas = new Texture2D(16, 16);
            public Texture2D metallicMapAtlas = new Texture2D(16, 16);
            public Texture2D specularMapAtlas = new Texture2D(16, 16);
            public Texture2D normalMapAtlas = new Texture2D(16, 16);
            public Texture2D normalMap2Atlas = new Texture2D(16, 16);
            public Texture2D heightMapAtlas = new Texture2D(16, 16);
            public Texture2D occlusionMapAtlas = new Texture2D(16, 16);
            public Texture2D detailMapAtlas = new Texture2D(16, 16);
            public Texture2D detailMaskAtlas = new Texture2D(16, 16);
            public Rect[] atlasRects = new Rect[0];
            public Texture2D[] originalMainTexturesUsedAndOrdenedAccordingToAtlasRect = new Texture2D[0];

            //Return the respective id of rect that the informed texture is posicioned
            public int GetRectIndexOfThatMainTexture(Texture2D texture)
            {
                //Prepare the storage
                int index = -1;

                foreach (Texture2D tex in originalMainTexturesUsedAndOrdenedAccordingToAtlasRect)
                {
                    //Increase de index in onee
                    index += 1;

                    //If the texture informed is equal to original texture used, break this loop and return the respective index
                    if (tex == texture)
                        break;
                }

                //Return the data
                return index;
            }
        }

        private enum TextureType
        {
            //This enum stores type of texture
            MainTexture,
            MetallicMap,
            SpecularMap,
            NormalMap,
            HeightMap,
            OcclusionMap,
            DetailMap,
            DetailMask
        }

        private class ColorData
        {
            //This class stores a color and your respective name
            public string colorName;
            public Color color;

            public ColorData(string colorName, Color color)
            {
                this.colorName = colorName;
                this.color = color;
            }
        }

        //Used in Just Material Colors

        private class UvDataAndColorOfThisSubmesh
        {
            //This class stores all UV data of a submesh
            public Texture2D textureColor;
            public int startOfUvVerticesIndex;
            public Vector2[] originalUvVertices;
        }

        private class ColorAtlasData
        {
            //This class store a atlas data
            public Texture2D colorAtlas = new Texture2D(16, 16);
            public Rect[] atlasRects = new Rect[0];
            public Texture2D[] originalTexturesUsedAndOrdenedAccordingToAtlasRect = new Texture2D[0];

            //Return the respective id of rect that the informed texture is posicioned
            public int GetRectIndexOfThatMainTexture(Texture2D texture)
            {
                //Prepare the storage
                int index = -1;

                foreach (Texture2D tex in originalTexturesUsedAndOrdenedAccordingToAtlasRect)
                {
                    //Increase de index in onee
                    index += 1;

                    //If the texture informed is equal to original texture used, break this loop and return the respective index
                    if (tex == texture)
                        break;
                }

                //Return the data
                return index;
            }
        }

        //API Methods For Interface Editor And Core Methods
        private void LaunchLog(string content, LogTypeOf logType)
        {
            if (logType == LogTypeOf.Assert || logType == LogTypeOf.Error || logType == LogTypeOf.Exception)
            {
                Debug.LogError(content);
            }
            if (logType == LogTypeOf.Log)
            {
                Debug.Log(content);
            }
            if (logType == LogTypeOf.Warning)
            {
                Debug.LogWarning(content);
            }
        }

        private void ValidateAllVariables()
        {
            //Additional effects
            if (allInOneParams.specularMapSupport == true && allInOneParams.metallicMapSupport == true)
            {
                allInOneParams.metallicMapSupport = false;
                allInOneParams.specularMapSupport = false;
            }

            //If blendshapes multiplier is equal to zero, reset to one
            if (blendShapesMultiplier == 0)
                blendShapesMultiplier = 1.0f;

            //If the merge name is empty, set as default
            if (String.IsNullOrEmpty(nameOfThisMerge) == true)
                nameOfThisMerge = "Combined Meshes";

            //If have another scriptable render pipeline
            //if (CurrentRenderPipeline.haveAnotherSrpPackages == true && allInOneParams.useDefaultMainTextureProperty == true)
            //{
            //    if (CurrentRenderPipeline.packageDetected == "HDRP")   //<- Set default for HDRP/Lit
            //    {
            //        allInOneParams.mainTexturePropertyToFind = "_MainTex";
            //        allInOneParams.mainTexturePropertyToInsert = "_BaseColorMap";
            //    }
            //    if (CurrentRenderPipeline.packageDetected == "URP")    //<- Set default for URP/Lit
            //    {
            //        allInOneParams.mainTexturePropertyToFind = "_MainTex";
            //        allInOneParams.mainTexturePropertyToInsert = "_BaseMap";
            //    }
            //}
            //if (CurrentRenderPipeline.haveAnotherSrpPackages == true && justMaterialColorsParams.useDefaultColorProperty == true)
            //{
            //    if (CurrentRenderPipeline.packageDetected == "HDRP")   //<- Set default for HDRP/Lit
            //    {
            //        justMaterialColorsParams.colorPropertyToFind = "_BaseColor";
            //        justMaterialColorsParams.mainTexturePropertyToInsert = "_BaseColorMap";
            //    }
            //    if (CurrentRenderPipeline.packageDetected == "URP")   //<- Set default for URP/Lit
            //    {
            //        justMaterialColorsParams.colorPropertyToFind = "_BaseColor";
            //        justMaterialColorsParams.mainTexturePropertyToInsert = "_BaseMap";
            //    }
            //}

            ////If not have another scriptable render pipeline
            //if (CurrentRenderPipeline.haveAnotherSrpPackages == false && allInOneParams.useDefaultMainTextureProperty == true)
            //{
            //    allInOneParams.mainTexturePropertyToFind = "_MainTex";
            //    allInOneParams.mainTexturePropertyToInsert = "_MainTex";
            //}
            //if (CurrentRenderPipeline.haveAnotherSrpPackages == false && justMaterialColorsParams.useDefaultColorProperty == true)
            //{
            //    justMaterialColorsParams.colorPropertyToFind = "_Color";
            //    justMaterialColorsParams.mainTexturePropertyToInsert = "_MainTex";
            //}
        }

        private GameObject[] GetAllItemsForCombine(GameObject gameObject, bool includeItemsRegisteredToBeIgnored, bool launchLogs)
        {
            //Prepare the variable
            List<GameObject> itemsForCombineStart = new List<GameObject>();

            //Get all items for combine
            if (mergeMethod != MergeMethod.OnlyAnima2dMeshes)
            {
                SkinnedMeshRenderer[] renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(combineInactives);
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    if (renderer.name == "skinmesh")
                    {
                        continue;
                    }
                    itemsForCombineStart.Add(renderer.gameObject);
                }
            }
            //Return all itens, if is desired
            if (includeItemsRegisteredToBeIgnored == true)
            {
                return itemsForCombineStart.ToArray();
            }

            //Create the final list of items to combine
            List<GameObject> itemsForCombineFinal = new List<GameObject>();

            //Remove all GameObjects registered to be ignored
            for (int i = 0; i < itemsForCombineStart.Count; i++)
            {
                itemsForCombineFinal.Add(itemsForCombineStart[i]);
            }

            return itemsForCombineFinal.ToArray();
        }

        private void StopMergeByErrorWhileMerging(Exception exception)
        {
            //If occurred a exception
            if (exception != null)
            {
                //Launch log error
                LaunchLog("An error occurred while performing this merge. Read on for more details.\n\n" + exception.Message + "\n\n" + exception.StackTrace, LogTypeOf.Error);
                Debug.LogError("An error occurred during this merge. Check the log or console for more details.");

            }

            //If not occurred a exception
            if (exception == null)
            {
                Debug.LogError("An error occurred during this merge. Check the log or console for more details.");
            }
        }



        private SkinnedMeshRenderer[] GetAllSkinnedMeshsValidatedToCombine(GameObject[] gameObjectsToCombine)
        {
            //Prepare the storage
            List<SkinnedMeshRenderer> meshRenderers = new List<SkinnedMeshRenderer>();

            //Get skinned mesh renderers in all GameObjects to combine
            foreach (GameObject obj in gameObjectsToCombine)
            {
                //Get the Skinned Mesh of this GameObject
                SkinnedMeshRenderer meshRender = obj.GetComponent<SkinnedMeshRenderer>();
                if (meshRender != null)
                {
                    //Verify if msh renderer is disabled
                    if (meshRender.enabled == false)
                    {
                        LaunchLog("The mesh present in GameObject " + meshRender.gameObject.name + " was ignored during the merge process. Reason: " +
                            "The Skinned Mesh Renderer is disabled",
                            LogTypeOf.Log);
                        continue;
                    }

                    //Verify if the sharedmesh is null
                    if (meshRender.sharedMesh == null)
                    {
                        LaunchLog("The mesh present in GameObject " + meshRender.gameObject.name + " was ignored during the merge process. Reason: " +
                            "The mesh is null.",
                            LogTypeOf.Log);
                        continue;
                    }

                    //Verify if exists blendshapes
                    if (meshRender.sharedMesh.blendShapeCount > 0 && blendShapesSupport == BlendShapesSupport.Disabled)
                    {
                        LaunchLog("The mesh present in GameObject " + meshRender.gameObject.name + " was ignored during the merge process. Reason: " +
                                                    "The mesh contains Blendshapes, and the \"Ignore Blendshapes\" option is enabled.",
                                                    LogTypeOf.Log);
                        continue;
                    }

                    //Verify if shared materials is null
                    if (meshRender.sharedMaterials == null)
                    {
                        LaunchLog("The mesh present in GameObject " + meshRender.gameObject.name + " was ignored during the merge process. Reason: " +
                            "This mesh has no materials, and materials list is null.",
                            LogTypeOf.Log);
                        continue;
                    }

                    //Verify if not have materials
                    if (meshRender.sharedMaterials.Length == 0)
                    {
                        LaunchLog("The mesh present in GameObject " + meshRender.gameObject.name + " was ignored during the merge process. Reason: " +
                            "This mesh has no materials.",
                            LogTypeOf.Log);
                        continue;
                    }

                    //Verify if quantity of shared materials is different of submeshes
                    if (meshRender.sharedMaterials.Length != meshRender.sharedMesh.subMeshCount)
                    {
                        LaunchLog("The mesh present in GameObject " + meshRender.gameObject.name + " was ignored during the merge process. Reason: " +
                            "The amount of materials in this mesh does not match the number of sub-meshes.",
                            LogTypeOf.Log);
                        continue;
                    }

                    //Verify if exists null materials in this mesh
                    bool foundNullMaterials = false;
                    foreach (Material mat in meshRender.sharedMaterials)
                    {
                        if (mat == null)
                            foundNullMaterials = true;
                    }
                    if (foundNullMaterials == true)
                    {
                        LaunchLog("The mesh present in GameObject " + meshRender.gameObject.name + " was ignored during the merge process. Reason: " +
                            "Null materials were found in this mesh.",
                            LogTypeOf.Log);
                        continue;
                    }

                    //If the method of merge is "All In One" and "Merge All UV Sizes" is disabled, remove the mesh if the UV is greater than 1 or minor than 0
                    if (mergeMethod == MergeMethod.AllInOne && allInOneParams.mergeTiledTextures == MergeTiledTextures.SkipAll)
                    {
                        bool haveUvVerticesMajorThanOne = false;
                        foreach (Vector2 vertex in meshRender.sharedMesh.uv)
                        {
                            //Check if vertex is major than 1
                            if (vertex.x > 1.0f || vertex.y > 1.0f)
                            {
                                haveUvVerticesMajorThanOne = true;
                            }
                            //Check if vertex is major than 0
                            if (vertex.x < 0.0f || vertex.y < 0.0f)
                            {
                                haveUvVerticesMajorThanOne = true;
                            }
                        }
                        if (haveUvVerticesMajorThanOne == true)
                        {
                            LaunchLog("The mesh present in \"" + meshRender.transform.name + "\" has a larger UV map than the texture (tiled texture). If the \"Merge Tiled Texture\" option is disabled, this mesh was ignored during the merge process. Keep in mind that if this mesh uses a higher UV than its texture (tiled texture), the texture will have to be adapted to fit in an atlas, and this can end up untwisting the way the texture is rendered in this mesh.", LogTypeOf.Log);
                            continue;
                        }
                    }

                    //Add to list of valid Skinned Meshs, if can add
                    meshRenderers.Add(meshRender);
                }
            }

            //Return all Skinned Meshes
            return meshRenderers.ToArray();
        }

        private bool ExistsDifferentRootBones(SkinnedMeshRenderer[] skinnedMeshRenderers, bool launchLogs)
        {
            //Prepare the storage
            Transform lastRootBone = skinnedMeshRenderers[0].rootBone;

            //Verify in each skinned mesh renderer, if exists different root bones
            foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            {
                if (lastRootBone != smr.rootBone)
                {
                    if (launchLogs == true)
                        LaunchLog("Different root bones were found in your character's meshes. Combining meshes with different root bones can cause IK animation problems for example, but in general, the merge works without problems. If you activate the \"Only Equal Root Bones\" option, the Skinned Mesh Combiner will not combine meshes with different root bones in your character.", LogTypeOf.Log);
                    return true;
                }
            }

            return false;
        }

        private int CountVerticesInAllMeshes(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            //Return count of vertices
            int verticesCount = 0;

            //Count all
            foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            {
                verticesCount += smr.sharedMesh.vertexCount;
            }

            return verticesCount;
        }

        private Material GetValidatedCopyOfMaterial(Material targetMaterial, bool copyPropertiesOfTargetMaterial, bool clearAllTextures)
        {
            //Return a copy of target material
            Material material = allInOneParams.materialToUse;
            //Copy all propertyies, if is desired
            if (copyPropertiesOfTargetMaterial == true)
                material.CopyPropertiesFromMaterial(targetMaterial);
            return material;
        }

        private void MergeAndGetAllBlendShapeDataOfSkinnedMeshRenderers(SkinnedMeshRenderer[] skinnedMeshesToMerge, Mesh finalMesh, SkinnedMeshRenderer finalSkinnedMeshRenderer)
        {
            //Prepare the list of blendshapes processed
            List<BlendShapeData> allBlendShapeData = new List<BlendShapeData>();
            //Prepare the list of already added blendshapes name, to avoid duplicates
            Dictionary<string, int> alreadyAddedBlendshapesNames = new Dictionary<string, int>();

            //Verify each skinned mesh renderer and get info about all blendshapes of all meshes
            int totalVerticesVerifiedAtHereForBlendShapes = 0;
            foreach (SkinnedMeshRenderer combine in skinnedMeshesToMerge)
            {
                //Get all blendshapes names of this mesh
                string[] blendShapes = new string[combine.sharedMesh.blendShapeCount];
                for (int i = 0; i < combine.sharedMesh.blendShapeCount; i++)
                    blendShapes[i] = combine.sharedMesh.GetBlendShapeName(i);

                //Read all blendshapes data of this mesh
                for (int i = 0; i < blendShapes.Length; i++)
                {
                    //Get the current blendshape data
                    BlendShapeData blendShapeData = new BlendShapeData();
                    blendShapeData.blendShapeFrameName = blendShapes[i];
                    blendShapeData.blendShapeFrameIndex = combine.sharedMesh.GetBlendShapeIndex(blendShapes[i]);
                    blendShapeData.blendShapeCurrentValue = combine.GetBlendShapeWeight(blendShapeData.blendShapeFrameIndex);

                    //Get the vertices vector array of this mesh
                    Vector3[] originalDeltaVertices = new Vector3[combine.sharedMesh.vertexCount];
                    Vector3[] originalDeltaNormals = new Vector3[combine.sharedMesh.vertexCount];
                    Vector3[] originalDeltaTangents = new Vector3[combine.sharedMesh.vertexCount];

                    //Get the vertices vector array of final mesh
                    Vector3[] finalDeltaVertices = new Vector3[finalMesh.vertexCount];
                    Vector3[] finalDeltaNormals = new Vector3[finalMesh.vertexCount];
                    Vector3[] finalDeltaTangents = new Vector3[finalMesh.vertexCount];

                    //Fill the blendshape start vertices
                    blendShapeData.startDeltaVertices.AddRange(finalDeltaVertices);
                    blendShapeData.startDeltaNormals.AddRange(finalDeltaNormals);
                    blendShapeData.startDeltaTangents.AddRange(finalDeltaTangents);

                    //Fill the blendshape final vertices
                    blendShapeData.finalDeltaVertices.AddRange(finalDeltaVertices);
                    blendShapeData.finalDeltaNormals.AddRange(finalDeltaNormals);
                    blendShapeData.finalDeltaTangents.AddRange(finalDeltaTangents);

                    //If this mesh have data for this blendshape, get them. otherwise, just ignores and continues with zero values
                    if (combine.sharedMesh.GetBlendShapeIndex(blendShapes[i]) != -1)
                        combine.sharedMesh.GetBlendShapeFrameVertices(blendShapeData.blendShapeFrameIndex, combine.sharedMesh.GetBlendShapeFrameCount(blendShapeData.blendShapeFrameIndex) - 1, originalDeltaVertices, originalDeltaNormals, originalDeltaTangents);
                    if (combine.sharedMesh.GetBlendShapeIndex(blendShapes[i]) == -1)
                        LaunchLog("Mesh data could not be found in Blendshape \"" + blendShapes[i] + "\". This Blendshape may not work on the mesh resulting from the merge.", LogTypeOf.Warning);

                    //Fill the final blendshape vertices, where vertices that this blendshape will modify only, get from original vertices, normals and tangents
                    //Vertices
                    for (int x = 0; x < originalDeltaVertices.Length; x++)
                        blendShapeData.finalDeltaVertices[x + totalVerticesVerifiedAtHereForBlendShapes] = originalDeltaVertices[x] * blendShapesMultiplier;
                    //Normals
                    for (int x = 0; x < originalDeltaNormals.Length; x++)
                        blendShapeData.finalDeltaNormals[x + totalVerticesVerifiedAtHereForBlendShapes] = originalDeltaNormals[x] * blendShapesMultiplier;
                    //Tangents
                    for (int x = 0; x < originalDeltaTangents.Length; x++)
                        blendShapeData.finalDeltaTangents[x + totalVerticesVerifiedAtHereForBlendShapes] = originalDeltaTangents[x] * blendShapesMultiplier;

                    //Add this blendshape to merge
                    allBlendShapeData.Add(blendShapeData);
                }

                //Set vertices verified at here, after process all blendshapes for this mesh
                totalVerticesVerifiedAtHereForBlendShapes += combine.sharedMesh.vertexCount;
            }

            //Finally add all processed blendshapes of all meshes, into the final skinned mesh renderer
            foreach (BlendShapeData blendShape in allBlendShapeData)
            {
                //Prepare the blendshape name
                StringBuilder blendShapeName = new StringBuilder();
                blendShapeName.Append(blendShape.blendShapeFrameName);
                if (alreadyAddedBlendshapesNames.ContainsKey(blendShape.blendShapeFrameName) == true)
                {
                    blendShapeName.Append(" (");
                    blendShapeName.Append(alreadyAddedBlendshapesNames[blendShape.blendShapeFrameName]);
                    blendShapeName.Append(")");
                    LaunchLog("The Blendshape with the name of \"" + blendShape.blendShapeFrameName + "\" was found in more than one mesh. This would generate duplicates of the same Blendshape in the mesh resulting from the merge, so Blendshape \"" + blendShape.blendShapeFrameName + "\" (duplicate) received a duplicate counter (for example \"" + blendShape.blendShapeFrameName + " (0)\"). This will keep all Blendshapes working.", LogTypeOf.Warning);
                }

                //Add the start frame and final frame of current blendshape
                finalMesh.AddBlendShapeFrame(blendShapeName.ToString(), 0.0f, blendShape.startDeltaVertices.ToArray(), blendShape.startDeltaNormals.ToArray(), blendShape.startDeltaTangents.ToArray());
                finalMesh.AddBlendShapeFrame(blendShapeName.ToString(), 100.0f, blendShape.finalDeltaVertices.ToArray(), blendShape.finalDeltaNormals.ToArray(), blendShape.finalDeltaTangents.ToArray());

                //Save the name of this new blendshape, on the combined mesh, to sync later
                blendShape.blendShapeNameOnCombinedMesh = blendShapeName.ToString();

                //Add information that already added this blendshape name
                if (alreadyAddedBlendshapesNames.ContainsKey(blendShape.blendShapeFrameName) == true)
                    alreadyAddedBlendshapesNames[blendShape.blendShapeFrameName] += 1;
                if (alreadyAddedBlendshapesNames.ContainsKey(blendShape.blendShapeFrameName) == false)
                    alreadyAddedBlendshapesNames.Add(blendShape.blendShapeFrameName, 0);
            }

            //Now sync values of original blendshapes to merged blendshapes
            if (blendShapesSupport == BlendShapesSupport.FullSupport)
                foreach (BlendShapeData blendShape in allBlendShapeData)
                    if (blendShape.blendShapeCurrentValue > 0.0f)
                        finalSkinnedMeshRenderer.SetBlendShapeWeight(finalMesh.GetBlendShapeIndex(blendShape.blendShapeNameOnCombinedMesh), blendShape.blendShapeCurrentValue);
        }

        private Transform GetCorrectRootBoneFromAllOriginalSkinnedMeshRenderers(SkinnedMeshRenderer[] skinnedMeshesToMerge)
        {
            //If root bone to use is manual
            if (rootBoneToUse == RootBoneToUse.Manual)
                return manualRootBoneToUse;
            //If root bone to  use is automatic
            if (rootBoneToUse == RootBoneToUse.Automatic)
            {
                //Root bone to use
                Transform rootBoneToUse = null;

                //Create the dictionary of most used root bones
                Dictionary<Transform, int> rootBones = new Dictionary<Transform, int>();

                //Fill the dictionary
                foreach (SkinnedMeshRenderer render in skinnedMeshesToMerge)
                    if (render != null)
                        if (render.rootBone != null)
                        {
                            if (rootBones.ContainsKey(render.rootBone) == true)
                                rootBones[render.rootBone] += 1;
                            if (rootBones.ContainsKey(render.rootBone) == false)
                                rootBones.Add(render.rootBone, 1);
                        }

                //Verify the most used root bone, set the most used root bone, to be returned
                int lastBoneUsesTime = 0;
                foreach (var key in rootBones)
                    if (key.Value > lastBoneUsesTime)
                    {
                        rootBoneToUse = key.Key;
                        lastBoneUsesTime = key.Value;
                    }

                //Return the root bone to use
                return rootBoneToUse;
            }
            return null;
        }
        private ColorData GetDefaultAndNeutralColorForThisTexture(TextureType textureType)
        {
            //Return the neutral color for texture type
            switch (textureType)
            {
                case TextureType.MainTexture:
                    return new ColorData("RED", Color.red);
                case TextureType.MetallicMap:
                    return new ColorData("BLACK", Color.black);
                case TextureType.SpecularMap:
                    return new ColorData("BLACK", Color.black);
                case TextureType.NormalMap:
                    return new ColorData("PURPLE", new Color(128.0f / 255.0f, 128.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f));
                case TextureType.HeightMap:
                    return new ColorData("BLACK", Color.black);
                case TextureType.OcclusionMap:
                    return new ColorData("WHITE", Color.white);
                case TextureType.DetailMap:
                    return new ColorData("GRAY", Color.gray);
                case TextureType.DetailMask:
                    return new ColorData("WHITE", Color.white);
            }
            return new ColorData("RED", Color.red);
        }

        private int UvBoundToPixels(float uvSize, int textureSize)
        {
            return (int)(uvSize * (float)textureSize);
        }

        private float[] UvBoundSplitted(float uvSize)
        {
            //Convert to positive
            if (uvSize < 0.0f)
                uvSize = uvSize * -1.0f;
            //Result
            float[] result = new float[2];
            //Split
            string[] str = uvSize.ToString().Split(',');
            //Get result
            result[0] = float.Parse(str[0]);
            result[1] = 0.0f;
            if (str.Length > 1)
                result[1] = float.Parse("0," + str[1]);
            return result;
        }

        private Texture2D GetValidatedCopyOfTexture(Material materialToFindTexture, string propertyToFindTexture, int widthOfCorrespondentMainTexture, int heightOfCorrespondentMainTexture, TexturesSubMeshes.UvBounds boundsUvValues, TextureType textureType, bool showProgress, float progress)
        {
            Texture2D targetTexture = null;
            materialToFindTexture.EnableKeyword(propertyToFindTexture);

            //If found the property of texture
            if (materialToFindTexture.HasProperty(propertyToFindTexture) == true && materialToFindTexture.GetTexture(propertyToFindTexture) != null)
                targetTexture = (Texture2D)materialToFindTexture.GetTexture(propertyToFindTexture);

            //If not found the property of texture
            if (materialToFindTexture.HasProperty(propertyToFindTexture) == false || materialToFindTexture.GetTexture(propertyToFindTexture) == null)
            {
                //Get the default and neutral color for this texture
                ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);
                //Launch log
                LaunchLog("It was not possible to find the texture stored in property \"" + propertyToFindTexture + "\" of material \"" + materialToFindTexture.name + "\", so this Texture/Map was replaced by a " + defaultColor.colorName + " texture. This can affect how the texture or effect maps (such as Normal Maps, etc.) are displayed in the combined model. This can result in some small differences in the combined mesh when compared to the separate original meshes.", LogTypeOf.Warning);
                //Create a fake texture blank
                targetTexture = new Texture2D(widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture);
                //Create blank pixels
                Color[] colors = new Color[widthOfCorrespondentMainTexture * heightOfCorrespondentMainTexture];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = defaultColor.color;
                //Apply all pixels in void texture
                targetTexture.SetPixels(0, 0, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, colors, 0);
            }

            //-------------------------------------------- Start the creation of copyied texture
            //Prepare the storage for this texture that will be copyied
            Texture2D thisTexture = null;

            //If the texture is readable
            try
            {
                //-------------------------------------------- Calculate the size of copyied texture
                //Get desired edges size for each texture of atlas
                int edgesSize = GetEdgesSizeForTextures();

                //Calculate a preview of the total and final size of texture...
                int texWidth = 0;
                int texHeight = 0;
                int maxSizeOfTextures = 16384;
                bool overcameTheLimitationOf16k = false;
                //If is a normal texture
                if (isTiledTexture(boundsUvValues) == false)
                {
                    texWidth = edgesSize + targetTexture.width + edgesSize;
                    texHeight = edgesSize + targetTexture.height + edgesSize;
                }
                //If is a tiled texture
                if (isTiledTexture(boundsUvValues) == true)
                {
                    texWidth = edgesSize + UvBoundToPixels(boundsUvValues.spaceMinorX, targetTexture.width) + targetTexture.width + UvBoundToPixels(boundsUvValues.spaceMajorX, targetTexture.width) + edgesSize;
                    texHeight = edgesSize + UvBoundToPixels(boundsUvValues.spaceMinorY, targetTexture.height) + targetTexture.height + UvBoundToPixels(boundsUvValues.spaceMajorY, targetTexture.height) + edgesSize;
                }
                //Verify if the size of texture, as overcamed the limitation of 16384 pixels of Unity
                if (texWidth >= maxSizeOfTextures || texHeight >= maxSizeOfTextures)
                    overcameTheLimitationOf16k = true;
                //If overcamed the limitation of texture sizes of unity, create a texture with the size of target texture
                if (overcameTheLimitationOf16k == true)
                {
                    //Get the default and neutral color for this texture
                    ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);
                    if (String.IsNullOrEmpty(targetTexture.name) == false)
                        LaunchLog("It was not possible to process the \"" + targetTexture.name + "\" texture, as its size during processing was greater than Unity's " + maxSizeOfTextures.ToString() + " pixel limitation. This may have happened because its texture is larger than " + maxSizeOfTextures.ToString() + " pixels, or because the tiling of any of its meshes is too extensive. Try to use a texture smaller than " + maxSizeOfTextures.ToString() + " pixels and if that persists, skip any meshes that land a very large tile. During processing this texture reached the size of " + texWidth.ToString() + "x" + texHeight.ToString() + " pixels. This texture has been replaced by a simple texture of color " + defaultColor.colorName + ".", LogTypeOf.Warning);
                    texWidth = targetTexture.width;
                    texHeight = targetTexture.height;
                }
                //Create the texture with size calculated above
                thisTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false, false);

                //-------------------------------------------- Copy all original pixels from target texture reference
                //Copy all pixels of the target texture

                Color32[] targetTexturePixels = null;// targetTexture.GetPixels32(0);
                if (targetTexture.isReadable == false)
                {
                    targetTexture = duplicateTexture(targetTexture);
                }

                targetTexturePixels = targetTexture.GetPixels32(0);
                //If pink normal maps fix is enabled. If this is a normal map, try to get colors using different decoding (if have a compression format that uses different channels to store colors)
                if (allInOneParams.pinkNormalMapsFix == true && textureType == TextureType.NormalMap && targetTexture.format == TextureFormat.DXT5)
                    for (int i = 0; i < targetTexturePixels.Length; i++)
                    {
                        Color c = targetTexturePixels[i];
                        c.r = c.a * 2 - 1;  //red<-alpha (x<-w)
                        c.g = c.g * 2 - 1; //green is always the same (y)
                        Vector2 xy = new Vector2(c.r, c.g); //this is the xy vector
                        c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(xy, xy))); //recalculate the blue channel (z)
                        targetTexturePixels[i] = new Color(c.r * 0.5f + 0.5f, c.g * 0.5f + 0.5f, c.b * 0.5f + 0.5f); //back to 0-1 range
                    }

                //-------------------------------------------- Create a simple texture if the size of this copy texture has exceeded the limitation
                //Apply the copyied pixels to this texture, if is a texture that overcamed the limitation of pixels
                if (overcameTheLimitationOf16k == true)
                {
                    //Get the default color of this type of texture
                    ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);
                    //Create blank pixels
                    Color[] colors = new Color[targetTexture.width * targetTexture.height];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = defaultColor.color;
                    //Apply all pixels in void texture
                    thisTexture.SetPixels(0, 0, targetTexture.width, targetTexture.height, colors, 0);
                }
                //-------------------------------------------- Create a copy of target texture, if this copy of texture is a normal texture without tiling
                //Apply the copyied pixels to this texture if is normal texture
                if (isTiledTexture(boundsUvValues) == false && overcameTheLimitationOf16k == false)
                    thisTexture.SetPixels32(edgesSize, edgesSize, targetTexture.width, targetTexture.height, targetTexturePixels, 0);
                //-------------------------------------------- Create a copy of target texture with support to tiling, if this copy texture not exceed the limitation size of unity
                //Apply the copyied pixels to this texture if is a tiled texture, start the simulated texture tiles
                if (isTiledTexture(boundsUvValues) == true && overcameTheLimitationOf16k == false)
                {

                    //Prepare the vars
                    Color[] tempColorBlock = null;

                    //Add the left border
                    tempColorBlock = targetTexture.GetPixels(
                        targetTexture.width - (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]), 0,
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]), targetTexture.height, 0);
                    for (int i = 0; i < UvBoundSplitted(boundsUvValues.spaceMinorY)[0] + UvBoundSplitted(boundsUvValues.spaceMajorY)[0] + 1; i++)
                        thisTexture.SetPixels(
                            edgesSize, edgesSize + UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMinorY)[1], targetTexture.height) + (i * targetTexture.height),
                            UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMinorX)[1], targetTexture.width), targetTexture.height, tempColorBlock, 0);

                    //Fill the texture with repeated original textures
                    tempColorBlock = targetTexture.GetPixels(0, 0, targetTexture.width, targetTexture.height, 0);
                    for (int x = 0; x < UvBoundSplitted(boundsUvValues.spaceMinorX)[0] + UvBoundSplitted(boundsUvValues.spaceMajorX)[0] + 1; x++)
                        for (int y = 0; y < UvBoundSplitted(boundsUvValues.spaceMinorY)[0] + UvBoundSplitted(boundsUvValues.spaceMajorY)[0] + 1; y++)
                            thisTexture.SetPixels(
                                edgesSize + (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.spaceMinorX)[1]) + (x * targetTexture.width),
                                edgesSize + (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.spaceMinorY)[1]) + (y * targetTexture.height),
                                targetTexture.width, targetTexture.height, tempColorBlock, 0);

                    //Add the right border
                    tempColorBlock = targetTexture.GetPixels(0, 0, (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.majorX)[1]), targetTexture.height, 0);
                    for (int i = 0; i < UvBoundSplitted(boundsUvValues.spaceMinorY)[0] + UvBoundSplitted(boundsUvValues.spaceMajorY)[0] + 1; i++)
                        thisTexture.SetPixels(
                            edgesSize + (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.spaceMinorX)[1]) + (((int)UvBoundSplitted(boundsUvValues.spaceMinorX)[0] + (int)UvBoundSplitted(boundsUvValues.spaceMajorX)[0] + 1) * targetTexture.width),
                            edgesSize + UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMinorY)[1], targetTexture.height) + (i * targetTexture.height),
                            UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMajorX)[1], targetTexture.width), targetTexture.height, tempColorBlock, 0);

                    //Add the bottom border
                    tempColorBlock = targetTexture.GetPixels(
                        0, targetTexture.height - (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]),
                        targetTexture.width, (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]), 0);
                    for (int i = 0; i < UvBoundSplitted(boundsUvValues.spaceMinorX)[0] + UvBoundSplitted(boundsUvValues.spaceMajorX)[0] + 1; i++)
                        thisTexture.SetPixels(
                            edgesSize + UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMinorX)[1], targetTexture.width) + (i * targetTexture.width), edgesSize,
                            targetTexture.width, UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMinorY)[1], targetTexture.height), tempColorBlock, 0);

                    //Add the top border
                    tempColorBlock = targetTexture.GetPixels(0, 0, targetTexture.width, (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.majorY)[1]), 0);
                    for (int i = 0; i < UvBoundSplitted(boundsUvValues.spaceMinorX)[0] + UvBoundSplitted(boundsUvValues.spaceMajorX)[0] + 1; i++)
                        thisTexture.SetPixels(
                            edgesSize + UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMinorX)[1], targetTexture.width) + (i * targetTexture.width),
                            edgesSize + (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.spaceMinorY)[1]) + (((int)UvBoundSplitted(boundsUvValues.spaceMinorY)[0] + (int)UvBoundSplitted(boundsUvValues.spaceMajorY)[0] + 1) * targetTexture.height),
                            targetTexture.width, UvBoundToPixels(UvBoundSplitted(boundsUvValues.spaceMajorY)[1], targetTexture.height), tempColorBlock, 0);

                    //Add the bottom left corner
                    tempColorBlock = targetTexture.GetPixels(
                        targetTexture.width - (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]),
                        targetTexture.height - (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]),
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]),
                        (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]),
                        0);
                    thisTexture.SetPixels(edgesSize, edgesSize, (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]), (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]), tempColorBlock, 0);

                    //Add the bottom right corner
                    tempColorBlock = targetTexture.GetPixels(
                        0,
                        targetTexture.height - (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]),
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.majorX)[1]),
                        (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]),
                        0);
                    thisTexture.SetPixels(
                        thisTexture.width - edgesSize - (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.majorX)[1]), edgesSize,
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.majorX)[1]), (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.minorY)[1]), tempColorBlock, 0);

                    //Add the top left corner
                    tempColorBlock = targetTexture.GetPixels(
                        targetTexture.width - (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]),
                        0,
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]),
                        (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.majorY)[1]),
                        0);
                    thisTexture.SetPixels(
                        edgesSize, thisTexture.height - edgesSize - (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.majorY)[1]),
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.minorX)[1]), (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.majorY)[1]), tempColorBlock, 0);

                    //Add the top right corner
                    tempColorBlock = targetTexture.GetPixels(
                        0,
                        0,
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.majorX)[1]),
                        (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.majorY)[1]),
                        0);
                    thisTexture.SetPixels(
                        thisTexture.width - edgesSize - (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.majorX)[1]), thisTexture.height - edgesSize - (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.majorY)[1]),
                        (int)((float)targetTexture.width * UvBoundSplitted(boundsUvValues.majorX)[1]), (int)((float)targetTexture.height * UvBoundSplitted(boundsUvValues.majorY)[1]), tempColorBlock, 0);
                }

                //-------------------------------------------- Create the edges of copy texture, to support mip maps
                //If the edges size is minor than target texture size, uses the "SetPixels and GetPixels" to guarantee a faster copy
                if (edgesSize <= targetTexture.width && edgesSize <= targetTexture.height && overcameTheLimitationOf16k == false)
                {
                    //Prepare the var
                    Color[] copyiedPixels = null;

                    //Copy right border to left of current texture
                    copyiedPixels = thisTexture.GetPixels(thisTexture.width - edgesSize - edgesSize, 0, edgesSize, thisTexture.height, 0);
                    thisTexture.SetPixels(0, 0, edgesSize, thisTexture.height, copyiedPixels, 0);

                    //Copy left(original) border to right of current texture
                    copyiedPixels = thisTexture.GetPixels(edgesSize, 0, edgesSize, thisTexture.height, 0);
                    thisTexture.SetPixels(thisTexture.width - edgesSize, 0, edgesSize, thisTexture.height, copyiedPixels, 0);

                    //Copy bottom (original) border to top of current texture
                    copyiedPixels = thisTexture.GetPixels(0, edgesSize, thisTexture.width, edgesSize, 0);
                    thisTexture.SetPixels(0, thisTexture.height - edgesSize, thisTexture.width, edgesSize, copyiedPixels, 0);

                    //Copy top (original) border to bottom of current texture
                    copyiedPixels = thisTexture.GetPixels(0, thisTexture.height - edgesSize - edgesSize, thisTexture.width, edgesSize, 0);
                    thisTexture.SetPixels(0, 0, thisTexture.width, edgesSize, copyiedPixels, 0);
                }

                //If the edges size is major than target texture size, uses the "SetPixel and GetPixel" to repeat copy of pixels in target texture
                if (edgesSize > targetTexture.width || edgesSize > targetTexture.height && overcameTheLimitationOf16k == false)
                {
                    //Show the warning
                    LaunchLog("You have selected a texture border size (" + edgesSize + "px), where the border size is larger than this texture (\"" + targetTexture.name + "\" " + targetTexture.width + "x" + targetTexture.height + "px) size itself, causing this texture to repeat in the atlas. This increased the merging time due to the need for a new algorithm for creating the borders. It is recommended that the size of the edges of the textures in the atlas, does not exceed the size of the textures themselves.", LogTypeOf.Warning);

                    //Copy right (original) border to left of current texture
                    for (int x = 0; x < edgesSize; x++)
                        for (int y = 0; y < thisTexture.height; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel((targetTexture.width - edgesSize - edgesSize) + x, y));

                    //Copy left(original) border to right of current texture
                    for (int x = thisTexture.width - edgesSize; x < thisTexture.width; x++)
                        for (int y = 0; y < thisTexture.height; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel(targetTexture.width - x, y));

                    //Copy bottom (original) border to top of current texture
                    for (int x = 0; x < thisTexture.width; x++)
                        for (int y = 0; y < edgesSize; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel(x, (targetTexture.width - edgesSize) + y));

                    //Copy top (original) border to bottom of current texture
                    for (int x = 0; x < thisTexture.width; x++)
                        for (int y = thisTexture.height - edgesSize; y < thisTexture.height; y++)
                            thisTexture.SetPixel(x, y, targetTexture.GetPixel(x, edgesSize - (targetTexture.height - y)));
                }
            }
            //If the texture is not readable
            catch (UnityException e)
            {
                if (e.Message.StartsWith("Texture '" + targetTexture.name + "' is not readable"))
                {
                    //Get the default and neutral color for this texture
                    ColorData defaultColor = GetDefaultAndNeutralColorForThisTexture(textureType);

                    //Create the texture
                    thisTexture = new Texture2D(widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, TextureFormat.ARGB32, false, false);

                    //Create blank pixels
                    Color[] colors = new Color[widthOfCorrespondentMainTexture * heightOfCorrespondentMainTexture];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = defaultColor.color;

                    //Apply all pixels in void texture
                    thisTexture.SetPixels(0, 0, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture, colors, 0);

                    //Launch logs
                    LaunchLog("It was not possible to combine texture \"" + targetTexture.name + "\" within an atlas, as it is not marked as \"Readable\" in the import settings (\"Read/Write Enabled\"). The texture has been replaced with a " + defaultColor.colorName + " one.", LogTypeOf.Error);
                }
            }

            //-------------------------------------------- Calculate the use of edges of this texture, in percent
            //Only calculate if is the main texture, because main texture is more important and is the base texture for all uv mapping and calcs
            if (textureType == TextureType.MainTexture)
            {
                boundsUvValues.edgesUseX = (float)GetEdgesSizeForTextures() / (float)thisTexture.width;
                boundsUvValues.edgesUseY = (float)GetEdgesSizeForTextures() / (float)thisTexture.height;
            }

            //-------------------------------------------- Finally, resize the copy texture to mantain size equal to targe texture with edges
            //If this texture have the size differente of correspondent main texture size, resize it to be equal to main texture 
            if (thisTexture.width != widthOfCorrespondentMainTexture || thisTexture.height != heightOfCorrespondentMainTexture)
                SMCTextureResizer.Bilinear(thisTexture, widthOfCorrespondentMainTexture, heightOfCorrespondentMainTexture);

            //Return the texture 
            return thisTexture;
        }
        private Texture2D duplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
        private int GetAtlasMaxResolution()
        {
            //If is All In One
            if (mergeMethod == MergeMethod.AllInOne)
            {
                switch (allInOneParams.atlasResolution)
                {
                    case AtlasSize.Pixels32x32:
                        return 32;
                    case AtlasSize.Pixels64x64:
                        return 64;
                    case AtlasSize.Pixels128x128:
                        return 128;
                    case AtlasSize.Pixels256x256:
                        return 256;
                    case AtlasSize.Pixels512x512:
                        return 512;
                    case AtlasSize.Pixels1024x1024:
                        return 1024;
                    case AtlasSize.Pixels2048x2048:
                        return 2048;
                    case AtlasSize.Pixels4096x4096:
                        return 4096;
                    case AtlasSize.Pixels8192x8192:
                        return 8192;
                }
            }

            //Return the max resolution
            return 1024;
        }

        private AtlasData CreateAllAtlas(List<TexturesSubMeshes> copyiedTextures, int maxResolution, int paddingBetweenTextures, bool showProgress)
        {
            //Create a atlas
            AtlasData atlasData = new AtlasData();
            List<Texture2D> texturesToUse = new List<Texture2D>();

            texturesToUse.Clear();
            foreach (TexturesSubMeshes item in copyiedTextures)
                texturesToUse.Add(item.mainTexture);
            atlasData.originalMainTexturesUsedAndOrdenedAccordingToAtlasRect = texturesToUse.ToArray();
            atlasData.atlasRects = atlasData.mainTextureAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);

            //Create the metallic atlas if is desired
            if (allInOneParams.metallicMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.metallicMap);
                atlasData.metallicMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the specullar atlas if is desired
            if (allInOneParams.specularMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.specularMap);
                atlasData.specularMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the normal atlas if is desired
            if (allInOneParams.normalMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.normalMap);
                atlasData.normalMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the normal 2 atlas if is desired
            if (allInOneParams.normalMap2Support == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.normalMap2);
                atlasData.normalMap2Atlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the height atlas if is desired
            if (allInOneParams.heightMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.heightMap);
                atlasData.heightMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the occlusion atlas if is desired
            if (allInOneParams.occlusionMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.occlusionMap);
                atlasData.occlusionMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the detail atlas if is desired
            if (allInOneParams.detailAlbedoMapSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.detailMap);
                atlasData.detailMapAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Create the detail mask if is desired
            if (allInOneParams.detailMaskSupport == true)
            {
                texturesToUse.Clear();
                foreach (TexturesSubMeshes item in copyiedTextures)
                    texturesToUse.Add(item.detailMask);
                atlasData.detailMaskAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);
            }

            //Return the object
            return atlasData;
        }

        private void ApplyAtlasInPropertyOfMaterial(Material targetMaterial, string propertyToInsertTexture, Texture2D atlasTexture)
        {
            //If found the property
            if (targetMaterial.HasProperty(propertyToInsertTexture) == true)
            {
                //Try to enable this different keyword
                if (targetMaterial.IsKeywordEnabled(propertyToInsertTexture) == false)
                    targetMaterial.EnableKeyword(propertyToInsertTexture);

                //Apply the texture
                targetMaterial.SetTexture(propertyToInsertTexture, atlasTexture);

                //Try to enable this different keyword
                if (targetMaterial.IsKeywordEnabled(propertyToInsertTexture) == false)
                    targetMaterial.EnableKeyword(propertyToInsertTexture);

                //Forces enable all keyword, where is necessary
                if (propertyToInsertTexture == "_MetallicGlossMap" && targetMaterial.IsKeywordEnabled("_METALLICGLOSSMAP") == false && allInOneParams.metallicMapSupport == true)
                    targetMaterial.EnableKeyword("_METALLICGLOSSMAP");

                if (propertyToInsertTexture == "_SpecGlossMap" && targetMaterial.IsKeywordEnabled("_SPECGLOSSMAP") == false && allInOneParams.specularMapSupport == true)
                    targetMaterial.EnableKeyword("_SPECGLOSSMAP");

                if (propertyToInsertTexture == "_BumpMap" && targetMaterial.IsKeywordEnabled("_NORMALMAP") == false && allInOneParams.normalMapSupport == true)
                    targetMaterial.EnableKeyword("_NORMALMAP");

                if (propertyToInsertTexture == "_ParallaxMap" && targetMaterial.IsKeywordEnabled("_PARALLAXMAP") == false && allInOneParams.heightMapSupport == true)
                    targetMaterial.EnableKeyword("_PARALLAXMAP");

                if (propertyToInsertTexture == "_OcclusionMap" && targetMaterial.IsKeywordEnabled("_OcclusionMap") == false && allInOneParams.occlusionMapSupport == true)
                    targetMaterial.EnableKeyword("_OcclusionMap");

                if (propertyToInsertTexture == "_DetailAlbedoMap" && targetMaterial.IsKeywordEnabled("_DETAIL_MULX2") == false && allInOneParams.detailAlbedoMapSupport == true)
                    targetMaterial.EnableKeyword("_DETAIL_MULX2");

                if (propertyToInsertTexture == "_DetailNormalMap" && targetMaterial.IsKeywordEnabled("_DETAIL_MULX2") == false && allInOneParams.normalMap2Support == true)
                    targetMaterial.EnableKeyword("_DETAIL_MULX2");
            }
            //If not found the property
            if (targetMaterial.HasProperty(propertyToInsertTexture) == false)
                LaunchLog("It was not possible to find and apply the atlas on property \"" + propertyToInsertTexture + "\" of the material to use (\"" + targetMaterial.name + "\"). Therefore, no atlas was applied to this property.", LogTypeOf.Error);
        }

        private int GetEdgesSizeForTextures()
        {
            //If is All In One
            if (mergeMethod == MergeMethod.AllInOne)
            {
                switch (allInOneParams.mipMapEdgesSize)
                {
                    case MipMapEdgesSize.Pixels0x0:
                        return 0;
                    case MipMapEdgesSize.Pixels16x16:
                        return 16;
                    case MipMapEdgesSize.Pixels32x32:
                        return 32;
                    case MipMapEdgesSize.Pixels64x64:
                        return 64;
                    case MipMapEdgesSize.Pixels128x128:
                        return 128;
                    case MipMapEdgesSize.Pixels256x256:
                        return 256;
                    case MipMapEdgesSize.Pixels512x512:
                        return 512;
                    case MipMapEdgesSize.Pixels1024x1024:
                        return 1024;
                }
            }

            //Return the max resolution
            return 2;
        }

        private int GetAtlasPadding()
        {
            //If is All In One
            if (mergeMethod == MergeMethod.AllInOne)
            {
                switch (allInOneParams.atlasPadding)
                {
                    case AtlasPadding.Pixels0x0:
                        return 0;
                    case AtlasPadding.Pixels2x2:
                        return 2;
                    case AtlasPadding.Pixels4x4:
                        return 4;
                    case AtlasPadding.Pixels8x8:
                        return 8;
                    case AtlasPadding.Pixels16x16:
                        return 16;
                }
            }

            //Return the max resolution
            return 0;
        }

        private TexturesSubMeshes.UvBounds GetBoundValuesOfSubMeshUv(Vector2[] subMeshUv)
        {
            //Create the data size
            TexturesSubMeshes.UvBounds uvBounds = new TexturesSubMeshes.UvBounds();

            //Prepare the arrays
            float[] xAxis = new float[subMeshUv.Length];
            float[] yAxis = new float[subMeshUv.Length];

            //Fill all
            for (int i = 0; i < subMeshUv.Length; i++)
            {
                xAxis[i] = subMeshUv[i].x;
                yAxis[i] = subMeshUv[i].y;
            }

            //Return the data size
            uvBounds.majorX = Mathf.Max(xAxis);
            uvBounds.majorY = Mathf.Max(yAxis);
            uvBounds.minorX = Mathf.Min(xAxis);
            uvBounds.minorY = Mathf.Min(yAxis);
            return uvBounds;
        }

        private TexturesSubMeshes GetTheTextureSubMeshesOfMaterial(Material material, List<TexturesSubMeshes> listOfTexturesAndSubMeshes)
        {
            //Run a loop to return the texture and respective submeshes that use this material
            foreach (TexturesSubMeshes item in listOfTexturesAndSubMeshes)
                if (item.material == material && item.isTiledTexture == false)
                    return item;

            //If not found a item with this material, return null
            return null;
        }

        private bool isTiledTexture(TexturesSubMeshes.UvBounds bounds)
        {
            //Return if the bounds is major than one
            if (bounds.minorX < 0 || bounds.minorY < 0 || bounds.majorX > 1 || bounds.majorY > 1)
                return true;
            if (bounds.minorX >= 0 && bounds.minorY >= 0 && bounds.majorX <= 1 && bounds.majorY <= 1)
                return false;
            return false;
        }

        //Tools methods for Just Material Colors

        private Texture2D GetTextureFilledWithColorOfMaterial(Material targetMaterial, string colorPropertyToFind, int width, int height)
        {
            //Prepares the new texture, and color to fill the texture
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false);
            Color colorToFillTexture = Color.white;

            //If found the property of color
            if (targetMaterial.HasProperty(colorPropertyToFind) == true)
                colorToFillTexture = targetMaterial.GetColor(colorPropertyToFind);

            //If not found the property of color
            if (targetMaterial.HasProperty(colorPropertyToFind) == false)
            {
                //Launch log
                LaunchLog("It was not possible to find the color stored in property \"" + colorPropertyToFind + "\" of material \"" + targetMaterial.name + "\", so this Color was replaced by a GRAY texture.", LogTypeOf.Warning);

                //Set the fake color
                colorToFillTexture = Color.gray;
            }

            //Create all pixels
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = colorToFillTexture;

            //Fill the texture
            texture.SetPixels(0, 0, width, height, pixels, 0);

            //Return the texture
            return texture;
        }

        private ColorAtlasData CreateColorAtlas(UvDataAndColorOfThisSubmesh[] uvDatasAndColors, int maxResolution, int paddingBetweenTextures, bool showProgress)
        {
            //Create a atlas
            ColorAtlasData atlasData = new ColorAtlasData();
            List<Texture2D> texturesToUse = new List<Texture2D>();
            texturesToUse.Clear();
            foreach (UvDataAndColorOfThisSubmesh item in uvDatasAndColors)
                texturesToUse.Add(item.textureColor);
            atlasData.originalTexturesUsedAndOrdenedAccordingToAtlasRect = texturesToUse.ToArray();
            atlasData.atlasRects = atlasData.colorAtlas.PackTextures(texturesToUse.ToArray(), paddingBetweenTextures, maxResolution);

            //Return the object
            return atlasData;
        }
    }

    public static class SMCMeshClassExtension
    {
        /*
         * This is an extension class, which adds extra functions to the Mesh class. For example, counting vertices for each submesh.
         */

        public class Vertices
        {
            List<Vector3> verts = null;
            List<Vector2> uv1 = null;
            List<Vector2> uv2 = null;
            List<Vector2> uv3 = null;
            List<Vector2> uv4 = null;
            List<Vector3> normals = null;
            List<Vector4> tangents = null;
            List<Color32> colors = null;
            List<BoneWeight> boneWeights = null;

            public Vertices()
            {
                verts = new List<Vector3>();
            }

            public Vertices(Mesh aMesh)
            {
                verts = CreateList(aMesh.vertices);
                uv1 = CreateList(aMesh.uv);
                uv2 = CreateList(aMesh.uv2);
                uv3 = CreateList(aMesh.uv3);
                uv4 = CreateList(aMesh.uv4);
                normals = CreateList(aMesh.normals);
                tangents = CreateList(aMesh.tangents);
                colors = CreateList(aMesh.colors32);
                boneWeights = CreateList(aMesh.boneWeights);
            }

            private List<T> CreateList<T>(T[] aSource)
            {
                if (aSource == null || aSource.Length == 0)
                    return null;
                return new List<T>(aSource);
            }

            private void Copy<T>(ref List<T> aDest, List<T> aSource, int aIndex)
            {
                if (aSource == null)
                    return;
                if (aDest == null)
                    aDest = new List<T>();
                aDest.Add(aSource[aIndex]);
            }

            public int Add(Vertices aOther, int aIndex)
            {
                int i = verts.Count;
                Copy(ref verts, aOther.verts, aIndex);
                Copy(ref uv1, aOther.uv1, aIndex);
                Copy(ref uv2, aOther.uv2, aIndex);
                Copy(ref uv3, aOther.uv3, aIndex);
                Copy(ref uv4, aOther.uv4, aIndex);
                Copy(ref normals, aOther.normals, aIndex);
                Copy(ref tangents, aOther.tangents, aIndex);
                Copy(ref colors, aOther.colors, aIndex);
                Copy(ref boneWeights, aOther.boneWeights, aIndex);
                return i;
            }

            public void AssignTo(Mesh aTarget)
            {
                //Removes the limitation of 65k vertices, in case Unity supports.
                if (verts.Count > 65535)
                    aTarget.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                aTarget.SetVertices(verts);
                if (uv1 != null) aTarget.SetUVs(0, uv1);
                if (uv2 != null) aTarget.SetUVs(1, uv2);
                if (uv3 != null) aTarget.SetUVs(2, uv3);
                if (uv4 != null) aTarget.SetUVs(3, uv4);
                if (normals != null) aTarget.SetNormals(normals);
                if (tangents != null) aTarget.SetTangents(tangents);
                if (colors != null) aTarget.SetColors(colors);
                if (boneWeights != null) aTarget.boneWeights = boneWeights.ToArray();
            }
        }

        //Return count of vertices for submesh
        public static Mesh SMCGetSubmesh(this Mesh aMesh, int aSubMeshIndex)
        {
            if (aSubMeshIndex < 0 || aSubMeshIndex >= aMesh.subMeshCount)
                return null;
            int[] indices = aMesh.GetTriangles(aSubMeshIndex);
            Vertices source = new Vertices(aMesh);
            Vertices dest = new Vertices();
            Dictionary<int, int> map = new Dictionary<int, int>();
            int[] newIndices = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int o = indices[i];
                int n;
                if (!map.TryGetValue(o, out n))
                {
                    n = dest.Add(source, o);
                    map.Add(o, n);
                }
                newIndices[i] = n;
            }
            Mesh m = new Mesh();
            dest.AssignTo(m);
            m.triangles = newIndices;
            return m;
        }
    }

    public class SMCTextureResizer
    {
        public class ThreadData
        {
            public int start;
            public int end;
            public ThreadData(int s, int e)
            {
                start = s;
                end = e;
            }
        }

        private static Color[] texColors;
        private static Color[] newColors;
        private static int w;
        private static float ratioX;
        private static float ratioY;
        private static int w2;
        private static int finishCount;
        private static Mutex mutex;

        public static void Point(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, false);
        }

        public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, true);
        }

        private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight, bool useBilinear)
        {
            texColors = tex.GetPixels();
            newColors = new Color[newWidth * newHeight];
            if (useBilinear)
            {
                ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
                ratioY = 1.0f / ((float)newHeight / (tex.height - 1));
            }
            else
            {
                ratioX = ((float)tex.width) / newWidth;
                ratioY = ((float)tex.height) / newHeight;
            }
            w = tex.width;
            w2 = newWidth;
            var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
            var slice = newHeight / cores;

            finishCount = 0;
            if (mutex == null)
            {
                mutex = new Mutex(false);
            }
            if (cores > 1)
            {
                int i = 0;
                ThreadData threadData;
                for (i = 0; i < cores - 1; i++)
                {
                    threadData = new ThreadData(slice * i, slice * (i + 1));
                    ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart(BilinearScale) : new ParameterizedThreadStart(PointScale);
                    Thread thread = new Thread(ts);
                    thread.Start(threadData);
                }
                threadData = new ThreadData(slice * i, newHeight);
                if (useBilinear)
                {
                    BilinearScale(threadData);
                }
                else
                {
                    PointScale(threadData);
                }
                while (finishCount < cores)
                {
                    Thread.Sleep(1);
                }
            }
            else
            {
                ThreadData threadData = new ThreadData(0, newHeight);
                if (useBilinear)
                {
                    BilinearScale(threadData);
                }
                else
                {
                    PointScale(threadData);
                }
            }

            tex.Reinitialize(newWidth, newHeight);
            tex.SetPixels(newColors);
            tex.Apply();

            texColors = null;
            newColors = null;
        }

        public static void BilinearScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * w;
                var y2 = (yFloor + 1) * w;
                var yw = y * w2;

                for (var x = 0; x < w2; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp), ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp), y * ratioY - yFloor);
                }
            }

            mutex.WaitOne();
            finishCount++;
            mutex.ReleaseMutex();
        }

        public static void PointScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                var thisY = (int)(ratioY * y) * w;
                var yw = y * w2;
                for (var x = 0; x < w2; x++)
                {
                    newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                }
            }

            mutex.WaitOne();
            finishCount++;
            mutex.ReleaseMutex();
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value, c1.g + (c2.g - c1.g) * value, c1.b + (c2.b - c1.b) * value, c1.a + (c2.a - c1.a) * value);
        }
    }
    public static class Ex
    {
        private static Mono mono;
        class Mono : MonoBehaviour
        {

        }

        public static Texture2D ReadTexture2D(this RenderTexture texture)
        {
            RenderTexture.active = texture;
            Texture2D prev = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            prev.name = texture.name + "_" + Guid.NewGuid().ToString();
            prev.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            prev.Apply();
            RenderTexture.active = null;
            return prev;
        }

        private static void EnsureMonoIsCreate()
        {
            if (mono is not null)
            {
                return;
            }

            mono = new GameObject("Mono").AddComponent<Mono>();
        }
        public static void StartCoroutine(this IEnumerator enumerator)
        {
            EnsureMonoIsCreate();
            mono.StartCoroutine(enumerator);
        }

        public static void StartCoroutine(this IEnumerator enumerator, Action action)
        {
            EnsureMonoIsCreate();
            IEnumerator Waiting()
            {
                yield return enumerator;
                action?.Invoke();
            }
            mono.StartCoroutine(Waiting());
        }

        public static void StartCoroutine<T>(this IEnumerator enumerator, Action<T> action, T args)
        {
            EnsureMonoIsCreate();
            IEnumerator Waiting()
            {
                yield return enumerator;
                action?.Invoke(args);
            }
            mono.StartCoroutine(Waiting());
        }

        public static void StartCoroutine<T, T2>(this IEnumerator enumerator, Action<T, T2> action, T args, T2 args2)
        {
            EnsureMonoIsCreate();
            IEnumerator Waiting()
            {
                yield return enumerator;
                action?.Invoke(args, args2);
            }
            mono.StartCoroutine(Waiting());
        }

        public static bool IsNullOrEmpty(this string target)
        {
            return string.IsNullOrEmpty(target);
        }
        public static string GetMd5(this byte[] bytes)
        {
            try
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("md5file() fail, error:" + ex.Message);
            }
        }
        public static GameObject SetParent(this GameObject gameObject, GameObject parent, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (gameObject == null)
            {
                return default;
            }

            if (parent != null)
            {
                gameObject.transform.SetParent(parent.transform);
            }

            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.Euler(rotation);
            gameObject.transform.localScale = scale;
            return gameObject;
        }

        public static void ToCameraCenter(this GameObject gameObject)
        {
            Camera.main.ToViewCenter(gameObject);
        }



        public static void ToViewCenter(this Camera camera, GameObject gameObject)
        {
            if (camera == null || gameObject == null)
            {
                return;
            }
            var bound = gameObject.GetBoundingBox();
            var center = FocusCameraOnGameObject(camera, gameObject);
            camera.transform.localPosition = new Vector3(bound.center.x, bound.center.y, center.z);
            camera.transform.LookAt(bound.center, Vector3.up);
            camera.fieldOfView = 2.0f * Mathf.Atan(Mathf.Max(bound.size.y, bound.size.x) * 0.5f / Vector3.Distance(camera.transform.position, bound.center)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 获取物体包围盒
        /// </summary>
        /// <param name="obj">父物体</param>
        /// <returns>物体包围盒</returns>
        public static Bounds GetBoundingBox(this GameObject obj)
        {
            var bounds = new Bounds();
            if (obj != null)
            {
                var renders = obj.GetComponentsInChildren<Renderer>();
                if (renders != null)
                {
                    var boundscenter = Vector3.zero;
                    foreach (var item in renders)
                    {
                        boundscenter += item.bounds.center;
                    }

                    if (renders.Length > 0)
                        boundscenter /= renders.Length;
                    bounds = new Bounds(boundscenter, Vector3.zero);
                    foreach (var item in renders)
                    {
                        bounds.Encapsulate(item.bounds);
                    }
                }
            }

            return bounds;
        }

        public static Vector3 FocusCameraOnGameObject(Camera c, GameObject go)
        {
            Bounds b = GetBoundingBox(go);
            Vector3 max = b.size;
            // Get the radius of a sphere circumscribing the bounds
            float radius = max.magnitude / 2f;
            // Get the horizontal FOV, since it may be the limiting of the two FOVs to properly encapsulate the objects
            float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad / 2f) * c.aspect) * Mathf.Rad2Deg;
            // Use the smaller FOV as it limits what would get cut off by the frustum        
            float fov = Mathf.Min(c.fieldOfView, horizontalFOV);
            float dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f)) + 0.4f;
            c.transform.localPosition = new Vector3(c.transform.localPosition.x, c.transform.localPosition.y, -dist);
            if (c.orthographic)
                c.orthographicSize = radius;

            var pos = new Vector3(c.transform.localPosition.x, c.transform.localPosition.y, dist);
            return pos;
        }

        public static Texture2D Screenshot(this Camera camera, int width, int height, GameObject gameObject)
        {
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            Vector3 scale = camera.transform.localScale;
            float view = camera.fieldOfView;

            camera.ToViewCenter(gameObject);
            RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
            camera.targetTexture = renderTexture;
            RenderTexture.active = camera.targetTexture;
            camera.Render();
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.name = gameObject.name.Replace("(Clone)", "");
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            camera.transform.position = position;
            camera.transform.rotation = rotation;
            camera.transform.localScale = scale;
            camera.fieldOfView = view;
            return texture;
        }



        public static void SetRequestHeaders(this UnityWebRequest request, Dictionary<string, List<string>> headers)
        {
            if (request == null || headers == null || headers.Count <= 0)
            {
                return;
            }

            if (headers != null && headers.Count > 0)
            {
                foreach (var item in headers)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        request.SetRequestHeader(item.Key, item.Value[i]);
                    }
                }
            }
        }

        public class RequestCreateFileData
        {
            public string sid;
            public string name;
            public string md5;
            public int size;
            public string type;
            public string audit_status;

            [Preserve]
            public RequestCreateFileData()
            {
            }

            [Preserve]
            public RequestCreateFileData(string name, string md5, string type, string status, int size)
            {
                this.sid = "1";
                this.name = name;
                this.size = size;
                this.type = type;
                this.audit_status = status;
                this.md5 = md5;
            }
        }

        public class UploadData
        {
            public string id;
            public string sid;
            public string md5;
            public string name;
            public string type;
            public int size;
            public string audit_status;
            public string @object;
            public string url;
            [Preserve]
            public UploadData()
            {
            }
        }

        public class UploadAssetResponse
        {
            public int code;
            public string msg;
            public UploadData data;
            [Preserve]
            public UploadAssetResponse()
            {
            }
        }

        public class ResponseCreateFile
        {
            public int code;
            public string msg;
            public Data data;

            public UploadAssetResponse Generic()
            {
                return new UploadAssetResponse()
                {
                    code = 200,
                    msg = string.Empty,
                    data = new UploadData()
                    {
                        name = data.matter.name,
                        sid = data.matter.sid,
                        md5 = data.matter.md5,
                        type = data.matter.type,
                        url = data.matter.url,
                        size = data.matter.size,
                    }
                };
            }

            [Preserve]
            public ResponseCreateFile()
            {
            }
        }

        public class Data
        {
            public int code;
            public string msg;
            public Matter matter;
            public Dictionary<string, List<string>> headers;
            public string up_link;

            [Preserve]
            public Data()
            {
            }
        }

        public class Matter
        {
            public string id;
            public string sid;
            public string md5;
            public string name;
            public string type;
            public int size;
            public string audit_status;
            public string @object;
            public string url;

            [Preserve]
            public Matter()
            {
            }
        }

        public static IEnumerator UploadAsset(string address, string user, int pid, RequestCreateFileData requestCreate, byte[] bytes, Action<UploadAssetResponse, Exception> callback)
        {
            string postData = Newtonsoft.Json.JsonConvert.SerializeObject(requestCreate);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
            using UnityWebRequest request = UnityWebRequest.Post(address + "avatar/resource/v1/matter/create", postData);
            Debug.LogFormat("{0} {1}", request.url, postData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("userid", user);
            request.SetRequestHeader("pid", pid.ToString());
            yield return request.SendWebRequest();
            if (!request.isDone || request.result != UnityWebRequest.Result.Success)
            {
                callback(null, new Exception(request.error + "\n" + request.downloadHandler.text));
                yield break;
            }
            Debug.Log(request.downloadHandler.text);
            ResponseCreateFile responseCreateFile = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseCreateFile>(request.downloadHandler.text);
            if (responseCreateFile.code != 200)
            {
                callback(null, new Exception(responseCreateFile.msg));
                yield break;
            }
            if (string.IsNullOrEmpty(responseCreateFile.data.up_link))
            {
                callback(responseCreateFile.Generic(), null);
                yield break;
            }

            using var request1 = UnityWebRequest.Put(responseCreateFile.data.up_link, bytes);
            request1.SetRequestHeaders(responseCreateFile.data.headers);
            yield return request1.SendWebRequest();
            if (!request1.isDone || request1.result != UnityWebRequest.Result.Success)
            {
                callback(null, new Exception(request1.error + "\n" + request1.downloadHandler.text));
                yield break;
            }

            using var request2 = UnityWebRequest.Post(address + "avatar/resource/v1/matter/done?name=" + requestCreate.name, string.Empty);
            request2.SetRequestHeader("userid", user);
            request2.SetRequestHeader("pid", pid.ToString());
            yield return request2.SendWebRequest();
            if (!request2.isDone || request2.result != UnityWebRequest.Result.Success)
            {
                callback(null, new Exception(string.Format("{0}\n{1}\n{2}", request2.url, request2.error, request2.downloadHandler.text)));
            }
            else
            {
                Debug.Log(request2.downloadHandler.text);
                UploadAssetResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<UploadAssetResponse>(request2.downloadHandler.text);
                if (response.code != 200)
                {
                    callback(null, new Exception(response.msg));
                }
                else
                {
                    callback(response, null);
                }
            }
        }
        public enum PublishState : byte
        {
            None,
            Publish,
            Drafts,
            Process,
        }
        public static void UploadElementData(string address, string user, int pid, string id, string name, byte[] bytes2, GameObject gameObject, DressupData dressupData, PublishState state, Action<DressupData> onCompleted)
        {
            Executed().StartCoroutine();
            IEnumerator Executed()
            {
                Texture2D texture2D = Camera.main.Screenshot(256, 256, gameObject);
                byte[] iconDataBytes = texture2D.EncodeToPNG();
                RequestCreateFileData icon = new RequestCreateFileData(name + "_icon.png", iconDataBytes.GetMd5(), "image/png", "2", iconDataBytes.Length);
                UploadAssetResponse iconResponse = null;
                yield return Ex.UploadAsset(address, user, pid, icon, iconDataBytes, (response, exception) =>
                {
                    if (exception is not null)
                    {
                        onCompleted(null);
                        return;
                    }

                    iconResponse = response;
                });
                if (iconResponse == null)
                {
                    onCompleted(null);
                    yield break;
                }

                RequestCreateFileData drawingData = new RequestCreateFileData(name + ".png", bytes2.GetMd5(), "image/png", "2", bytes2.Length);

                yield return Ex.UploadAsset(address, user, pid, drawingData, bytes2, (response, exception) =>
                {
                    if (exception != null)
                    {
                        onCompleted(null);
                        return;
                    }

                    DressupData createElementData = new DressupData();
                    createElementData.name = name;
                    createElementData.id = id;
                    createElementData.texture = response.data.url;
                    createElementData.icon = iconResponse.data.url;
                    createElementData.model = dressupData.model;
                    createElementData.element = dressupData.element;
                    createElementData.model_name = dressupData.model_name;
                    createElementData.publish_status = (byte)state;
                    onCompleted(createElementData);
                });
            }
        }
    }
}