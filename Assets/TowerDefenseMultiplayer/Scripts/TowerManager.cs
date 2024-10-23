/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace TowerDefenseMP
{
    /// <summary>
    /// Handling all networked tower placement, upgrading and selling for all clients.
    /// This is a server-authoritative component so each action has a ServerRpc and ClientRpc method.
    /// </summary>
	public class TowerManager : NetworkBehaviour
    {
        //reference to this script instance
        private static TowerManager instance;

        /// <summary>
		/// Mask for all Tower game objects, if necessary for e.g. raycasting against them.
        /// </summary>
        public static int layerMask;

        /// <summary>
        /// Event fired when upgrading or selling a tower so the UI can act accordingly.
        /// </summary>
        public static event Action<ulong> changeTowerEvent;

        /// <summary>
        /// FX instantiated at the position of the floating tower and moved with it.
        /// </summary>
        public GameObject placeFX;

        /// <summary>
        /// FX instantiated at the position of a new tower.
        /// </summary>
        public GameObject buildFX;

        /// <summary>
        /// FX instantiated at the position of an upgraded tower.
        /// </summary>
        public GameObject upgradeFX;


        //initialize variables
        void Awake()
        {
            instance = this;
            layerMask = LayerMask.GetMask("Tower");
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static TowerManager GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// Request to place a new tower received from a client.
        /// Undergoes several checks that have to pass on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddTowerServerRpc(short towerIndex, Vector3 position, ServerRpcParams serverRpcParams = default)
        {
            //to be sure only the server executes this
            if (!IsServer)
                return;

            ulong playerIndex = serverRpcParams.Receive.SenderClientId;
            PlayerStruct playerStruct = NetworkManagerCustom.GetInstance().playerData[(int)playerIndex];
            short playerColor = (short)playerStruct.thisColor;

            ClassDataScriptableObject classData = GameData.GetInstance().classes[playerStruct.thisClass];
            GameObject towerRef = classData.towers[towerIndex].prefab;
            TowerDataScriptableObject towerData = classData.towers[towerIndex].data;

            if (towerData.price > GameManager.GetInstance().currency[playerColor])
            {
                Debug.Log("You do not have enough currency to buy this tower.");
                return;
            }

            GridPlacement grid = GridManager.GetInstance().GetGridAtPosition(position);
            if (grid == null)
            {
                Debug.Log("Position received for Tower placement was not on a grid.");
                return;
            }

            Vector3 localPosition = grid.transform.InverseTransformPoint(position);
            GridTile tile = grid.GetTileAtPosition(localPosition);
            if (tile == null || tile.IsOccupied())
            {
                Debug.Log("Can't place tower here. Tile null or occupied");
                return;
            }

            //all checks passed, substract currency
            GameManager.GetInstance().currency[playerColor] -= towerData.price; 

            //instantiate and spawn tower for player
            GameObject towerObj = Instantiate(towerRef, tile.transform.position, grid.transform.rotation);
            towerObj.GetComponent<NetworkObject>().SpawnWithOwnership(playerIndex);

            short gridIndex = (short)GridManager.GetInstance().grids.IndexOf(grid);
            AddTowerClientRpc(gridIndex, localPosition, playerColor);
        }


        /// <summary>
        /// Request to set a tile to occupied after spawning a tower, sent from the server to all clients.
        /// </summary>
        [ClientRpc]
        protected void AddTowerClientRpc(short gridIndex, Vector3 position, short playerColor)
        {
            GridTile tile = GridManager.GetInstance().grids[gridIndex].GetTileAtPosition(position);
            tile.SetOccupied(playerColor);

            PoolManager.Spawn(buildFX, tile.transform.position, Quaternion.identity);
        }


        /// <summary>
        /// Request to upgrade an existing tower received from a client.
        /// Undergoes several checks that have to pass on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpgradeTowerServerRpc(ulong towerNetworkId, ServerRpcParams serverRpcParams = default)
        {
            //to be sure only the server executes this
            if (!IsServer)
                return;

            ulong playerIndex = serverRpcParams.Receive.SenderClientId;
            PlayerStruct playerStruct = NetworkManagerCustom.GetInstance().playerData[(int)playerIndex];
            short playerColor = (short)playerStruct.thisColor;
            NetworkObject networkObject = GetNetworkObject(towerNetworkId);

            if (networkObject == null)
            {
                Debug.Log("This tower does not exist anymore, eventually it was sold.");
                return;
            }

            GameObject towerObj = networkObject.gameObject;
            Tower tower = towerObj.GetComponent<Tower>();
            GameObject towerRef = tower.data.nextPrefab;

            if (towerRef == null)
            {
                Debug.Log("There is no upgrade for this tower.");
                return;
            }

            if (tower.data.nextData.price > GameManager.GetInstance().currency[playerColor])
            {
                Debug.Log("You do not have enough currency for upgrading.");
                return;
            }

            //all checks passed, substract currency
            GameManager.GetInstance().currency[playerColor] -= tower.data.nextData.price;

            //instantiate and spawn tower for player
            GameObject newTowerObj = Instantiate(towerRef, tower.transform.position, tower.transform.rotation);
            newTowerObj.GetComponent<NetworkObject>().SpawnWithOwnership(networkObject.OwnerClientId);

            networkObject.Despawn();

            //upgrade tower
            UpgradeTowerClientRpc(towerNetworkId, newTowerObj.GetComponent<NetworkObject>().NetworkObjectId);
        }


        /// <summary>
        /// Request to invoke the changeTowerEvent after upgrading a tower, sent from the server to all clients.
        /// </summary>
        [ClientRpc]
        protected void UpgradeTowerClientRpc(ulong oldNetworkId, ulong newNetworkId)
        {
            GameObject towerObj = GetNetworkObject(newNetworkId).gameObject;
            Tower tower = towerObj.GetComponent<Tower>();

            changeTowerEvent?.Invoke(oldNetworkId);
            PoolManager.Spawn(upgradeFX, tower.transform.position, Quaternion.identity);
        }


        /// <summary>
        /// Request to sell an existing tower received from a client.
        /// Undergoes several checks that have to pass on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SellTowerServerRpc(ulong towerNetworkId, ServerRpcParams serverRpcParams = default)
        {
            //to be sure only the server executes this
            if (!IsServer)
                return;

            ulong playerIndex = serverRpcParams.Receive.SenderClientId;
            PlayerStruct playerStruct = NetworkManagerCustom.GetInstance().playerData[(int)playerIndex];
            short playerColor = (short)playerStruct.thisColor;

            ClassDataScriptableObject classData = GameData.GetInstance().classes[playerStruct.thisClass];
            NetworkObject networkObject = GetNetworkObject(towerNetworkId);

            if (playerIndex != networkObject.OwnerClientId)
            {
                Debug.Log("You cannot sell a tower you do not own.");
                return;
            }

            GameObject towerObj = networkObject.gameObject;
            Tower tower = towerObj.GetComponent<Tower>();
            Vector3 towerPos = towerObj.transform.position;

            //all checks passed, add back currency
            GameManager.GetInstance().currency[playerColor] += GetSellAmount(tower, classData);
            networkObject.Despawn();

            //sell tower
            SellTowerClientRpc(towerNetworkId, towerPos);
        }


        /// <summary>
        /// Request to free a grid and invoke the changeTowerEvent after selling a tower, sent from the server to all clients.
        /// </summary>
        [ClientRpc]
        protected void SellTowerClientRpc(ulong networkId, Vector3 position)
        {
            GridPlacement grid = GridManager.GetInstance().GetGridAtPosition(position);
            position = grid.transform.InverseTransformPoint(position);
            GridTile tile = grid.GetTileAtPosition(position);
            tile.ClearOccupied();

            changeTowerEvent?.Invoke(networkId);
        }


        /// <summary>
        /// Calculation of final tower sell price with all previous upgrade costs included.
        /// </summary>
        public int GetSellAmount(Tower tower, ClassDataScriptableObject classData = null)
        {
            //if we are a client we can only access our own class
            if (!classData)
                classData = GameData.GetInstance().GetMyClass();

            int towerIndex = -1;
            int upgradeIndex = 0;
            for (int i = 0; i < classData.towers.Count; i++)
            {
                TowerDataScriptableObject data = classData.towers[i].data;
                upgradeIndex = 0;

                if (tower.data == data)
                {
                    towerIndex = i;
                    break;
                }

                while (data.nextPrefab != null)
                {
                    data = data.nextData;
                    upgradeIndex++;

                    if (tower.data == data)
                    {
                        towerIndex = i;
                        break;
                    }
                }

                if (towerIndex >= 0)
                {
                    break;
                }
            }

            int amount = 0;
            TowerDataScriptableObject towerData = classData.towers[towerIndex].data;
            for (int i = 0; i <= upgradeIndex; i++)
            {
                amount += towerData.price;
                towerData = towerData.nextData;
            }

            return amount;
        }
    }
}
