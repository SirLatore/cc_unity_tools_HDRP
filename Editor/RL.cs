/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC_Unity_Tools <https://github.com/soupday/CC_Unity_Tools>
 * 
 * CC_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using UnityEditor.Animations;

namespace Reallusion.Import
{
    public enum BaseGeneration
    {
        None,
        Unknown,
        GameBase,
        G1,
        G3,
        G3Plus,
        ActorCore,
        ActorBuild
    };

    /// <summary>
    ///     Functions taken from old Reallusion AutoSetup...
    /// </summary>
    public class RL
    {
        // Applicable CC character generation:        

        // Applicable CC Character Uid EBaseGeneration:
        public static readonly Dictionary<string, BaseGeneration> GENERATION_MAP = new Dictionary<string, BaseGeneration>
        {
            { "RL_CC3_Plus", BaseGeneration.G3Plus },
            { "RL_CharacterCreator_Base_Game_G1_Divide_Eyelash_UV", BaseGeneration.GameBase },
            { "RL_CharacterCreator_Base_Game_G1_Multi_UV", BaseGeneration.GameBase },
            { "RL_CharacterCreator_Base_Game_G1_One_UV", BaseGeneration.GameBase },
            { "RL_CharacterCreator_Base_Std_G3", BaseGeneration.G3 },
            { "RL_G6_Standard_Series", BaseGeneration.G1 },
            { "NonStdLookAtDataCopyFromCCBase", BaseGeneration.ActorCore },
            { "ActorBuild", BaseGeneration.ActorBuild },
            { "ActorScan", BaseGeneration.ActorCore }
        };
        
        public static BaseGeneration GetCharacterGeneration(GameObject fbx, string generationString)
        {
            if (!string.IsNullOrEmpty(generationString))
            {
                if (GENERATION_MAP.TryGetValue(generationString, out BaseGeneration gen)) return gen;
            }
            else
            {
                if (fbx)
                {
                    Transform[] children = fbx.transform.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        string objectName = child.gameObject.name;

                        if (objectName.iContains("RootNode_0_")) return BaseGeneration.ActorCore;
                        if (objectName.iContains("CC_Base_L_Pinky3")) return BaseGeneration.G3;
                        if (objectName.iContains("pinky_03_l")) return BaseGeneration.GameBase;
                        if (objectName.iContains("CC_Base_L_Finger42")) return BaseGeneration.G1;
                        if (objectName.iContains("RL_BoneRoot"))
                        {
                            if (child.Find("CC_Base_Hip"))
                                return BaseGeneration.G3;
                        }
                    }

                    foreach (Transform child in children)
                    {
                        string objectName = child.gameObject.name;

                        if (objectName.iContains("CC_Game_Body") || objectName.iContains("CC_Game_Tongue"))
                        {
                            return BaseGeneration.GameBase;
                        }

                        if (objectName == "CC_Base_Body")
                        {
                            Renderer renderer = child.GetComponent<Renderer>();
                            foreach (Material mat in renderer.sharedMaterials)
                            {
                                string materialName = mat.name;
                                if (materialName.iContains("Skin_Body"))
                                    return BaseGeneration.G1;
                                else if (materialName.iContains("Std_Skin_Body"))
                                    return BaseGeneration.G3;
                                else if (materialName.iContains("ga_skin_body"))
                                    return BaseGeneration.GameBase;
                            }
                        }
                    }
                }                
            }
            return BaseGeneration.Unknown;
        }

