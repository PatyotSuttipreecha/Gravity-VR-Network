/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEditor;
using UnityEngine;

namespace TowerDefenseMP
{
    public class PluginSetup : EditorWindow
    {
        private static Texture2D[] pluginImages;
        private static string packagesPath;

        private enum Packages
        {
            UnityNetcode = 0
            //PhotonPUN = 1
        }


        [MenuItem("Window/Tower Defense Multiplayer/Network Setup")]
        static void Init()
        {
            packagesPath = "/Packages/";
            EditorWindow window = GetWindowWithRect(typeof(PluginSetup), new Rect(0, 0, 850, 420), false, "Network Setup");

            var script = MonoScript.FromScriptableObject(window);
            string thisPath = AssetDatabase.GetAssetPath(script);
            packagesPath = thisPath.Replace("/PluginSetup.cs", packagesPath);
        }


        void OnGUI()
        {
            if (pluginImages == null)
            {
                var script = MonoScript.FromScriptableObject(this);
                string path = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(script)) + "/EditorFiles/";

                int enumLength = System.Enum.GetNames(typeof(Packages)).Length;
                pluginImages = new Texture2D[enumLength];
                for (int i = 0; i < enumLength; i++)
                    pluginImages[i] = AssetDatabase.LoadAssetAtPath(path + ((Packages)i).ToString() + ".png", typeof(Texture2D)) as Texture2D;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tower Defense Multiplayer - Network Setup", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Please select the network integration you are going to use this asset with (must be imported beforehand!).");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Note: For a detailed comparison about features and pricing, please refer to the provider's website.");
            //EditorGUILayout.LabelField("If possible, the features of this asset are the same across all multiplayer services.");
            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < pluginImages.Length; i++)
            {
                EditorGUILayout.BeginVertical();
                if (GUILayout.Button(pluginImages[i]))
                {
                    Setup(i);
                }

                Packages thisPackage = (Packages)i;
                if (GUILayout.Button("[ ? ] " + thisPackage.ToString(), GUILayout.Width(120)))
                {
                    switch (thisPackage)
                    {
                        case Packages.UnityNetcode:
                            Application.OpenURL("https://docs-multiplayer.unity3d.com/");
                            break;

                        /*
                        case Packages.PhotonPUN:
                            Application.OpenURL("https://www.photonengine.com/en/Realtime");
                            break;
                        */
                    }
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(20);
        }


        void Setup(int index)
        {
            switch (index)
            {
                case (int)Packages.UnityNetcode:
                    AssetDatabase.ImportPackage(packagesPath + Packages.UnityNetcode.ToString() + ".unitypackage", true);
                    break;

                /*
                case (int)Packages.PhotonPUN:
                    AssetDatabase.ImportPackage(packagesPath + Packages.PhotonPUN.ToString() + ".unitypackage", true);
                    break;
                */
            }
        }
    }
}