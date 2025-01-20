﻿using GameNetcodeStuff;
using lethalCompanyRevive.Helpers;
using lethalCompanyRevive.Misc;
using Unity.Netcode;
using UnityEngine;
using System;

namespace lethalCompanyRevive.Managers
{
    public class ReviveStore : NetworkBehaviour
    {
        public static ReviveStore Instance { get; private set; }

        int dailyRevivesUsed;
        DateTime lastReviveDay = DateTime.Now.Date;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReviveServerRpc(ulong playerId)
        {
            if (!CheckDayReset()) return;
            if (!Plugin.cfg.EnableRevive.Value) return;
            if (!CanReviveNow()) return;
            var p = Helper.GetPlayer(playerId.ToString());
            if (p == null || !p.isPlayerDead) return;
            int cost = ComputeReviveCost(dailyRevivesUsed);
            if (!CanAfford(cost)) return;
            DeductCredits(cost);
            ReviveSinglePlayer(p);
            IncrementDailyRevives();
        }

        bool CheckDayReset()
        {
            var today = DateTime.Now.Date;
            if (today != lastReviveDay)
            {
                lastReviveDay = today;
                dailyRevivesUsed = 0;
            }
            return true;
        }

        public bool CanReviveNow()
        {
            if (!Plugin.cfg.EnableRevive.Value) return false;
            if (Plugin.cfg.EnableMaxRevivesPerDay.Value)
            {
                if (dailyRevivesUsed >= Plugin.cfg.MaxRevivesPerDay.Value) return false;
            }
            return true;
        }

        void IncrementDailyRevives()
        {
            if (Plugin.cfg.EnableMaxRevivesPerDay.Value)
                dailyRevivesUsed++;
        }

        bool CanAfford(int cost)
        {
            Terminal t = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            return (t != null && t.groupCredits >= cost);
        }

        void DeductCredits(int cost)
        {
            Terminal t = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            if (t == null) return;
            t.groupCredits -= cost;
            SyncCreditsServerRpc(t.groupCredits);
        }

        [ServerRpc(RequireOwnership = false)]
        void SyncCreditsServerRpc(int newCredits)
        {
            SyncCreditsClientRpc(newCredits);
        }

        [ClientRpc]
        void SyncCreditsClientRpc(int newCredits)
        {
            Terminal t = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            if (t != null) t.groupCredits = newCredits;
        }

        int ComputeReviveCost(int usageIndex)
        {
            string algo = Plugin.cfg.ReviveCostAlgorithm.Value.ToLower();
            int baseCost = Plugin.cfg.BaseReviveCost.Value;
            switch (algo)
            {
                case "flat":
                    return baseCost;
                case "exponential":
                    return (int)(baseCost * Mathf.Pow(2, usageIndex));
                case "quota":
                default:
                    if (TimeOfDay.Instance == null || StartOfRound.Instance == null)
                        return 100;
                    int totalPlayers = StartOfRound.Instance.connectedPlayersAmount + 1;
                    float quota = TimeOfDay.Instance.profitQuota;
                    int cost = (int)(quota / totalPlayers);
                    if (cost < 1) cost = 1;
                    return cost;
            }
        }

        void ReviveSinglePlayer(PlayerControllerB p)
        {
            Vector3 spawn = GetPlayerSpawnPosition(GetPlayerIndex(p.playerUsername), false);
            var nbRef = new NetworkBehaviourReference(p);
            RevivePlayer(spawn, nbRef);
        }

        void RevivePlayer(Vector3 position, NetworkBehaviourReference netRef)
        {
            RevivePlayerClientRpc(position, netRef);
            SyncLivingPlayersServerRpc();
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
                    if (IsServer && rags[x].NetworkObject.IsSpawned)
                        rags[x].NetworkObject.Despawn();
                    else
                        UnityEngine.Object.Destroy(rags[x].gameObject);
                }
                else if (rags[x].isHeld && rags[x].playerHeldBy != null)
                {
                    rags[x].playerHeldBy.DropAllHeldItems();
                }
            }
            DeadBodyInfo[] bodies = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
            for (int y = 0; y < bodies.Length; y++)
                UnityEngine.Object.Destroy(bodies[y].gameObject);
            if (IsServer)
            {
                StartOfRound.Instance.livingPlayers++;
                StartOfRound.Instance.allPlayersDead = false;
            }
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
        }

        [ServerRpc(RequireOwnership = false)]
        void SyncLivingPlayersServerRpc()
        {
            var so = StartOfRound.Instance;
            int newCount = 0;
            foreach (var pc in so.allPlayerScripts)
            {
                if (pc != null && pc.isPlayerControlled && !pc.isPlayerDead)
                    newCount++;
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
            var so = StartOfRound.Instance;
            if (so == null) return -1;
            var ps = so.allPlayerScripts;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i] != null && ps[i].playerUsername == username)
                    return i;
            }
            return -1;
        }

        Vector3 GetPlayerSpawnPosition(int playerNum, bool simpleTeleport)
        {
            var so = StartOfRound.Instance;
            if (so == null || so.playerSpawnPositions == null)
                return Vector3.zero;
            if (simpleTeleport ||
                playerNum < 0 ||
                playerNum >= so.playerSpawnPositions.Length)
                return so.playerSpawnPositions[0].position;
            var spawns = so.playerSpawnPositions;
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
            System.Random random = new System.Random(65);
            float y = spawns[0].position.y;
            for (int attempt = 0; attempt < 15; attempt++)
            {
                Bounds b = so.shipInnerRoomBounds.bounds;
                int xMin = (int)b.min.x;
                int xMax = (int)b.max.x;
                int zMin = (int)b.min.z;
                int zMax = (int)b.max.z;
                float randX = random.Next(xMin, xMax);
                float randZ = random.Next(zMin, zMax);
                Vector3 candidate = new Vector3(randX, y, randZ);
                if (!Physics.CheckSphere(candidate, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
                    return candidate;
            }
            return spawns[0].position + Vector3.up * 0.5f;
        }
    }
}