        public static void HumanoidImportSettings(GameObject fbx, ModelImporter importer, string characterName, BaseGeneration generation, CharacterInfo.RigOverride rigOverride, QuickJSON jsonData)
        {
            importer.importNormals = ModelImporterNormals.Calculate;
            //importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importBlendShapes = true;
            importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            importer.generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.autoGenerateAvatarMappingIfUnspecified = true;

            if (generation == BaseGeneration.Unknown)
            {
                if (!characterName.Contains("_Motion"))
                {
                    switch (rigOverride)
                    {
                        case CharacterInfo.RigOverride.None:
                            importer.animationType = ModelImporterAnimationType.None;
                            break;                        
                        case CharacterInfo.RigOverride.Humanoid:
                            importer.animationType = ModelImporterAnimationType.Human;
                            break;
                        case CharacterInfo.RigOverride.Generic:
                        default:
                            importer.animationType = ModelImporterAnimationType.Generic;
                            break;
                    }                    
                }
                return;
            }

            HumanDescription human = importer.humanDescription;
            Func<string, string, HumanBone> Bone = (humanName, boneName) => new HumanBone()
            {
                humanName = humanName,
                boneName = boneName
            };
            List<HumanBone> boneList = new List<HumanBone>();

            #region HumanBoneDescription
            if (generation == BaseGeneration.G3 || 
                generation == BaseGeneration.G3Plus || 
                generation == BaseGeneration.ActorCore ||
                generation == BaseGeneration.ActorBuild)
            {
                boneList = new List<HumanBone> {                 
                        Bone("Chest", "CC_Base_Spine01"),
                        Bone("Head", "CC_Base_Head"),
                        Bone("Hips", "CC_Base_Hip"),
                        Bone("Jaw", "CC_Base_JawRoot"),
                        Bone("Left Index Distal", "CC_Base_L_Index3"),
                        Bone("Left Index Intermediate", "CC_Base_L_Index2"),
                        Bone("Left Index Proximal", "CC_Base_L_Index1"),
                        Bone("Left Little Distal","CC_Base_L_Pinky3"),
                        Bone("Left Little Intermediate","CC_Base_L_Pinky2"),
                        Bone("Left Little Proximal","CC_Base_L_Pinky1"),
                        Bone("Left Middle Distal", "CC_Base_L_Mid3"),
                        Bone("Left Middle Intermediate", "CC_Base_L_Mid2"),
                        Bone("Left Middle Proximal", "CC_Base_L_Mid1"),
                        Bone("Left Ring Distal", "CC_Base_L_Ring3"),
                        Bone("Left Ring Intermediate", "CC_Base_L_Ring2"),
                        Bone("Left Ring Proximal", "CC_Base_L_Ring1"),
                        Bone("Left Thumb Distal", "CC_Base_L_Thumb3"),
                        Bone("Left Thumb Intermediate", "CC_Base_L_Thumb2"),
                        Bone("Left Thumb Proximal", "CC_Base_L_Thumb1"),
                        Bone("LeftEye","CC_Base_L_Eye"),
                        Bone("LeftFoot", "CC_Base_L_Foot"),
                        Bone("LeftHand", "CC_Base_L_Hand"),
                        Bone("LeftLowerArm", "CC_Base_L_Forearm"),
                        Bone("LeftLowerLeg", "CC_Base_L_Calf"),
                        Bone("LeftShoulder", "CC_Base_L_Clavicle"),
                        Bone("LeftToes", "CC_Base_L_ToeBase"),
                        Bone("LeftUpperArm", "CC_Base_L_Upperarm"),
                        Bone("LeftUpperLeg", "CC_Base_L_Thigh"),
                        Bone("Neck", "CC_Base_NeckTwist01"),
                        Bone("Right Index Distal", "CC_Base_R_Index3"),
                        Bone("Right Index Intermediate", "CC_Base_R_Index2"),
                        Bone("Right Index Proximal", "CC_Base_R_Index1"),
                        Bone("Right Little Distal","CC_Base_R_Pinky3"),
                        Bone("Right Little Intermediate","CC_Base_R_Pinky2"),
                        Bone("Right Little Proximal","CC_Base_R_Pinky1"),
                        Bone("Right Middle Distal", "CC_Base_R_Mid3"),
                        Bone("Right Middle Intermediate", "CC_Base_R_Mid2"),
                        Bone("Right Middle Proximal", "CC_Base_R_Mid1"),
                        Bone("Right Ring Distal", "CC_Base_R_Ring3"),
                        Bone("Right Ring Intermediate", "CC_Base_R_Ring2"),
                        Bone("Right Ring Proximal", "CC_Base_R_Ring1"),
                        Bone("Right Thumb Distal", "CC_Base_R_Thumb3"),
                        Bone("Right Thumb Intermediate", "CC_Base_R_Thumb2"),
                        Bone("Right Thumb Proximal", "CC_Base_R_Thumb1"),
                        Bone("RightEye","CC_Base_R_Eye"),
                        Bone("RightFoot", "CC_Base_R_Foot"),
                        Bone("RightHand", "CC_Base_R_Hand"),
                        Bone("RightLowerArm", "CC_Base_R_Forearm"),
                        Bone("RightLowerLeg", "CC_Base_R_Calf"),
                        Bone("RightShoulder", "CC_Base_R_Clavicle"),
                        Bone("RightToes", "CC_Base_R_ToeBase"),
                        Bone("RightUpperArm", "CC_Base_R_Upperarm"),
                        Bone("RightUpperLeg", "CC_Base_R_Thigh"),
                        Bone("Spine", "CC_Base_Waist"),
                        Bone("UpperChest", "CC_Base_Spine02"),
                    };
            }
            else if (generation == BaseGeneration.G1)
            {
                boneList = new List<HumanBone> {
                        Bone("Chest", "CC_Base_Spine01"),
                        Bone("Head", "CC_Base_Head"),
                        Bone("Hips", "CC_Base_Hip"),
                        Bone("Jaw", "CC_Base_JawRoot"),
                        Bone("Left Index Distal", "CC_Base_L_Finger12"),
                        Bone("Left Index Intermediate", "CC_Base_L_Finger11"),
                        Bone("Left Index Proximal", "CC_Base_L_Finger10"),
                        Bone("Left Little Distal","CC_Base_L_Finger42"),
                        Bone("Left Little Intermediate","CC_Base_L_Finger41"),
                        Bone("Left Little Proximal","CC_Base_L_Finger40"),
                        Bone("Left Middle Distal", "CC_Base_L_Finger22"),
                        Bone("Left Middle Intermediate", "CC_Base_L_Finger21"),
                        Bone("Left Middle Proximal", "CC_Base_L_Finger20"),
                        Bone("Left Ring Distal", "CC_Base_L_Finger32"),
                        Bone("Left Ring Intermediate", "CC_Base_L_Finger31"),
                        Bone("Left Ring Proximal", "CC_Base_L_Finger30"),
                        Bone("Left Thumb Distal", "CC_Base_L_Finger02"),
                        Bone("Left Thumb Intermediate", "CC_Base_L_Finger01"),
                        Bone("Left Thumb Proximal", "CC_Base_L_Finger00"),
                        Bone("LeftEye","CC_Base_L_Eye"),
                        Bone("LeftFoot", "CC_Base_L_Foot"),
                        Bone("LeftHand", "CC_Base_L_Hand"),
                        Bone("LeftLowerArm", "CC_Base_L_Forearm"),
                        Bone("LeftLowerLeg", "CC_Base_L_Calf"),
                        Bone("LeftShoulder", "CC_Base_L_Clavicle"),
                        Bone("LeftToes", "CC_Base_L_ToeBase"),
                        Bone("LeftUpperArm", "CC_Base_L_Upperarm"),
                        Bone("LeftUpperLeg", "CC_Base_L_Thigh"),
                        Bone("Neck", "CC_Base_NeckTwist01"),
                        Bone("Right Index Distal", "CC_Base_R_Finger12"),
                        Bone("Right Index Intermediate", "CC_Base_R_Finger11"),
                        Bone("Right Index Proximal", "CC_Base_R_Finger10"),
                        Bone("Right Little Distal","CC_Base_R_Finger42"),
                        Bone("Right Little Intermediate","CC_Base_R_Finger41"),
                        Bone("Right Little Proximal","CC_Base_R_Finger40"),
                        Bone("Right Middle Distal", "CC_Base_R_Finger22"),
                        Bone("Right Middle Intermediate", "CC_Base_R_Finger21"),
                        Bone("Right Middle Proximal", "CC_Base_R_Finger20"),
                        Bone("Right Ring Distal", "CC_Base_R_Finger32"),
                        Bone("Right Ring Intermediate", "CC_Base_R_Finger31"),
                        Bone("Right Ring Proximal", "CC_Base_R_Finger30"),
                        Bone("Right Thumb Distal", "CC_Base_R_Finger02"),
                        Bone("Right Thumb Intermediate", "CC_Base_R_Finger01"),
                        Bone("Right Thumb Proximal", "CC_Base_R_Finger00"),
                        Bone("RightEye","CC_Base_R_Eye"),
                        Bone("RightFoot", "CC_Base_R_Foot"),
                        Bone("RightHand", "CC_Base_R_Hand"),
                        Bone("RightLowerArm", "CC_Base_R_Forearm"),
                        Bone("RightLowerLeg", "CC_Base_R_Calf"),
                        Bone("RightShoulder", "CC_Base_R_Clavicle"),
                        Bone("RightToes", "CC_Base_R_ToeBase"),
                        Bone("RightUpperArm", "CC_Base_R_Upperarm"),
                        Bone("RightUpperLeg", "CC_Base_R_Thigh"),
                        Bone("Spine", "CC_Base_Waist"),
                        Bone("UpperChest", "CC_Base_Spine02"),
                    };
            }
            else if (generation == BaseGeneration.GameBase)
            {
                boneList = new List<HumanBone> {
                        Bone("Chest", "spine_02"),
                        Bone("Head", "head"),
                        Bone("Hips", "pelvis"),
                        Bone("Jaw", "CC_Base_JawRoot"),
                        Bone("Left Index Distal", "index_03_l"),
                        Bone("Left Index Intermediate", "index_02_l"),
                        Bone("Left Index Proximal", "index_01_l"),
                        Bone("Left Little Distal","pinky_03_l"),
                        Bone("Left Little Intermediate","pinky_02_l"),
                        Bone("Left Little Proximal","pinky_01_l"),
                        Bone("Left Middle Distal", "middle_03_l"),
                        Bone("Left Middle Intermediate", "middle_02_l"),
                        Bone("Left Middle Proximal", "middle_01_l"),
                        Bone("Left Ring Distal", "ring_03_l"),
                        Bone("Left Ring Intermediate", "ring_02_l"),
                        Bone("Left Ring Proximal", "ring_01_l"),
                        Bone("Left Thumb Distal", "thumb_03_l"),
                        Bone("Left Thumb Intermediate", "thumb_02_l"),
                        Bone("Left Thumb Proximal", "thumb_01_l"),
                        Bone("LeftEye","CC_Base_L_Eye"),
                        Bone("LeftFoot", "foot_l"),
                        Bone("LeftHand", "hand_l"),
                        Bone("LeftLowerArm", "lowerarm_l"),
                        Bone("LeftLowerLeg", "calf_l"),
                        Bone("LeftShoulder", "clavicle_l"),
                        Bone("LeftToes", "ball_l"),
                        Bone("LeftUpperArm", "upperarm_l"),
                        Bone("LeftUpperLeg", "thigh_l"),
                        Bone("Neck", "neck_01"),
                        Bone("Right Index Distal", "index_03_r"),
                        Bone("Right Index Intermediate", "index_02_r"),
                        Bone("Right Index Proximal", "index_01_r"),
                        Bone("Right Little Distal","pinky_03_r"),
                        Bone("Right Little Intermediate","pinky_02_r"),
                        Bone("Right Little Proximal","pinky_01_r"),
                        Bone("Right Middle Distal", "middle_03_r"),
                        Bone("Right Middle Intermediate", "middle_02_r"),
                        Bone("Right Middle Proximal", "middle_01_r"),
                        Bone("Right Ring Distal", "ring_03_r"),
                        Bone("Right Ring Intermediate", "ring_02_r"),
                        Bone("Right Ring Proximal", "ring_01_r"),
                        Bone("Right Thumb Distal", "thumb_03_r"),
                        Bone("Right Thumb Intermediate", "thumb_02_r"),
                        Bone("Right Thumb Proximal", "thumb_01_r"),
                        Bone("RightEye","CC_Base_R_Eye"),
                        Bone("RightFoot", "foot_r"),
                        Bone("RightHand", "hand_r"),
                        Bone("RightLowerArm", "lowerarm_r"),
                        Bone("RightLowerLeg", "calf_r"),
                        Bone("RightShoulder", "clavicle_r"),
                        Bone("RightToes", "ball_r"),
                        Bone("RightUpperArm", "upperarm_r"),
                        Bone("RightUpperLeg", "thigh_r"),
                        Bone("Spine", "spine_01"),
                        Bone("UpperChest", "spine_03"),
                    };
            }

            // clean up bone list for missing bones (from bone LOD exports)
            for (int b = 0; b < boneList.Count; b++)
            {
                if (Util.FindChildRecursive(fbx.transform, boneList[b].boneName) == null)
                {
                    //Debug.LogWarning("Missing bone: " + boneList[b].boneName);
                    boneList.RemoveAt(b--);
                }
            }
            
            if (boneList.Count > 0)
                human.human = boneList.ToArray();

            #endregion

            for (int i = 0; i < human.human.Length; ++i)
            {
                human.human[i].limit.useDefaultValues = true;
            }

            human.upperArmTwist = 0.5f;
            human.lowerArmTwist = 0.5f;
            human.upperLegTwist = 0.5f;
            human.lowerLegTwist = 0.5f;
            human.armStretch = 0.05f;
            human.legStretch = 0.05f;
            human.feetSpacing = 0.0f;
            human.hasTranslationDoF = false;

            if (jsonData != null || !characterName.iContains("_Motion"))
            {
                Transform[] transforms = fbx.GetComponentsInChildren<Transform>();
                SkeletonBone[] bones = new SkeletonBone[transforms.Length];
                for (int i = 0; i < transforms.Length; i++)
                {
                    bones[i].name = transforms[i].name;
                    bones[i].position = transforms[i].localPosition;
                    bones[i].rotation = transforms[i].localRotation;
                    bones[i].scale = transforms[i].localScale;
                }
                human.skeleton = bones;                
            }

            importer.humanDescription = human;
        }
        
