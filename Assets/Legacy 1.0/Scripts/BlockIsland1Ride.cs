using System.Collections.Generic;
using UnityEngine;

// Block Island 1.0 migration: ride along with a 1.0 ride (roller coaster) and get off again with G.
// G near a ride = get on (view switches to the ride camera, walking/building paused),
// G while riding = get off (player view and controls restored exactly as before).
public class BlockIsland1Ride : MonoBehaviour {
    class Ride { public Camera cam; public Transform owner; public Bounds bounds; }

    const float EnterDistance = 48f;
    static readonly List<Ride> rides = new List<Ride>();
    static BlockIsland1Ride instance;

    Ride riding;
    bool isRiding;
    Camera playerCam;
    int playerMask;
    readonly List<Behaviour> paused = new List<Behaviour>();

    public static void Register(Camera cam, Transform owner) {
        // Rides like the roller coaster are huge: measure the distance to the model, not to its pivot.
        Bounds b = new Bounds(owner.position, Vector3.zero);
        foreach(Renderer r in owner.GetComponentsInChildren<Renderer>()) if(r is MeshRenderer || r is SkinnedMeshRenderer) b.Encapsulate(r.bounds);
        rides.Add(new Ride { cam = cam, owner = owner, bounds = b });
        if(instance == null) instance = new GameObject("BlockIsland1Ride").AddComponent<BlockIsland1Ride>();
    }

    void Update() {
        rides.RemoveAll(r => r.cam == null || r.owner == null);
        // The ride was removed or its chunk unloaded while riding: get off automatically.
        if(isRiding && (riding == null || riding.cam == null)) Exit();
        if(!Input.GetKeyDown(KeyCode.G) || !GameState.IsPlaying) return;
        if(isRiding) Exit(); else Enter(Nearest());
    }

    Ride Nearest() {
        GameObject player = GameObject.FindWithTag("Player");
        if(player == null) return null;
        Ride best = null; float bestD = EnterDistance;
        foreach(Ride r in rides) {
            if(r.cam == null || r.owner == null) continue;   // chunk rebuilt or block removed this frame
            float d = Mathf.Sqrt(r.bounds.SqrDistance(player.transform.position));
            if(d < bestD) { bestD = d; best = r; }
        }
        return best;
    }

    void Enter(Ride r) {
        GameObject player = GameObject.FindWithTag("Player");
        playerCam = player != null ? player.GetComponentInChildren<Camera>() : Camera.main;
        if(r == null || r.cam == null || player == null || playerCam == null) return;
        riding = r; isRiding = true;
        // The player camera stays enabled (Camera.main drives world streaming, sky, wildlife);
        // it just renders nothing while the ride camera draws on top.
        playerMask = playerCam.cullingMask;
        playerCam.cullingMask = 0;
        r.cam.depth = playerCam.depth + 10f;
        r.cam.enabled = true;
        paused.Clear();
        foreach(MonoBehaviour mb in player.GetComponentsInChildren<MonoBehaviour>()) {
            // Missing-script components come back as null entries.
            if(mb != null && mb.enabled && (mb is Builder || mb is CharacterMotor || mb is CharacterInputController || mb is MouseLook)) { mb.enabled = false; paused.Add(mb); }
        }
    }

    void Exit() {
        if(!isRiding) return;
        if(riding != null && riding.cam != null) riding.cam.enabled = false;
        if(playerCam != null) playerCam.cullingMask = playerMask;
        foreach(Behaviour b in paused) if(b != null) b.enabled = true;
        paused.Clear();
        riding = null; isRiding = false;
    }
    // No on-screen sign: G gets on and off without a hint.
}
