﻿using GameNetcodeStuff;
using lethalCompanyRevive.Helpers;
using lethalCompanyRevive.Misc;
using Unity.Netcode;
using UnityEngine;

namespace lethalCompanyRevive.Managers
{
    public class ReviveStore : NetworkBehaviour
    {
        public static ReviveStore Instance { get; private set; }
        int dailyRevivesUsed;

        public override void OnNetworkSpawn()
        {
            // If there's an old instance, despawn it so we only keep this new one.
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (Instance != null && Instance != this)
                {
                    var oldNet = Instance.GetComponent<NetworkObject>();
                    if (oldNet != null && oldNet.IsSpawned)
                        oldNet.Despawn();
                }
            }
            Instance = this;
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (Instance == this) Instance = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReviveServerRpc(ulong playerId)
        {
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

        public void ResetDailyRevives()
        {
            dailyRevivesUsed = 0;
        }

        public bool CanReviveNow()
        {
            if (!Plugin.cfg.EnableRevive.Value)
                return false;

            if (Plugin.cfg.EnableMaxRevivesPerDay.Value &&
                dailyRevivesUsed >= Plugin.cfg.MaxRevivesPerDay.Value)
                return false;

            return true;
        }

        void IncrementDailyRevives()
        {
            if (Plugin.cfg.EnableMaxRevivesPerDay.Value)
                dailyRevivesUsed++;
        }

        bool CanAfford(int cost)
        {
            Terminal t = GameObject.Find("TerminalScript")?.GetComponent<Terminal>();
            return (t != null && t.groupCredits >= cost);
        }

        void DeductCredits(int cost)
        {
            Terminal t = GameObject.Find("TerminalScript")?.GetComponent<Terminal>();
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
            Terminal t = GameObject.Find("TerminalScript")?.GetComponent<Terminal>();
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
            PlayerControllerB plr = nb.GetComponent<PlayerControllerB>();
            if (plr == null) return;

            int i = GetPlayerIndex(plr.playerUsername);

            plr.ResetPlayerBloodObjects(plr.isPlayerDead || plr.isPlayerControlled);
            plr.isClimbingLadder = false;
            plr.clampLooking = false;
            plr.inVehicleAnimation = false;
            plr.disableMoveInput = false;
            plr.disableLookInput = false;
            plr.disableInteract = false;
            plr.ResetZAndXRotation();
            plr.thisController.enabled = true;
            plr.health = 100;
            plr.hasBeenCriticallyInjured = false;
            plr.disableSyncInAnimation = false;

            if (plr.isPlayerDead)
            {
                plr.isPlayerDead = false;
                plr.isPlayerControlled = true;
                plr.isInElevator = true;
                plr.isInHangarShipRoom = true;
                plr.isInsideFactory = false;
                plr.parentedToElevatorLastFrame = false;
                plr.overrideGameOverSpectatePivot = null;
                if (plr.IsOwner)
                    StartOfRound.Instance.SetPlayerObjectExtrapolate(false);

                plr.TeleportPlayer(spawnPosition);
                plr.setPositionOfDeadPlayer = false;
                plr.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[i], true, true);
                plr.helmetLight.enabled = false;
                plr.Crouch(false);
                plr.criticallyInjured = false;
                if (plr.playerBodyAnimator != null) plr.playerBodyAnimator.SetBool("Limp", false);
                plr.bleedingHeavily = false;
                plr.activatingItem = false;
                plr.twoHanded = false;
                plr.inShockingMinigame = false;
                plr.inSpecialInteractAnimation = false;
                plr.freeRotationInInteractAnimation = false;
                plr.inAnimationWithEnemy = null;
                plr.holdingWalkieTalkie = false;
                plr.speakingToWalkieTalkie = false;
                plr.isSinking = false;
                plr.isUnderwater = false;
                plr.sinkingValue = 0f;
                plr.statusEffectAudio.Stop();
                plr.DisableJetpackControlsLocally();
                plr.health = 100;
                plr.mapRadarDotAnimator.SetBool("dead", false);
                plr.externalForceAutoFade = Vector3.zero;

                if (plr.IsOwner)
                {
                    HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
                    plr.hasBegunSpectating = false;
                    HUDManager.Instance.RemoveSpectateUI();
                    HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                    plr.hinderedMultiplier = 1f;
                    plr.isMovementHindered = 0;
                    plr.sourcesCausingSinking = 0;
                    plr.reverbPreset = StartOfRound.Instance.shipReverb;
                }
            }

            SoundManager.Instance.earsRingingTimer = 0f;
            plr.voiceMuffledByEnemy = false;
            SoundManager.Instance.playerVoicePitchTargets[i] = 1f;
            SoundManager.Instance.SetPlayerPitch(1f, i);

            if (plr.currentVoiceChatIngameSettings == null)
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();

            if (plr.currentVoiceChatIngameSettings != null)
            {
                if (plr.currentVoiceChatIngameSettings.voiceAudio == null)
                    plr.currentVoiceChatIngameSettings.InitializeComponents();
                if (plr.currentVoiceChatIngameSettings.voiceAudio != null)
                    plr.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
            }

            PlayerControllerB localP = GameNetworkManager.Instance.localPlayerController;
            if (localP == plr)
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
