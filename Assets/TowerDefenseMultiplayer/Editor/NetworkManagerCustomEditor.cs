/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using Unity.Netcode.Editor;
using UnityEditor;

namespace TowerDefenseMP
{
    /// <summary>
    /// Extension of default NetworkManagerEditor with custom variables of NetworkManagerCustom.
    /// </summary>
    [CustomEditor(typeof(NetworkManagerCustom), true)]
    public class NetworkManagerCustomEditor : NetworkManagerEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            base.OnInspectorGUI();
        }
    }
}
