/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

namespace TowerDefenseMP
{
    /// <summary>
    /// List of all keys saved on the user's device, be it for settings or selections.
    /// </summary>
    public class PrefsKeys
    {
        /// <summary>
		/// PlayerPrefs key for player name: UserXXXX
		/// </summary>
        public const string playerName = "TD_playerName";

        /// <summary>
        /// PlayerPrefs key for selected network mode: 0, 1 or 2
        /// </summary>
        public const string networkMode = "TD_networkMode";

        /// <summary>
        /// PlayerPrefs key for background music state: true/false
        /// </summary>
        public const string playMusic = "TD_playMusic";

        /// <summary>
        /// PlayerPrefs key for global audio volume: 0-1 range
        /// </summary>
        public const string appVolume = "TD_appVolume";
    }
}
