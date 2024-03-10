using GameNetcodeStuff;
using lethalCompanyRevive.Helpers;
using lethalCompanyRevive.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace lethalCompanyRevive.Managers
{
    public class ReviveStore : NetworkBehaviour
    {
        public static ReviveStore Instance { get; private set; }
        private const int ReviveCost = 100;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }

		private bool CanAffordRevive(PlayerControllerB player)
		{
			Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
			return terminal.groupCredits >= ReviveCost;
		}

		private void DeductCredits(PlayerControllerB player)
		{
			Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
			terminal.groupCredits -= ReviveCost;
			SyncCreditsServerRpc(terminal.groupCredits);
		}

		[ClientRpc]
		private void RevivePlayer(PlayerControllerB l_player)
		{
			Debug.Log("Revive Player");
			int playerIndex = GetPlayerIndex(l_player.playerUsername);
			PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
			Debug.Log($"Player Index: {playerIndex}");

			Debug.Log("Reviving players A");
			player.ResetPlayerBloodObjects(player.isPlayerDead);
			player.isClimbingLadder = false;
			player.ResetZAndXRotation();
			player.thisController.enabled = true;
			player.health = 100;
			player.disableLookInput = false;
			Debug.Log("Reviving players B");
			if (player.isPlayerDead)
			{
				player.isPlayerDead = false;
				player.isPlayerControlled = true;
				player.isInElevator = true;
				player.isInHangarShipRoom = true;
				player.isInsideFactory = false;
				player.wasInElevatorLastFrame = false;
				StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
				player.TeleportPlayer(GetPlayerSpawnPosition(playerIndex, false));
				player.setPositionOfDeadPlayer = false;
				player.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[playerIndex], true, true);
				player.helmetLight.enabled = false;

				Debug.Log("Reviving players C");
				player.Crouch(crouch: false);
				player.criticallyInjured = false;
				if (player.playerBodyAnimator != null)
				{
					player.playerBodyAnimator.SetBool("Limp", false);
				}
				player.bleedingHeavily = false;
				player.activatingItem = false;
				player.twoHanded = false;
				player.inSpecialInteractAnimation = false;
				player.disableSyncInAnimation = false;
				player.inAnimationWithEnemy = null;
				player.holdingWalkieTalkie = false;
				player.speakingToWalkieTalkie = false;

				Debug.Log("Reviving players D");
				player.isSinking = false;
				player.isUnderwater = false;
				player.sinkingValue = 0f;
				player.statusEffectAudio.Stop();
				player.DisableJetpackControlsLocally();
				player.health = 100;

				Debug.Log("Reviving players E");
				player.mapRadarDotAnimator.SetBool("dead", false);

				HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
				player.hasBegunSpectating = false;
				HUDManager.Instance.RemoveSpectateUI();
				HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
				player.hinderedMultiplier = 1f;
				player.isMovementHindered = 0;
				player.sourcesCausingSinking = 0;

				Debug.Log("Reviving players E2");
				player.reverbPreset = StartOfRound.Instance.shipReverb;
			}

			Debug.Log("Reviving players F");
			SoundManager.Instance.earsRingingTimer = 0f;
			player.voiceMuffledByEnemy = false;
			SoundManager.Instance.playerVoicePitchTargets[playerIndex] = 1f;
			SoundManager.Instance.SetPlayerPitch(1f, playerIndex);
			if (player.currentVoiceChatIngameSettings == null)
			{
				StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
			}
			if (player.currentVoiceChatIngameSettings != null)
			{
				if (player.currentVoiceChatIngameSettings.voiceAudio == null)
				{
					player.currentVoiceChatIngameSettings.InitializeComponents();
				}
				if (player.currentVoiceChatIngameSettings.voiceAudio == null)
				{
					return;
				}
				player.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
			}

			Debug.Log("Reviving players G");
			PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
			playerControllerB.bleedingHeavily = false;
			playerControllerB.criticallyInjured = false;
			playerControllerB.playerBodyAnimator.SetBool("Limp", false);
			playerControllerB.health = 100;
			HUDManager.Instance.UpdateHealthUI(100, false);
			playerControllerB.spectatedPlayerScript = null;
			HUDManager.Instance.audioListenerLowPass.enabled = false;

			Debug.Log("Reviving players H");
			StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, playerControllerB);
			RagdollGrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<RagdollGrabbableObject>();
			for (int j = 0; j < array.Length; j++)
			{
				if (!array[j].isHeld)
				{
					if (base.IsServer)
					{
						if (array[j].NetworkObject.IsSpawned)
						{
							array[j].NetworkObject.Despawn();
						}
						else
						{
							UnityEngine.Object.Destroy(array[j].gameObject);
						}
					}
				}
				else if (array[j].isHeld && array[j].playerHeldBy != null)
				{
					array[j].playerHeldBy.DropAllHeldItems();
				}
			}
			DeadBodyInfo[] array2 = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
			for (int k = 0; k < array2.Length; k++)
			{
				UnityEngine.Object.Destroy(array2[k].gameObject);
			}
			StartOfRound.Instance.livingPlayers++;
			StartOfRound.Instance.allPlayersDead = false;
			StartOfRound.Instance.UpdatePlayerVoiceEffects();
			StartOfRound.Instance.PlayerHasRevivedServerRpc();
		}

		private int GetPlayerIndex(string playerName)
		{
			PlayerControllerB[] allPlayers = StartOfRound.Instance.allPlayerScripts;
			Debug.Log($"All Players: {allPlayers}");

			for (int i = 0; i < allPlayers.Length; i++)
			{
				Debug.Log("Player: " + allPlayers[i]);
				if (allPlayers[i] != null && allPlayers[i].playerUsername == playerName)
				{
					return i;
				}
			}

			return -1;
		}

		[ClientRpc]
		private Vector3 GetPlayerSpawnPosition(int playerNum, bool simpleTeleport = false)
		{
			Debug.Log("Get Player Spawn Position");
			if (simpleTeleport)
			{
				return StartOfRound.Instance.playerSpawnPositions[0].position;
			}
			Debug.DrawRay(StartOfRound.Instance.playerSpawnPositions[playerNum].position, Vector3.up, Color.red, 15f);
			if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[playerNum].position, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
			{
				return StartOfRound.Instance.playerSpawnPositions[playerNum].position;
			}
			if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[playerNum].position + Vector3.up, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
			{
				return StartOfRound.Instance.playerSpawnPositions[playerNum].position + Vector3.up * 0.5f;
			}
			for (int i = 0; i < StartOfRound.Instance.playerSpawnPositions.Length; i++)
			{
				if (i != playerNum)
				{
					Debug.DrawRay(StartOfRound.Instance.playerSpawnPositions[i].position, Vector3.up, Color.green, 15f);
					if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[i].position, 0.12f, -67108865, QueryTriggerInteraction.Ignore))
					{
						return StartOfRound.Instance.playerSpawnPositions[i].position;
					}
					if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[i].position + Vector3.up, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
					{
						return StartOfRound.Instance.playerSpawnPositions[i].position + Vector3.up * 0.5f;
					}
				}
			}
			System.Random random = new System.Random(65);
			float y = StartOfRound.Instance.playerSpawnPositions[0].position.y;
			for (int j = 0; j < 15; j++)
			{
				Vector3 vector = new Vector3(random.Next((int)StartOfRound.Instance.shipInnerRoomBounds.bounds.min.x, (int)StartOfRound.Instance.shipInnerRoomBounds.bounds.max.x), y, random.Next((int)StartOfRound.Instance.shipInnerRoomBounds.bounds.min.z, (int)StartOfRound.Instance.shipInnerRoomBounds.bounds.max.z));
				vector = StartOfRound.Instance.shipInnerRoomBounds.transform.InverseTransformPoint(vector);
				Debug.DrawRay(vector, Vector3.up, Color.yellow, 15f);
				if (!Physics.CheckSphere(vector, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
				{
					return StartOfRound.Instance.playerSpawnPositions[j].position;
				}
			}
			return StartOfRound.Instance.playerSpawnPositions[0].position + Vector3.up * 0.5f;
		}

		[ServerRpc(RequireOwnership = false)]
		public void SyncCreditsServerRpc(int newCredits)
		{
			SyncCreditsClientRpc(newCredits);
		}

		[ClientRpc]
		private void SyncCreditsClientRpc(int newCredits)
		{
			Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
			terminal.groupCredits = newCredits;
		}

		[ServerRpc(RequireOwnership = false)]
		private void RevivePlayerServerRpc(PlayerControllerB player)
		{
			RevivePlayer(player);
		}

		[ServerRpc(RequireOwnership = false)]
		public void RequestReviveServerRpc(ulong playerId)
		{
			PlayerControllerB playerToRevive = Helper.GetPlayer(playerId.ToString());
			if (playerToRevive != null && playerToRevive.isPlayerDead)
			{
				if (CanAffordRevive(playerToRevive))
				{
					DeductCredits(playerToRevive);
					RevivePlayerServerRpc(playerToRevive);
				}
			}
		}
	}
}
