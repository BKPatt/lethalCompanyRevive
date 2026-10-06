using System;
using GameNetcodeStuff;
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
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (Instance != null && Instance != this)
                {
                    var oldNet = Instance.GetComponent<NetworkObject>();
                    if (oldNet != null && oldNet.IsSpawned) oldNet.Despawn(true);
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

            var p = GetPlayerBySlot(playerId);
            if (p == null || !p.isPlayerDead) return;

            int cost = ComputeReviveCost(dailyRevivesUsed);
            if (!CanAfford(cost)) return;

            DeductCredits(cost);
            ReviveSinglePlayer(p);
            dailyRevivesUsed++;
        }

        public void ResetDailyRevives()
        {
            dailyRevivesUsed = 0;
        }

        public bool CanReviveNow()
        {
            if (!Plugin.cfg.EnableRevive.Value) return false;
            if (Plugin.cfg.EnableMaxRevivesPerDay.Value &&
                dailyRevivesUsed >= Plugin.cfg.MaxRevivesPerDay.Value)
                return false;
            return true;
        }

        static Terminal GetTerminal() => FindObjectOfType<Terminal>();

        static PlayerControllerB GetPlayerBySlot(ulong playerId)
        {
            var players = StartOfRound.Instance?.allPlayerScripts;
            if (players == null || playerId >= (ulong)players.Length) return null;
            return players[playerId];
        }

        bool CanAfford(int cost)
        {
            Terminal t = GetTerminal();
            return (t != null && t.groupCredits >= cost);
        }

        void DeductCredits(int cost)
        {
            Terminal t = GetTerminal();
            if (t == null) return;
            t.groupCredits -= cost;
            SyncCreditsClientRpc(t.groupCredits);
        }

        [ClientRpc]
        void SyncCreditsClientRpc(int newCredits)
        {
            Terminal t = GetTerminal();
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
            Vector3 spawn = GetPlayerSpawnPosition(GetPlayerIndex(p), false);
            RevivePlayerClientRpc(spawn, new NetworkBehaviourReference(p));
        }

        // Mirrors the game's StartOfRound.ReviveDeadPlayers, but for a single player.
        [ClientRpc]
        void RevivePlayerClientRpc(Vector3 spawnPosition, NetworkBehaviourReference netRef)
        {
            if (!netRef.TryGet(out NetworkBehaviour nb)) return;
            PlayerControllerB plr = nb.GetComponent<PlayerControllerB>();
            if (plr == null) return;

            var so = StartOfRound.Instance;
            int i = GetPlayerIndex(plr);
            if (i < 0) return;

            plr.ResetPlayerBloodObjects(plr.isPlayerDead);
            plr.isClimbingLadder = false;
            plr.clampLooking = false;
            plr.inVehicleAnimation = false;
            if (plr.gameplayCamera != null)
            {
                Vector3 camAngles = plr.gameplayCamera.transform.localEulerAngles;
                plr.gameplayCamera.transform.localEulerAngles = new Vector3(camAngles.x, 0f, camAngles.z);
            }
            plr.overridePoisonValue = false;
            plr.disableMoveInput = false;
            plr.disableLookInput = false;
            plr.disableInteract = false;
            plr.ResetZAndXRotation();
            plr.thisController.enabled = true;
            plr.health = 100;
            plr.hasBeenCriticallyInjured = false;
            plr.disableSyncInAnimation = false;
            if (plr.nightVisionRadar != null) plr.nightVisionRadar.enabled = false;

            if (plr.isPlayerDead)
            {
                plr.isPlayerDead = false;
                plr.enemyWaitingForBodyRagdoll = null;
                plr.isPlayerControlled = true;
                plr.isInElevator = true;
                plr.isInHangarShipRoom = true;
                plr.isInsideFactory = false;
                plr.parentedToElevatorLastFrame = false;
                plr.overrideGameOverSpectatePivot = null;
                if (plr.IsOwner) so.SetPlayerObjectExtrapolate(false);

                plr.TeleportPlayer(spawnPosition);
                plr.setPositionOfDeadPlayer = false;
                plr.DisablePlayerModel(so.allPlayerObjects[i], true, true);
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
                plr.carryWeight = 1f;

                if (plr.IsOwner)
                {
                    HUDManager.Instance.SetCracksOnVisor(100f);
                    HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
                    plr.hasBegunSpectating = false;
                    HUDManager.Instance.RemoveSpectateUI();
                    HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                    plr.hinderedMultiplier = 1f;
                    plr.isMovementHindered = 0;
                    plr.sourcesCausingSinking = 0;
                    so.SendChangedWeightEvent();
                    plr.reverbPreset = so.shipReverb;
                }
            }

            plr.voiceMuffledByEnemy = false;
            SoundManager.Instance.playerVoicePitchTargets[i] = 1f;
            SoundManager.Instance.SetPlayerPitch(1f, i);

            if (plr.currentVoiceChatIngameSettings == null)
                so.RefreshPlayerVoicePlaybackObjects();

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
                HUDManager.Instance.spitOnCameraAlpha = 1f;
                HUDManager.Instance.cadaverFilter = 0f;
                SoundManager.Instance.earsRingingTimer = 0f;
                SoundManager.Instance.alternateEarsRinging = false;
                localP.bleedingHeavily = false;
                localP.criticallyInjured = false;
                if (localP.playerBodyAnimator != null) localP.playerBodyAnimator.SetBool("Limp", false);
                localP.health = 100;
                HUDManager.Instance.UpdateHealthUI(100, false);
                localP.spectatedPlayerScript = null;
                HUDManager.Instance.audioListenerLowPass.enabled = false;
                so.SetSpectateCameraToGameOverMode(false, localP);
            }

            RemovePlayerBody(plr, i);

            if (IsServer) SyncLivingPlayers();

            so.UpdatePlayerVoiceEffects();
        }

        // Only clears the revived player's corpse; other dead players keep theirs.
        void RemovePlayerBody(PlayerControllerB plr, int playerIndex)
        {
            RagdollGrabbableObject[] rags = FindObjectsOfType<RagdollGrabbableObject>();
            foreach (var rag in rags)
            {
                bool belongsToPlayer = rag.bodyID == playerIndex ||
                    (rag.ragdoll != null && rag.ragdoll.playerObjectId == playerIndex);
                if (!belongsToPlayer) continue;

                if (rag.isHeld && rag.playerHeldBy != null)
                    rag.playerHeldBy.DropAllHeldItems();

                if (IsServer && rag.NetworkObject != null && rag.NetworkObject.IsSpawned)
                    rag.NetworkObject.Despawn();
                else if (!rag.NetworkObject || !rag.NetworkObject.IsSpawned)
                    Destroy(rag.gameObject);
            }

            DeadBodyInfo[] bodies = FindObjectsOfType<DeadBodyInfo>(true);
            foreach (var body in bodies)
            {
                if (body.playerObjectId == playerIndex)
                    Destroy(body.gameObject);
            }
            plr.deadBody = null;
        }

        void SyncLivingPlayers()
        {
            var so = StartOfRound.Instance;
            int newCount = 0;
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

        static int GetPlayerIndex(PlayerControllerB player)
        {
            var so = StartOfRound.Instance;
            if (so == null) return -1;
            return Array.IndexOf(so.allPlayerScripts, player);
        }

        Vector3 GetPlayerSpawnPosition(int playerNum, bool simpleTeleport)
        {
            var so = StartOfRound.Instance;
            if (so == null || so.playerSpawnPositions == null) return Vector3.zero;

            var spawns = so.playerSpawnPositions;
            if (spawns.Length == 0) return Vector3.zero;

            if (simpleTeleport ||
                playerNum < 0 ||
                playerNum >= spawns.Length)
                return spawns[0].position;

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

        public void ResetAllValues()
        {
            dailyRevivesUsed = 0;
        }
    }
}
