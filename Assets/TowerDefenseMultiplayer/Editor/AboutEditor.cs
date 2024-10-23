/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEditor;

namespace TowerDefenseMP
{
    //our about/help/support editor window
    public class AboutEditor : EditorWindow
    {
        [MenuItem("Window/Tower Defense Multiplayer/About")]
        static void Init()
        {
            AboutEditor aboutWindow = (AboutEditor)EditorWindow.GetWindowWithRect
                    (typeof(AboutEditor), new Rect(0, 0, 300, 320), false, "About");
            aboutWindow.Show();
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(70);
            GUILayout.Label("Tower Defense Multiplayer", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(70);
            GUILayout.Label("by FLOBUK");
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.Label("Info", EditorStyles.boldLabel);
            if (GUILayout.Button("Homepage"))
            {
                Help.BrowseURL("https://flobuk.com");
            }
            GUILayout.Space(5);

            GUILayout.Label("Support", EditorStyles.boldLabel);
            if (GUILayout.Button("Online Documentation"))
            {
                Help.BrowseURL("https://flobuk.gitlab.io/assets/docs/towermp/");
            }
            if (GUILayout.Button("Scripting Reference"))
            {
                Help.BrowseURL("https://flobuk.gitlab.io/assets/docs/towermp/api/");
            }
            if (GUILayout.Button("Support Forum"))
            {
                Help.BrowseURL("https://forum.unity3d.com/threads/1450453/");
            }
            GUILayout.Space(5);

            GUILayout.Label("Support me!", EditorStyles.boldLabel);
            if (GUILayout.Button("Review Asset"))
            {
                Help.BrowseURL("https://assetstore.unity.com/packages/slug/258417?aid=1011lGiF&pubref=editor_towermp");
            }
            if (GUILayout.Button("Donation & GitHub Access"))
            {
                Help.BrowseURL("https://flobuk.com/#github");
            }
        }
    }
}