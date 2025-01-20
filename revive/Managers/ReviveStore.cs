﻿using GameNetcodeStuff;
using lethalCompanyRevive.Helpers;
using lethalCompanyRevive.Misc;
using Unity.Netcode;
using UnityEngine;

// This file has been updated to use a dynamic revive cost:
// cost = (TimeOfDay.Instance.profitQuota / (StartOfRound.Instance.connectedPlayersAmount + 1))

namespace lethalCompanyRevive.Managers
{
    public class ReviveStore : NetworkBehaviour
    {
        public static ReviveStore Instance { get; private set; }

        // Instead of a fixed 100-credit cost, we compute it dynamically.
        // Example formula: profitQuota / totalPlayers
        int GetReviveCost()
        {
            // If TimeOfDay or StartOfRound is unexpectedly null, default to 100 just to avoid errors.
            if (TimeOfDay.Instance == null || StartOfRound.Instance == null)
                return 100;

            int totalPlayers = StartOfRound.Instance.connectedPlayersAmount + 1;
            float quota = TimeOfDay.Instance.profitQuota;
            // Convert to int. If quota < totalPlayers, cost can be zero with integer division.
            // We'll ensure at least 1 credit cost if needed:
            int cost = (int)(quota / totalPlayers);
            return cost < 1 ? 1 : cost;
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            base.OnNetworkSpawn();
        }

        bool CanAffordRevive()
        {
            Terminal t = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            return t.groupCredits >= GetReviveCost();
        }

        void DeductCredits()
        {
            Terminal t = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            t.groupCredits -= GetReviveCost();
            SyncCreditsServerRpc(t.groupCredits);
        }

        [ClientRpc]
        void RevivePlayerClientRpc(Vector3 spawnPosition, NetworkBehaviourReference netRef)
        {
            if (!netRef.TryGet(out NetworkBehaviour nb)) return;
            PlayerControllerB p = nb.GetComponent<PlayerControllerB>();
            if (p == null) return;

            int i = GetPlayerIndex(p.playerUsername);
            p.ResetPlayerBloodObjects(p.isPlayerDead || p.isPlayerControlled);
            p.isClimbingLadder = false;
            p.clampLooking = false;
            p.inVehicleAnimation = false;
            p.disableMoveInput = false;
            p.disableLookInput = false;
            p.disableInteract = false;
            p.ResetZAndXRotation();
            p.thisController.enabled = true;
            p.health = 100;
            p.hasBeenCriticallyInjured = false;
            p.disableSyncInAnimation = false;

            if (p.isPlayerDead)
            {
                p.isPlayerDead = false;
                p.isPlayerControlled = true;
                p.isInElevator = true;
                p.isInHangarShipRoom = true;
                p.isInsideFactory = false;
                p.parentedToElevatorLastFrame = false;
                p.overrideGameOverSpectatePivot = null;

                if (p.IsOwner)
                    StartOfRound.Instance.SetPlayerObjectExtrapolate(false);

                p.TeleportPlayer(spawnPosition);
                p.setPositionOfDeadPlayer = false;
                p.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[i], true, true);
                p.helmetLight.enabled = false;
                p.Crouch(false);
                p.criticallyInjured = false;
                if (p.playerBodyAnimator != null) p.playerBodyAnimator.SetBool("Limp", false);
                p.bleedingHeavily = false;
                p.activatingItem = false;
                p.twoHanded = false;
                p.inShockingMinigame = false;
                p.inSpecialInteractAnimation = false;
                p.freeRotationInInteractAnimation = false;
                p.inAnimationWithEnemy = null;
                p.holdingWalkieTalkie = false;
                p.speakingToWalkieTalkie = false;
                p.isSinking = false;
                p.isUnderwater = false;
                p.sinkingValue = 0f;
                p.statusEffectAudio.Stop();
                p.DisableJetpackControlsLocally();
                p.health = 100;
                p.mapRadarDotAnimator.SetBool("dead", false);
                p.externalForceAutoFade = Vector3.zero;

                if (p.IsOwner)
                {
                    HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
                    p.hasBegunSpectating = false;
                    HUDManager.Instance.RemoveSpectateUI();
                    HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                    p.hinderedMultiplier = 1f;
                    p.isMovementHindered = 0;
                    p.sourcesCausingSinking = 0;
                    p.reverbPreset = StartOfRound.Instance.shipReverb;
                }
            }

            SoundManager.Instance.earsRingingTimer = 0f;
            p.voiceMuffledByEnemy = false;
            SoundManager.Instance.playerVoicePitchTargets[i] = 1f;
            SoundManager.Instance.SetPlayerPitch(1f, i);

            if (p.currentVoiceChatIngameSettings == null)
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();

            if (p.currentVoiceChatIngameSettings != null)
            {
                if (p.currentVoiceChatIngameSettings.voiceAudio == null)
                    p.currentVoiceChatIngameSettings.InitializeComponents();

                if (p.currentVoiceChatIngameSettings.voiceAudio != null)
                    p.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
            }

