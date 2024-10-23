/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;

namespace TowerDefenseMP
{
    /// <summary>
	/// Component attached to the UI window when trying to buy a tower.
	/// </summary>
    public class UIBuyTooltip : MonoBehaviour
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


        /// <summary>
		/// Initializes this script with the tower passed in.
		/// </summary>
        public void Initialize(ClassTower selectedTower)
        {
            gameObject.SetActive(true);

            header.text = selectedTower.name;
            damage.text = selectedTower.data.damage.ToString();
            range.text = selectedTower.data.range.ToString();
            delay.text = selectedTower.data.delay.ToString();
        }
    }
}