        public static AnimatorController AutoCreateAnimator(GameObject fbx, string assetPath, ModelImporter importer)
        {
            string animatorPath = Path.GetDirectoryName(assetPath) + "/" + fbx.name + "_animator.controller";
            
            AnimatorController controller = null;

            if (!File.Exists(animatorPath))
            {
                ModelImporterClipAnimation[] clipAnimations = importer.defaultClipAnimations;

                if (clipAnimations.Length != 0)
                {
                    AnimatorController.CreateAnimatorControllerAtPath(animatorPath);
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
                    AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

                    UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (UnityEngine.Object obj in assets)
                    {
                        AnimationClip clip = obj as AnimationClip;
                        clip = AnimRetargetGUI.TryGetRetargetedAnimationClip(fbx, clip);

                        if (clip)
                        {
                            if (clip.name.iContains("__preview__") || clip.name.iContains("t-pose"))
                                continue;

                            controller.AddMotion(clip, 0);
                        }
                    }

                    if (AssetDatabase.WriteImportSettingsIfDirty(assetPath))
                    {
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }
            else
            {
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorPath);
            }

            return controller;
        }
        
        public static void SetupAnimation(ModelImporter importer, CharacterInfo characterInfo, bool forceUpdate)
        {
            if (importer.defaultClipAnimations.Length > 0)
            {
                if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
                    importer.clipAnimations = importer.defaultClipAnimations;
            }

            ModelImporterClipAnimation[] animations = importer.clipAnimations;
            if (animations == null) return;

            bool changed = false;

            foreach (ModelImporterClipAnimation anim in animations)
            {
                if (!anim.keepOriginalOrientation || !anim.keepOriginalPositionY || !anim.keepOriginalPositionXZ ||
                    !anim.lockRootRotation || !anim.lockRootHeightY)
                {
                    anim.keepOriginalOrientation = true;
                    anim.keepOriginalPositionY = true;
                    anim.keepOriginalPositionXZ = true;
                    anim.lockRootRotation = true;
                    anim.lockRootHeightY = true;
                    changed = true;
                }

                if (anim.name.iContains("idle") && !anim.lockRootPositionXZ)
                {
                    anim.lockRootPositionXZ = true;
                    changed = true;
                }

                if (anim.name.iContains("_loop") && !anim.loopTime)
                {
                    anim.loopTime = true;
                    changed = true;
                }
            }

            if (changed)
            {
                importer.clipAnimations = animations;
                if (forceUpdate)
                {
                    AssetDatabase.WriteImportSettingsIfDirty(characterInfo.path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            characterInfo.animationSetup = true;
        }

        public static void SetAnimationImport(CharacterInfo info, GameObject fbx)
        {            
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(info.path);                        
            SetupAnimation(importer, info, true);            
            ApplyAnimatorController(info, AutoCreateAnimator(fbx, info.path, importer));
        }

        public static void ApplyAnimatorController(CharacterInfo info, AnimatorController controller)
        {
            string prefabFolder = Util.CreateFolder(info.folder, Importer.PREFABS_FOLDER);
            string prefabPath = Path.Combine(prefabFolder, info.name + ".prefab");
            string prefabBakedPath = Path.Combine(prefabFolder, info.name + Importer.BAKE_SUFFIX + ".prefab");

            if (controller)
            {
                if (File.Exists(prefabPath))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    Animator animator = prefab.GetComponent<Animator>();

                    if (animator && !animator.runtimeAnimatorController)
                    {
                        animator.runtimeAnimatorController = controller;
                        animator.applyRootMotion = true;
                        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    }
                }

                if (File.Exists(prefabBakedPath))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabBakedPath);
                    Animator animator = prefab.GetComponent<Animator>();

                    if (animator && !animator.runtimeAnimatorController)
                    {
                        animator.runtimeAnimatorController = controller;
                        animator.applyRootMotion = true;
                        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    }
                }
            }
        }

        public static GameObject CreatePrefabFromFbx(CharacterInfo info, GameObject fbx, out GameObject sceneInstance)
        {
            bool noMotion = !info.name.iContains("_Motion");
            sceneInstance = null;

            if (noMotion)
            {
                // Set the Prefab
                if (info.path.iContains("_lod") && CountLODs(fbx) > 1)
                {                    
                    return CreateOneLODPrefabFromModel(info, fbx, "", out sceneInstance);
                }
                else
                {                    
                    return CreatePrefabFromModel(info, fbx, out sceneInstance);
                }
            }

            return null;
        }

        /// <summary>
        ///     Note: no longer deletes the clone. Use SaveAndRemoveScenePrefab() to finalize the prefab.
        /// </summary>
        public static GameObject CreatePrefabFromModel(CharacterInfo info, GameObject fbx, out GameObject sceneInstance)
        {
            // Create a Prefab folder:          
            string prefabFolder = Util.CreateFolder(info.folder, Importer.PREFABS_FOLDER);
            //string namedPrefabFolder = Util.CreateFolder(prefabFolder, info.name);
            string prefabPath = Path.Combine(prefabFolder, info.name + ".prefab");
            string animatorControllerPath = Path.Combine(info.folder, info.name + "_animator.controller");            

            // Apply to the scene:
            sceneInstance = PrefabUtility.InstantiatePrefab(fbx) as GameObject;            

            // Apply Animator:
            if (!sceneInstance.GetComponent<Animator>().runtimeAnimatorController)
            {
                if (File.Exists(animatorControllerPath))
                    sceneInstance.GetComponent<Animator>().runtimeAnimatorController = 
                            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);

                sceneInstance.GetComponent<Animator>().applyRootMotion = true;
                sceneInstance.GetComponent<Animator>().cullingMode = AnimatorCullingMode.CullUpdateTransforms;                
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sceneInstance, prefabPath);

            return prefab;
        }

