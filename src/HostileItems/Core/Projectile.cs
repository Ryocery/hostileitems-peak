using HostileItems.Handlers;
using UnityEngine;

namespace HostileItems.Core;

public class Projectile : MonoBehaviour {
    private bool _hasHit;
    private float _creationTime;

    private const float RagdollTime = 1f;
    private const float BonkForce = 40f;
    private const float BonkRange = 6f;
    private const float MinBonkVelocity = 5f;
    private const float ArmingDelay = 0.05f;

    private void Start() {
        _creationTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision) {
        if (_hasHit || Time.time - _creationTime < ArmingDelay) return;
        
        if (collision.relativeVelocity.magnitude < MinBonkVelocity) {
            Destroy(this);
            return;
        }

        Character? victim = collision.gameObject.GetComponentInParent<Character>();

        if (victim && !victim.data.dead && !victim.data.fullyPassedOut) {
            PerformBonk(victim, collision);
        }
    }

    private void PerformBonk(Character victim, Collision collision) {
        Debug.Log($"[HostileItems] BONK! {name} knocked out {victim.characterName}");
        
        victim.Fall(RagdollTime);
        
        if (collision.contacts.Length > 0) {
            ContactPoint contact = collision.contacts[0];
            victim.AddForceAtPosition(-collision.relativeVelocity.normalized * BonkForce, contact.point, BonkRange);
        }

        NetworkHandler.SendBonk(transform.position);

        _hasHit = true;
        Destroy(this);
    }
}