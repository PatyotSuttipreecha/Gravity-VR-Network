/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;

namespace TowerDefenseMP
{
    /// <summary>
	/// Component attached to the UI window when trying to upgrade a tower.
	/// </summary>
    public class UIUpgradeTooltip : MonoBehaviour
    {
        /// <summary>
		/// Text reference for the tower name.
		/// </summary>
        public Text header;

        /// <summary>
		/// Text reference for the tower damage.
		/// </summary>
        public Text damage;

        /// <summary>
		/// Text reference for the tower range.
		/// </summary>
        public Text range;

        /// <summary>
		/// Text reference for the tower shot delay.
		/// </summary>
        public Text delay;

        private string upgradeColor = "#00FF00";


        /// <summary>
		/// Initializes this script with the tower passed in.
		/// </summary>
        public void Initialize(Tower selectedTower)
        {
            gameObject.SetActive(true);

            header.text = GameData.GetInstance().GetClassTowerFromData(selectedTower.data).name;
            damage.text = selectedTower.data.damage + (selectedTower.data.nextData != null ? $"<color={upgradeColor}> + " + (selectedTower.data.nextData.damage - selectedTower.data.damage) + "</color>" : string.Empty);
            range.text = selectedTower.data.range + (selectedTower.data.nextData != null ? $"<color={upgradeColor}> + " + (selectedTower.data.nextData.range - selectedTower.data.range) + "</color>" : string.Empty);
            delay.text = selectedTower.data.delay + (selectedTower.data.nextData != null ? $"<color={upgradeColor}> + " + (selectedTower.data.nextData.delay - selectedTower.data.delay) + "</color>" : string.Empty);
        }
    }
}