        public static GameObject SaveAndRemovePrefabInstance(GameObject prefabAsset, GameObject prefabInstance)
        {            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabInstance, AssetDatabase.GetAssetPath(prefabAsset));
            UnityEngine.Object.DestroyImmediate(prefabInstance);
            return prefab;
        }

        public static int CountLODs(GameObject fbx)
        {
            List<int> levels = new List<int>(5);
            Renderer[] renderers = fbx.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                int index = r.name.LastIndexOf("_LOD");                
                if (index >= 0 && r.name.Length - index == 5 && char.IsDigit(r.name[r.name.Length - 1]))
                {
                    // any mesh with a _LOD<N> suffix is a LOD level
                    string levelString = r.name.Substring(r.name.Length - 1, 1);
                    if (int.TryParse(levelString, out int level))
                    {
                        if (!levels.Contains(level)) levels.Add(level);
                    }
                }
                else
                {
                    // assume any mesh without a _LOD<N> suffix is the original model (LOD0)
                    int level = 0;
                    if (!levels.Contains(level)) levels.Add(level);
                }
            }
            return levels.Count;
        }
        
        public static GameObject CreateOneLODPrefabFromModel(CharacterInfo info, GameObject fbx, string suffix, out GameObject sceneLODInstance)
        {
            sceneLODInstance = new GameObject();
            LODGroup lodGroup = sceneLODInstance.AddComponent<LODGroup>();
            string prefabFolder = Util.CreateFolder(info.folder, Importer.PREFABS_FOLDER);
            //string namedPrefabFolder = Util.CreateFolder(prefabFolder, info.name);
            string prefabPath = Path.Combine(prefabFolder, info.name + suffix + ".prefab");
            string animatorControllerPath = Path.Combine(info.folder, info.name + "_animator.controller");            

            Renderer[] renderers = fbx.transform.GetComponentsInChildren<Renderer>(true);
            int lodLevel = 0;
            foreach (Renderer child in renderers)
            {
                if (child.name.Contains("_LOD"))
                {
                    string level = child.name.Substring((child.name.Length - 1), 1);
                    lodLevel = Math.Max(lodLevel, int.Parse(level));
                }
            }

            if (renderers.Length == lodLevel)
            {
                LOD[] lods = new LOD[lodLevel];
                GameObject lodPrefabTemp = PrefabUtility.InstantiatePrefab(fbx) as GameObject;
                lodPrefabTemp.transform.SetParent(sceneLODInstance.transform, false);
                Renderer[] prefabRenderers = lodPrefabTemp.transform.GetComponentsInChildren<Renderer>(true);

                for (int i = 0; i < lodLevel; ++i) // Does not process LOD0
                {
                    string LODLevel = "_LOD" + (i + 1);
                    for (int j = 0; j < prefabRenderers.Length; j++)
                    {
                        if (prefabRenderers[j].name.Contains(LODLevel))
                        {
                            Renderer[] rendererLOD = new Renderer[1];
                            rendererLOD[0] = prefabRenderers[j];
                            lods[i] = new LOD(1.0F / (i + 2), rendererLOD);
                        }

                        if (i == lodLevel - 1)
                        {
                            lods[i].screenRelativeTransitionHeight = (0.02f);
                        }
                    }
                }

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();
            }
            else
            {
                lodLevel++;
                LOD[] lods = new LOD[lodLevel];
                GameObject lodPrefabTemp = PrefabUtility.InstantiatePrefab(fbx) as GameObject;
                lodPrefabTemp.transform.SetParent(sceneLODInstance.transform, false);
                Renderer[] prefabRenderers = lodPrefabTemp.transform.GetComponentsInChildren<Renderer>(true);

                if (File.Exists(animatorControllerPath))
                    lodPrefabTemp.GetComponent<Animator>().runtimeAnimatorController = 
                            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);

                List<Renderer> renderersListLOD0 = new List<Renderer>();
                for (int i = 0; i < prefabRenderers.Length; i++) // Process LOD0
                {
                    if (!prefabRenderers[i].name.Contains("_LOD"))
                    {
                        renderersListLOD0.Add(prefabRenderers[i]);
                    }
                }
                Renderer[] renderersLOD0 = renderersListLOD0.ToArray();
                lods[0] = new LOD((1.0F / (2)), renderersLOD0);
                for (int i = 1; i < lodLevel; i++)
                {
                    string LODLevel = "_LOD" + i;
                    for (int j = 0; j < prefabRenderers.Length; j++)
                    {
                        if (prefabRenderers[j].name.Contains(LODLevel))
                        {
                            Renderer[] rendererLOD = new Renderer[1];
                            rendererLOD[0] = prefabRenderers[j];
                            lods[i] = new LOD(1.0F / (i + 2), rendererLOD);
                        }
                        if (i == lodLevel - 1)
                        {
                            lods[i].screenRelativeTransitionHeight = (0.02f);
                        }
                    }
                }
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sceneLODInstance, prefabPath);

            return prefab;
        }        
        
        public static bool IsBodyMesh(SkinnedMeshRenderer smr)
        {
            string meshName = smr.gameObject.name;

            if (meshName.iEquals("CC_Base_Body")) return true;
            if (meshName.iEquals("CC_Game_Body")) return true;

            foreach (Material mat in smr.sharedMaterials)
            {
                if (mat.name.iContains("Std_Skin_")) return true;
                if (mat.shader.name.iContains(Pipeline.SHADER_HQ_HEAD) ||
                    mat.shader.name.iContains(Pipeline.SHADER_HQ_SKIN)) return true;
            }

            return false;
        }

        public static bool IsHairMesh(SkinnedMeshRenderer smr)
        {
            string meshName = smr.gameObject.name;

            foreach (Material mat in smr.sharedMaterials)
            {
                if (mat.name.iContains("Hair") && mat.name.iContains("Transparency")) return true;
                if (mat.shader.name.iContains(Pipeline.SHADER_HQ_HAIR)) return true;
            }

            return false;
        }
    }
}