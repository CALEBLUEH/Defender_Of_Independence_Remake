using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MuseumMigrationUtility
{
    private const string ScenePath = "Assets/Museum/Scenes/Museum.unity";
    private const string DefaultMaterialPath = "Assets/Museum/Art/Materials/MuseumDefault.mat";

    private sealed class MaterialSpec
    {
        public string Path { get; }
        public string[] BaseTextureNames { get; }
        public string[] NormalTextureNames { get; }
        public string[] OcclusionTextureNames { get; }

        public MaterialSpec(string path, string[] baseTextureNames, string[] normalTextureNames, string[] occlusionTextureNames = null)
        {
            Path = path;
            BaseTextureNames = baseTextureNames;
            NormalTextureNames = normalTextureNames;
            OcclusionTextureNames = occlusionTextureNames ?? Array.Empty<string>();
        }
    }

    private static readonly MaterialSpec[] MaterialSpecs =
    {
        new MaterialSpec(
            "Assets/Museum/Art/Architecture/Materials/M_Pillar_A.mat",
            new[] { "_Color_Texture", "_BaseMap", "_MainTex" },
            new[] { "_Normal_Texture", "_BumpMap", "_NormalMap" }),
        new MaterialSpec(
            "Assets/Museum/Art/Architecture/Materials/M_Wall.mat",
            new[] { "_Color_Texture", "_BaseMap", "_MainTex" },
            new[] { "_Normal_Texture", "_BumpMap", "_NormalMap" }),
        new MaterialSpec(
            "Assets/Museum/Art/Environment/Materials/Floor.mat",
            new[] { "_BaseColorMap", "_BaseMap", "_MainTex" },
            new[] { "_NormalMap", "_BumpMap" }),
        new MaterialSpec(
            "Assets/Museum/Art/Environment/Materials/light.mat",
            new[] { "_BaseColorMap", "_BaseMap", "_MainTex" },
            new[] { "_NormalMap", "_BumpMap" }),
        new MaterialSpec(
            "Assets/Museum/Art/Props/Radio/Materials/Radio.mat",
            new[] { "_MainTex", "_BaseColorMap", "_BaseMap" },
            new[] { "_BumpMap", "_NormalMap" },
            new[] { "_OcclusionMap" })
    };

    private static readonly string[] PrefabPaths =
    {
        "Assets/Museum/Art/Architecture/Prefabs/Pillar_A.prefab",
        "Assets/Museum/Art/Architecture/Prefabs/Wall_A.prefab",
        "Assets/Museum/Art/Architecture/Prefabs/Wall_B.prefab",
        "Assets/Museum/Art/Props/Radio/Prefabs/Radio.prefab",
        "Assets/Museum/Prefabs/Environment/Benches.prefab",
        "Assets/Museum/Prefabs/Environment/Cabinet.prefab",
        "Assets/Museum/Prefabs/Environment/Frame.prefab"
    };

    public static void RunFromCommandLine()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                throw new InvalidOperationException("The URP Lit shader is unavailable. Confirm that URP is installed and active.");
            }

            Material defaultMaterial = GetOrCreateDefaultMaterial(urpLit);
            foreach (MaterialSpec spec in MaterialSpecs)
            {
                ConvertMaterial(spec, urpLit);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (string prefabPath in PrefabPaths)
            {
                RepairPrefabMaterials(prefabPath, defaultMaterial);
            }

            RepairScene(defaultMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MUSEUM_MIGRATION_SUCCESS");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    private static Material GetOrCreateDefaultMaterial(Shader urpLit)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
        if (material == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DefaultMaterialPath) ?? "Assets/Museum/Art");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            material = new Material(urpLit) { name = "MuseumDefault" };
            material.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f, 1f));
            AssetDatabase.CreateAsset(material, DefaultMaterialPath);
        }
        else
        {
            material.shader = urpLit;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static void ConvertMaterial(MaterialSpec spec, Shader urpLit)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(spec.Path);
        if (material == null)
        {
            throw new FileNotFoundException("Required museum material was not imported.", spec.Path);
        }

        Texture baseTexture = GetSavedTexture(material, spec.BaseTextureNames);
        Texture normalTexture = GetSavedTexture(material, spec.NormalTextureNames);
        Texture occlusionTexture = GetSavedTexture(material, spec.OcclusionTextureNames);
        Color? baseColor = GetSavedColor(material, "_BaseColor", "_Color");

        material.shader = urpLit;
        material.renderQueue = -1;

        if (baseTexture != null)
        {
            material.SetTexture("_BaseMap", baseTexture);
        }

        if (baseColor.HasValue)
        {
            material.SetColor("_BaseColor", baseColor.Value);
        }

        if (normalTexture != null)
        {
            material.SetTexture("_BumpMap", normalTexture);
            material.EnableKeyword("_NORMALMAP");
        }
        else
        {
            material.DisableKeyword("_NORMALMAP");
        }

        if (occlusionTexture != null)
        {
            material.SetTexture("_OcclusionMap", occlusionTexture);
        }

        EditorUtility.SetDirty(material);
        Debug.Log($"Converted museum material to URP Lit: {spec.Path}");
    }

    private static Texture GetSavedTexture(Material material, IReadOnlyCollection<string> candidateNames)
    {
        if (candidateNames.Count == 0)
        {
            return null;
        }

        SerializedObject serializedMaterial = new SerializedObject(material);
        SerializedProperty entries = serializedMaterial.FindProperty("m_SavedProperties.m_TexEnvs");
        if (entries == null || !entries.isArray)
        {
            return null;
        }

        foreach (string candidate in candidateNames)
        {
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("first").stringValue != candidate)
                {
                    continue;
                }

                SerializedProperty textureProperty = entry.FindPropertyRelative("second.m_Texture");
                if (textureProperty.objectReferenceValue is Texture texture)
                {
                    return texture;
                }
            }
        }

        return null;
    }

    private static Color? GetSavedColor(Material material, params string[] candidateNames)
    {
        SerializedObject serializedMaterial = new SerializedObject(material);
        SerializedProperty entries = serializedMaterial.FindProperty("m_SavedProperties.m_Colors");
        if (entries == null || !entries.isArray)
        {
            return null;
        }

        foreach (string candidate in candidateNames)
        {
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("first").stringValue == candidate)
                {
                    return entry.FindPropertyRelative("second").colorValue;
                }
            }
        }

        return null;
    }

    private static void RepairPrefabMaterials(string prefabPath, Material defaultMaterial)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            int replacements = ReplaceMissingMaterials(prefabRoot, defaultMaterial);
            if (replacements > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"Repaired {replacements} missing material reference(s) in {prefabPath}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void RepairScene(Material defaultMaterial)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int removedScripts = 0;
        int replacedMaterials = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                removedScripts += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            }

            replacedMaterials += ReplaceMissingMaterials(root, defaultMaterial);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new InvalidOperationException("Unity could not save the migrated museum scene.");
        }

        int remainingMissingScripts = CountMissingScripts(scene);
        int remainingMissingMaterials = CountMissingMaterials(scene);
        Debug.Log($"Museum scene repaired. Removed missing scripts: {removedScripts}; replaced material slots: {replacedMaterials}.");

        if (remainingMissingScripts != 0 || remainingMissingMaterials != 0)
        {
            throw new InvalidOperationException(
                $"Museum validation failed. Missing scripts: {remainingMissingScripts}; missing material slots: {remainingMissingMaterials}.");
        }
    }

    private static int ReplaceMissingMaterials(GameObject root, Material defaultMaterial)
    {
        int replacements = 0;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int index = 0; index < materials.Length; index++)
            {
                if (materials[index] != null)
                {
                    continue;
                }

                materials[index] = defaultMaterial;
                replacements++;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        return replacements;
    }

    private static int CountMissingScripts(Scene scene)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            }
        }

        return count;
    }

    private static int CountMissingMaterials(Scene scene)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }
}