            PlayerControllerB localP = GameNetworkManager.Instance.localPlayerController;
            if (localP == p)
            {
                localP.bleedingHeavily = false;
                localP.criticallyInjured = false;
                if (localP.playerBodyAnimator != null) localP.playerBodyAnimator.SetBool("Limp", false);
                localP.health = 100;
                HUDManager.Instance.UpdateHealthUI(100, false);
                localP.spectatedPlayerScript = null;
                HUDManager.Instance.audioListenerLowPass.enabled = false;
                HUDManager.Instance.RemoveSpectateUI();
                HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, localP);
            }

            RagdollGrabbableObject[] rags = UnityEngine.Object.FindObjectsOfType<RagdollGrabbableObject>();
            for (int x = 0; x < rags.Length; x++)
            {
                if (!rags[x].isHeld)
                {
                    if (IsServer)
                    {
                        if (rags[x].NetworkObject.IsSpawned) rags[x].NetworkObject.Despawn();
                        else UnityEngine.Object.Destroy(rags[x].gameObject);
                    }
                }
                else if (rags[x].isHeld && rags[x].playerHeldBy != null)
                {
                    rags[x].playerHeldBy.DropAllHeldItems();
                }
            }
            DeadBodyInfo[] bodies = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
            for (int y = 0; y < bodies.Length; y++) UnityEngine.Object.Destroy(bodies[y].gameObject);

            if (IsServer)
            {
                StartOfRound.Instance.livingPlayers++;
                StartOfRound.Instance.allPlayersDead = false;
            }

            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReviveServerRpc(ulong playerId)
        {
            var p = Helper.GetPlayer(playerId.ToString());
            if (p == null) return;
            if (p.isPlayerDead && CanAffordRevive())
            {
                DeductCredits();
                int idx = GetPlayerIndex(p.playerUsername);
                Vector3 spawn = GetPlayerSpawnPosition(idx, false);
                var nbRef = new NetworkBehaviourReference(p);
                RevivePlayer(spawn, nbRef);
            }
        }

        void RevivePlayer(Vector3 position, NetworkBehaviourReference netRef)
        {
            RevivePlayerClientRpc(position, netRef);
            SyncLivingPlayersServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        void SyncLivingPlayersServerRpc()
        {
            int newCount = 0;
            var so = StartOfRound.Instance;
            foreach (var pc in so.allPlayerScripts)
            {
                if (pc != null && pc.isPlayerControlled && !pc.isPlayerDead) newCount++;
            }
            so.livingPlayers = newCount;
            so.allPlayersDead = (newCount == 0);
            SyncLivingPlayersClientRpc(newCount, so.allPlayersDead);
        }

        [ClientRpc]
        void SyncLivingPlayersClientRpc(int newLiving, bool allDead)
        {
            var so = StartOfRound.Instance;
            so.livingPlayers = newLiving;
            so.allPlayersDead = allDead;
        }

        int GetPlayerIndex(string username)
        {
            if (StartOfRound.Instance == null) return -1;
            PlayerControllerB[] ps = StartOfRound.Instance.allPlayerScripts;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i] != null && ps[i].playerUsername == username) return i;
            }
            return -1;
        }

        Vector3 GetPlayerSpawnPosition(int playerNum, bool simpleTeleport = false)
        {
            if (StartOfRound.Instance == null ||
                StartOfRound.Instance.playerSpawnPositions == null)
                return Vector3.zero;

            if (simpleTeleport ||
                playerNum < 0 ||
                playerNum >= StartOfRound.Instance.playerSpawnPositions.Length)
                return StartOfRound.Instance.playerSpawnPositions[0].position;

            var spawns = StartOfRound.Instance.playerSpawnPositions;
            if (spawns.Length == 0) return Vector3.zero;

            if (!Physics.CheckSphere(spawns[playerNum].position, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
                return spawns[playerNum].position;

            if (!Physics.CheckSphere(spawns[playerNum].position + Vector3.up, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
                return spawns[playerNum].position + Vector3.up * 0.5f;

            for (int i = 0; i < spawns.Length; i++)
            {
                if (i == playerNum) continue;
                if (!Physics.CheckSphere(spawns[i].position, 0.12f, -67108865, QueryTriggerInteraction.Ignore))
                    return spawns[i].position;
                if (!Physics.CheckSphere(spawns[i].position + Vector3.up, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
                    return spawns[i].position + Vector3.up * 0.5f;
            }
            System.Random random = new(65);
            float y = spawns[0].position.y;
            for (int attempt = 0; attempt < 15; attempt++)
            {
                Bounds b = StartOfRound.Instance.shipInnerRoomBounds.bounds;
                int xMin = (int)b.min.x; int xMax = (int)b.max.x;
                int zMin = (int)b.min.z; int zMax = (int)b.max.z;
                float randX = random.Next(xMin, xMax);
                float randZ = random.Next(zMin, zMax);
                Vector3 candidate = new(randX, y, randZ);
                if (!Physics.CheckSphere(candidate, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
                    return candidate;
            }
            return spawns[0].position + Vector3.up * 0.5f;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SyncCreditsServerRpc(int newCredits)
        {
            SyncCreditsClientRpc(newCredits);
        }

        [ClientRpc]
        void SyncCreditsClientRpc(int newCredits)
        {
            Terminal t = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            t.groupCredits = newCredits;
        }
    }
}
