using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Zorro.Core; // Required for ItemInstanceData

namespace InsanePhysics.Features.HostileItems;

public class HostileItems : MonoBehaviour, IOnEventCallback {
    private static SFX_Instance[]? _cachedBonkSounds;
    private const byte BonkEventCode = 42;
    private const byte WakeUpEventCode = 43;

    private float _timer;
    private const float CheckInterval = 0.3f;
    private const float Chance = 0.01f;
    private const float DetectionRadius = 150.0f;
    private int _layerMask;

    private void Start() {
        _layerMask = Physics.AllLayers;
        StartCoroutine(LoadBonkSounds());
    }

    private void OnEnable() {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable() {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private static IEnumerator LoadBonkSounds() {
        yield return null;
        Item? coconut = Resources.FindObjectsOfTypeAll<Item>().FirstOrDefault(x => x.name.Contains("Coconut"));
        Bonkable? bonk = coconut?.GetComponent<Bonkable>();
        if (bonk?.bonk == null) yield break;

        _cachedBonkSounds = bonk.bonk;
        Debug.Log($"[InsanePhysics] Stole audio from {coconut?.name}");
    }

    public static void SendBonkEvent(Vector3 position) {
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.All };
        object[] content = [position];
        PhotonNetwork.RaiseEvent(BonkEventCode, content, options, SendOptions.SendReliable);
    }

    private static void SendWakeUpEvent(int viewID) {
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.Others };
        object[] content = [viewID];
        PhotonNetwork.RaiseEvent(WakeUpEventCode, content, options, SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == BonkEventCode) {
            object[] data = (object[])photonEvent.CustomData;
            Vector3 pos = (Vector3)data[0];

            if (_cachedBonkSounds != null && _cachedBonkSounds.Length > 0) {
                foreach (SFX_Instance sfx in _cachedBonkSounds) {
                    if (sfx != null) sfx.Play(pos);
                }
            }
        }
        else if (photonEvent.Code == WakeUpEventCode) {
            object[] data = (object[])photonEvent.CustomData;
            int viewID = (int)data[0];

            PhotonView view = PhotonView.Find(viewID);
            if (view != null && view.GetComponent<Rigidbody>() != null) {
                WakeUpItem(view.GetComponent<Rigidbody>());
            }
        }
    }

    private void Update() {
        if (!PhotonNetwork.IsMasterClient) return;
        _timer += Time.deltaTime;
        if (_timer < CheckInterval) return;
        _timer = 0f;

        List<Character> validTargets = Character.AllCharacters
            .Where(c => c is not null && !c.data.dead && !c.data.fullyPassedOut)
            .ToList();

        if (validTargets.Count > 0) {
            Character victim = validTargets[Random.Range(0, validTargets.Count)];
            AttemptHostilePhysics(victim);
        }
    }
    
    private static Character? GetHolder(Item item) {
        return Character.AllCharacters.FirstOrDefault(character => character.data.currentItem == item);
    }

    private void AttemptHostilePhysics(Character player) {
        Collider[] nearbyColliders = Physics.OverlapSphere(player.Center, DetectionRadius);

        foreach (Collider col in nearbyColliders) {
            if (Random.value > Chance) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (rb is null) continue;

            Item? itemComponent = rb.GetComponentInParent<Item>();

            if (itemComponent is not null) {
                Character? holder = GetHolder(itemComponent);
                if (holder is not null) {
                    StartCoroutine(DropHeldItemAndAttack(holder, player));
                    break;
                }
            }
            
            if (rb.GetComponentInParent<Character>() is not null) continue;
            if (rb.GetComponent<HostileProjectile>() is not null) continue;
            
            Vector3 directionToPlayer = player.Center - rb.transform.position;
            float distance = directionToPlayer.magnitude;

            if (Physics.Raycast(rb.transform.position, directionToPlayer.normalized, out RaycastHit hit, distance, _layerMask)) {
                Character hitCharacter = hit.collider.GetComponentInParent<Character>();
                if (hitCharacter != player) {
                    if (hit.collider.transform.root != rb.transform.root) continue;
                }
            }
            
            LaunchObjectAtPlayer(rb, player);
            break;
        }
    }
    
    private IEnumerator DropHeldItemAndAttack(Character holder, Character target) {
        if (holder.data.currentItem is null || !holder.refs.items.currentSelectedSlot.IsSome) yield break;

        byte slot = holder.refs.items.currentSelectedSlot.Value;
        ItemSlot itemSlot = holder.player.GetItemSlot(slot);
        Item heldItem = holder.data.currentItem;
        
        holder.refs.items.photonView.RPC("DropItemRpc", RpcTarget.All, 
            0f,
            slot, 
            heldItem.transform.position, 
            heldItem.rig.linearVelocity, 
            heldItem.transform.rotation, 
            itemSlot.data, 
            true
        );

        yield return new WaitForSeconds(0.1f);
        if (holder.refs.items.droppedItems.Count <= 0) yield break;
        
        PhotonView droppedView = holder.refs.items.droppedItems[^1];
        Rigidbody? newRb = droppedView?.GetComponent<Rigidbody>();
        
        if (newRb is not null) {
            LaunchObjectAtPlayer(newRb, target);
        }
    }

    private static void WakeUpItem(Rigidbody rb) {
        rb.transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;

        if (rb.GetComponent<Collider>() is not null) {
            rb.GetComponent<Collider>().enabled = true;
        }
    }

    private static void LaunchObjectAtPlayer(Rigidbody rb, Character player) {
        Debug.Log($"[InsanePhysics] {rb.name} is attacking {player.characterName}!");
        
        WakeUpItem(rb);
        
        PhotonView view = rb.GetComponent<PhotonView>();
        if (view is not null) {
            SendWakeUpEvent(view.ViewID);
            if (!view.IsMine) {
                view.RequestOwnership();
            }
        }
        
        rb.gameObject.AddComponent<HostileProjectile>();

        Vector3 attackDir = (player.Center - rb.transform.position).normalized;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float launchForce = Plugin.HostileObjectPower.Value;
        rb.AddForce(attackDir * launchForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
    }
}