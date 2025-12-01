using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace HostileItems.Handlers;

public class NetworkHandler : MonoBehaviour, IOnEventCallback {
    private const byte BonkEventCode = 42;
    private const byte WakeUpEventCode = 43;

    private void OnEnable() => PhotonNetwork.AddCallbackTarget(this);
    private void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);
    
    public void Dispose() { }

    public static void SendBonk(Vector3 position) {
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.All };
        object[] content = [position];
        PhotonNetwork.RaiseEvent(BonkEventCode, content, options, SendOptions.SendReliable);
    }

    public static void SendWakeUp(int viewID) {
        RaiseEventOptions options = new() { Receivers = ReceiverGroup.Others };
        object[] content = [viewID];
        PhotonNetwork.RaiseEvent(WakeUpEventCode, content, options, SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent) {
        switch (photonEvent.Code) {
            case BonkEventCode:
                HandleBonkEvent(photonEvent);
                break;
            case WakeUpEventCode:
                HandleWakeUpEvent(photonEvent);
                break;
        }
    }

    private static void HandleBonkEvent(EventData photonEvent) {
        object[] data = (object[])photonEvent.CustomData;
        Vector3 pos = (Vector3)data[0];
        AudioHandler.PlayBonk(pos);
    }

    private static void HandleWakeUpEvent(EventData photonEvent) {
        object[] data = (object[])photonEvent.CustomData;
        int viewID = (int)data[0];

        PhotonView view = PhotonView.Find(viewID);
        if (view && view.TryGetComponent(out Rigidbody rb)) {
            ProjectileHandler.WakeUpItem(rb);
        }
    }
}