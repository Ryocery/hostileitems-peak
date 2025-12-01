using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HostileItems.Core;
using InsanePhysics;
using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HostileItems.Handlers;

public class ProjectileHandler : MonoBehaviour {
    private readonly MonoBehaviour _runner;
    private readonly Config _config;
    
    private float _timer;
    private const float CheckInterval = 0.3f;
    private const float Chance = 0.01f;
    private const float DetectionRadius = 150.0f;
    private int _layerMask;
    
    public ProjectileHandler(MonoBehaviour runner, Config config) {
        _runner = runner;
        _config = config;
        _layerMask = Physics.AllLayers;
        _runner.StartCoroutine(AudioHandler.LoadSounds());
    }
    
    public void Dispose() { }

    private void Start() {
        _layerMask = Physics.AllLayers;
        StartCoroutine(AudioHandler.LoadSounds());

        if (!gameObject.GetComponent<NetworkHandler>()) {
            gameObject.AddComponent<NetworkHandler>();
        }
    }

    internal void Update() {
        if (!PhotonNetwork.IsMasterClient) return;

        _timer += Time.deltaTime;
        if (_timer < CheckInterval) return;
        _timer = 0f;

        List<Character> validTargets = Character.AllCharacters
            .Where(c => c && !c.data.dead && !c.data.fullyPassedOut)
            .ToList();

        if (validTargets.Count <= 0) return;
        Character victim = validTargets[Random.Range(0, validTargets.Count)];
        ScanAndAttack(victim);
    }

    private void ScanAndAttack(Character player) {
        Collider[] nearbyColliders = Physics.OverlapSphere(player.Center, DetectionRadius);

        foreach (Collider col in nearbyColliders) {
            if (Random.value > Chance) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (!rb) continue;
            
            Item? itemComponent = rb.GetComponentInParent<Item>();
            if (itemComponent) {
                Character? holder = GetHolder(itemComponent);
                if (holder) {
                    StartCoroutine(DropHeldItemAndAttack(holder, player));
                    break;
                }
            }
            
            if (rb.GetComponentInParent<Character>()) continue;
            if (rb.GetComponent<Projectile>()) continue;
            
            if (!HasLineOfSight(rb.transform, player)) continue;

            LaunchObjectAtPlayer(rb, player);
            break;
        }
    }

    private bool HasLineOfSight(Transform source, Character target) {
        Vector3 directionToPlayer = target.Center - source.position;
        float distance = directionToPlayer.magnitude;

        if (!Physics.Raycast(source.position, directionToPlayer.normalized, out RaycastHit hit, distance, _layerMask)) return true;
        Character hitCharacter = hit.collider.GetComponentInParent<Character>();
        if (hitCharacter == target) return true;
        return hit.collider.transform.root == source.root;
    }

    private static Character? GetHolder(Item item) {
        return Character.AllCharacters.FirstOrDefault(character => character.data.currentItem == item);
    }
    
    private IEnumerator DropHeldItemAndAttack(Character holder, Character target) {
        if (!holder.data.currentItem || !holder.refs.items.currentSelectedSlot.IsSome) yield break;

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
            
        if (newRb) {
            LaunchObjectAtPlayer(newRb, target);
        }
    }

    public static void WakeUpItem(Rigidbody rb) {
        rb.transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;

        if (rb.TryGetComponent(out Collider col)) {
            col.enabled = true;
        }
    }

    private void LaunchObjectAtPlayer(Rigidbody rb, Character player) {
        Debug.Log($"[HostileItems] {rb.name} is attacking {player.characterName}!");
        
        WakeUpItem(rb);
        
        PhotonView view = rb.GetComponent<PhotonView>();
        if (view) {
            NetworkHandler.SendWakeUp(view.ViewID);
            if (!view.IsMine) {
                view.RequestOwnership();
            }
        }
        
        rb.gameObject.AddComponent<Projectile>();

        Vector3 attackDir = (player.Center - rb.transform.position).normalized;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        float launchForce = _config.HostileObjectPower.Value;
        rb.AddForce(attackDir * launchForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
    }
}