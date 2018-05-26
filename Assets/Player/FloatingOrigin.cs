// FloatingOrigin.cs
// Written by Peter Stirling
// 11 November 2010
// Uploaded to Unify Community Wiki on 11 November 2010
// Updated to Unity 5.x particle system by Tony Lovell 14 January, 2016
// fix to ensure ALL particles get moved by Tony Lovell 8 September, 2016
// URL: http://wiki.unity3d.com/index.php/Floating_Origin
using UnityEngine;
using System.Collections;

public class FloatingOrigin : MonoBehaviour {
    public float threshold = 100.0f;
    public float physicsThreshold = 1000.0f; // Set to zero to disable

    public float defaultSleepThreshold = 0.14f;

    ParticleSystem.Particle[] parts = null;

    void LateUpdate() {
        Vector3 cameraPosition = gameObject.transform.position;
        cameraPosition.y = 0f;
        if (cameraPosition.magnitude > threshold) {
            Object[] objects = FindObjectsOfType(typeof(Transform));
            foreach (Object o in objects) {
                Transform t = (Transform)o;
                if (t.parent == null) {
                    t.position -= cameraPosition;
                }
            }

            // new particles... very similar to old version above
            objects = FindObjectsOfType(typeof(ParticleSystem));
            foreach (UnityEngine.Object o in objects) {
                ParticleSystem sys = (ParticleSystem)o;

                if (sys.simulationSpace != ParticleSystemSimulationSpace.World)
                    continue;

                int particlesNeeded = sys.maxParticles;

                if (particlesNeeded <= 0)
                    continue;

                bool wasPaused = sys.isPaused;
                bool wasPlaying = sys.isPlaying;

                if (!wasPaused)
                    sys.Pause();

                // ensure a sufficiently large array in which to store the particles
                if (parts == null || parts.Length < particlesNeeded) {
                    parts = new ParticleSystem.Particle[particlesNeeded];
                }

                // now get the particles
                int num = sys.GetParticles(parts);

                for (int i = 0; i < num; i++) {
                    parts[i].position -= cameraPosition;
                }

                sys.SetParticles(parts, num);

                if (wasPlaying)
                    sys.Play();
            }

            if (physicsThreshold > 0f) {
                float physicsThreshold2 = physicsThreshold * physicsThreshold; // simplify check on threshold
                objects = FindObjectsOfType(typeof(Rigidbody));
                foreach (UnityEngine.Object o in objects) {
                    Rigidbody r = (Rigidbody)o;
                    if (r.gameObject.transform.position.sqrMagnitude > physicsThreshold2) {
                        r.sleepThreshold = float.MaxValue;
                    } else {
                        r.sleepThreshold = defaultSleepThreshold;
                    }
                }
            }
        }
    }
}