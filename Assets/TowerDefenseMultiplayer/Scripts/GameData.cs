/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// MonoBehaviour acting as a container for all game related data and ScriptableObjects.
    /// This holds and provides access to all class and tower data in public methods.
    /// </summary>
    public class GameData : MonoBehaviour
    {
        //reference to this script instance
        private static GameData instance;

        /// <summary>
        /// List to all available classes. New classes need to be added to this in the inspector.
        /// </summary>
        public List<ClassDataScriptableObject> classes;


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static GameData GetInstance()
        {
            return instance;
        }


        //initialize data container references for towers
        void Awake()
        {
            if (instance != null)
                return;

            instance = this;

            for(int i = 0; i < classes.Count; i++)
            {
                for(int j = 0; j < classes[i].towers.Count; j++)
                {
                    TowerDataScriptableObject towerData = classes[i].towers[j].prefab.GetComponent<Tower>().data;
                    classes[i].towers[j].data = towerData;

                    while(towerData.nextPrefab != null)
                    {
                        TowerDataScriptableObject upgradeData = towerData.nextPrefab.GetComponent<Tower>().data;
                        towerData.nextData = upgradeData;
                        towerData = upgradeData;
                    }
                }
            }
        }


        /// <summary>
        /// Returns the player's selected class data container.
        /// </summary>
        public ClassDataScriptableObject GetMyClass()
        {
            return classes[GameManager.GetInstance().localPlayer.thisClass];
        }


        /// <summary>
        /// Returns the tower data in the player's class based on index passed in.
        /// </summary>
        public ClassTower GetMyClassTower(int index)
        {
            return GetMyClass().towers[index];
        }


        /// <summary>
        /// Return the list of names from all classes.
        /// </summary>
        public List<string> GetClassNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < classes.Count; i++)
            {
                names.Add(classes[i].className);
            }

            return names;
        }


        /// <summary>
        /// Returns the tower data in the player's class based on a tower data container.
        /// This method needs to loop through all towers in order to find it.
        /// </summary>
        public ClassTower GetClassTowerFromData(TowerDataScriptableObject data)
        {
            ClassDataScriptableObject classData = GetMyClass();
            for(int i = 0; i < classData.towers.Count; i++)
            {
                TowerDataScriptableObject towerData = classData.towers[i].data;
                if (towerData == data) return classData.towers[i];

                while(towerData.nextPrefab != null)
                {
                    towerData = towerData.nextData;
                    if (towerData == data) return classData.towers[i];
                }
            }

            return null;
        }
    }
}